using System;
using System.Collections.Generic;
using Arsenal.Core.Data;
using Arsenal.Core.Profile;
using Arsenal.Core.Rules;

namespace Arsenal.Core.Services
{
    /// <summary>
    /// Gestion de l'arsenal du joueur : configurations (variantes) d'armes, installation d'accessoires,
    /// revêtements, sélection. Toute modification déclenche <see cref="Changed"/> (sauvegarde automatique).
    /// </summary>
    public class ArsenalService
    {
        public const int MaxNameLength = 24;

        private readonly GameDatabase _db;
        private readonly PlayerProfile _profile;
        private readonly Func<string> _newId;
        private readonly Func<DateTime> _now;

        public event Action Changed;

        public ArsenalService(GameDatabase db, PlayerProfile profile, Func<string> idGenerator = null, Func<DateTime> clock = null)
        {
            _db = db;
            _profile = profile;
            _newId = idGenerator ?? (() => Guid.NewGuid().ToString("N"));
            _now = clock ?? (() => DateTime.UtcNow);
        }

        public PlayerProfile Profile => _profile;
        public GameDatabase Database => _db;

        public WeaponBuild SelectedBuild => _profile.FindBuild(_profile.selectedBuildId);

        public List<WeaponBuild> GetBuilds(string weaponId)
        {
            var list = new List<WeaponBuild>();
            foreach (var b in _profile.builds) if (b.weaponId == weaponId) list.Add(b);
            return list;
        }

        /// <summary>Crée une configuration "Standard" pour chaque arme possédée qui n'en a pas, et répare la sélection.</summary>
        public void EnsureDefaultBuilds()
        {
            bool changed = false;
            foreach (var weaponId in _profile.ownedWeapons)
            {
                if (_db.GetWeapon(weaponId) == null) continue;
                if (GetBuilds(weaponId).Count == 0)
                {
                    _profile.builds.Add(NewBuild(weaponId, "Standard"));
                    changed = true;
                }
            }
            // Supprime les configurations orphelines (arme retirée des données).
            changed |= _profile.builds.RemoveAll(b => _db.GetWeapon(b.weaponId) == null || !_profile.OwnsWeapon(b.weaponId)) > 0;

            if (SelectedBuild == null && _profile.builds.Count > 0)
            {
                _profile.selectedBuildId = _profile.builds[0].id;
                changed = true;
            }
            if (changed) RaiseChanged();
        }

        private WeaponBuild NewBuild(string weaponId, string name)
        {
            var weapon = _db.GetWeapon(weaponId);
            var now = _now().Ticks;
            var build = new WeaponBuild
            {
                id = _newId(),
                weaponId = weaponId,
                name = name,
                finishId = _db.Economy.defaultFinish,
                createdUtcTicks = now,
                modifiedUtcTicks = now
            };
            foreach (var slot in weapon.slots)
                if (!string.IsNullOrEmpty(slot.defaultAttachment))
                    build.SetAttachment(slot.id, slot.defaultAttachment);
            return build;
        }

        public OperationResult CreateBuild(string weaponId, string name, out WeaponBuild build)
        {
            build = null;
            if (_db.GetWeapon(weaponId) == null) return OperationResult.Fail(ResultCodes.NotFound, "Arme inconnue.");
            if (!_profile.OwnsWeapon(weaponId)) return OperationResult.Fail(ResultCodes.NotOwned, "Vous ne possédez pas cette arme.");
            if (GetBuilds(weaponId).Count >= _db.Economy.maxBuildsPerWeapon)
                return OperationResult.Fail(ResultCodes.LimitReached, "Nombre maximal de configurations atteint pour cette arme.");
            var n = SanitizeName(name);
            if (n == null) return OperationResult.Fail(ResultCodes.InvalidName, "Nom invalide.");
            build = NewBuild(weaponId, n);
            _profile.builds.Add(build);
            RaiseChanged();
            return OperationResult.Ok("Configuration créée.");
        }

        public OperationResult DuplicateBuild(string buildId, string newName, out WeaponBuild copy)
        {
            copy = null;
            var src = _profile.FindBuild(buildId);
            if (src == null) return OperationResult.Fail(ResultCodes.NotFound, "Configuration introuvable.");
            if (GetBuilds(src.weaponId).Count >= _db.Economy.maxBuildsPerWeapon)
                return OperationResult.Fail(ResultCodes.LimitReached, "Nombre maximal de configurations atteint pour cette arme.");
            var n = SanitizeName(newName);
            if (n == null) return OperationResult.Fail(ResultCodes.InvalidName, "Nom invalide.");
            copy = src.DeepClone();
            copy.id = _newId();
            copy.name = n;
            copy.createdUtcTicks = copy.modifiedUtcTicks = _now().Ticks;
            _profile.builds.Add(copy);
            RaiseChanged();
            return OperationResult.Ok("Configuration dupliquée.");
        }

        public OperationResult RenameBuild(string buildId, string newName)
        {
            var b = _profile.FindBuild(buildId);
            if (b == null) return OperationResult.Fail(ResultCodes.NotFound, "Configuration introuvable.");
            var n = SanitizeName(newName);
            if (n == null) return OperationResult.Fail(ResultCodes.InvalidName, "Nom invalide.");
            b.name = n;
            Touch(b);
            return OperationResult.Ok();
        }

        public OperationResult DeleteBuild(string buildId)
        {
            var b = _profile.FindBuild(buildId);
            if (b == null) return OperationResult.Fail(ResultCodes.NotFound, "Configuration introuvable.");
            if (GetBuilds(b.weaponId).Count <= 1)
                return OperationResult.Fail(ResultCodes.LastBuild, "Chaque arme doit conserver au moins une configuration.");
            _profile.builds.Remove(b);
            if (_profile.selectedBuildId == buildId) _profile.selectedBuildId = GetBuilds(b.weaponId)[0].id;
            RaiseChanged();
            return OperationResult.Ok();
        }

        public OperationResult SelectBuild(string buildId)
        {
            if (_profile.FindBuild(buildId) == null) return OperationResult.Fail(ResultCodes.NotFound, "Configuration introuvable.");
            if (_profile.selectedBuildId == buildId) return OperationResult.Ok();
            _profile.selectedBuildId = buildId;
            RaiseChanged();
            return OperationResult.Ok();
        }

        /// <summary>Vérifie sans modifier : possession, niveau de déblocage et règles de compatibilité.</summary>
        public OperationResult CanInstall(string buildId, string slotId, string attachmentId)
        {
            var b = _profile.FindBuild(buildId);
            if (b == null) return OperationResult.Fail(ResultCodes.NotFound, "Configuration introuvable.");
            var weapon = _db.GetWeapon(b.weaponId);
            var att = _db.GetAttachment(attachmentId);
            if (att == null) return OperationResult.Fail(ResultCodes.NotFound, "Accessoire inconnu.");
            if (!_profile.OwnsAttachment(attachmentId)) return OperationResult.Fail(ResultCodes.NotOwned, "Accessoire non possédé.");
            return CompatibilityService.CheckInstall(_db, weapon, b, slotId, att);
        }

        public OperationResult Install(string buildId, string slotId, string attachmentId)
        {
            var r = CanInstall(buildId, slotId, attachmentId);
            if (!r.Success) return r;
            var b = _profile.FindBuild(buildId);
            b.SetAttachment(slotId, attachmentId);
            Touch(b);
            return OperationResult.Ok();
        }

        /// <summary>Retire l'accessoire d'un emplacement. Un emplacement obligatoire revient à sa pièce par défaut.</summary>
        public OperationResult Uninstall(string buildId, string slotId)
        {
            var b = _profile.FindBuild(buildId);
            if (b == null) return OperationResult.Fail(ResultCodes.NotFound, "Configuration introuvable.");
            var slot = _db.GetWeapon(b.weaponId)?.FindSlot(slotId);
            if (slot == null) return OperationResult.Fail(ResultCodes.NotFound, "Emplacement inconnu.");
            if (slot.required)
            {
                if (b.GetAttachment(slotId) == slot.defaultAttachment)
                    return OperationResult.Fail(ResultCodes.SlotRequired, "Cet emplacement ne peut pas rester vide.");
                b.SetAttachment(slotId, slot.defaultAttachment);
            }
            else b.SetAttachment(slotId, null);
            Touch(b);
            return OperationResult.Ok();
        }

        public OperationResult SetFinish(string buildId, string finishId)
        {
            var b = _profile.FindBuild(buildId);
            if (b == null) return OperationResult.Fail(ResultCodes.NotFound, "Configuration introuvable.");
            if (_db.GetFinish(finishId) == null) return OperationResult.Fail(ResultCodes.NotFound, "Revêtement inconnu.");
            if (!_profile.OwnsFinish(finishId)) return OperationResult.Fail(ResultCodes.NotOwned, "Revêtement non possédé.");
            b.finishId = finishId;
            Touch(b);
            return OperationResult.Ok();
        }

        public List<AttachmentDefinition> GetInstalledAttachments(WeaponBuild build)
        {
            var list = new List<AttachmentDefinition>();
            if (build == null) return list;
            foreach (var a in build.attachments)
            {
                var def = _db.GetAttachment(a.attachmentId);
                if (def != null) list.Add(def);
            }
            return list;
        }

        public StatBlock GetFinalStats(WeaponBuild build)
        {
            var weapon = _db.GetWeapon(build.weaponId);
            return StatCalculator.Compute(weapon.baseStats, GetInstalledAttachments(build));
        }

        private void Touch(WeaponBuild b)
        {
            b.modifiedUtcTicks = _now().Ticks;
            RaiseChanged();
        }

        internal void RaiseChanged() => Changed?.Invoke();

        public static string SanitizeName(string name)
        {
            if (name == null) return null;
            var n = name.Trim();
            if (n.Length == 0) return null;
            if (n.Length > MaxNameLength) n = n.Substring(0, MaxNameLength);
            foreach (var c in n) if (char.IsControl(c)) return null;
            return n;
        }
    }
}
