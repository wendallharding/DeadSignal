using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalWindowsBuild
    {
        public const string OUTPUT_DIRECTORY = "Build/Windows";
        public const string EXECUTABLE_PATH = OUTPUT_DIRECTORY + "/DeadSignal.exe";

        private const string APPLICATION_ICON_PATH = "Assets/DeadSignal/Branding/DeadSignalAppIcon.png";
        private const string BUILD_MENU = "DEAD SIGNAL/Build Windows Development";

        [MenuItem(BUILD_MENU)]
        public static void BuildDevelopmentPlayer()
        {
            DeadSignalProjectSetup.EnsureReflexSettings();
            DeadSignalProjectSetup.EnsureRuntimeMaterialTemplates();
            DeadSignalProjectSetup.EnsureMaintenanceDeckAssets();
            DeadSignalProjectSetup.EnsureMaintenanceRoomShellAssets();
            DeadSignalProjectSetup.EnsureSignalTowerAssets();
            DeadSignalProjectSetup.EnsureExtractionPadAssets();
            DeadSignalProjectSetup.EnsureShortcutGateAssets();
            DeadSignalProjectSetup.EnsureSignalRoutingAssets();
            DeadSignalProjectSetup.EnsureStationMachineAssets();
            DeadSignalProjectSetup.EnsureSalvageCacheAssets();
            DeadSignalProjectSetup.EnsurePlayerDroneAssets();
            DeadSignalActorSetup.EnsureSecurityWardenAssets();
            DeadSignalActorSetup.EnsureSignalSapperAssets();
            DeadSignalProjectileSetup.EnsureAssets();
            DeadSignalSapperTelegraphSetup.EnsureAssets();
            DeadSignalWardenTelegraphSetup.EnsureAssets();
            DeadSignalTowerJunctionSetup.EnsureAssets();
            DeadSignalSalvageAnnexSetup.EnsureAssets();
            DeadSignalDepartureChannelSetup.EnsureAssets();
            DeadSignalCoolantGauntletSetup.EnsureAssets();
            DeadSignalRelayForkSetup.EnsureAssets();
            DeadSignalWardenBaySetup.EnsureAssets();
            DeadSignalSapperCradleSetup.EnsureAssets();
            _validateBuildInputs();
            _configureWindowsPlayer();

            var enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            var options = new BuildPlayerOptions
            {
                scenes = enabledScenes,
                locationPathName = Path.GetFullPath(EXECUTABLE_PATH),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"DEAD SIGNAL Windows build failed with {report.summary.totalErrors} errors. " +
                    $"Result: {report.summary.result}.");
            }

            Debug.Log(
                $"[DEAD SIGNAL BUILD] PASS | {Path.GetFullPath(EXECUTABLE_PATH)} | " +
                $"{report.summary.totalSize} bytes | {report.summary.totalTime.TotalSeconds:0.00}s");
        }

        private static void _validateBuildInputs()
        {
            var enabledScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            if (enabledScenes.Length == 0)
            {
                throw new BuildFailedException("DEAD SIGNAL requires at least one enabled build scene.");
            }

            foreach (var scene in enabledScenes)
            {
                if (string.IsNullOrWhiteSpace(scene.path) || !File.Exists(scene.path))
                {
                    throw new BuildFailedException($"Enabled build scene is missing: {scene.path}");
                }
            }

            if (!DeadSignalProjectSetup.HasReflexSettings)
            {
                throw new BuildFailedException("The Reflex settings resource is missing.");
            }

            if (!DeadSignalProjectSetup.HasRuntimeMaterialTemplates)
            {
                throw new BuildFailedException("Runtime material templates are missing.");
            }

            if (!DeadSignalProjectSetup.HasMaintenanceDeckAssets)
            {
                throw new BuildFailedException("The authored maintenance deck assets are missing.");
            }

            if (!DeadSignalProjectSetup.HasMaintenanceRoomShellAssets)
            {
                throw new BuildFailedException("The authored maintenance room-shell assets are missing.");
            }

            if (!DeadSignalProjectSetup.HasSignalTowerAssets)
            {
                throw new BuildFailedException("The authored Signal tower assets are missing.");
            }

            if (!DeadSignalProjectSetup.HasExtractionPadAssets)
            {
                throw new BuildFailedException("The authored extraction pad assets are missing.");
            }

            if (!DeadSignalProjectSetup.HasShortcutGateAssets)
            {
                throw new BuildFailedException("The authored shortcut gate assets are missing.");
            }

            if (!DeadSignalProjectSetup.HasSignalRoutingAssets)
            {
                throw new BuildFailedException("The authored Signal-routing assets are missing.");
            }

            if (!DeadSignalProjectSetup.HasStationMachineAssets)
            {
                throw new BuildFailedException("The authored station-machine assets are missing.");
            }

            if (!DeadSignalProjectSetup.HasSalvageCacheAssets)
            {
                throw new BuildFailedException("The authored salvage-cache assets are missing.");
            }

            if (!DeadSignalProjectSetup.HasPlayerDroneAssets)
            {
                throw new BuildFailedException("The authored maintenance-drone assets are missing.");
            }

            if (!DeadSignalActorSetup.HasSecurityWardenAssets)
            {
                throw new BuildFailedException("The authored Security Warden model or materials are missing.");
            }

            if (!DeadSignalActorSetup.HasSignalSapperAssets)
            {
                throw new BuildFailedException("The authored Signal Sapper model or materials are missing.");
            }

            if (!DeadSignalProjectileSetup.HasAssets)
            {
                throw new BuildFailedException("The authored Signal bolt model or materials are missing.");
            }

            if (!DeadSignalSapperTelegraphSetup.HasAssets)
            {
                throw new BuildFailedException("The authored Signal Sapper telegraph assets are missing.");
            }

            if (!DeadSignalWardenTelegraphSetup.HasAssets)
            {
                throw new BuildFailedException("The authored Warden strike-warning assets are missing.");
            }

            if (!DeadSignalTowerJunctionSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored tower-approach junction assets are missing.");
            }

            if (!DeadSignalSalvageAnnexSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored salvage-annex assets are missing.");
            }

            if (!DeadSignalDepartureChannelSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored extraction departure-channel assets are missing.");
            }

            if (!DeadSignalCoolantGauntletSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored southeast coolant-gauntlet assets are missing.");
            }

            if (!DeadSignalRelayForkSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored northwest relay-fork assets are missing.");
            }

            if (!DeadSignalWardenBaySetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Security Warden staging-bay assets are missing.");
            }

            if (!DeadSignalSapperCradleSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Signal Sapper service-cradle assets are missing.");
            }

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(APPLICATION_ICON_PATH) == null)
            {
                throw new BuildFailedException($"The application icon is missing or invalid: {APPLICATION_ICON_PATH}");
            }
        }

        private static void _configureWindowsPlayer()
        {
            var applicationIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(APPLICATION_ICON_PATH);
            var iconKind = IconKind.Application;
            var iconSizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, iconKind);
            if (iconSizes.Length == 0)
            {
                iconKind = IconKind.Any;
                iconSizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, iconKind);
            }

            if (iconSizes.Length == 0)
            {
                throw new BuildFailedException("Unity reported no Windows application icon slots.");
            }

            PlayerSettings.SetIcons(
                NamedBuildTarget.Standalone,
                Enumerable.Repeat(applicationIcon, iconSizes.Length).ToArray(),
                iconKind);
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            AssetDatabase.SaveAssets();
        }
    }
}
