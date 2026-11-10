using Arsenal.Core.Data;
using UnityEngine;

namespace Arsenal.Game.Bootstrap
{
    /// <summary>
    /// Charge le contenu du jeu depuis Assets/_Project/Resources/Data/*.json.
    /// Ajouter une arme ou un accessoire = éditer ces fichiers, sans toucher au code.
    /// </summary>
    public static class DatabaseLoader
    {
        public static GameDatabase Load()
        {
            var weapons = Read<WeaponCatalog>("Data/weapons");
            var attachments = Read<AttachmentCatalog>("Data/attachments");
            var finishes = Read<FinishCatalog>("Data/finishes");
            var economy = Read<EconomyConfig>("Data/economy");
            var db = new GameDatabase(weapons, attachments, finishes, economy);

            foreach (var error in DatabaseValidator.Validate(db))
                Debug.LogError("[Données] " + error);
            return db;
        }

        private static T Read<T>(string path) where T : new()
        {
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null)
            {
                Debug.LogError($"[Données] Fichier introuvable : Resources/{path}.json");
                return new T();
            }
            return JsonUtility.FromJson<T>(asset.text);
        }
    }
}
