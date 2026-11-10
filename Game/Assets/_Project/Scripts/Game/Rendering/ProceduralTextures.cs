using UnityEngine;

namespace Arsenal.Game.Rendering
{
    /// <summary>
    /// Textures générées au démarrage (aucun fichier image requis) pour donner du grain aux matériaux
    /// provisoires. À remplacer par des textures PBR (Poly Haven / ambientCG, CC0) — voir docs/03-assets.md.
    /// </summary>
    public static class ProceduralTextures
    {
        private static Texture2D _brushed, _fineNoise, _concrete, _pegboard, _wood, _panel;

        public static Texture2D BrushedMetal => _brushed ? _brushed : (_brushed = Generate("BrushedMetal", 128, (x, y) =>
        {
            float streak = Mathf.PerlinNoise(x * 0.02f, y * 0.9f);
            float fine = Mathf.PerlinNoise(x * 0.5f + 17f, y * 3.3f);
            float v = 0.82f + streak * 0.12f + fine * 0.06f;
            return new Color(v, v, v, 1f);
        }));

        public static Texture2D FineNoise => _fineNoise ? _fineNoise : (_fineNoise = Generate("FineNoise", 128, (x, y) =>
        {
            float v = 0.88f + Mathf.PerlinNoise(x * 0.35f, y * 0.35f) * 0.12f;
            return new Color(v, v, v, 1f);
        }));

        public static Texture2D Concrete => _concrete ? _concrete : (_concrete = Generate("Concrete", 256, (x, y) =>
        {
            float a = Mathf.PerlinNoise(x * 0.015f, y * 0.015f);
            float b = Mathf.PerlinNoise(x * 0.08f + 5f, y * 0.08f + 9f);
            float c = Mathf.PerlinNoise(x * 0.4f + 31f, y * 0.4f + 7f);
            float v = 0.62f + a * 0.22f + b * 0.1f + c * 0.06f;
            return new Color(v, v * 0.99f, v * 0.97f, 1f);
        }));

        public static Texture2D Pegboard => _pegboard ? _pegboard : (_pegboard = Generate("Pegboard", 128, (x, y) =>
        {
            float cx = (x % 32) - 16f, cy = (y % 32) - 16f;
            float hole = Mathf.Sqrt(cx * cx + cy * cy) < 4.2f ? 0.18f : 1f;
            float n = 0.92f + Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.08f;
            return new Color(hole * n, hole * n, hole * n, 1f);
        }));

        public static Texture2D WoodGrain => _wood ? _wood : (_wood = Generate("Wood", 128, (x, y) =>
        {
            float rings = Mathf.Sin((y + Mathf.PerlinNoise(x * 0.05f, y * 0.02f) * 40f) * 0.35f) * 0.5f + 0.5f;
            float v = 0.75f + rings * 0.2f + Mathf.PerlinNoise(x * 0.6f, y * 0.1f) * 0.05f;
            return new Color(v, v, v, 1f);
        }));

        public static Texture2D WallPanel => _panel ? _panel : (_panel = Generate("WallPanel", 256, (x, y) =>
        {
            bool seam = (x % 128) < 2 || (y % 128) < 2;
            float n = 0.9f + Mathf.PerlinNoise(x * 0.03f, y * 0.03f) * 0.1f;
            float v = seam ? 0.45f : n;
            return new Color(v, v, v, 1f);
        }));

        private static Texture2D Generate(string name, int size, System.Func<int, int, Color> f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = f(x, y);
            tex.SetPixels(pixels);
            tex.Apply(true, true);
            return tex;
        }
    }
}
