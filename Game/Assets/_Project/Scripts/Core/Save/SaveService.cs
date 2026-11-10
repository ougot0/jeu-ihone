using System;
using System.IO;
using System.Text;
using Arsenal.Core.Profile;

namespace Arsenal.Core.Save
{
    public interface IJsonSerializer
    {
        string ToJson(object value);
        T FromJson<T>(string json);
    }

    /// <summary>Stockage brut d'un texte sous une clé (fichier, mémoire pour les tests...).</summary>
    public interface ISaveStorage
    {
        bool TryRead(string key, out string content);
        /// <summary>Lit la copie de secours (version précédente) si elle existe.</summary>
        bool TryReadBackup(string key, out string content);
        void Write(string key, string content);
        bool Exists(string key);
        void Delete(string key);
    }

    /// <summary>
    /// Stockage fichier fiable : écriture dans un fichier temporaire puis remplacement, avec copie de secours (.bak).
    /// Une coupure pendant l'écriture ne peut donc pas détruire la dernière sauvegarde valide.
    /// </summary>
    public class FileSaveStorage : ISaveStorage
    {
        private readonly string _directory;

        public FileSaveStorage(string directory)
        {
            _directory = directory;
            Directory.CreateDirectory(directory);
        }

        public string PathFor(string key) => Path.Combine(_directory, key + ".json");

        public bool Exists(string key) => File.Exists(PathFor(key));

        public bool TryRead(string key, out string content)
        {
            content = null;
            var path = PathFor(key);
            if (!File.Exists(path)) return false;
            try { content = File.ReadAllText(path, Encoding.UTF8); return true; }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        public bool TryReadBackup(string key, out string content)
        {
            content = null;
            var path = PathFor(key) + ".bak";
            if (!File.Exists(path)) return false;
            try { content = File.ReadAllText(path, Encoding.UTF8); return true; }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        public void Write(string key, string content)
        {
            var path = PathFor(key);
            var tmp = path + ".tmp";
            var bak = path + ".bak";
            File.WriteAllText(tmp, content, new UTF8Encoding(false));
            if (File.Exists(path))
            {
                try { File.Replace(tmp, path, bak); return; }
                catch (PlatformNotSupportedException) { }
                catch (IOException) { }
                File.Copy(path, bak, true);
                File.Copy(tmp, path, true);
                File.Delete(tmp);
            }
            else File.Move(tmp, path);
        }

        public void Delete(string key)
        {
            var path = PathFor(key);
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
        }
    }

    /// <summary>Stockage en mémoire (tests, mode invité).</summary>
    public class MemorySaveStorage : ISaveStorage
    {
        public readonly System.Collections.Generic.Dictionary<string, string> Data = new System.Collections.Generic.Dictionary<string, string>();
        public bool TryRead(string key, out string content) => Data.TryGetValue(key, out content);
        public bool TryReadBackup(string key, out string content) => Data.TryGetValue(key + ".bak", out content);
        public void Write(string key, string content)
        {
            if (Data.TryGetValue(key, out var previous)) Data[key + ".bak"] = previous;
            Data[key] = content;
        }
        public bool Exists(string key) => Data.ContainsKey(key);
        public void Delete(string key) { Data.Remove(key); Data.Remove(key + ".bak"); }
    }

    [Serializable]
    public class SaveEnvelope
    {
        public int version;
        public long savedUtcTicks;
        public string checksum;
        public string payload;
    }

    public enum LoadStatus { NewProfile, Loaded, RecoveredFromBackup, CorruptedReset }

    /// <summary>
    /// Sauvegarde locale du profil : enveloppe versionnée + somme de contrôle (détection de corruption),
    /// repli automatique sur la copie de secours, migrations de version.
    /// </summary>
    public class SaveService
    {
        public const string ProfileKey = "profile";

        private readonly ISaveStorage _storage;
        private readonly IJsonSerializer _json;

        public SaveService(ISaveStorage storage, IJsonSerializer json)
        {
            _storage = storage; _json = json;
        }

        public void Save(PlayerProfile profile)
        {
            profile.version = PlayerProfile.CurrentVersion;
            var payload = _json.ToJson(profile);
            var env = new SaveEnvelope
            {
                version = PlayerProfile.CurrentVersion,
                savedUtcTicks = DateTime.UtcNow.Ticks,
                checksum = Checksum(payload),
                payload = payload
            };
            _storage.Write(ProfileKey, _json.ToJson(env));
        }

        /// <summary>Charge le profil. <paramref name="createNew"/> fournit un profil neuf si rien n'est récupérable.</summary>
        public PlayerProfile Load(Func<PlayerProfile> createNew, out LoadStatus status)
        {
            string bak;
            if (!_storage.Exists(ProfileKey) && !_storage.TryReadBackup(ProfileKey, out bak))
            {
                status = LoadStatus.NewProfile;
                return createNew();
            }

            if (_storage.TryRead(ProfileKey, out var main) && TryDecode(main, out var profile))
            {
                status = LoadStatus.Loaded;
                return profile;
            }

            if (_storage.TryReadBackup(ProfileKey, out bak) && TryDecode(bak, out profile))
            {
                status = LoadStatus.RecoveredFromBackup;
                return profile;
            }

            status = LoadStatus.CorruptedReset;
            return createNew();
        }

        private bool TryDecode(string raw, out PlayerProfile profile)
        {
            profile = null;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            try
            {
                var env = _json.FromJson<SaveEnvelope>(raw);
                if (env == null || string.IsNullOrEmpty(env.payload)) return false;
                if (env.checksum != Checksum(env.payload)) return false;
                profile = _json.FromJson<PlayerProfile>(env.payload);
                if (profile == null) return false;
                Migrate(profile, env.version);
                profile.EnsureIntegrity();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Point d'entrée des migrations futures (ajouter un cas par nouvelle version).</summary>
        private static void Migrate(PlayerProfile profile, int fromVersion)
        {
            // Version 1 : format initial, aucune migration.
            profile.version = PlayerProfile.CurrentVersion;
        }

        public void DeleteAll() => _storage.Delete(ProfileKey);

        /// <summary>FNV-1a 64 bits : détecte une corruption accidentelle (pas une protection anti-triche).</summary>
        public static string Checksum(string text)
        {
            unchecked
            {
                ulong hash = 14695981039346656037UL;
                foreach (char c in text)
                {
                    hash ^= c;
                    hash *= 1099511628211UL;
                }
                return hash.ToString("x16");
            }
        }
    }
}
