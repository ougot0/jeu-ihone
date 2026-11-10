using System;
using System.Collections.Generic;
using Arsenal.Core.Data;

namespace Arsenal.Core.Rules
{
    /// <summary>
    /// Règles de compatibilité des pièces. Ce sont des RÈGLES DE JEU explicites et fictives
    /// (types d'emplacement, interfaces de fixation imaginaires, familles, conflits), sans lien avec
    /// une quelconque procédure de fabrication ou de modification d'armes réelles.
    /// </summary>
    public static class CompatibilityService
    {
        /// <summary>Vérifie qu'un accessoire peut aller dans un emplacement (sans tenir compte des autres pièces).</summary>
        public static OperationResult CheckSlot(WeaponDefinition weapon, SlotDefinition slot, AttachmentDefinition attachment)
        {
            if (weapon == null || slot == null || attachment == null)
                return OperationResult.Fail(ResultCodes.NotFound, "Arme, emplacement ou accessoire introuvable.");

            if (slot.types == null || Array.IndexOf(slot.types, attachment.type) < 0)
                return OperationResult.Fail(ResultCodes.WrongType,
                    $"L'emplacement « {slot.id} » n'accepte pas : {AttachmentTypes.DisplayName(attachment.type)}.");

            if (!string.IsNullOrEmpty(slot.mount) &&
                (attachment.mounts == null || Array.IndexOf(attachment.mounts, slot.mount) < 0))
                return OperationResult.Fail(ResultCodes.WrongMount,
                    $"{attachment.displayName} n'utilise pas la fixation « {slot.mount} ».");

            if (attachment.families != null && attachment.families.Length > 0 &&
                Array.IndexOf(attachment.families, weapon.family) < 0)
                return OperationResult.Fail(ResultCodes.WrongFamily,
                    $"{attachment.displayName} n'est pas prévu pour : {WeaponFamilies.DisplayName(weapon.family)}.");

            return OperationResult.Ok();
        }

        /// <summary>
        /// Vérifie l'installation complète : emplacement + conflits avec les accessoires déjà installés
        /// dans les AUTRES emplacements de la configuration.
        /// </summary>
        public static OperationResult CheckInstall(GameDatabase db, WeaponDefinition weapon, IWeaponBuildView build,
            string slotId, AttachmentDefinition attachment)
        {
            var slot = weapon?.FindSlot(slotId);
            if (slot == null) return OperationResult.Fail(ResultCodes.NotFound, $"Emplacement inconnu : {slotId}.");

            var r = CheckSlot(weapon, slot, attachment);
            if (!r.Success) return r;

            foreach (var installed in build.InstalledAttachmentIds(exceptSlot: slotId))
            {
                var other = db.GetAttachment(installed);
                if (other == null) continue;
                if (Conflicts(attachment, other) || Conflicts(other, attachment))
                    return OperationResult.Fail(ResultCodes.Conflict,
                        $"{attachment.displayName} est incompatible avec {other.displayName}.");
            }
            return OperationResult.Ok();
        }

        private static bool Conflicts(AttachmentDefinition a, AttachmentDefinition b)
        {
            if (a.conflicts == null) return false;
            return Array.IndexOf(a.conflicts, b.id) >= 0 || Array.IndexOf(a.conflicts, b.type) >= 0;
        }

        /// <summary>Liste tous les accessoires de la base compatibles avec un emplacement (affichage de l'atelier).</summary>
        public static List<AttachmentDefinition> CompatibleWith(GameDatabase db, WeaponDefinition weapon, string slotId)
        {
            var list = new List<AttachmentDefinition>();
            var slot = weapon?.FindSlot(slotId);
            if (slot == null) return list;
            foreach (var a in db.Attachments)
                if (CheckSlot(weapon, slot, a).Success) list.Add(a);
            return list;
        }
    }

    /// <summary>Vue minimale d'une configuration pour les règles (évite une dépendance circulaire avec Profile).</summary>
    public interface IWeaponBuildView
    {
        IEnumerable<string> InstalledAttachmentIds(string exceptSlot);
    }
}
