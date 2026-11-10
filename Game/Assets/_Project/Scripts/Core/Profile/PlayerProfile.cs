using System;
using System.Collections.Generic;
using Arsenal.Core.Rules;

namespace Arsenal.Core.Profile
{
    [Serializable]
    public class InstalledAttachment
    {
        public string slotId;
        public string attachmentId;

        public InstalledAttachment() { }
        public InstalledAttachment(string slotId, string attachmentId) { this.slotId = slotId; this.attachmentId = attachmentId; }
    }

    /// <summary>Une configuration (variante) créée par le joueur à partir d'une arme de base.</summary>
    [Serializable]
    public class WeaponBuild : IWeaponBuildView
    {
        public string id;
        public string weaponId;
        public string name;
        public string finishId;
        public List<InstalledAttachment> attachments = new List<InstalledAttachment>();
        public long createdUtcTicks;
        public long modifiedUtcTicks;

        public string GetAttachment(string slotId)
        {
            foreach (var a in attachments) if (a.slotId == slotId) return a.attachmentId;
            return null;
        }

        public void SetAttachment(string slotId, string attachmentId)
        {
            attachments.RemoveAll(a => a.slotId == slotId);
            if (!string.IsNullOrEmpty(attachmentId)) attachments.Add(new InstalledAttachment(slotId, attachmentId));
        }

        public IEnumerable<string> InstalledAttachmentIds(string exceptSlot)
        {
            foreach (var a in attachments)
                if (a.slotId != exceptSlot && !string.IsNullOrEmpty(a.attachmentId)) yield return a.attachmentId;
        }

        public WeaponBuild DeepClone()
        {
            var c = (WeaponBuild)MemberwiseClone();
            c.attachments = new List<InstalledAttachment>();
            foreach (var a in attachments) c.attachments.Add(new InstalledAttachment(a.slotId, a.attachmentId));
            return c;
        }
    }

    /// <summary>Réglages joueur (persistés avec la sauvegarde).</summary>
    [Serializable]
    public class GameSettings
    {
        public bool gyroEnabled;
        public float gyroSensitivity = 1f;
        public bool gyroInvertX;
        public bool gyroInvertY;
        public float touchSensitivity = 1f;
        public bool invertLookY;
        /// <summary>0 = Économie, 1 = Équilibré, 2 = Qualité.</summary>
        public int graphicsQuality = 1;
        public float masterVolume = 0.8f;
        public bool showArSafetyNotice = true;
    }

    [Serializable]
    public class PlayerStatistics
    {
        public int shotsFired;
        public int shotsHit;
        public int missionsCompleted;
        public int totalMoneyEarned;
    }

    /// <summary>Etat persistant complet du joueur.</summary>
    [Serializable]
    public class PlayerProfile
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int money;
        public int xp;
        public List<string> ownedWeapons = new List<string>();
        public List<string> ownedAttachments = new List<string>();
        public List<string> ownedFinishes = new List<string>();
        public List<WeaponBuild> builds = new List<WeaponBuild>();
        public string selectedBuildId;
        public GameSettings settings = new GameSettings();
        public PlayerStatistics statistics = new PlayerStatistics();

        public bool OwnsWeapon(string id) => ownedWeapons.Contains(id);
        public bool OwnsAttachment(string id) => ownedAttachments.Contains(id);
        public bool OwnsFinish(string id) => ownedFinishes.Contains(id);

        public WeaponBuild FindBuild(string buildId)
        {
            foreach (var b in builds) if (b.id == buildId) return b;
            return null;
        }

        /// <summary>Répare les champs null après désérialisation (anciennes sauvegardes, fichier modifié à la main).</summary>
        public void EnsureIntegrity()
        {
            ownedWeapons = ownedWeapons ?? new List<string>();
            ownedAttachments = ownedAttachments ?? new List<string>();
            ownedFinishes = ownedFinishes ?? new List<string>();
            builds = builds ?? new List<WeaponBuild>();
            settings = settings ?? new GameSettings();
            statistics = statistics ?? new PlayerStatistics();
            builds.RemoveAll(b => b == null || string.IsNullOrEmpty(b.id));
            foreach (var b in builds) b.attachments = b.attachments ?? new List<InstalledAttachment>();
            if (money < 0) money = 0;
            if (xp < 0) xp = 0;
        }
    }
}
