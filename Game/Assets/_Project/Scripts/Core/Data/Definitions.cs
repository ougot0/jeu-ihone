using System;
using System.Collections.Generic;

namespace Arsenal.Core.Data
{
    /// <summary>Familles d'armes (fictives, inspirées de grandes catégories génériques).</summary>
    public static class WeaponFamilies
    {
        public const string Pistol = "pistol";
        public const string Revolver = "revolver";
        public const string Smg = "smg";
        public const string Rifle = "rifle";
        public const string Sniper = "sniper";
        public const string Shotgun = "shotgun";

        public static readonly string[] All = { Pistol, Revolver, Smg, Rifle, Sniper, Shotgun };

        public static string DisplayName(string family)
        {
            switch (family)
            {
                case Pistol: return "Pistolet semi-automatique";
                case Revolver: return "Revolver";
                case Smg: return "Pistolet-mitrailleur";
                case Rifle: return "Fusil d'assaut";
                case Sniper: return "Fusil de précision";
                case Shotgun: return "Fusil à pompe";
                default: return family;
            }
        }
    }

    /// <summary>Types d'accessoires. Un emplacement déclare la liste des types qu'il accepte.</summary>
    public static class AttachmentTypes
    {
        public const string Optic = "optic";
        public const string Muzzle = "muzzle";
        public const string Grip = "grip";
        public const string Stock = "stock";
        public const string Magazine = "magazine";
        public const string RailAccessory = "rail";
        public const string Light = "light";
        public const string Laser = "laser";
        public const string Cosmetic = "cosmetic";

        public static readonly string[] All = { Optic, Muzzle, Grip, Stock, Magazine, RailAccessory, Light, Laser, Cosmetic };

        public static string DisplayName(string type)
        {
            switch (type)
            {
                case Optic: return "Optique";
                case Muzzle: return "Bouche";
                case Grip: return "Poignée";
                case Stock: return "Crosse";
                case Magazine: return "Chargeur";
                case RailAccessory: return "Rail";
                case Light: return "Lampe";
                case Laser: return "Laser";
                case Cosmetic: return "Cosmétique";
                default: return type;
            }
        }
    }

    /// <summary>
    /// Une pièce visuelle "provisoire" (primitive) décrite dans les données. Permet de construire des modèles
    /// temporaires sans aucun fichier 3D ; remplaçables par de vrais modèles (voir docs/03-assets.md).
    /// shape : box | cylinder | sphere | capsule. material : body | metal | polymer | accent | glass | emissive | rubber | wood.
    /// Le cylindre Unity mesure 2 unités de haut sur l'axe Y : scale.y = demi-longueur.
    /// </summary>
    [Serializable]
    public class PartDefinition
    {
        public string name;
        public string shape = "box";
        public string material = "body";
        public Vec3 position = Vec3.Zero;
        public Vec3 rotation = Vec3.Zero;
        public Vec3 scale = Vec3.One;
    }

    /// <summary>Emplacement d'accessoire d'une arme : position du point de fixation + règles d'acceptation.</summary>
    [Serializable]
    public class SlotDefinition
    {
        public string id;
        /// <summary>Types d'accessoires acceptés (voir <see cref="AttachmentTypes"/>).</summary>
        public string[] types = Array.Empty<string>();
        /// <summary>Interface de fixation fictive requise (ex. "rail-t", "filetage-s"). Vide = aucune contrainte.</summary>
        public string mount = "";
        /// <summary>Accessoire installé par défaut (offert avec l'arme). Vide = emplacement libre.</summary>
        public string defaultAttachment = "";
        /// <summary>Si vrai, l'emplacement ne peut pas rester vide (ex. chargeur).</summary>
        public bool required;
        public Vec3 position = Vec3.Zero;
        public Vec3 rotation = Vec3.Zero;
    }

    [Serializable]
    public class StatModifier
    {
        public string stat;
        public float add;
        public float mul = 1f;
    }

    [Serializable]
    public class WeaponDefinition
    {
        public string id;
        public string displayName;
        public string family;
        public string description;
        public int price;
        public int unlockLevel = 1;
        /// <summary>Projectiles par tir (fusil à pompe &gt; 1).</summary>
        public int pellets = 1;
        /// <summary>"semi" | "auto" | "pump" | "bolt".</summary>
        public string fireMode = "semi";
        /// <summary>Identifiant du modèle 3D définitif (Resources/Models/Weapons/{modelId}). Vide = modèle provisoire.</summary>
        public string modelId = "";
        public StatBlock baseStats = new StatBlock();
        public SlotDefinition[] slots = Array.Empty<SlotDefinition>();
        public PartDefinition[] parts = Array.Empty<PartDefinition>();

        public SlotDefinition FindSlot(string slotId)
        {
            if (slots == null) return null;
            foreach (var s in slots) if (s.id == slotId) return s;
            return null;
        }
    }

    [Serializable]
    public class AttachmentDefinition
    {
        public string id;
        public string displayName;
        public string type;
        public string description;
        public int price;
        public int unlockLevel = 1;
        /// <summary>Interfaces de fixation fictives compatibles.</summary>
        public string[] mounts = Array.Empty<string>();
        /// <summary>Familles d'armes autorisées. Vide = toutes.</summary>
        public string[] families = Array.Empty<string>();
        /// <summary>Ids ou types d'accessoires incompatibles lorsqu'ils sont installés en même temps.</summary>
        public string[] conflicts = Array.Empty<string>();
        /// <summary>Noms des pièces de l'arme à masquer quand l'accessoire est installé (ex. guidon remplacé par une optique).</summary>
        public string[] hidesParts = Array.Empty<string>();
        public StatModifier[] modifiers = Array.Empty<StatModifier>();
        public string modelId = "";
        public PartDefinition[] parts = Array.Empty<PartDefinition>();
    }

    /// <summary>Revêtement / peinture : teinte les pièces "body" de l'arme.</summary>
    [Serializable]
    public class FinishDefinition
    {
        public string id;
        public string displayName;
        public int price;
        public int unlockLevel = 1;
        public ColorRgba color = new ColorRgba(0.1f, 0.1f, 0.1f);
        public float metallic = 0.5f;
        public float smoothness = 0.5f;
    }

    [Serializable]
    public class EconomyConfig
    {
        public int startingMoney = 1500;
        public string[] startingWeapons = Array.Empty<string>();
        public string[] startingAttachments = Array.Empty<string>();
        public string[] startingFinishes = Array.Empty<string>();
        public string defaultFinish = "";
        /// <summary>XP nécessaire pour passer du niveau 1 au niveau 2.</summary>
        public int xpBase = 400;
        /// <summary>Multiplicateur appliqué à chaque niveau suivant.</summary>
        public float xpGrowth = 1.35f;
        public int maxLevel = 30;
        public int maxBuildsPerWeapon = 6;
    }

    // ---- Enveloppes des fichiers JSON (JsonUtility ne désérialise pas de tableaux racine) ----

    [Serializable] public class WeaponCatalog { public WeaponDefinition[] weapons = Array.Empty<WeaponDefinition>(); }
    [Serializable] public class AttachmentCatalog { public AttachmentDefinition[] attachments = Array.Empty<AttachmentDefinition>(); }
    [Serializable] public class FinishCatalog { public FinishDefinition[] finishes = Array.Empty<FinishDefinition>(); }

    /// <summary>Base de données en lecture seule du contenu du jeu, construite depuis les fichiers de données.</summary>
    public class GameDatabase
    {
        private readonly Dictionary<string, WeaponDefinition> _weapons = new Dictionary<string, WeaponDefinition>();
        private readonly Dictionary<string, AttachmentDefinition> _attachments = new Dictionary<string, AttachmentDefinition>();
        private readonly Dictionary<string, FinishDefinition> _finishes = new Dictionary<string, FinishDefinition>();

        public IReadOnlyList<WeaponDefinition> Weapons { get; }
        public IReadOnlyList<AttachmentDefinition> Attachments { get; }
        public IReadOnlyList<FinishDefinition> Finishes { get; }
        public EconomyConfig Economy { get; }

        public GameDatabase(WeaponCatalog weapons, AttachmentCatalog attachments, FinishCatalog finishes, EconomyConfig economy)
        {
            var w = new List<WeaponDefinition>(weapons?.weapons ?? Array.Empty<WeaponDefinition>());
            var a = new List<AttachmentDefinition>(attachments?.attachments ?? Array.Empty<AttachmentDefinition>());
            var f = new List<FinishDefinition>(finishes?.finishes ?? Array.Empty<FinishDefinition>());
            foreach (var x in w) if (!string.IsNullOrEmpty(x.id)) _weapons[x.id] = x;
            foreach (var x in a) if (!string.IsNullOrEmpty(x.id)) _attachments[x.id] = x;
            foreach (var x in f) if (!string.IsNullOrEmpty(x.id)) _finishes[x.id] = x;
            Weapons = w;
            Attachments = a;
            Finishes = f;
            Economy = economy ?? new EconomyConfig();
        }

        public WeaponDefinition GetWeapon(string id) => id != null && _weapons.TryGetValue(id, out var v) ? v : null;
        public AttachmentDefinition GetAttachment(string id) => id != null && _attachments.TryGetValue(id, out var v) ? v : null;
        public FinishDefinition GetFinish(string id) => id != null && _finishes.TryGetValue(id, out var v) ? v : null;
    }
}
