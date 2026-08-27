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
            DeadSignalSuppressorFieldSetup.EnsureAssets();
            DeadSignalTowerJunctionSetup.EnsureAssets();
            DeadSignalSalvageAnnexSetup.EnsureAssets();
            DeadSignalDepartureChannelSetup.EnsureAssets();
            DeadSignalCoolantGauntletSetup.EnsureAssets();
            DeadSignalRelayForkSetup.EnsureAssets();
            DeadSignalWardenBaySetup.EnsureAssets();
            DeadSignalSapperCradleSetup.EnsureAssets();
            DeadSignalCameraSetup.EnsureAssets();
            DeadSignalEastVaultSetup.EnsureAssets();
            DeadSignalRelayFoundrySetup.EnsureAssets();
            DeadSignalRelayCoolingGantrySetup.EnsureAssets();
            DeadSignalCapacitorSpineSetup.EnsureAssets();
            DeadSignalSpineDischargeTrenchSetup.EnsureAssets();
            DeadSignalSpineInductionGallerySetup.EnsureAssets();
            DeadSignalConvergenceChamberSetup.EnsureAssets();
            DeadSignalConvergenceBreakerGallerySetup.EnsureAssets();
            DeadSignalFluxBypassSetup.EnsureAssets();
            DeadSignalArcFurnaceSetup.EnsureAssets();
            DeadSignalQuenchLoopSetup.EnsureAssets();
            DeadSignalEasternCombatScenarioSetup.EnsureAssets();
            DeadSignalSecurityTrialSetup.EnsureAssets();
            DeadSignalStationBackdropSetup.EnsureAssets();
            DeadSignalForegroundCutawaySetup.EnsureAssets();
            DeadSignalSignalSpineSetup.EnsureAssets();
            DeadSignalBoundaryThresholdSetup.EnsureAssets();
            DeadSignalHudSetup.EnsureAssets();
            DeadSignalThreatSetup.EnsureAssets();
            DeadSignalInterceptorSetup.EnsureAssets();
            DeadSignalSwarmerSetup.EnsureAssets();
            DeadSignalSalvageChainSetup.EnsureAssets();
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

            if (!DeadSignalHudSetup.HasAssets)
            {
                throw new BuildFailedException("The authored Signal HUD assets are missing.");
            }

            if (!DeadSignalThreatSetup.HasAssets)
            {
                throw new BuildFailedException("Threat, overclock, or Signal recovery content is missing.");
            }

            if (!DeadSignalInterceptorSetup.HasAssets)
            {
                throw new BuildFailedException("The authored Security Interceptor and flank gates are missing.");
            }

            if (!DeadSignalSwarmerSetup.HasAssets)
            {
                throw new BuildFailedException("The Swarmer pressure prefab or tuning asset is missing.");
            }

            if (!DeadSignalSecurityTrialSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Security Trial Wing is missing or incomplete.");
            }

            if (!DeadSignalSalvageChainSetup.HasAssets)
            {
                throw new BuildFailedException("The authored salvage-chain feedback art is missing.");
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

            if (!DeadSignalRelayFoundrySetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Relay Foundry region is missing or incomplete.");
            }

            if (!DeadSignalRelayCoolingGantrySetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Relay Cooling Gantry is missing or incomplete.");
            }

            if (!DeadSignalCapacitorSpineSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Capacitor Spine region is missing or incomplete.");
            }

            if (!DeadSignalSpineDischargeTrenchSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Spine Discharge Trench is missing or incomplete.");
            }

            if (!DeadSignalSpineInductionGallerySetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Spine Induction Gallery is missing or incomplete.");
            }

            if (!DeadSignalConvergenceChamberSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Convergence Chamber is missing or incomplete.");
            }

            if (!DeadSignalConvergenceBreakerGallerySetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Convergence Breaker Gallery is missing or incomplete.");
            }

            if (!DeadSignalFluxBypassSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Flux Bypass is missing or incomplete.");
            }

            if (!DeadSignalArcFurnaceSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Arc Furnace is missing or incomplete.");
            }

            if (!DeadSignalQuenchLoopSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Quench Loop is missing or incomplete.");
            }

            if (!DeadSignalEasternCombatScenarioSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored eastern combat laboratory is missing or incomplete.");
            }

            if (!DeadSignalStationBackdropSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored station underdeck backdrop is missing or incomplete.");
            }

            if (!DeadSignalForegroundCutawaySetup.HasAssets)
            {
                throw new BuildFailedException("The foreground-cutaway footprint assets are missing or incomplete.");
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

            if (!DeadSignalCameraSetup.HasAssets)
            {
                throw new BuildFailedException("The player follow-camera tuning asset is missing.");
            }

            if (!DeadSignalEastVaultSetup.HasAssets)
            {
                throw new BuildFailedException("The optional east salvage-vault assets are missing.");
            }

            if (!DeadSignalSignalSpineSetup.HasAssets)
            {
                throw new BuildFailedException("The authored opening Signal-spine assets are missing.");
            }

            if (!DeadSignalBoundaryThresholdSetup.HasAssets)
            {
                throw new BuildFailedException("The authored Signal boundary-threshold assets are missing.");
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
