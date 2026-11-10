using System.Collections.Generic;
using Arsenal.Core.Data;
using Arsenal.Core.Profile;
using Arsenal.Game.Rendering;
using UnityEngine;

namespace Arsenal.Game.Weapons
{
    /// <summary>
    /// Représentation 3D d'une arme configurée : pièces de base, points de fixation (sockets) et accessoires
    /// réellement instanciés comme objets 3D enfants des sockets. Modifier la configuration met à jour le modèle.
    /// </summary>
    public class WeaponVisual : MonoBehaviour
    {
        public WeaponDefinition Definition { get; private set; }
        public Transform Root { get; private set; }

        private readonly Dictionary<string, Transform> _sockets = new Dictionary<string, Transform>();
        private readonly Dictionary<string, GameObject> _parts = new Dictionary<string, GameObject>();
        private readonly List<Renderer> _bodyRenderers = new List<Renderer>();
        private readonly Dictionary<string, GameObject> _installed = new Dictionary<string, GameObject>();
        private Material _bodyMaterial;

        internal void Initialize(WeaponDefinition def, Transform root)
        {
            Definition = def;
            Root = root;
            _bodyMaterial = MaterialLibrary.Create($"Body_{def.id}", new Color(0.1f, 0.1f, 0.11f), 0.45f, 0.5f, ProceduralTextures.FineNoise, new Vector2(2f, 2f));
        }

        internal Material BodyMaterial => _bodyMaterial;
        internal void RegisterSocket(string slotId, Transform t) => _sockets[slotId] = t;
        internal void RegisterPart(string name, GameObject go) { if (!string.IsNullOrEmpty(name)) _parts[name] = go; }
        internal void RegisterBodyRenderer(Renderer r) => _bodyRenderers.Add(r);

        public Transform GetSocket(string slotId) => _sockets.TryGetValue(slotId, out var t) ? t : null;
        public IReadOnlyDictionary<string, Transform> Sockets => _sockets;

        /// <summary>Applique une configuration complète : accessoires 3D + revêtement.</summary>
        public void ApplyBuild(WeaponBuild build, GameDatabase db)
        {
            foreach (var go in _installed.Values) if (go != null) Destroy(go);
            _installed.Clear();
            foreach (var part in _parts.Values) part.SetActive(true);

            if (build != null)
            {
                foreach (var a in build.attachments)
                {
                    var def = db.GetAttachment(a.attachmentId);
                    var socket = GetSocket(a.slotId);
                    if (def == null || socket == null) continue;
                    var go = WeaponVisualBuilder.BuildAttachment(def, socket, _bodyMaterial);
                    _installed[a.slotId] = go;
                    if (def.hidesParts != null)
                        foreach (var hidden in def.hidesParts)
                            if (_parts.TryGetValue(hidden, out var p)) p.SetActive(false);
                }
                ApplyFinish(db.GetFinish(build.finishId));
            }
        }

        public void ApplyFinish(FinishDefinition finish)
        {
            if (finish == null) return;
            var c = new Color(finish.color.r, finish.color.g, finish.color.b, 1f);
            MaterialLibrary.SetColor(_bodyMaterial, c);
            MaterialLibrary.SetPbr(_bodyMaterial, finish.metallic, finish.smoothness);
        }

        /// <summary>Boîte englobante de tous les rendus visibles (cadrage caméra, présentoirs).</summary>
        public Bounds GetBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(transform.position, Vector3.one * 0.2f);
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        public void SetShadows(bool cast)
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = cast ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void OnDestroy()
        {
            if (_bodyMaterial != null) Destroy(_bodyMaterial);
        }
    }
}
