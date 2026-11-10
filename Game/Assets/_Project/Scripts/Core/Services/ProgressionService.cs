using System;
using Arsenal.Core.Data;

namespace Arsenal.Core.Services
{
    /// <summary>Courbe d'expérience configurable depuis economy.json (xpBase, xpGrowth, maxLevel).</summary>
    public class ProgressionService
    {
        private readonly EconomyConfig _config;

        public ProgressionService(EconomyConfig config) { _config = config; }

        /// <summary>XP cumulée nécessaire pour atteindre <paramref name="level"/> (niveau 1 = 0 XP).</summary>
        public int TotalXpForLevel(int level)
        {
            if (level <= 1) return 0;
            double total = 0, step = _config.xpBase;
            for (int l = 2; l <= level; l++)
            {
                total += step;
                step *= _config.xpGrowth;
            }
            return (int)Math.Round(total);
        }

        public int LevelForXp(int xp)
        {
            int level = 1;
            while (level < _config.maxLevel && xp >= TotalXpForLevel(level + 1)) level++;
            return level;
        }

        /// <summary>Progression 0..1 dans le niveau courant.</summary>
        public float LevelProgress(int xp)
        {
            int level = LevelForXp(xp);
            if (level >= _config.maxLevel) return 1f;
            int a = TotalXpForLevel(level), b = TotalXpForLevel(level + 1);
            return b > a ? (float)(xp - a) / (b - a) : 1f;
        }
    }
}
