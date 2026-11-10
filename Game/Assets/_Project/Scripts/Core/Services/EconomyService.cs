using System;
using Arsenal.Core.Data;
using Arsenal.Core.Profile;

namespace Arsenal.Core.Services
{
    /// <summary>
    /// Monnaie virtuelle (crédits), achats et récompenses. Aucune transaction réelle : les crédits
    /// se gagnent uniquement en jouant. Tous les prix et paliers viennent des fichiers de données.
    /// </summary>
    public class EconomyService
    {
        private readonly GameDatabase _db;
        private readonly PlayerProfile _profile;
        private readonly ArsenalService _arsenal;
        private readonly ProgressionService _progression;

        public event Action Changed;
        /// <summary>Déclenché avec le nouveau niveau lors d'une montée de niveau.</summary>
        public event Action<int> LevelUp;

        public EconomyService(GameDatabase db, PlayerProfile profile, ArsenalService arsenal, ProgressionService progression)
        {
            _db = db; _profile = profile; _arsenal = arsenal; _progression = progression;
        }

        public int Money => _profile.money;
        public int Level => _progression.LevelForXp(_profile.xp);
        public ProgressionService Progression => _progression;

        /// <summary>Crée un profil neuf selon economy.json.</summary>
        public static PlayerProfile CreateNewProfile(GameDatabase db)
        {
            var eco = db.Economy;
            var p = new PlayerProfile { money = eco.startingMoney };
            foreach (var w in eco.startingWeapons) GrantWeaponContent(db, p, w);
            foreach (var a in eco.startingAttachments) if (!p.OwnsAttachment(a)) p.ownedAttachments.Add(a);
            foreach (var f in eco.startingFinishes) if (!p.OwnsFinish(f)) p.ownedFinishes.Add(f);
            if (!string.IsNullOrEmpty(eco.defaultFinish) && !p.OwnsFinish(eco.defaultFinish)) p.ownedFinishes.Add(eco.defaultFinish);
            return p;
        }

        /// <summary>Ajoute l'arme et ses accessoires par défaut au profil.</summary>
        private static void GrantWeaponContent(GameDatabase db, PlayerProfile p, string weaponId)
        {
            var w = db.GetWeapon(weaponId);
            if (w == null) return;
            if (!p.OwnsWeapon(weaponId)) p.ownedWeapons.Add(weaponId);
            foreach (var s in w.slots)
                if (!string.IsNullOrEmpty(s.defaultAttachment) && !p.OwnsAttachment(s.defaultAttachment))
                    p.ownedAttachments.Add(s.defaultAttachment);
        }

        public OperationResult CanBuyWeapon(string weaponId)
        {
            var w = _db.GetWeapon(weaponId);
            if (w == null) return OperationResult.Fail(ResultCodes.NotFound, "Arme inconnue.");
            if (_profile.OwnsWeapon(weaponId)) return OperationResult.Fail(ResultCodes.AlreadyOwned, "Déjà dans votre arsenal.");
            return CheckPriceAndLevel(w.price, w.unlockLevel);
        }

        public OperationResult BuyWeapon(string weaponId)
        {
            var r = CanBuyWeapon(weaponId);
            if (!r.Success) return r;
            var w = _db.GetWeapon(weaponId);
            _profile.money -= w.price;
            GrantWeaponContent(_db, _profile, weaponId);
            _arsenal.EnsureDefaultBuilds();
            Changed?.Invoke();
            return OperationResult.Ok($"{w.displayName} ajoutée à votre arsenal.");
        }

        public OperationResult CanBuyAttachment(string attachmentId)
        {
            var a = _db.GetAttachment(attachmentId);
            if (a == null) return OperationResult.Fail(ResultCodes.NotFound, "Accessoire inconnu.");
            if (_profile.OwnsAttachment(attachmentId)) return OperationResult.Fail(ResultCodes.AlreadyOwned, "Déjà possédé.");
            return CheckPriceAndLevel(a.price, a.unlockLevel);
        }

        public OperationResult BuyAttachment(string attachmentId)
        {
            var r = CanBuyAttachment(attachmentId);
            if (!r.Success) return r;
            var a = _db.GetAttachment(attachmentId);
            _profile.money -= a.price;
            _profile.ownedAttachments.Add(attachmentId);
            Changed?.Invoke();
            return OperationResult.Ok($"{a.displayName} acheté.");
        }

        public OperationResult CanBuyFinish(string finishId)
        {
            var f = _db.GetFinish(finishId);
            if (f == null) return OperationResult.Fail(ResultCodes.NotFound, "Revêtement inconnu.");
            if (_profile.OwnsFinish(finishId)) return OperationResult.Fail(ResultCodes.AlreadyOwned, "Déjà possédé.");
            return CheckPriceAndLevel(f.price, f.unlockLevel);
        }

        public OperationResult BuyFinish(string finishId)
        {
            var r = CanBuyFinish(finishId);
            if (!r.Success) return r;
            var f = _db.GetFinish(finishId);
            _profile.money -= f.price;
            _profile.ownedFinishes.Add(finishId);
            Changed?.Invoke();
            return OperationResult.Ok($"{f.displayName} acheté.");
        }

        private OperationResult CheckPriceAndLevel(int price, int unlockLevel)
        {
            if (Level < unlockLevel) return OperationResult.Fail(ResultCodes.LevelTooLow, $"Niveau {unlockLevel} requis.");
            if (_profile.money < price) return OperationResult.Fail(ResultCodes.InsufficientFunds, $"Crédits insuffisants ({price - _profile.money} manquants).");
            return OperationResult.Ok();
        }

        /// <summary>Accorde une récompense (missions, défis, stand de tir). Retourne le nombre de niveaux gagnés.</summary>
        public int GrantReward(int money, int xp)
        {
            if (money < 0 || xp < 0) throw new ArgumentOutOfRangeException(nameof(money), "Récompense négative.");
            int before = Level;
            _profile.money += money;
            _profile.xp += xp;
            _profile.statistics.totalMoneyEarned += money;
            int after = Level;
            Changed?.Invoke();
            for (int l = before + 1; l <= after; l++) LevelUp?.Invoke(l);
            return after - before;
        }
    }
}
