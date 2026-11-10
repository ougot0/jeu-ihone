using System;
using System.Collections.Generic;
using Arsenal.Core.Data;

namespace Arsenal.Core.Rules
{
    /// <summary>
    /// Calcule les statistiques finales : (base + somme des ajouts) × produit des multiplicateurs, puis bornage.
    /// </summary>
    public static class StatCalculator
    {
        public static StatBlock Compute(StatBlock baseStats, IEnumerable<AttachmentDefinition> attachments)
        {
            var result = baseStats.Clone();
            foreach (var stat in StatIds.All)
            {
                float add = 0f, mul = 1f;
                if (attachments != null)
                {
                    foreach (var a in attachments)
                    {
                        if (a?.modifiers == null) continue;
                        foreach (var m in a.modifiers)
                        {
                            if (m.stat != stat) continue;
                            add += m.add;
                            mul *= m.mul;
                        }
                    }
                }
                var value = (baseStats.Get(stat) + add) * mul;
                result.Set(stat, Clamp(stat, value));
            }
            return result;
        }

        public static float Clamp(string stat, float value)
        {
            switch (stat)
            {
                case StatIds.Accuracy:
                case StatIds.Recoil:
                case StatIds.Handling:
                    return Math.Max(0f, Math.Min(100f, value));
                case StatIds.Damage: return Math.Max(1f, value);
                case StatIds.FireRate: return Math.Max(30f, Math.Min(1200f, value));
                case StatIds.MagazineSize: return Math.Max(1f, Math.Min(200f, value));
                case StatIds.ReloadTime: return Math.Max(0.3f, Math.Min(10f, value));
                default: return value;
            }
        }

        /// <summary>Valeur normalisée 0..1 pour l'affichage des jauges (1 = meilleur).</summary>
        public static float Normalized(string stat, float value)
        {
            float n;
            switch (stat)
            {
                case StatIds.Damage: n = value / 120f; break;
                case StatIds.Accuracy:
                case StatIds.Handling: n = value / 100f; break;
                case StatIds.Recoil: n = 1f - value / 100f; break;
                case StatIds.FireRate: n = value / 1000f; break;
                case StatIds.MagazineSize: n = value / 60f; break;
                case StatIds.ReloadTime: n = 1f - (value - 0.5f) / 5f; break;
                default: n = 0.5f; break;
            }
            return Math.Max(0.02f, Math.Min(1f, n));
        }

        public static string Format(string stat, float value)
        {
            switch (stat)
            {
                case StatIds.FireRate: return $"{value:0} cps/min";
                case StatIds.MagazineSize: return $"{value:0}";
                case StatIds.ReloadTime: return $"{value:0.0} s";
                default: return $"{value:0}";
            }
        }
    }
}
