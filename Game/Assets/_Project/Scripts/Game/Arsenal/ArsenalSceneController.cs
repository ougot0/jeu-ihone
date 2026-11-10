using System.Collections;
using System.Collections.Generic;
using Arsenal.Core.Data;
using Arsenal.Core.Profile;
using Arsenal.Core.Save;
using Arsenal.Game.Bootstrap;
using Arsenal.Game.InputHelpers;
using Arsenal.Game.Rendering;
using Arsenal.Game.UI;
using Arsenal.Game.Weapons;
using UnityEngine;

namespace Arsenal.Game.ArsenalScene
{
    /// <summary>
    /// Scène principale : l'armurerie 3D. Assemble décor, caméra, présentoir, mur de collection et interface,
    /// et relie l'interface aux services (achat, équipement, configurations).
    /// </summary>
    public class ArsenalSceneController : SceneControllerBase
    {
        public int SelectedIndex { get; private set; }
        public int BuildIndex { get; private set; }
        public WeaponDefinition SelectedWeapon => SelectedIndex >= 0 && SelectedIndex < _gs.Database.Weapons.Count ? _gs.Database.Weapons[SelectedIndex] : null;
        /// <summary>Configuration affichée : une configuration du joueur, ou la configuration d'origine pour une arme non possédée.</summary>
        public WeaponBuild DisplayedBuild { get; private set; }

        private GameServices _gs;
        private ArsenalEnvironment _env;
        private Camera _camera;
        private WeaponTurntableViewer _viewer;
        private Transform _pivot;
        private WeaponVisual _current;
        private ArsenalHud _hud;
        private readonly List<GameObject> _wallDisplays = new List<GameObject>();
        private Coroutine _swap;
        private int _ownedCount;
        private Material _ownedTag, _lockedTag;

        private void Start()
        {
            _gs = GameServices.Instance;
            int quality = _gs.Profile.settings.graphicsQuality;

            _env = ArsenalEnvironment.Build(transform, quality);
            BuildCamera(quality);

            _pivot = new GameObject("DisplayPivot").transform;
            _pivot.SetParent(transform, false);
            _pivot.position = _env.DisplayPoint;

            _viewer = gameObject.AddComponent<WeaponTurntableViewer>();
            _viewer.ViewCamera = _camera;
            _viewer.Pivot = _pivot;

            PointerInput.EnsureEventSystem();
            _hud = new GameObject("HUD").AddComponent<ArsenalHud>();
            _hud.transform.SetParent(transform, false);

            // Sélection initiale : l'arme équipée.
            var selected = _gs.Arsenal.SelectedBuild;
            SelectedIndex = 0;
            if (selected != null)
            {
                var list = _gs.Database.Weapons;
                for (int i = 0; i < list.Count; i++) if (list[i].id == selected.weaponId) SelectedIndex = i;
            }
            ResolveDisplayedBuild(preferEquipped: true);
            _hud.Build(this);
            ShowWeapon(immediate: true);
            RebuildWallDisplays();
            _env.RefreshReflections();

            _gs.StateChanged += OnStateChanged;
            _gs.LevelUp += OnLevelUp;

            switch (_gs.LastLoadStatus)
            {
                case LoadStatus.RecoveredFromBackup: _hud.ShowToast("Sauvegarde restaurée depuis la copie de secours.", false); break;
                case LoadStatus.CorruptedReset: _hud.ShowToast("Sauvegarde illisible : nouveau profil créé.", true); break;
                case LoadStatus.NewProfile: _hud.ShowToast("Bienvenue ! Votre première arme vous attend.", false); break;
            }
        }

        private void OnDestroy()
        {
            if (_gs != null)
            {
                _gs.StateChanged -= OnStateChanged;
                _gs.LevelUp -= OnLevelUp;
            }
        }

        private void BuildCamera(int quality)
        {
            var go = new GameObject("MainCamera") { tag = "MainCamera" };
            go.transform.SetParent(transform, false);
            go.transform.position = _env.CameraPoint;
            go.transform.LookAt(_env.DisplayPoint);
            _camera = go.AddComponent<Camera>();
            _camera.fieldOfView = 32f;
            _camera.nearClipPlane = 0.03f;
            _camera.farClipPlane = 30f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.03f, 0.03f, 0.035f);
            _camera.allowHDR = true;
            go.AddComponent<AudioListener>();
            PostProcessingSetup.Apply(_camera, transform, quality);
        }

        // ------------------------------------------------------------------ Actions de l'interface

        public void SelectWeapon(int index)
        {
            if (index == SelectedIndex) return;
            SelectedIndex = Mathf.Clamp(index, 0, _gs.Database.Weapons.Count - 1);
            ResolveDisplayedBuild(preferEquipped: true);
            ShowWeapon(immediate: false);
            _hud.Refresh();
        }

        public void CycleBuild(int direction)
        {
            var builds = _gs.Arsenal.GetBuilds(SelectedWeapon.id);
            if (builds.Count < 2) return;
            BuildIndex = (BuildIndex + direction + builds.Count) % builds.Count;
            DisplayedBuild = builds[BuildIndex];
            _current.ApplyBuild(DisplayedBuild, _gs.Database);
            _hud.Refresh();
        }

        /// <summary>Acheter (arme non possédée) ou équiper la configuration affichée.</summary>
        public void PrimaryAction()
        {
            var weapon = SelectedWeapon;
            if (!_gs.Profile.OwnsWeapon(weapon.id))
            {
                var r = _gs.Economy.BuyWeapon(weapon.id);
                _hud.ShowToast(r.Message, !r.Success);
                if (r.Success)
                {
                    ResolveDisplayedBuild(preferEquipped: false);
                    _current.ApplyBuild(DisplayedBuild, _gs.Database);
                }
            }
            else
            {
                var r = _gs.Arsenal.SelectBuild(DisplayedBuild.id);
                if (r.Success) _hud.ShowToast($"{weapon.displayName} « {DisplayedBuild.name} » équipée.", false);
            }
            _hud.Refresh();
        }

        public void OnQualityChanged()
        {
            // Les réglages de qualité Unity sont appliqués par GameServices ; l'ombre de la lumière clé suit.
            foreach (var l in GetComponentsInChildren<Light>())
                if (l.name == "Light_Key") l.shadows = _gs.Profile.settings.graphicsQuality >= 1 ? LightShadows.Soft : LightShadows.Hard;
        }

        // ------------------------------------------------------------------ Affichage

        private void ResolveDisplayedBuild(bool preferEquipped)
        {
            var weapon = SelectedWeapon;
            var builds = _gs.Arsenal.GetBuilds(weapon.id);
            BuildIndex = 0;
            if (builds.Count > 0)
            {
                var equipped = _gs.Arsenal.SelectedBuild;
                if (preferEquipped && equipped != null)
                    for (int i = 0; i < builds.Count; i++) if (builds[i].id == equipped.id) BuildIndex = i;
                DisplayedBuild = builds[BuildIndex];
            }
            else DisplayedBuild = PreviewBuild(weapon);
        }

        /// <summary>Configuration d'origine d'une arme non possédée (non sauvegardée).</summary>
        private WeaponBuild PreviewBuild(WeaponDefinition weapon)
        {
            var b = new WeaponBuild { id = "preview", weaponId = weapon.id, name = "Origine", finishId = _gs.Database.Economy.defaultFinish };
            foreach (var s in weapon.slots)
                if (!string.IsNullOrEmpty(s.defaultAttachment)) b.SetAttachment(s.id, s.defaultAttachment);
            return b;
        }

        private void ShowWeapon(bool immediate)
        {
            if (_swap != null) StopCoroutine(_swap);
            _swap = StartCoroutine(SwapRoutine(immediate));
        }

        private IEnumerator SwapRoutine(bool immediate)
        {
            if (_current != null && !immediate)
            {
                // Sortie : l'arme descend dans le piédestal.
                var old = _current;
                var start = old.transform.localPosition;
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime * 5f)
                {
                    old.transform.localPosition = start + Vector3.down * (t * t * 0.35f);
                    old.transform.localScale = Vector3.one * (1f - t * 0.6f);
                    yield return null;
                }
                Destroy(old.gameObject);
            }
            else if (_current != null) Destroy(_current.gameObject);

            _pivot.rotation = Quaternion.identity;
            _current = WeaponVisualBuilder.Build(SelectedWeapon, DisplayedBuild, _gs.Database, _pivot);
            yield return null; // laisse Unity calculer les bornes
            var bounds = _current.GetBounds();
            var centerOffset = _pivot.InverseTransformPoint(bounds.center);
            _current.Root.localPosition = -centerOffset;
            _viewer.Frame(bounds);

            var target = Vector3.zero;
            if (!immediate)
            {
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime * 3.5f)
                {
                    float e = 1f - Mathf.Pow(1f - t, 3f);
                    _current.transform.localPosition = target + Vector3.down * ((1f - e) * 0.3f);
                    _current.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, e);
                    yield return null;
                }
            }
            _current.transform.localPosition = target;
            _current.transform.localScale = Vector3.one;
            _swap = null;
        }

        /// <summary>Expose toute la collection sur le mur : armes possédées éclairées, verrouillées en silhouette.</summary>
        private void RebuildWallDisplays()
        {
            foreach (var go in _wallDisplays) if (go != null) Destroy(go);
            _wallDisplays.Clear();
            var weapons = _gs.Database.Weapons;
            if (_ownedTag == null) _ownedTag = MaterialLibrary.CreateEmissive("Tag_Owned", new Color(0.1f, 0.3f, 0.12f), new Color(0.3f, 1.4f, 0.4f));
            if (_lockedTag == null) _lockedTag = MaterialLibrary.CreateEmissive("Tag_Locked", new Color(0.3f, 0.08f, 0.05f), new Color(1.2f, 0.25f, 0.15f));
            _ownedCount = _gs.Profile.ownedWeapons.Count;

            for (int i = 0; i < weapons.Count && i < _env.WallMounts.Count; i++)
            {
                var w = weapons[i];
                var mount = _env.WallMounts[i];
                bool owned = _gs.Profile.OwnsWeapon(w.id);
                var builds = _gs.Arsenal.GetBuilds(w.id);
                var build = builds.Count > 0 ? builds[0] : PreviewBuild(w);

                var holder = new GameObject("Display_" + w.id).transform;
                holder.SetParent(mount, false);
                var visual = WeaponVisualBuilder.Build(w, build, _gs.Database, holder);
                visual.SetShadows(false);
                if (!owned)
                {
                    MaterialLibrary.SetColor(visual.BodyMaterial, new Color(0.02f, 0.02f, 0.02f));
                    MaterialLibrary.SetPbr(visual.BodyMaterial, 0.2f, 0.3f);
                }

                // Mise à l'échelle pour tenir dans une case d'environ 1,5 m, arme de profil, canon vers la droite.
                holder.localRotation = Quaternion.Euler(0f, 90f, 0f);
                var b = visual.GetBounds();
                float length = Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.z));
                float scale = Mathf.Min(1.5f / length, 3f);
                holder.localScale = Vector3.one * scale;
                b = visual.GetBounds();
                var center = holder.InverseTransformPoint(b.center);
                visual.Root.localPosition -= center;

                var tag = PrimitiveMeshes.Create("StatusTag", PrimitiveType.Cube, owned ? _ownedTag : _lockedTag, mount);
                tag.transform.localPosition = new Vector3(0f, -0.38f, 0.02f);
                tag.transform.localScale = new Vector3(0.5f, 0.02f, 0.02f);
                _wallDisplays.Add(holder.gameObject);
                _wallDisplays.Add(tag);
            }
        }

        // ------------------------------------------------------------------ Evénements

        private void OnStateChanged()
        {
            if (_hud == null) return;
            // Après une réinitialisation de progression, la configuration affichée peut ne plus exister.
            if (DisplayedBuild != null && DisplayedBuild.id != "preview" && _gs.Profile.FindBuild(DisplayedBuild.id) == null)
            {
                ResolveDisplayedBuild(preferEquipped: true);
                ShowWeapon(immediate: true);
                RebuildWallDisplays();
            }
            else if (_gs.Profile.ownedWeapons.Count != _ownedCount) RebuildWallDisplays();
            _hud.Refresh();
        }

        private void OnLevelUp(int level)
        {
            _hud.ShowToast($"Niveau {level} atteint ! De nouvelles armes sont disponibles.", false);
        }
    }
}
