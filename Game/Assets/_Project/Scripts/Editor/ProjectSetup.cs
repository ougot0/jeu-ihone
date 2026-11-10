using System.IO;
using Arsenal.Game.ArsenalScene;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Arsenal.EditorTools
{
    /// <summary>
    /// Configuration automatique du projet (idempotente) : pipeline URP, matériaux modèles
    /// (inclusion des shaders dans le build iOS), réglages iOS, scène principale et Build Settings.
    /// Lancée automatiquement à la première ouverture, et par le script de build CI.
    /// Menu : Arsenal > Configurer le projet.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string SettingsFolder = "Assets/_Project/Settings";
        public const string MaterialsFolder = "Assets/_Project/Resources/Materials";
        public const string ScenePath = "Assets/_Project/Scenes/Arsenal.unity";
        private const string UrpPackagePath = "Packages/com.unity.render-pipelines.universal";

        static ProjectSetup()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += () =>
            {
                if (!IsConfigured())
                {
                    Debug.Log("[Arsenal] Première ouverture : configuration automatique du projet...");
                    Run(openScene: true);
                }
            };
        }

        public static bool IsConfigured() =>
            GraphicsSettings.defaultRenderPipeline != null &&
            File.Exists(MaterialsFolder + "/ArsenalLit.mat") &&
            File.Exists(ScenePath);

        [MenuItem("Arsenal/Configurer le projet (URP, iOS, scènes)", priority = 0)]
        public static void RunFromMenu() => Run(openScene: true);

        public static void Run(bool openScene)
        {
            EnsureFolder(SettingsFolder);
            EnsureFolder(MaterialsFolder);
            EnsureFolder("Assets/_Project/Scenes");
            SetupUrp();
            SetupMaterials();
            SetupPlayerSettings();
            SetupScene(openScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Arsenal] Configuration terminée. Appuyez sur Play dans la scène Arsenal.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void SetupUrp()
        {
            var assetPath = SettingsFolder + "/Arsenal_URP.asset";
            var rendererPath = SettingsFolder + "/Arsenal_URP_Renderer.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                var post = AssetDatabase.LoadAssetAtPath<PostProcessData>(UrpPackagePath + "/Runtime/Data/PostProcessData.asset");
                if (post != null) renderer.postProcessData = post;
                else Debug.LogWarning("[Arsenal] PostProcessData introuvable : post-traitement désactivé.");
                AssetDatabase.CreateAsset(renderer, rendererPath);

                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.supportsHDR = true;
                pipeline.msaaSampleCount = 1;
                pipeline.shadowDistance = 10f;
                pipeline.renderScale = 1f;
                AssetDatabase.CreateAsset(pipeline, assetPath);
            }
            if (GraphicsSettings.defaultRenderPipeline != pipeline)
                GraphicsSettings.defaultRenderPipeline = pipeline;

            // Chaque niveau de qualité utilise le pipeline par défaut.
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                if (QualitySettings.renderPipeline != null && QualitySettings.renderPipeline != pipeline)
                    QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        private static void SetupMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("[Arsenal] Shader URP/Lit introuvable : le package URP est-il installé ?");
                return;
            }
            CreateMaterial("ArsenalLit", shader, false);
            CreateMaterial("ArsenalLitEmissive", shader, true);
        }

        private static void CreateMaterial(string name, Shader shader, bool emissive)
        {
            var path = MaterialsFolder + "/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var m = new Material(shader) { name = name };
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.white);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(m, path);
        }

        private static void SetupPlayerSettings()
        {
            PlayerSettings.companyName = "ArsenalStudio";
            PlayerSettings.productName = "Arsenal";
            var ios = NamedBuildTarget.iOS;
            var id = PlayerSettings.GetApplicationIdentifier(ios);
            if (string.IsNullOrEmpty(id) || id.Contains("DefaultCompany"))
                PlayerSettings.SetApplicationIdentifier(ios, "com.example.arsenal"); // à remplacer par votre identifiant (voir docs/06)
            PlayerSettings.SetScriptingBackend(ios, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.cameraUsageDescription =
                "La caméra est utilisée uniquement pour le mode Réalité mixte : afficher des cibles virtuelles dans votre pièce.";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        private static void SetupScene(bool openScene)
        {
            if (!File.Exists(ScenePath))
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("[ArsenalScene]").AddComponent<ArsenalSceneController>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            if (openScene && !Application.isBatchMode && SceneManager.GetActiveScene().path != ScenePath)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(ScenePath);
            }
        }
    }
}
