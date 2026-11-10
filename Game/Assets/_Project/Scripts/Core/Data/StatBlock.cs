using System;

namespace Arsenal.Core.Data
{
    /// <summary>Identifiants des statistiques (utilisés dans les fichiers de données pour les modificateurs).</summary>
    public static class StatIds
    {
        public const string Damage = "damage";
        public const string Accuracy = "accuracy";
        public const string Recoil = "recoil";
        public const string FireRate = "fireRate";
        public const string MagazineSize = "magazineSize";
        public const string Handling = "handling";
        public const string ReloadTime = "reloadTime";

        public static readonly string[] All =
        {
            Damage, Accuracy, Recoil, FireRate, MagazineSize, Handling, ReloadTime
        };

        /// <summary>Vrai si une valeur plus basse est meilleure (recul, temps de rechargement).</summary>
        public static bool LowerIsBetter(string statId) => statId == Recoil || statId == ReloadTime;

        public static string DisplayName(string statId)
        {
            switch (statId)
            {
                case Damage: return "Dégâts";
                case Accuracy: return "Précision";
                case Recoil: return "Recul";
                case FireRate: return "Cadence";
                case MagazineSize: return "Chargeur";
                case Handling: return "Maniabilité";
                case ReloadTime: return "Rechargement";
                default: return statId;
            }
        }

        public static bool IsKnown(string statId) => Array.IndexOf(All, statId) >= 0;
    }

    /// <summary>
    /// Caractéristiques de gameplay d'une arme.
    /// damage : dégâts par projectile ; accuracy, recoil, handling : 0..100 ;
    /// fireRate : coups/minute ; magazineSize : cartouches ; reloadTime : secondes.
    /// </summary>
    [Serializable]
    public class StatBlock
    {
        public float damage;
        public float accuracy;
        public float recoil;
        public float fireRate;
        public int magazineSize;
        public float handling;
        public float reloadTime;

        public StatBlock Clone() => (StatBlock)MemberwiseClone();

        public float Get(string statId)
        {
            switch (statId)
            {
                case StatIds.Damage: return damage;
                case StatIds.Accuracy: return accuracy;
                case StatIds.Recoil: return recoil;
                case StatIds.FireRate: return fireRate;
                case StatIds.MagazineSize: return magazineSize;
                case StatIds.Handling: return handling;
                case StatIds.ReloadTime: return reloadTime;
                default: throw new ArgumentException("Statistique inconnue : " + statId);
            }
        }

        public void Set(string statId, float value)
        {
            switch (statId)
            {
                case StatIds.Damage: damage = value; break;
                case StatIds.Accuracy: accuracy = value; break;
                case StatIds.Recoil: recoil = value; break;
                case StatIds.FireRate: fireRate = value; break;
                case StatIds.MagazineSize: magazineSize = (int)Math.Round(value); break;
                case StatIds.Handling: handling = value; break;
                case StatIds.ReloadTime: reloadTime = value; break;
                default: throw new ArgumentException("Statistique inconnue : " + statId);
            }
        }
    }
}
