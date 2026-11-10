using System;
using Arsenal.Core.Data;

namespace Arsenal.Core.Simulation
{
    public enum FireResult { Fired, Empty, Reloading, Cooldown, TriggerNotReset }

    /// <summary>
    /// Modèle de tir indépendant du moteur : munitions, cadence, rechargement, dispersion et recul
    /// dérivés des statistiques finales de la configuration. La couche Unity ne fait que l'afficher.
    /// Toutes les grandeurs sont fictives et calibrées pour le gameplay.
    /// </summary>
    public class WeaponSimulation
    {
        public StatBlock Stats { get; private set; }
        public string FireMode { get; }
        public int Pellets { get; }

        public int Ammo { get; private set; }
        public bool IsReloading { get; private set; }
        public float ReloadProgress => IsReloading ? Math.Min(1f, _reloadElapsed / Stats.reloadTime) : 0f;
        /// <summary>Chaleur de recul accumulée (0..1), augmente la dispersion en tir soutenu, retombe avec le temps.</summary>
        public float RecoilHeat { get; private set; }
        /// <summary>0 = hanche, 1 = visée complète.</summary>
        public float AimBlend { get; private set; }
        public bool IsAiming { get; set; }

        private float _cooldown;
        private float _reloadElapsed;
        private bool _triggerHeld;

        public WeaponSimulation(StatBlock finalStats, string fireMode = "semi", int pellets = 1)
        {
            Stats = finalStats.Clone();
            FireMode = string.IsNullOrEmpty(fireMode) ? "semi" : fireMode;
            Pellets = Math.Max(1, pellets);
            Ammo = Stats.magazineSize;
        }

        /// <summary>Intervalle minimal entre deux tirs (s).</summary>
        public float ShotInterval => 60f / Stats.fireRate;

        /// <summary>Temps pour passer en visée (s) : 0,12 s (maniabilité 100) à 0,45 s (maniabilité 0).</summary>
        public float AimTime => 0.45f - 0.33f * (Stats.handling / 100f);

        /// <summary>Dispersion (demi-angle en degrés) au moment du tir.</summary>
        public float CurrentSpreadDegrees
        {
            get
            {
                float baseSpread = 0.15f + (100f - Stats.accuracy) * 0.06f;      // 0,15° à 6,15°
                float hipPenalty = 1f + 1.5f * (1f - AimBlend);                   // la visée réduit la dispersion
                float heat = 1f + RecoilHeat * (0.5f + Stats.recoil / 50f);        // tir soutenu
                return baseSpread * hipPenalty * heat;
            }
        }

        /// <summary>Recul vertical appliqué à la caméra par tir (degrés).</summary>
        public float RecoilKickDegrees => (0.3f + Stats.recoil * 0.045f) * (1f - 0.35f * AimBlend);

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            if (_cooldown > 0f) _cooldown = Math.Max(0f, _cooldown - dt);

            float aimSpeed = 1f / Math.Max(0.05f, AimTime);
            AimBlend = IsAiming ? Math.Min(1f, AimBlend + dt * aimSpeed) : Math.Max(0f, AimBlend - dt * aimSpeed);

            float recovery = 0.8f + Stats.handling / 50f;   // récupération du recul par seconde
            RecoilHeat = Math.Max(0f, RecoilHeat - dt * recovery);

            if (IsReloading)
            {
                _reloadElapsed += dt;
                if (_reloadElapsed >= Stats.reloadTime)
                {
                    IsReloading = false;
                    Ammo = Stats.magazineSize;
                }
            }
        }

        /// <summary>Appelé à chaque frame où la détente est enfoncée.</summary>
        public FireResult TriggerHeld()
        {
            if (IsReloading) return FireResult.Reloading;
            bool automatic = FireMode == "auto";
            if (_triggerHeld && !automatic) return FireResult.TriggerNotReset;
            if (_cooldown > 0f) return FireResult.Cooldown;
            _triggerHeld = true;
            if (Ammo <= 0) return FireResult.Empty;

            Ammo--;
            _cooldown = ShotInterval;
            RecoilHeat = Math.Min(1f, RecoilHeat + 0.08f + Stats.recoil / 600f);
            return FireResult.Fired;
        }

        public void TriggerReleased() => _triggerHeld = false;

        public bool StartReload()
        {
            if (IsReloading || Ammo >= Stats.magazineSize) return false;
            IsReloading = true;
            _reloadElapsed = 0f;
            return true;
        }

        public void CancelReload()
        {
            IsReloading = false;
            _reloadElapsed = 0f;
        }

        /// <summary>Change les statistiques (nouvel accessoire) en conservant les munitions dans la limite du chargeur.</summary>
        public void UpdateStats(StatBlock finalStats)
        {
            Stats = finalStats.Clone();
            Ammo = Math.Min(Ammo, Stats.magazineSize);
        }

        /// <summary>Dégâts infligés par un projectile selon la distance (portée efficace fictive) et la zone touchée.</summary>
        public float DamageAt(float distanceMeters, float zoneMultiplier = 1f)
        {
            float effective = 15f + Stats.accuracy * 0.6f;   // 15 m à 75 m
            float falloff = distanceMeters <= effective ? 1f : Math.Max(0.4f, 1f - (distanceMeters - effective) / (effective * 2f));
            return Stats.damage * falloff * zoneMultiplier;
        }
    }
}
