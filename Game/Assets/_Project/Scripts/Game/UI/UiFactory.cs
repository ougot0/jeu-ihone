using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Arsenal.Game.UI
{
    /// <summary>Palette et fabrique de composants d'interface (uGUI construit par code, sans prefab).</summary>
    public static class UiFactory
    {
        public static readonly Color PanelBg = new Color(0.04f, 0.045f, 0.05f, 0.78f);
        public static readonly Color PanelBgLight = new Color(0.1f, 0.11f, 0.12f, 0.85f);
        public static readonly Color Text = new Color(0.93f, 0.93f, 0.92f);
        public static readonly Color TextDim = new Color(0.62f, 0.64f, 0.66f);
        public static readonly Color Accent = new Color(0.95f, 0.62f, 0.2f);
        public static readonly Color Positive = new Color(0.45f, 0.85f, 0.5f);
        public static readonly Color Negative = new Color(0.95f, 0.38f, 0.32f);
        public static readonly Color Disabled = new Color(0.25f, 0.26f, 0.27f, 0.8f);

        private static Font _font;
        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Place un RectTransform par ancres (0..1) et marges en pixels de référence.</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        /// <summary>Place un élément de taille fixe par rapport à une ancre unique.</summary>
        public static RectTransform PlaceFixed(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Panel(string name, Transform parent, Color color, bool raycast = true)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }

        public class ButtonRefs
        {
            public Button Button;
            public Image Background;
            public Text Label;
            public Text SubLabel;
        }

        public static ButtonRefs Button(string name, Transform parent, string label, UnityAction onClick,
            Color? background = null, int fontSize = 26, string subLabel = null)
        {
            var bg = Panel(name, parent, background ?? PanelBgLight);
            var btn = bg.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            btn.targetGraphic = bg;
            if (onClick != null) btn.onClick.AddListener(onClick);

            var refs = new ButtonRefs { Button = btn, Background = bg };
            refs.Label = Label("Label", bg.transform, label, fontSize, Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (subLabel != null)
            {
                refs.Label.rectTransform.Place(new Vector2(0, 0.42f), Vector2.one, Vector2.zero, Vector2.zero);
                refs.SubLabel = Label("SubLabel", bg.transform, subLabel, Mathf.RoundToInt(fontSize * 0.62f), TextDim, TextAnchor.MiddleCenter);
                refs.SubLabel.rectTransform.Place(Vector2.zero, new Vector2(1, 0.48f), Vector2.zero, Vector2.zero);
            }
            else refs.Label.rectTransform.Place(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return refs;
        }

        /// <summary>Jauge horizontale (fond + remplissage piloté par l'ancre max).</summary>
        public class Bar
        {
            public RectTransform Fill;
            public Image FillImage;
            public RectTransform Ghost;

            public void Set(float value01, float compare01 = -1f)
            {
                Fill.anchorMax = new Vector2(Mathf.Clamp01(value01), 1f);
                if (Ghost != null)
                {
                    bool show = compare01 >= 0f && Mathf.Abs(compare01 - value01) > 0.005f;
                    Ghost.gameObject.SetActive(show);
                    if (show) Ghost.anchorMax = new Vector2(Mathf.Clamp01(compare01), 1f);
                }
            }
        }

        public static Bar CreateBar(string name, Transform parent, Color fill)
        {
            var bg = Panel(name, parent, new Color(1f, 1f, 1f, 0.08f), false);
            var ghost = Panel("Ghost", bg.transform, new Color(1f, 1f, 1f, 0.25f), false).rectTransform;
            ghost.Place(Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            var fillImg = Panel("Fill", bg.transform, fill, false);
            fillImg.rectTransform.Place(Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            ghost.gameObject.SetActive(false);
            return new Bar { Fill = fillImg.rectTransform, FillImage = fillImg, Ghost = ghost };
        }

        /// <summary>Formate un montant de crédits virtuels : 12 500 cr.</summary>
        public static string FormatMoney(int value)
        {
            var digits = Math.Abs(value).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < digits.Length; i++)
            {
                if (i > 0 && (digits.Length - i) % 3 == 0) sb.Append(' ');
                sb.Append(digits[i]);
            }
            return (value < 0 ? "-" : "") + sb + " cr";
        }
    }
}
