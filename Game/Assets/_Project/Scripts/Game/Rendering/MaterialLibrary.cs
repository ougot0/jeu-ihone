using System.Collections.Generic;
using UnityEngine;

namespace Arsenal.Game.Rendering
{
    /// <summary>
    /// Matériaux PBR du jeu. Utilise les matériaux modèles de Resources/Materials (créés par le menu
    /// Arsenal > Configurer le projet, ce qui garantit l'inclusion des shaders URP dans le build iOS),
    /// sinon Shader.Find (éditeur), sinon le shader Standard (pipeline intégré).
    /// </summary>
    public static class MaterialLibrary
    {
        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        private static Material _litTemplate;
        private static Material _emissiveTemplate;
        private static bool _isUrp;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int Metallic = Shader.PropertyToID("_Metallic");
        private static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
        private static readonly int Glossiness = Shader.PropertyToID("_Glossiness");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");

        private static void EnsureTemplates()
        {
            if (_litTemplate != null) return;
            _litTemplate = Resources.Load<Material>("Materials/ArsenalLit");
            _emissiveTemplate = Resources.Load<Material>("Materials/ArsenalLitEmissive");
            if (_litTemplate == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null || UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null)
                    shader = Shader.Find("Standard");
                _litTemplate = new Material(shader);
            }
            if (_emissiveTemplate == null) _emissiveTemplate = _litTemplate;
            _isUrp = _litTemplate.HasProperty(BaseColor);
        }

        public static Material Create(string name, Color color, float metallic, float smoothness, Texture texture = null, Vector2? tiling = null)
        {
            EnsureTemplates();
            var m = new Material(_litTemplate) { name = name };
            SetColor(m, color);
            m.SetFloat(Metallic, metallic);
            m.SetFloat(_isUrp ? Smoothness : Glossiness, smoothness);
            if (texture != null)
            {
                int texId = _isUrp ? BaseMap : MainTex;
                m.SetTexture(texId, texture);
                if (tiling.HasValue) m.SetTextureScale(texId, tiling.Value);
            }
            return m;
        }

        public static Material CreateEmissive(string name, Color baseColor, Color emission)
        {
            EnsureTemplates();
            var m = new Material(_emissiveTemplate) { name = name };
            SetColor(m, baseColor);
            m.SetFloat(Metallic, 0f);
            m.SetFloat(_isUrp ? Smoothness : Glossiness, 0.8f);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor(EmissionColor, emission);
            return m;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m.HasProperty(BaseColor)) m.SetColor(BaseColor, c);
            if (m.HasProperty(ColorId)) m.SetColor(ColorId, c);
        }

        public static void SetPbr(Material m, float metallic, float smoothness)
        {
            m.SetFloat(Metallic, metallic);
            if (m.HasProperty(Smoothness)) m.SetFloat(Smoothness, smoothness);
            if (m.HasProperty(Glossiness)) m.SetFloat(Glossiness, smoothness);
        }

        /// <summary>Matériaux partagés des pièces d'armes (hors "body", teinté par le revêtement).</summary>
        public static Material Get(string role)
        {
            if (Cache.TryGetValue(role, out var cached) && cached != null) return cached;
            Material m;
            switch (role)
            {
                case "metal": m = Create("Weapon_Metal", new Color(0.16f, 0.165f, 0.17f), 0.9f, 0.62f, ProceduralTextures.BrushedMetal, new Vector2(1f, 4f)); break;
                case "polymer": m = Create("Weapon_Polymer", new Color(0.075f, 0.075f, 0.08f), 0f, 0.32f, ProceduralTextures.FineNoise, new Vector2(3f, 3f)); break;
                case "rubber": m = Create("Weapon_Rubber", new Color(0.045f, 0.045f, 0.05f), 0f, 0.15f, ProceduralTextures.FineNoise, new Vector2(6f, 6f)); break;
                case "accent": m = Create("Weapon_Accent", new Color(0.78f, 0.6f, 0.32f), 1f, 0.8f); break;
                case "wood": m = Create("Weapon_Wood", new Color(0.42f, 0.22f, 0.1f), 0f, 0.55f, ProceduralTextures.WoodGrain, new Vector2(1f, 1f)); break;
                case "glass": m = CreateEmissive("Weapon_Glass", new Color(0.02f, 0.04f, 0.06f), new Color(0.02f, 0.06f, 0.08f)); SetPbr(m, 1f, 0.97f); break;
                case "emissive": m = CreateEmissive("Weapon_Emissive", new Color(0.6f, 0.02f, 0.02f), new Color(4f, 0.1f, 0.1f)); break;
                case "lamp": m = CreateEmissive("Weapon_Lamp", new Color(0.9f, 0.9f, 0.85f), new Color(2.5f, 2.5f, 2.2f)); break;
                default: m = Create("Weapon_Body_Default", new Color(0.1f, 0.1f, 0.11f), 0.45f, 0.5f); break;
            }
            Cache[role] = m;
            return m;
        }
    }
}
