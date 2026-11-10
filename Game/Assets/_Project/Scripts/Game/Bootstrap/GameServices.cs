using System;
using Arsenal.Core.Data;
using Arsenal.Core.Profile;
using Arsenal.Core.Save;
using Arsenal.Core.Services;
using UnityEngine;

namespace Arsenal.Game.Bootstrap
{
    /// <summary>
    /// Point d'accès unique aux services du jeu (persistant entre les scènes).
    /// Créé automatiquement au premier accès ; gère la sauvegarde automatique.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameServices : MonoBehaviour
    {
        private static GameServices _instance;

        public static GameServices Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[GameServices]");
                    _instance = go.AddComponent<GameServices>();
                    DontDestroyOnLoad(go);
                    _instance.Initialize();
                }
                return _instance;
            }
        }

        public GameDatabase Database { get; private set; }
        public PlayerProfile Profile { get; private set; }
        public ArsenalService Arsenal { get; private set; }
        public EconomyService Economy { get; private set; }
        public SaveService Save { get; private set; }
        public LoadStatus LastLoadStatus { get; private set; }

        /// <summary>Déclenché après tout changement d'état (arsenal, économie, réglages).</summary>
        public event Action StateChanged;
        /// <summary>Montée de niveau (relais stable, même après une réinitialisation de progression).</summary>
        public event Action<int> LevelUp;

        private const float AutosaveDelay = 0.75f;
        private float _saveAt = -1f;

        private void Initialize()
        {
            Application.targetFrameRate = 60;
            Database = DatabaseLoader.Load();
            Save = new SaveService(new FileSaveStorage(Application.persistentDataPath + "/saves"), new UnityJsonSerializer());
            Profile = Save.Load(() => EconomyService.CreateNewProfile(Database), out var status);
            LastLoadStatus = status;
            Debug.Log($"[Sauvegarde] {status} — {Application.persistentDataPath}/saves");
            BuildServices();
            if (status != LoadStatus.Loaded) SaveNow();
            ApplySettings();
        }

        private void BuildServices()
        {
            Arsenal = new ArsenalService(Database, Profile);
            Economy = new EconomyService(Database, Profile, Arsenal, new ProgressionService(Database.Economy));
            Arsenal.Changed += OnStateChanged;
            Economy.Changed += OnStateChanged;
            Economy.LevelUp += level => LevelUp?.Invoke(level);
            Arsenal.EnsureDefaultBuilds();
        }

        private void OnStateChanged()
        {
            _saveAt = Time.unscaledTime + AutosaveDelay;
            StateChanged?.Invoke();
        }

        /// <summary>À appeler après modification de <see cref="PlayerProfile.settings"/>.</summary>
        public void NotifySettingsChanged()
        {
            ApplySettings();
            OnStateChanged();
        }

        public void ApplySettings()
        {
            var s = Profile.settings;
            int levels = QualitySettings.names.Length;
            if (levels > 0)
            {
                // 0 = Économie (niveau le plus bas), 2 = Qualité (niveau le plus haut).
                int target = Mathf.Clamp(Mathf.RoundToInt(s.graphicsQuality / 2f * (levels - 1)), 0, levels - 1);
                if (QualitySettings.GetQualityLevel() != target) QualitySettings.SetQualityLevel(target, true);
            }
            AudioListener.volume = Mathf.Clamp01(s.masterVolume);
        }

        private void Update()
        {
            if (_saveAt > 0f && Time.unscaledTime >= _saveAt) SaveNow();
        }

        public void SaveNow()
        {
            _saveAt = -1f;
            try { Save.Save(Profile); }
            catch (Exception e) { Debug.LogError("[Sauvegarde] Echec : " + e); }
        }

        /// <summary>Efface la progression et repart d'un profil neuf.</summary>
        public void ResetProgress()
        {
            Arsenal.Changed -= OnStateChanged;
            Economy.Changed -= OnStateChanged;
            Save.DeleteAll();
            Profile = EconomyService.CreateNewProfile(Database);
            BuildServices();
            SaveNow();
            ApplySettings();
            StateChanged?.Invoke();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _saveAt > 0f) SaveNow();
        }

        private void OnApplicationQuit()
        {
            if (_saveAt > 0f) SaveNow();
        }
    }
}
