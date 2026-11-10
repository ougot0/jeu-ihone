using System.Collections.Generic;
using UnityEngine;

namespace Arsenal.Game.Rendering
{
    /// <summary>Met en cache les maillages primitifs Unity (sans créer de colliders inutiles).</summary>
    public static class PrimitiveMeshes
    {
        private static readonly Dictionary<PrimitiveType, Mesh> Cache = new Dictionary<PrimitiveType, Mesh>();

        public static Mesh Get(PrimitiveType type)
        {
            if (Cache.TryGetValue(type, out var mesh) && mesh != null) return mesh;
            var go = GameObject.CreatePrimitive(type);
            mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(go);
            Cache[type] = mesh;
            return mesh;
        }

        public static PrimitiveType FromShape(string shape)
        {
            switch (shape)
            {
                case "cylinder": return PrimitiveType.Cylinder;
                case "sphere": return PrimitiveType.Sphere;
                case "capsule": return PrimitiveType.Capsule;
                case "quad": return PrimitiveType.Quad;
                case "plane": return PrimitiveType.Plane;
                default: return PrimitiveType.Cube;
            }
        }

        /// <summary>Crée un objet rendu (sans collider) avec le maillage et le matériau donnés.</summary>
        public static GameObject Create(string name, PrimitiveType type, Material material, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = Get(type);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            return go;
        }
    }
}
