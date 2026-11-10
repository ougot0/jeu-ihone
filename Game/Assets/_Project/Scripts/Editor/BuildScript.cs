using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Arsenal.EditorTools
{
    /// <summary>
    /// Points d'entrée de build (éditeur et CI).
    /// CI iOS : -executeMethod Arsenal.EditorTools.BuildScript.BuildiOS -customBuildPath build/iOS
    /// Le résultat iOS est un PROJET XCODE : sa compilation/signature se fait ensuite sur macOS (voir docs/06).
    /// </summary>
    public static class BuildScript
    {
        [MenuItem("Arsenal/Build/Projet Xcode iOS", priority = 100)]
        public static void BuildiOS() => Build(BuildTarget.iOS, BuildTargetGroup.iOS, Arg("-customBuildPath") ?? "Builds/iOS");

        [MenuItem("Arsenal/Build/Windows 64 bits (test)", priority = 101)]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone,
            Arg("-customBuildPath") ?? "Builds/Windows/Arsenal.exe");

        private static void Build(BuildTarget target, BuildTargetGroup group, string path)
        {
            ProjectSetup.Run(openScene: false);
            var version = Arg("-buildVersion");
            if (!string.IsNullOrEmpty(version) && !version.StartsWith("none")) PlayerSettings.bundleVersion = version;
            if (target == BuildTarget.iOS)
            {
                // La signature est faite par le job macOS (certificat et profil fournis via les secrets GitHub).
                PlayerSettings.iOS.appleEnableAutomaticSigning = false;
                var buildNumber = Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER");
                if (!string.IsNullOrEmpty(buildNumber)) PlayerSettings.iOS.buildNumber = buildNumber;
                // Identifiants non secrets transmis par la CI : écrits par Unity dans la cible Unity-iPhone du projet Xcode.
                var bundleId = Environment.GetEnvironmentVariable("IOS_BUNDLE_ID");
                if (!string.IsNullOrEmpty(bundleId)) PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, bundleId);
                var team = Environment.GetEnvironmentVariable("APPLE_TEAM_ID");
                if (!string.IsNullOrEmpty(team)) PlayerSettings.iOS.appleDeveloperTeamID = team;
                var profile = Environment.GetEnvironmentVariable("IOS_PROFILE_UUID");
                if (!string.IsNullOrEmpty(profile))
                {
                    PlayerSettings.iOS.iOSManualProvisioningProfileID = profile;
                    PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Distribution;
                }
            }

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = path,
                target = target,
                targetGroup = group,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[Build] {target} : {summary.result}, {summary.totalErrors} erreur(s), {summary.totalSize / (1024 * 1024)} Mo, {summary.totalTime}");
            if (Application.isBatchMode && summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        private static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
