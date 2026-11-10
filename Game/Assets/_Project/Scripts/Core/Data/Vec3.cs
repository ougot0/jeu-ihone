using System;

namespace Arsenal.Core.Data
{
    /// <summary>Vecteur sérialisable indépendant de Unity (le coeur logique n'a aucune dépendance moteur).</summary>
    [Serializable]
    public class Vec3
    {
        public float x;
        public float y;
        public float z;

        public Vec3() { }
        public Vec3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

        public static Vec3 Zero => new Vec3(0f, 0f, 0f);
        public static Vec3 One => new Vec3(1f, 1f, 1f);
        public override string ToString() => $"({x}, {y}, {z})";
    }

    [Serializable]
    public class ColorRgba
    {
        public float r = 1f;
        public float g = 1f;
        public float b = 1f;
        public float a = 1f;

        public ColorRgba() { }
        public ColorRgba(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }
}
