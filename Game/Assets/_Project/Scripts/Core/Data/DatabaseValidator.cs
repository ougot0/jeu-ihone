using System;
using System.Collections.Generic;

namespace Arsenal.Core.Data
{
    /// <summary>
    /// Vérifie la cohérence des fichiers de données (ids uniques, références valides, valeurs plausibles).
    /// Exécuté au chargement du jeu et dans les tests automatisés.
    /// </summary>
    public static class DatabaseValidator
    {
        public static List<string> Validate(GameDatabase db)
        {
            var errors = new List<string>();
            var ids = new HashSet<string>();

            foreach (var w in db.Weapons)
            {
                if (string.IsNullOrEmpty(w.id)) { errors.Add("Arme sans id."); continue; }
                if (!ids.Add(w.id)) errors.Add($"Id dupliqué : {w.id}");
                if (Array.IndexOf(WeaponFamilies.All, w.family) < 0) errors.Add($"{w.id} : famille inconnue '{w.family}'.");
                if (w.baseStats == null) { errors.Add($"{w.id} : baseStats manquant."); continue; }
                if (w.baseStats.damage <= 0) errors.Add($"{w.id} : dégâts doivent être > 0.");
                if (w.baseStats.fireRate <= 0) errors.Add($"{w.id} : cadence doit être > 0.");
                if (w.baseStats.magazineSize <= 0) errors.Add($"{w.id} : chargeur doit être > 0.");
                if (w.baseStats.reloadTime <= 0) errors.Add($"{w.id} : rechargement doit être > 0.");
                if (w.price < 0) errors.Add($"{w.id} : prix négatif.");
                if (w.pellets < 1) errors.Add($"{w.id} : pellets doit être >= 1.");
                if (w.parts == null || w.parts.Length == 0) errors.Add($"{w.id} : aucune pièce visuelle.");

                var slotIds = new HashSet<string>();
                foreach (var s in w.slots ?? Array.Empty<SlotDefinition>())
                {
                    if (!slotIds.Add(s.id)) errors.Add($"{w.id} : emplacement dupliqué '{s.id}'.");
                    foreach (var t in s.types ?? Array.Empty<string>())
                        if (Array.IndexOf(AttachmentTypes.All, t) < 0) errors.Add($"{w.id}/{s.id} : type inconnu '{t}'.");
                    if (!string.IsNullOrEmpty(s.defaultAttachment))
                    {
                        var att = db.GetAttachment(s.defaultAttachment);
                        if (att == null) errors.Add($"{w.id}/{s.id} : accessoire par défaut introuvable '{s.defaultAttachment}'.");
                        else
                        {
                            var r = Rules.CompatibilityService.CheckSlot(w, s, att);
                            if (!r.Success) errors.Add($"{w.id}/{s.id} : accessoire par défaut incompatible ({r.Message}).");
                        }
                    }
                    else if (s.required) errors.Add($"{w.id}/{s.id} : emplacement obligatoire sans accessoire par défaut.");
                }
            }

            foreach (var a in db.Attachments)
            {
                if (string.IsNullOrEmpty(a.id)) { errors.Add("Accessoire sans id."); continue; }
                if (!ids.Add(a.id)) errors.Add($"Id dupliqué : {a.id}");
                if (Array.IndexOf(AttachmentTypes.All, a.type) < 0) errors.Add($"{a.id} : type inconnu '{a.type}'.");
                foreach (var f in a.families ?? Array.Empty<string>())
                    if (Array.IndexOf(WeaponFamilies.All, f) < 0) errors.Add($"{a.id} : famille inconnue '{f}'.");
                foreach (var m in a.modifiers ?? Array.Empty<StatModifier>())
                    if (!StatIds.IsKnown(m.stat)) errors.Add($"{a.id} : statistique inconnue '{m.stat}'.");
                if (a.price < 0) errors.Add($"{a.id} : prix négatif.");
            }

            foreach (var f in db.Finishes)
            {
                if (string.IsNullOrEmpty(f.id)) { errors.Add("Revêtement sans id."); continue; }
                if (!ids.Add(f.id)) errors.Add($"Id dupliqué : {f.id}");
            }

            var eco = db.Economy;
            foreach (var id in eco.startingWeapons ?? Array.Empty<string>())
                if (db.GetWeapon(id) == null) errors.Add($"economy : arme de départ introuvable '{id}'.");
            foreach (var id in eco.startingAttachments ?? Array.Empty<string>())
                if (db.GetAttachment(id) == null) errors.Add($"economy : accessoire de départ introuvable '{id}'.");
            foreach (var id in eco.startingFinishes ?? Array.Empty<string>())
                if (db.GetFinish(id) == null) errors.Add($"economy : revêtement de départ introuvable '{id}'.");
            if (db.GetFinish(eco.defaultFinish) == null) errors.Add($"economy : revêtement par défaut introuvable '{eco.defaultFinish}'.");
            if (eco.startingWeapons == null || eco.startingWeapons.Length == 0) errors.Add("economy : aucune arme de départ.");

            return errors;
        }
    }
}
