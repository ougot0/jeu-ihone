using System.Collections.Generic;
using Arsenal.Core.Data;
using Arsenal.Core.Rules;
using Arsenal.Game.ArsenalScene;
using Arsenal.Game.Bootstrap;
using UnityEngine;
using UnityEngine.UI;

namespace Arsenal.Game.UI
{
    /// <summary>
    /// Interface de l'arsenal : catalogue (armes possédées et verrouillées), fiche technique avec jauges,
    /// achat / équipement, configurations, accès aux modes et réglages. Discrète, lisible, en paysage.
    /// </summary>
    public class ArsenalHud : MonoBehaviour
    {
        private ArsenalSceneController _ctl;
        private GameServices _gs;

        private Text _money, _level, _toast;
        private UiFactory.Bar _xp;
        private CanvasGroup _toastGroup;
        private float _toastUntil;

        private RectTransform _listContent;
        private readonly List<ListItem> _items = new List<ListItem>();

        private Text _name, _family, _desc, _buildName, _lockInfo, _compareHint;
        private readonly Dictionary<string, StatRow> _stats = new Dictionary<string, StatRow>();
        private UiFactory.ButtonRefs _primary, _prevBuild, _nextBuild;

        private GameObject _settingsRoot;
        private Text _gyroValue, _gyroSensValue, _gyroInvertValue, _touchSensValue, _qualityValue, _volumeValue;
        private UiFactory.ButtonRefs _resetButton;
        private bool _resetArmed;

        private class ListItem
        {
            public string WeaponId;
            public Image Background;
            public Image AccentBar;
            public Text Name, Family, Status;
        }

        private class StatRow
        {
            public UiFactory.Bar Bar;
            public Text Value;
        }

        public void Build(ArsenalSceneController controller)
        {
            _ctl = controller;
            _gs = GameServices.Instance;

            var canvasGo = new GameObject("ArsenalCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var safe = UiFactory.Rect("SafeArea", canvasGo.transform);
            safe.Place(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            BuildTopBar(safe);
            BuildCatalog(safe);
            BuildDetails(safe);
            BuildBottom(safe);
            BuildToast(safe);
            BuildSettings(canvasGo.transform);
            Refresh();
        }

        // ------------------------------------------------------------------ Construction

        private void BuildTopBar(RectTransform root)
        {
            var title = UiFactory.Label("Title", root, "ARSENAL", 50, UiFactory.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            title.rectTransform.PlaceFixed(new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -20), new Vector2(500, 64));
            var sub = UiFactory.Label("Subtitle", root, "COLLECTION PRIVÉE  ·  ARMES ET CIBLES 100 % VIRTUELLES", 17, UiFactory.TextDim, TextAnchor.UpperLeft);
            sub.rectTransform.PlaceFixed(new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -80), new Vector2(700, 28));
            var accent = UiFactory.Panel("TitleAccent", root, UiFactory.Accent, false);
            accent.rectTransform.PlaceFixed(new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -112), new Vector2(64, 4));

            var wallet = UiFactory.Panel("Wallet", root, UiFactory.PanelBg);
            wallet.rectTransform.PlaceFixed(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-230, -24), new Vector2(420, 92));
            var walletLabel = UiFactory.Label("Label", wallet.transform, "CRÉDITS", 16, UiFactory.TextDim, TextAnchor.UpperLeft);
            walletLabel.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(20, 0), new Vector2(-20, -10));
            _money = UiFactory.Label("Money", wallet.transform, "", 34, UiFactory.Accent, TextAnchor.LowerLeft, FontStyle.Bold);
            _money.rectTransform.Place(Vector2.zero, new Vector2(0.55f, 1), new Vector2(20, 12), new Vector2(0, 0));
            _level = UiFactory.Label("Level", wallet.transform, "", 22, UiFactory.Text, TextAnchor.UpperRight, FontStyle.Bold);
            _level.rectTransform.Place(new Vector2(0.55f, 0), Vector2.one, new Vector2(0, 0), new Vector2(-20, -12));
            var xpHolder = UiFactory.Rect("XpHolder", wallet.transform);
            xpHolder.Place(new Vector2(0.58f, 0), new Vector2(1, 0), new Vector2(0, 22), new Vector2(-20, 30));
            _xp = UiFactory.CreateBar("Xp", xpHolder, UiFactory.Accent);
            _xp.Fill.parent.GetComponent<RectTransform>().Place(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var settings = UiFactory.Button("SettingsButton", root, "RÉGLAGES", () => ShowSettings(true), UiFactory.PanelBg, 20);
            settings.Background.rectTransform.PlaceFixed(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(190, 92));
        }

        private void BuildCatalog(RectTransform root)
        {
            var panel = UiFactory.Panel("Catalog", root, UiFactory.PanelBg);
            panel.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 1), new Vector2(24, 190), new Vector2(474, -140));

            var header = UiFactory.Label("Header", panel.transform, "CATALOGUE", 18, UiFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);
            header.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(22, -50), new Vector2(-22, 0));

            var viewport = UiFactory.Panel("Viewport", panel.transform, new Color(0, 0, 0, 0.001f));
            viewport.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -56));
            viewport.gameObject.AddComponent<RectMask2D>();

            _listContent = UiFactory.Rect("Content", viewport.transform);
            _listContent.anchorMin = new Vector2(0, 1);
            _listContent.anchorMax = new Vector2(1, 1);
            _listContent.pivot = new Vector2(0.5f, 1f);
            _listContent.offsetMin = _listContent.offsetMax = Vector2.zero;
            var layout = _listContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = _listContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform;
            scroll.content = _listContent;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            var weapons = _gs.Database.Weapons;
            for (int i = 0; i < weapons.Count; i++)
            {
                int index = i;
                var w = weapons[i];
                var bg = UiFactory.Panel("Item_" + w.id, _listContent, UiFactory.PanelBgLight);
                bg.gameObject.AddComponent<LayoutElement>().preferredHeight = 104;
                var btn = bg.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() => _ctl.SelectWeapon(index));

                var item = new ListItem { WeaponId = w.id, Background = bg };
                item.AccentBar = UiFactory.Panel("Accent", bg.transform, UiFactory.Accent, false);
                item.AccentBar.rectTransform.Place(Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(6, 0));
                item.Name = UiFactory.Label("Name", bg.transform, w.displayName, 28, UiFactory.Text, TextAnchor.LowerLeft, FontStyle.Bold);
                item.Name.rectTransform.Place(new Vector2(0, 0.45f), new Vector2(0.62f, 1), new Vector2(24, 0), new Vector2(0, -8));
                item.Family = UiFactory.Label("Family", bg.transform, WeaponFamilies.DisplayName(w.family), 18, UiFactory.TextDim, TextAnchor.UpperLeft);
                item.Family.rectTransform.Place(new Vector2(0, 0), new Vector2(0.7f, 0.45f), new Vector2(24, 8), new Vector2(0, -4));
                item.Status = UiFactory.Label("Status", bg.transform, "", 19, UiFactory.Text, TextAnchor.MiddleRight, FontStyle.Bold);
                item.Status.rectTransform.Place(new Vector2(0.55f, 0), Vector2.one, Vector2.zero, new Vector2(-20, 0));
                _items.Add(item);
            }
        }

        private void BuildDetails(RectTransform root)
        {
            var panel = UiFactory.Panel("Details", root, UiFactory.PanelBg);
            panel.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 1), new Vector2(-574, 190), new Vector2(-24, -140));
            var t = panel.transform;

            _name = UiFactory.Label("Name", t, "", 40, UiFactory.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            _name.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -74), new Vector2(-26, -18));
            _family = UiFactory.Label("Family", t, "", 19, UiFactory.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            _family.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -106), new Vector2(-26, -78));
            _desc = UiFactory.Label("Description", t, "", 19, UiFactory.TextDim, TextAnchor.UpperLeft);
            _desc.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -186), new Vector2(-26, -114));

            float y = -206f;
            foreach (var stat in StatIds.All)
            {
                var label = UiFactory.Label("Stat_" + stat, t, StatIds.DisplayName(stat).ToUpperInvariant(), 17, UiFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);
                label.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(26, y - 34), new Vector2(196, y));
                var holder = UiFactory.Rect("BarHolder", t);
                holder.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(200, y - 24), new Vector2(-150, y - 10));
                var bar = UiFactory.CreateBar("Bar", holder, UiFactory.Text);
                ((RectTransform)bar.Fill.parent).Place(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var value = UiFactory.Label("Value", t, "", 19, UiFactory.Text, TextAnchor.MiddleRight, FontStyle.Bold);
                value.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-146, y - 34), new Vector2(-26, y));
                _stats[stat] = new StatRow { Bar = bar, Value = value };
                y -= 44f;
            }

            _compareHint = UiFactory.Label("CompareHint", t, "", 15, UiFactory.TextDim, TextAnchor.MiddleLeft);
            _compareHint.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, y - 26), new Vector2(-26, y));

            // Configuration (variante) active
            var buildRow = UiFactory.Panel("BuildRow", t, UiFactory.PanelBgLight);
            buildRow.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(18, 18), new Vector2(-18, 96));
            _prevBuild = UiFactory.Button("PrevBuild", buildRow.transform, "<", () => _ctl.CycleBuild(-1), new Color(1, 1, 1, 0.06f), 30);
            _prevBuild.Background.rectTransform.Place(Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(70, 0));
            _nextBuild = UiFactory.Button("NextBuild", buildRow.transform, ">", () => _ctl.CycleBuild(1), new Color(1, 1, 1, 0.06f), 30);
            _nextBuild.Background.rectTransform.Place(new Vector2(1, 0), Vector2.one, new Vector2(-70, 0), Vector2.zero);
            var cfgLabel = UiFactory.Label("Caption", buildRow.transform, "CONFIGURATION", 14, UiFactory.TextDim, TextAnchor.UpperCenter, FontStyle.Bold);
            cfgLabel.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(80, 0), new Vector2(-80, -8));
            _buildName = UiFactory.Label("BuildName", buildRow.transform, "", 24, UiFactory.Text, TextAnchor.LowerCenter, FontStyle.Bold);
            _buildName.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(80, 10), new Vector2(-80, 0));
        }

        private void BuildBottom(RectTransform root)
        {
            _primary = UiFactory.Button("PrimaryAction", root, "", () => _ctl.PrimaryAction(), UiFactory.Accent, 28);
            _primary.Background.rectTransform.PlaceFixed(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(460, 82));
            _primary.Label.color = new Color(0.08f, 0.06f, 0.03f);

            _lockInfo = UiFactory.Label("LockInfo", root, "", 19, UiFactory.TextDim, TextAnchor.MiddleCenter);
            _lockInfo.rectTransform.PlaceFixed(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 208), new Vector2(760, 30));

            var hint = UiFactory.Label("Hint", root, "Glisser : pivoter   ·   Pincer / molette : zoomer   ·   2 doigts : parcourir   ·   Double-toucher : recentrer", 16, new Color(1, 1, 1, 0.45f), TextAnchor.MiddleCenter);
            hint.rectTransform.PlaceFixed(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(900, 26));

            // Modes de jeu : affichés mais désactivés tant qu'ils ne sont pas implémentés (aucun faux bouton).
            var modes = new[]
            {
                ("ATELIER", "Phase 3"),
                ("STAND DE TIR", "Phase 4"),
                ("RÉALITÉ MIXTE", "Phase 5"),
                ("MISSIONS", "Phase 6"),
            };
            var bar = UiFactory.Rect("ModeBar", root);
            bar.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(24, 18), new Vector2(-24, 98));
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            foreach (var (label, phase) in modes)
            {
                var b = UiFactory.Button("Mode_" + label, bar, label, null, UiFactory.PanelBg, 22, "Bientôt · " + phase);
                b.Button.interactable = false;
                b.Label.color = UiFactory.TextDim;
            }
        }

        private void BuildToast(RectTransform root)
        {
            var bg = UiFactory.Panel("Toast", root, new Color(0.02f, 0.02f, 0.025f, 0.92f), false);
            bg.rectTransform.PlaceFixed(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -196), new Vector2(760, 64));
            _toastGroup = bg.gameObject.AddComponent<CanvasGroup>();
            _toastGroup.alpha = 0f;
            _toastGroup.blocksRaycasts = false;
            _toast = UiFactory.Label("Text", bg.transform, "", 22, UiFactory.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _toast.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(16, 0), new Vector2(-16, 0));
        }

        private void BuildSettings(Transform canvas)
        {
            var dim = UiFactory.Panel("SettingsModal", canvas, new Color(0, 0, 0, 0.7f));
            dim.rectTransform.Place(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _settingsRoot = dim.gameObject;

            var panel = UiFactory.Panel("Panel", dim.transform, new Color(0.07f, 0.075f, 0.08f, 0.98f));
            panel.rectTransform.PlaceFixed(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 860));
            var p = panel.transform;

            var title = UiFactory.Label("Title", p, "RÉGLAGES", 36, UiFactory.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -90), new Vector2(-40, -20));

            var s = _gs.Profile.settings;
            float y = -110f;
            _gyroValue = ToggleRow(p, ref y, "Gyroscope (stand de tir, missions)", () => { s.gyroEnabled = !s.gyroEnabled; SettingsChanged(); });
            _gyroSensValue = StepperRow(p, ref y, "Sensibilité du gyroscope", d => { s.gyroSensitivity = Mathf.Clamp(Round1(s.gyroSensitivity + d * 0.1f), 0.2f, 3f); SettingsChanged(); });
            _gyroInvertValue = ToggleRow(p, ref y, "Inverser l'axe vertical du gyroscope", () => { s.gyroInvertY = !s.gyroInvertY; SettingsChanged(); });
            _touchSensValue = StepperRow(p, ref y, "Sensibilité tactile", d => { s.touchSensitivity = Mathf.Clamp(Round1(s.touchSensitivity + d * 0.1f), 0.2f, 3f); SettingsChanged(); });
            _qualityValue = ToggleRow(p, ref y, "Qualité graphique", () => { s.graphicsQuality = (s.graphicsQuality + 1) % 3; SettingsChanged(); _ctl.OnQualityChanged(); });
            _volumeValue = StepperRow(p, ref y, "Volume général", d => { s.masterVolume = Mathf.Clamp01(Round1(s.masterVolume + d * 0.1f)); SettingsChanged(); });

            var note = UiFactory.Label("Note", p,
                "Le gyroscope est facultatif et se combine au tactile ; il sera utilisé dans les modes FPS (calibration et recentrage inclus). " +
                "En réalité mixte, le jeu utilise le suivi spatial d'ARKit.", 17, UiFactory.TextDim, TextAnchor.UpperLeft);
            note.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, y - 70), new Vector2(-40, y - 10));

            _resetButton = UiFactory.Button("Reset", p, "RÉINITIALISER LA PROGRESSION", OnResetClicked, new Color(0.35f, 0.08f, 0.07f, 1f), 20);
            _resetButton.Background.rectTransform.PlaceFixed(new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 36), new Vector2(440, 70));
            var close = UiFactory.Button("Close", p, "FERMER", () => ShowSettings(false), UiFactory.Accent, 24);
            close.Background.rectTransform.PlaceFixed(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 36), new Vector2(240, 70));
            close.Label.color = new Color(0.08f, 0.06f, 0.03f);

            _settingsRoot.SetActive(false);
        }

        private static float Round1(float v) => Mathf.Round(v * 10f) / 10f;

        private Text ToggleRow(Transform p, ref float y, string label, UnityEngine.Events.UnityAction onClick)
        {
            RowLabel(p, y, label);
            var b = UiFactory.Button("Toggle", p, "", onClick, UiFactory.PanelBgLight, 20);
            b.Background.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-300, y - 70), new Vector2(-40, y - 10));
            y -= 80f;
            return b.Label;
        }

        private Text StepperRow(Transform p, ref float y, string label, System.Action<int> onStep)
        {
            RowLabel(p, y, label);
            var minus = UiFactory.Button("Minus", p, "–", () => onStep(-1), UiFactory.PanelBgLight, 30);
            minus.Background.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-300, y - 70), new Vector2(-230, y - 10));
            var plus = UiFactory.Button("Plus", p, "+", () => onStep(1), UiFactory.PanelBgLight, 30);
            plus.Background.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-110, y - 70), new Vector2(-40, y - 10));
            var value = UiFactory.Label("Value", p, "", 22, UiFactory.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            value.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-230, y - 70), new Vector2(-110, y - 10));
            y -= 80f;
            return value;
        }

        private static void RowLabel(Transform p, float y, string text)
        {
            var l = UiFactory.Label("Row", p, text, 22, UiFactory.Text, TextAnchor.MiddleLeft);
            l.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, y - 70), new Vector2(-320, y - 10));
        }

        private void SettingsChanged()
        {
            _gs.NotifySettingsChanged();
            RefreshSettings();
        }

        private void OnResetClicked()
        {
            if (!_resetArmed)
            {
                _resetArmed = true;
                _resetButton.Label.text = "CONFIRMER : TOUT EFFACER ?";
                return;
            }
            _resetArmed = false;
            _resetButton.Label.text = "RÉINITIALISER LA PROGRESSION";
            _gs.ResetProgress();
            ShowSettings(false);
            ShowToast("Progression réinitialisée.", false);
        }

        public void ShowSettings(bool show)
        {
            _resetArmed = false;
            if (_resetButton != null) _resetButton.Label.text = "RÉINITIALISER LA PROGRESSION";
            _settingsRoot.SetActive(show);
            if (show) RefreshSettings();
        }

        private void RefreshSettings()
        {
            var s = _gs.Profile.settings;
            bool gyroAvailable = SystemInfo.supportsGyroscope;
            _gyroValue.text = s.gyroEnabled ? (gyroAvailable ? "ACTIVÉ" : "ACTIVÉ (absent)") : "DÉSACTIVÉ";
            _gyroSensValue.text = s.gyroSensitivity.ToString("0.0") + "x";
            _gyroInvertValue.text = s.gyroInvertY ? "OUI" : "NON";
            _touchSensValue.text = s.touchSensitivity.ToString("0.0") + "x";
            _qualityValue.text = s.graphicsQuality == 0 ? "ÉCONOMIE" : s.graphicsQuality == 1 ? "ÉQUILIBRÉE" : "QUALITÉ";
            _volumeValue.text = Mathf.RoundToInt(s.masterVolume * 100f) + " %";
        }

        // ------------------------------------------------------------------ Mise à jour

        public void ShowToast(string message, bool error)
        {
            _toast.text = message;
            _toast.color = error ? UiFactory.Negative : UiFactory.Text;
            _toastUntil = Time.unscaledTime + 2.6f;
        }

        private void Update()
        {
            if (_toastGroup == null) return;
            float target = Time.unscaledTime < _toastUntil ? 1f : 0f;
            _toastGroup.alpha = Mathf.MoveTowards(_toastGroup.alpha, target, Time.unscaledDeltaTime * 5f);
        }

        public void Refresh()
        {
            var profile = _gs.Profile;
            var economy = _gs.Economy;
            var db = _gs.Database;

            _money.text = UiFactory.FormatMoney(profile.money);
            int level = economy.Level;
            _level.text = "NIVEAU " + level;
            _xp.Set(economy.Progression.LevelProgress(profile.xp));

            var equipped = _gs.Arsenal.SelectedBuild;
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var w = db.GetWeapon(item.WeaponId);
                bool owned = profile.OwnsWeapon(w.id);
                bool selected = i == _ctl.SelectedIndex;
                bool isEquipped = equipped != null && equipped.weaponId == w.id;
                item.Background.color = selected ? new Color(0.17f, 0.18f, 0.19f, 0.95f) : UiFactory.PanelBgLight;
                item.AccentBar.gameObject.SetActive(selected);
                if (isEquipped) { item.Status.text = "ÉQUIPÉE"; item.Status.color = UiFactory.Accent; }
                else if (owned) { item.Status.text = "POSSÉDÉE"; item.Status.color = UiFactory.Positive; }
                else if (level < w.unlockLevel) { item.Status.text = "NIV. " + w.unlockLevel; item.Status.color = UiFactory.Negative; }
                else { item.Status.text = UiFactory.FormatMoney(w.price); item.Status.color = UiFactory.Text; }
                item.Name.color = owned ? UiFactory.Text : UiFactory.TextDim;
            }

            var weapon = _ctl.SelectedWeapon;
            var build = _ctl.DisplayedBuild;
            if (weapon == null) return;
            bool ownedSel = profile.OwnsWeapon(weapon.id);
            _name.text = weapon.displayName.ToUpperInvariant();
            _family.text = WeaponFamilies.DisplayName(weapon.family).ToUpperInvariant() + "   ·   " + FireModeName(weapon.fireMode);
            _desc.text = weapon.description;

            var stats = _gs.Arsenal.GetFinalStats(build);
            StatBlock compare = null;
            if (equipped != null && equipped.weaponId != weapon.id) compare = _gs.Arsenal.GetFinalStats(equipped);
            foreach (var stat in StatIds.All)
            {
                float v = stats.Get(stat);
                var row = _stats[stat];
                float c = compare != null ? StatCalculator.Normalized(stat, compare.Get(stat)) : -1f;
                row.Bar.Set(StatCalculator.Normalized(stat, v), c);
                string text = StatCalculator.Format(stat, v);
                if (stat == StatIds.Damage && weapon.pellets > 1) text = $"{v:0} x{weapon.pellets}";
                row.Value.text = text;
            }
            _compareHint.text = compare != null
                ? "Repère clair : arme équipée (" + db.GetWeapon(equipped.weaponId).displayName + ")"
                : "";

            int buildCount = ownedSel ? _gs.Arsenal.GetBuilds(weapon.id).Count : 0;
            _buildName.text = ownedSel ? build.name + (buildCount > 1 ? $"  ({_ctl.BuildIndex + 1}/{buildCount})" : "") : "Configuration d'origine";
            _prevBuild.Button.interactable = _nextBuild.Button.interactable = buildCount > 1;

            // Action principale
            if (!ownedSel)
            {
                var can = economy.CanBuyWeapon(weapon.id);
                _primary.Label.text = "ACHETER  —  " + UiFactory.FormatMoney(weapon.price);
                _primary.Button.interactable = can.Success;
                _lockInfo.text = can.Success ? "Achat avec des crédits virtuels gagnés en jeu." : can.Message;
                _lockInfo.color = can.Success ? UiFactory.TextDim : UiFactory.Negative;
            }
            else if (equipped != null && equipped.id == build.id)
            {
                _primary.Label.text = "ÉQUIPÉE";
                _primary.Button.interactable = false;
                _lockInfo.text = "Cette configuration sera utilisée au stand de tir et en mission.";
                _lockInfo.color = UiFactory.TextDim;
            }
            else
            {
                _primary.Label.text = "ÉQUIPER";
                _primary.Button.interactable = true;
                _lockInfo.text = "";
            }
        }

        private static string FireModeName(string mode)
        {
            switch (mode)
            {
                case "auto": return "AUTOMATIQUE";
                case "pump": return "À POMPE";
                case "bolt": return "À VERROU";
                default: return "SEMI-AUTOMATIQUE";
            }
        }
    }
}
