using System;
using System.IO;
using System.Text.Json;
using Arsenal.Core.Data;
using Arsenal.Core.Profile;
using Arsenal.Core.Save;
using Arsenal.Core.Services;

namespace Arsenal.Core.Tests
{
    /// <summary>Sérialiseur de test qui imite JsonUtility (champs publics, noms identiques).</summary>
    public class TestJson : IJsonSerializer
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true };
        public string ToJson(object value) => JsonSerializer.Serialize(value, value.GetType(), Options);
        public T FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
    }

    public static class TestData
    {
        private static GameDatabase _db;

        /// <summary>Charge les VRAIS fichiers de données du jeu.</summary>
        public static GameDatabase Database
        {
            get
            {
                if (_db != null) return _db;
                var json = new TestJson();
                string dir = Path.Combine(AppContext.BaseDirectory, "Data");
                T Read<T>(string f) => json.FromJson<T>(File.ReadAllText(Path.Combine(dir, f)));
                _db = new GameDatabase(Read<WeaponCatalog>("weapons.json"), Read<AttachmentCatalog>("attachments.json"),
                    Read<FinishCatalog>("finishes.json"), Read<EconomyConfig>("economy.json"));
                return _db;
            }
        }

        public static (PlayerProfile profile, ArsenalService arsenal, EconomyService economy) NewGame()
        {
            var db = Database;
            var profile = EconomyService.CreateNewProfile(db);
            int n = 0;
            var arsenal = new ArsenalService(db, profile, () => "b" + (n++));
            arsenal.EnsureDefaultBuilds();
            var economy = new EconomyService(db, profile, arsenal, new ProgressionService(db.Economy));
            return (profile, arsenal, economy);
        }
    }
}
