using Arsenal.Core.Data;
using Arsenal.Core.Profile;
using Arsenal.Game.Rendering;
using UnityEngine;

namespace Arsenal.Game.Weapons
{
    /// <summary>
    /// Construit le modèle 3D d'une arme.
    /// 1) Si un modèle définitif existe dans Resources/Models/Weapons/{modelId}, il est utilisé :
    ///    ses points de fixation doivent être des objets enfants nommés "Socket_{idEmplacement}" et
    ///    ses matériaux teintables doivent contenir "Body" dans leur nom.
    /// 2) Sinon, un modèle provisoire est assemblé à partir des pièces décrites dans weapons.json.
    /// Le reste du jeu ne voit que <see cref="WeaponVisual"/> : remplacer un modèle ne change aucun code.
    /// </summary>
    public static class WeaponVisualBuilder
    {
        public const string SocketPrefix = "Socket_";

        public static WeaponVisual Build(WeaponDefinition def, WeaponBuild build, GameDatabase db, Transform parent)
        {
            var go = new GameObject($"Weapon_{def.id}");
            go.transform.SetParent(parent, false);
            var visual = go.AddComponent<WeaponVisual>();
            var root = new GameObject("Model").transform;
            root.SetParent(go.transform, false);
            visual.Initialize(def, root);

            var prefab = string.IsNullOrEmpty(def.modelId) ? null : Resources.Load<GameObject>("Models/Weapons/" + def.modelId);
            if (prefab != null) BuildFromPrefab(visual, def, prefab, root);
            else BuildFromParts(visual, def, root);

            visual.ApplyBuild(build, db);
            return visual;
        }

        private static void BuildFromParts(WeaponVisual visual, WeaponDefinition def, Transform root)
        {
            foreach (var part in def.parts)
            {
                var go = CreatePart(part, root, visual.BodyMaterial);
                visual.RegisterPart(part.name, go);
                if (part.material == "body") visual.RegisterBodyRenderer(go.GetComponent<Renderer>());
            }
            foreach (var slot in def.slots)
            {
                var socket = new GameObject(SocketPrefix + slot.id).transform;
                socket.SetParent(root, false);
                socket.localPosition = ToVector(slot.position);
                socket.localRotation = Quaternion.Euler(ToVector(slot.rotation));
                visual.RegisterSocket(slot.id, socket);
            }
        }

        private static void BuildFromPrefab(WeaponVisual visual, WeaponDefinition def, GameObject prefab, Transform root)
        {
            var instance = Object.Instantiate(prefab, root, false);
            foreach (var t in instance.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith(SocketPrefix)) visual.RegisterSocket(t.name.Substring(SocketPrefix.Length), t);
                else visual.RegisterPart(t.name, t.gameObject);
            }
            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && mats[i].name.Contains("Body")) { mats[i] = visual.BodyMaterial; changed = true; }
                }
                if (changed) { r.sharedMaterials = mats; visual.RegisterBodyRenderer(r); }
            }
            foreach (var slot in def.slots)
            {
                if (visual.GetSocket(slot.id) != null) continue;
                Debug.LogWarning($"[Modèle] {def.modelId} : socket '{SocketPrefix}{slot.id}' manquant, position des données utilisée.");
                var socket = new GameObject(SocketPrefix + slot.id).transform;
                socket.SetParent(root, false);
                socket.localPosition = ToVector(slot.position);
                socket.localRotation = Quaternion.Euler(ToVector(slot.rotation));
                visual.RegisterSocket(slot.id, socket);
            }
        }

        /// <summary>Instancie un accessoire comme objet 3D fixé au socket.</summary>
        public static GameObject BuildAttachment(AttachmentDefinition def, Transform socket, Material bodyMaterial)
        {
            var prefab = string.IsNullOrEmpty(def.modelId) ? null : Resources.Load<GameObject>("Models/Attachments/" + def.modelId);
            if (prefab != null)
            {
                var inst = Object.Instantiate(prefab, socket, false);
                inst.name = "Attachment_" + def.id;
                return inst;
            }
            var go = new GameObject("Attachment_" + def.id);
            go.transform.SetParent(socket, false);
            foreach (var part in def.parts) CreatePart(part, go.transform, bodyMaterial);
            return go;
        }

        private static GameObject CreatePart(PartDefinition part, Transform parent, Material bodyMaterial)
        {
            var mat = part.material == "body" ? bodyMaterial : MaterialLibrary.Get(part.material);
            var go = PrimitiveMeshes.Create(string.IsNullOrEmpty(part.name) ? "part" : part.name, PrimitiveMeshes.FromShape(part.shape), mat, parent);
            go.transform.localPosition = ToVector(part.position);
            go.transform.localRotation = Quaternion.Euler(ToVector(part.rotation));
            go.transform.localScale = ToVector(part.scale);
            return go;
        }

        public static Vector3 ToVector(Vec3 v) => v == null ? Vector3.zero : new Vector3(v.x, v.y, v.z);
    }
}
