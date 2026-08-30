using System;
using System.IO;
using System.Linq;
using DeadSignal.Missions;
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
            DeadSignalExtractionDockReadabilitySetup.EnsureAssets();
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
            DeadSignalCentralInstallationSetup.EnsureAssets();
            DeadSignalCentralReadabilitySetup.EnsureAssets();
            DeadSignalActOneRouteReadabilitySetup.EnsureAssets();
            DeadSignalActOneCompositionSetup.EnsureAssets();
            DeadSignalRelayFoundrySetup.EnsureAssets();
            DeadSignalRelayCoolingGantrySetup.EnsureAssets();
            DeadSignalActTwoReadabilitySetup.EnsureAssets();
            DeadSignalActTwoCompositionSetup.EnsureAssets();
            DeadSignalActTwoCalibrationReadabilitySetup.EnsureAssets();
            DeadSignalCapacitorSpineSetup.EnsureAssets();
            DeadSignalSpineDischargeTrenchSetup.EnsureAssets();
            DeadSignalSpineNetworkReadabilitySetup.EnsureAssets();
            DeadSignalSpineReturnReadabilitySetup.EnsureAssets();
            DeadSignalSpineCoreReadabilitySetup.EnsureAssets();
            DeadSignalSpineInductionGallerySetup.EnsureAssets();
            DeadSignalConvergenceChamberSetup.EnsureAssets();
            DeadSignalConvergenceBreakerGallerySetup.EnsureAssets();
            DeadSignalFluxBypassSetup.EnsureAssets();
            DeadSignalDeepCoreReadabilitySetup.EnsureAssets();
            DeadSignalArcFurnaceSetup.EnsureAssets();
            DeadSignalQuenchLoopSetup.EnsureAssets();
            DeadSignalActThreeCompositionSetup.EnsureAssets();
            DeadSignalEasternCombatScenarioSetup.EnsureAssets();
            DeadSignalSecurityTrialSetup.EnsureAssets();
            DeadSignalConvergenceReadabilitySetup.EnsureAssets();
            DeadSignalBreakerReadabilitySetup.EnsureAssets();
            DeadSignalCoreProcessingReadabilitySetup.EnsureAssets();
            DeadSignalSecurityTrialReadabilitySetup.EnsureAssets();
            DeadSignalSecurityLockdownReadabilitySetup.EnsureAssets();
            DeadSignalMissionObjectiveSetup.CreateCompatibilityMissionObjectives();
            DeadSignalStationBackdropSetup.EnsureAssets();
            DeadSignalForegroundCutawaySetup.EnsureAssets();
            DeadSignalSignalSpineSetup.EnsureAssets();
            DeadSignalBoundaryThresholdSetup.EnsureAssets();
            DeadSignalHudSetup.EnsureAssets();
            DeadSignalProductShellSetup.EnsureAssets();
            DeadSignalScreenFeedbackSetup.EnsureAssets();
            DeadSignalStationStateFeedbackSetup.EnsureAssets();
            DeadSignalWeaponTransformationFeedbackSetup.EnsureAssets();
            DeadSignalExtractionOutcomeFeedbackSetup.EnsureAssets();
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

            var missionObjectives = AssetDatabase.LoadAssetAtPath<MissionObjectiveGraphConfiguration>(
                "Assets/DeadSignal/Resources/Tuning/CompatibilityMissionObjectives.asset");
            if (missionObjectives == null || missionObjectives.ObjectiveCount != 23)
            {
                throw new BuildFailedException("The compatibility mission objective configuration is missing or incomplete.");
            }

            if (!DeadSignalHudSetup.HasAssets)
            {
                throw new BuildFailedException("The authored Signal HUD assets are missing.");
            }

            if (!DeadSignalScreenFeedbackSetup.HasAssets)
            {
                throw new BuildFailedException("The authored screen feedback assets are missing.");
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

            if (!DeadSignalExtractionDockReadabilitySetup.HasAssets)
            {
                throw new BuildFailedException("The authored Extraction Dock readability assets are missing.");
            }

            if (!DeadSignalProjectSetup.HasShortcutGateAssets)
            {
                throw new BuildFailedException("The authored shortcut gate assets are missing.");
            }

            if (!DeadSignalActOneRouteReadabilitySetup.HasAssets)
            {
                throw new BuildFailedException("The authored Act I route-readability assets are missing.");
            }

            if (!DeadSignalActOneCompositionSetup.HasAssets)
            {
                throw new BuildFailedException("The authored Act I presentation composition is missing or incomplete.");
            }

            if (!DeadSignalActTwoReadabilitySetup.HasAssets || !DeadSignalActTwoCalibrationReadabilitySetup.HasAssets)
            {
                throw new BuildFailedException("The authored Act II readability assets are missing.");
            }

            if (!DeadSignalActTwoCompositionSetup.HasAssets)
            {
                throw new BuildFailedException("The authored Act II presentation composition is missing or incomplete.");
            }

            if (!DeadSignalActThreeCompositionSetup.HasAssets)
            {
                throw new BuildFailedException("The authored Act III deep-core presentation composition is missing or incomplete.");
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

            if (!DeadSignalSpineDischargeTrenchSetup.HasAssets || !DeadSignalSpineNetworkReadabilitySetup.HasAssets ||
                !DeadSignalSpineReturnReadabilitySetup.HasAssets || !DeadSignalSpineCoreReadabilitySetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Spine network readability is missing or incomplete.");
            }

            if (!DeadSignalSpineInductionGallerySetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Spine Induction Gallery is missing or incomplete.");
            }

            if (!DeadSignalConvergenceChamberSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Convergence Chamber is missing or incomplete.");
            }

            if (!DeadSignalConvergenceReadabilitySetup.HasAssets)
            {
                throw new BuildFailedException("The Convergence calibration readability is missing or incomplete.");
            }

            if (!DeadSignalConvergenceBreakerGallerySetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Convergence Breaker Gallery is missing or incomplete.");
            }

            if (!DeadSignalBreakerReadabilitySetup.HasAssets)
            {
                throw new BuildFailedException("The Breaker distribution readability is missing or incomplete.");
            }

            if (!DeadSignalFluxBypassSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Flux Bypass is missing or incomplete.");
            }

            if (!DeadSignalDeepCoreReadabilitySetup.HasAssets)
            {
                throw new BuildFailedException("The deep-core machinery readability is missing or incomplete.");
            }

            if (!DeadSignalArcFurnaceSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Arc Furnace is missing or incomplete.");
            }

            if (!DeadSignalQuenchLoopSetup.HasAssets)
            {
                throw new BuildFailedException("The scene-authored Quench Loop is missing or incomplete.");
            }

            if (!DeadSignalCoreProcessingReadabilitySetup.HasAssets)
            {
                throw new BuildFailedException("The core-processing readability is missing or incomplete.");
            }

            if (!DeadSignalSecurityTrialReadabilitySetup.HasAssets)
            {
                throw new BuildFailedException("The Security Trial commitment readability is missing or incomplete.");
            }

            if (!DeadSignalSecurityLockdownReadabilitySetup.HasAssets)
            {
                throw new BuildFailedException("The Security Trial lockdown readability is missing or incomplete.");
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
