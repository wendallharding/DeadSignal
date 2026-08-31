using System;
using System.Collections;
using UnityEngine;
using DeadSignal.Application;
using DeadSignal.Combat;
using DeadSignal.Missions;
using DeadSignal.Player;
using DeadSignal.Presentation;
using DeadSignal.Salvage;
using DeadSignal.World;

namespace DeadSignal.Diagnostics
{
    /// <summary>
    /// Provides a command-line-only health check for the built player without affecting ordinary runs.
    /// </summary>
    public sealed class StandaloneBuildSmokeProbe : MonoBehaviour
    {
        public const string COMMAND_LINE_ARGUMENT = "-deadSignalBuildSmoke";
        public const string PASS_MARKER = "[DEAD SIGNAL STANDALONE SMOKE] PASS";

        private const int EXPECTED_AUTHORED_OBSTACLE_COUNT = 138;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void _startWhenRequested()
        {
            if (!IsRequested(Environment.GetCommandLineArgs()))
            {
                return;
            }

            var probeObject = new GameObject("Standalone Build Smoke Probe");
            DontDestroyOnLoad(probeObject);
            probeObject.AddComponent<StandaloneBuildSmokeProbe>();
        }

        public static bool IsRequested(string[] arguments)
        {
            return arguments != null && Array.Exists(
                arguments,
                argument => string.Equals(argument, COMMAND_LINE_ARGUMENT, StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;

            var game = FindFirstObjectByType<DeadSignalGame>();
            var missionObjectives = Resources.Load<MissionObjectiveGraphConfiguration>("Tuning/CompatibilityMissionObjectives");
            var missionObjectivesReady = missionObjectives != null && missionObjectives.ObjectiveCount == 23;
            var productShell = FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include);
            var productShellReady = productShell != null && !productShell.IsMenuVisible &&
                                    game != null && !game.IsMainMenuOpen &&
                                    Resources.Load<Texture2D>("UI/MainMenuStationBackdrop") != null;
            var objectiveConsumersReady = game != null &&
                                          game.CurrentMissionObjectiveId == MissionObjectiveId.CentralTower &&
                                          game.CurrentMissionGuidanceTitle == "RESTORE CENTRAL" &&
                                          game.CurrentObjectiveBeaconLabel == game.CurrentMissionGuidanceAction;
            var cargoAnnexObjective = FindFirstObjectByType<AuthoredCargoAnnexObjective>(FindObjectsInactive.Include);
            var cargoAnnexReady = cargoAnnexObjective != null && cargoAnnexObjective.IsConfigured &&
                                  cargoAnnexObjective.Phase == CargoCouplingRetrievalPhase.AwaitingCommit;
            var coolantObjective = FindFirstObjectByType<AuthoredCoolantReclamationObjective>(FindObjectsInactive.Include);
            var coolantReady = coolantObjective != null && coolantObjective.IsConfigured &&
                               coolantObjective.Phase == CoolantSealThreadingPhase.AwaitingFirstBaffle;
            var relayForkObjective = FindFirstObjectByType<AuthoredRelayForkObjective>(FindObjectsInactive.Include);
            var transferVaultObjective = FindFirstObjectByType<AuthoredTransferVaultObjective>(FindObjectsInactive.Include);
            var centralInstallationObjective =
                FindFirstObjectByType<AuthoredCentralInstallationObjective>(FindObjectsInactive.Include);
            var relayPayloadObjective =
                FindFirstObjectByType<AuthoredRelayPayloadObjective>(FindObjectsInactive.Include);
            var spineVentingObjective =
                FindFirstObjectByType<AuthoredSpineVentingObjective>(FindObjectsInactive.Include);
            var spineCoreInstallationObjective =
                FindFirstObjectByType<AuthoredSpineCoreInstallationObjective>(FindObjectsInactive.Include);
            var inductionLatticeObjective =
                FindFirstObjectByType<AuthoredInductionLatticeObjective>(FindObjectsInactive.Include);
            var fluxShuntObjective =
                FindFirstObjectByType<AuthoredFluxShuntObjective>(FindObjectsInactive.Include);
            var convergenceCalibrationObjective =
                FindFirstObjectByType<AuthoredConvergenceCalibrationObjective>(FindObjectsInactive.Include);
            var breakerResetObjective =
                FindFirstObjectByType<AuthoredBreakerResetObjective>(FindObjectsInactive.Include);
            var furnaceForgeObjective =
                FindFirstObjectByType<AuthoredFurnaceForgeObjective>(FindObjectsInactive.Include);
            var quenchStabilizationObjective =
                FindFirstObjectByType<AuthoredQuenchStabilizationObjective>(FindObjectsInactive.Include);
            var centralTransferReady = relayForkObjective != null && relayForkObjective.IsConfigured &&
                                       transferVaultObjective != null && transferVaultObjective.IsConfigured &&
                                       centralInstallationObjective != null && centralInstallationObjective.IsConfigured &&
                                       transferVaultObjective.IsRouteConfigured && !transferVaultObjective.IsRelayRouteOpen;
            var relayPayloadReady = relayPayloadObjective != null && relayPayloadObjective.IsConfigured &&
                                    relayPayloadObjective.HasReadabilityAssets &&
                                    relayPayloadObjective.PresentationState ==
                                    RelayCalibrationPresentationState.PrerequisiteLocked &&
                                    relayPayloadObjective.GetComponent<AuthoredRouteDoorReadability>()?.PresentationState ==
                                    RouteDoorPresentationState.Locked;
            var spineVentingReady = spineVentingObjective != null && spineVentingObjective.IsConfigured &&
                                    game != null && !game.IsSpineBerthVented;
            var spineCoreInstallationReady = spineCoreInstallationObjective != null &&
                                             spineCoreInstallationObjective.IsConfigured &&
                                             spineCoreInstallationObjective.HasReadabilityAssets &&
                                             game != null && !game.IsSpineCoreInstalled;
            var inductionLatticeReady = inductionLatticeObjective != null && inductionLatticeObjective.IsConfigured &&
                                        game != null && !game.IsInductionLatticeCharged;
            var fluxShuntReady = fluxShuntObjective != null && fluxShuntObjective.IsConfigured &&
                                 game != null && !game.IsFluxShuntRouted;
            var convergenceCalibrationReady = convergenceCalibrationObjective != null &&
                                              convergenceCalibrationObjective.IsConfigured &&
                                              convergenceCalibrationObjective.HasReadabilityAssets &&
                                              convergenceCalibrationObjective.PresentationState ==
                                              ConvergenceCalibrationPresentationState.Dormant &&
                                              Resources.Load<Texture2D>(
                                                  "Environment/ConvergenceCalibrationStatusPanel") != null &&
                                              Resources.Load<Mesh>(
                                                  "Environment/ConvergenceCalibrationStatusReadability") != null &&
                                              Resources.Load<Material>(
                                                  "Materials/ConvergenceChamber/ConvergenceCalibrationStatus") != null &&
                                              Resources.Load<ConvergenceCalibrationTuning>(
                                                  "Tuning/ConvergenceCalibrationTuning") != null &&
                                              game != null && !game.IsConvergenceCalibrated;
            var breakerResetReady = breakerResetObjective != null && breakerResetObjective.IsConfigured &&
                                    breakerResetObjective.HasReadabilityAssets &&
                                    breakerResetObjective.PresentationState ==
                                    BreakerResetPresentationState.DistributionLocked &&
                                    Resources.Load<Texture2D>("Environment/BreakerDistributionStatusPanel") != null &&
                                    Resources.Load<Mesh>("Environment/BreakerDistributionStatusReadability") != null &&
                                    Resources.Load<Material>(
                                        "Materials/ConvergenceBreakerGallery/BreakerDistributionStatus") != null &&
                                    game != null && !game.IsBreakerDistributionReset;
            var quenchDoorReadability = quenchStabilizationObjective != null
                ? quenchStabilizationObjective.GetComponent<AuthoredRouteDoorReadability>()
                : null;
            var coreProcessingReady = furnaceForgeObjective != null && furnaceForgeObjective.IsConfigured &&
                                      furnaceForgeObjective.HasReadabilityAssets &&
                                      furnaceForgeObjective.PresentationState == CoreProcessingPresentationState.Locked &&
                                      quenchStabilizationObjective != null && quenchStabilizationObjective.IsConfigured &&
                                      quenchStabilizationObjective.HasReadabilityAssets &&
                                      quenchStabilizationObjective.PresentationState ==
                                      CoreProcessingPresentationState.Locked &&
                                      quenchDoorReadability?.PresentationState == RouteDoorPresentationState.Locked &&
                                      Resources.Load<Texture2D>("Environment/CoreProcessingStatusPanel") != null &&
                                      Resources.Load<Mesh>("Environment/FurnaceForgeStatusReadability") != null &&
                                      Resources.Load<Mesh>("Environment/QuenchStabilizationStatusReadability") != null &&
                                      Resources.Load<Material>(
                                          "Materials/CoreProcessingReadability/CoreProcessingStatus") != null &&
                                      game != null && !game.IsLatticeForged && !game.IsCoreStabilized;
            var weaponTextureReady = Resources.Load<Texture2D>("Environment/RelayFoundryWeaponCalibrationDecal") != null;
            var weaponMaterialReady =
                Resources.Load<Material>("Materials/RelayFoundry/RelayFoundryWeaponCalibrationDecal") != null;
            var weaponDecalReady = game != null &&
                                   game.transform.Find("Relay Foundry Region/Relay Weapon Calibration Decal") != null;
            var lockdownTextureReady = Resources.Load<Texture2D>("Environment/RelayFoundryLockdownDecal") != null;
            var lockdownMaterialReady =
                Resources.Load<Material>("Materials/RelayFoundry/RelayFoundryLockdownDecal") != null;
            var lockdownDecalsReady = game != null &&
                                      game.transform.Find("Relay Foundry Region/Foundry North Lockdown Decal") != null &&
                                      game.transform.Find("Relay Foundry Region/Foundry South Lockdown Decal") != null;
            var departureReturnTextureReady =
                Resources.Load<Texture2D>("Environment/DepartureCargoReturnDecal") != null;
            var departureReturnMaterialReady =
                Resources.Load<Material>("Materials/DepartureCargoReturnDecal") != null;
            var departureCapacitorReady = Resources.Load<GameObject>("Environment/DepartureCapacitor") != null;
            var departureChannelReady = Resources.Load<GameObject>("Environment/ExtractionDepartureChannel") != null;
            var departureAlbedoReady = Resources.Load<Texture2D>("Environment/DepartureCapacitorAlbedo") != null;
            var departureSurgeTextureReady = Resources.Load<Texture2D>("Environment/DepartureCapacitorSurgeDecal") != null;
            var departureSurgeMaterialReady = Resources.Load<Material>("Materials/DepartureCapacitorSurgeDecal") != null;
            var combatLabTextureReady = Resources.Load<Texture2D>("Environment/EasternCombatLabTarget") != null;
            var combatLabMaterialReady =
                Resources.Load<Material>("Materials/EasternCombatLab/EasternCombatLabTarget") != null;
            var combatLabAnchorsReady = game != null && game.transform.Find(
                "Spine Induction Gallery Region/Convergence Chamber Region/" +
                "Arc Furnace Region/Eastern Combat Scenario") != null;
            var swarmerPrefabReady = Resources.Load<GameObject>("Actors/SwarmerAssembly") != null;
            var swarmerAlbedoReady = Resources.Load<Texture2D>("Actors/SecuritySwarmerAlbedo") != null;
            var swarmerTuningReady = Resources.Load<SwarmerPressureTuning>("Tuning/SwarmerPressureTuning") != null;
            var wardenBayPrefab = Resources.Load<GameObject>("Environment/WardenStagingBay");
            var sapperCradlePrefab = Resources.Load<GameObject>("Environment/SignalSapperCradle");
            var withdrawalPursuitReady =
                wardenBayPrefab != null &&
                wardenBayPrefab.TryGetComponent<AuthoredWithdrawalPursuitLandmark>(out var wardenBayLandmark) &&
                wardenBayLandmark.IsConfigured && wardenBayLandmark.Phase == PoweredWithdrawalPhase.WardenBay &&
                sapperCradlePrefab != null &&
                sapperCradlePrefab.TryGetComponent<AuthoredWithdrawalPursuitLandmark>(out var sapperCradleLandmark) &&
                sapperCradleLandmark.IsConfigured && sapperCradleLandmark.Phase == PoweredWithdrawalPhase.SapperCradle;
            var securityTrialPrefabReady =
                Resources.Load<GameObject>("Environment/SecurityTrialWingRegion") != null;
            var securityTrialSceneReady = game != null && game.HasAuthoredCombatChamber;
            var securityTrialReadabilityReady = game != null &&
                                                game.CurrentCombatChamberState == CombatChamberState.Dormant &&
                                                FindFirstObjectByType<AuthoredCombatChamber>() is
                                                { HasCommitmentReadabilityAssets: true } chamber &&
                                                chamber.HasLockdownReadabilityAssets &&
                                                chamber.CommitmentPresentationState ==
                                                TrialCommitmentPresentationState.Locked &&
                                                chamber.LockdownPresentationState ==
                                                LockdownChamberPresentationState.Dormant &&
                                                chamber.CapacitorPresentationState ==
                                                TrialCapacitorPresentationState.Locked &&
                                                Resources.Load<Texture2D>(
                                                    "Environment/SecurityTrialCommitmentStatusPanel") != null &&
                                                Resources.Load<Mesh>(
                                                    "Environment/SecurityTrialCommitmentStatusReadability") != null &&
                                                Resources.Load<Material>(
                                                    "Materials/SecurityTrialReadability/SecurityTrialCommitmentStatus") != null &&
                                                Resources.Load<Texture2D>(
                                                    "Environment/SecurityTrialLockdownStatusAtlas") != null &&
                                                Resources.Load<Mesh>(
                                                    "Environment/SecurityTrialLockdownStatusReadability") != null &&
                                                Resources.Load<Mesh>(
                                                    "Environment/SecurityTrialDoorStatusReadability") != null &&
                                                Resources.Load<Mesh>(
                                                    "Environment/SecurityTrialCapacitorStatusReadability") != null &&
                                                Resources.Load<Material>(
                                                    "Materials/SecurityTrialReadability/SecurityTrialLockdownStatus") != null;
            var stationBackdropTextureReady =
                Resources.Load<Texture2D>("Environment/StationUnderdeckAlbedo") != null;
            var stationBackdropMaterialReady = Resources.Load<Material>("Materials/StationUnderdeck") != null;
            var stationBackdropPrefabReady = Resources.Load<GameObject>("Environment/StationUnderdeckBackdrop") != null;
            var stationBackdropSceneReady = FindFirstObjectByType<AuthoredStationBackdrop>() != null;
            var actOneCompositionPrefabReady = Resources.Load<GameObject>("Environment/ActOneComposition") != null;
            var actOneCompositionSceneReady =
                FindFirstObjectByType<AuthoredActOneComposition>() is { IsConfigured: true };
            var actTwoCompositionPrefabReady = Resources.Load<GameObject>("Environment/ActTwoComposition") != null;
            var actTwoCompositionSceneReady =
                FindFirstObjectByType<AuthoredActTwoComposition>() is { IsConfigured: true };
            var actThreeCompositionPrefabReady =
                Resources.Load<GameObject>("Environment/ActThreeDeepCoreComposition") != null;
            var actThreeCompositionSceneReady =
                FindFirstObjectByType<AuthoredActThreeComposition>() is { IsConfigured: true };
            var securityTrialCompositionPrefabReady =
                Resources.Load<GameObject>("Environment/SecurityTrialComposition") != null;
            var securityTrialCompositionSceneReady =
                FindFirstObjectByType<AuthoredSecurityTrialComposition>() is { IsConfigured: true };
            var withdrawalDockCompositionPrefabReady =
                Resources.Load<GameObject>("Environment/WithdrawalDockComposition") != null;
            var withdrawalDockCompositionSceneReady =
                FindFirstObjectByType<AuthoredWithdrawalDockComposition>() is { IsConfigured: true };
            var foregroundCutawayTextureReady =
                Resources.Load<Texture2D>("VFX/ForegroundCutawayFootprint") != null;
            var foregroundCutawayMaterialReady =
                Resources.Load<Material>("Materials/ForegroundCutawayFootprint") != null;
            var authoredCutawayTextureReady =
                Resources.Load<Texture2D>("VFX/ForegroundCutawayFootprintAuthored") != null;
            var authoredCutawayMaterialReady =
                Resources.Load<Material>("Materials/ForegroundCutawayFootprintAuthored") != null;
            var wideCutawayTextureReady =
                Resources.Load<Texture2D>("VFX/ForegroundCutawayFootprintWide") != null;
            var wideCutawayMaterialReady =
                Resources.Load<Material>("Materials/ForegroundCutawayFootprintWide") != null;
            var authoredCutawayBindingCount = FindObjectsByType<AuthoredForegroundCutaway>(
                FindObjectsSortMode.None).Length;
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] RELAY WEAPON | " +
                      $"decal={weaponDecalReady} texture={weaponTextureReady} material={weaponMaterialReady} " +
                      $"obstacles={game?.AuthoredMapObstacleCount ?? -1}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] DEPARTURE RETURN | " +
                      $"texture={departureReturnTextureReady} material={departureReturnMaterialReady} " +
                      $"capacitor={departureCapacitorReady} channel={departureChannelReady} albedo={departureAlbedoReady} " +
                      $"surgeTexture={departureSurgeTextureReady} surgeMaterial={departureSurgeMaterialReady} " +
                      $"salvage={game?.SalvageCacheInstanceCount ?? -1}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] EASTERN COMBAT LAB | " +
                      $"anchors={combatLabAnchorsReady} texture={combatLabTextureReady} material={combatLabMaterialReady} " +
                      $"swarmerPrefab={swarmerPrefabReady} swarmerTuning={swarmerTuningReady}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] SECURITY TRIAL | " +
                      $"scene={securityTrialSceneReady} prefab={securityTrialPrefabReady} " +
                      $"state={game?.CurrentCombatChamberState}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] SUPPRESSOR FIELD | " +
                      $"runtime={game?.HasSuppressorFieldTexture ?? false} " +
                      $"texture={Resources.Load<Texture2D>("VFX/SuppressorFieldActive") != null}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] STATION UNDERDECK | " +
                      $"scene={stationBackdropSceneReady} texture={stationBackdropTextureReady} " +
                      $"material={stationBackdropMaterialReady} prefab={stationBackdropPrefabReady}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] ACT I COMPOSITION | " +
                      $"scene={actOneCompositionSceneReady} prefab={actOneCompositionPrefabReady}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] ACT II COMPOSITION | " +
                      $"scene={actTwoCompositionSceneReady} prefab={actTwoCompositionPrefabReady}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] ACT III DEEP-CORE COMPOSITION | " +
                      $"scene={actThreeCompositionSceneReady} prefab={actThreeCompositionPrefabReady}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] SECURITY TRIAL COMPOSITION | " +
                      $"scene={securityTrialCompositionSceneReady} prefab={securityTrialCompositionPrefabReady}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] WITHDRAWAL AND DOCK COMPOSITION | " +
                      $"scene={withdrawalDockCompositionSceneReady} prefab={withdrawalDockCompositionPrefabReady}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] FOREGROUND CUTAWAY | " +
                      $"runtimeEnabled={game?.HasForegroundOcclusion ?? false} texture={foregroundCutawayTextureReady} " +
                      $"material={foregroundCutawayMaterialReady} authoredTexture={authoredCutawayTextureReady} " +
                      $"authoredMaterial={authoredCutawayMaterialReady} wideTexture={wideCutawayTextureReady} " +
                      $"wideMaterial={wideCutawayMaterialReady} bindings={authoredCutawayBindingCount}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] MISSION OBJECTIVES | " +
                      $"resource={missionObjectives != null} objectives={missionObjectives?.ObjectiveCount ?? 0} " +
                      $"consumers={objectiveConsumersReady} current={game?.CurrentMissionObjectiveId}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] PRODUCT SHELL | " +
                      $"controller={productShell != null} backdrop={Resources.Load<Texture2D>("UI/MainMenuStationBackdrop") != null} " +
                      $"commandLineBypass={productShell != null && !productShell.IsMenuVisible && game != null && !game.IsMainMenuOpen}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] CARGO ANNEX | " +
                      $"configured={cargoAnnexObjective?.IsConfigured ?? false} phase={cargoAnnexObjective?.Phase}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] COOLANT RECLAMATION | " +
                      $"configured={coolantObjective?.IsConfigured ?? false} phase={coolantObjective?.Phase}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] CENTRAL TRANSFER | " +
                      $"fork={relayForkObjective?.IsConfigured ?? false} vault={transferVaultObjective?.IsConfigured ?? false} " +
                      $"install={centralInstallationObjective?.IsConfigured ?? false} " +
                      $"routeConfigured={transferVaultObjective?.IsRouteConfigured ?? false} " +
                      $"routeOpen={transferVaultObjective?.IsRelayRouteOpen ?? false}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] RELAY PAYLOAD | " +
                      $"configured={relayPayloadObjective?.IsConfigured ?? false} " +
                      $"stabilized={game?.IsRelayPayloadStabilized ?? false}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] SPINE VENTING | " +
                      $"configured={spineVentingObjective?.IsConfigured ?? false} " +
                      $"vented={game?.IsSpineBerthVented ?? false}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] SPINE CORE INSTALLATION | " +
                      $"configured={spineCoreInstallationObjective?.IsConfigured ?? false} " +
                      $"installed={game?.IsSpineCoreInstalled ?? false}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] INDUCTION LATTICE | " +
                      $"configured={inductionLatticeObjective?.IsConfigured ?? false} " +
                      $"charged={game?.IsInductionLatticeCharged ?? false}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] FLUX SHUNT | " +
                      $"configured={fluxShuntObjective?.IsConfigured ?? false} " +
                      $"routed={game?.IsFluxShuntRouted ?? false}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] CONVERGENCE CALIBRATION | " +
                      $"configured={convergenceCalibrationObjective?.IsConfigured ?? false} " +
                      $"active={game?.IsConvergenceCalibrationActive ?? false} " +
                      $"complete={game?.IsConvergenceCalibrated ?? false}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] BREAKER RESET | " +
                      $"configured={breakerResetObjective?.IsConfigured ?? false} " +
                      $"complete={game?.IsBreakerDistributionReset ?? false}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] CORE PROCESSING | " +
                      $"furnace={furnaceForgeObjective?.IsConfigured ?? false} " +
                      $"quench={quenchStabilizationObjective?.IsConfigured ?? false} " +
                      $"forged={game?.IsLatticeForged ?? false} stabilized={game?.IsCoreStabilized ?? false}");
            var runtimeReady = game != null &&
                                productShellReady &&
                                missionObjectivesReady &&
                                objectiveConsumersReady &&
                                cargoAnnexReady &&
                                coolantReady &&
                                centralTransferReady &&
                                relayPayloadReady &&
                                spineVentingReady &&
                                spineCoreInstallationReady &&
                                inductionLatticeReady &&
                                fluxShuntReady &&
                                convergenceCalibrationReady &&
                                breakerResetReady &&
                                coreProcessingReady &&
                                game.transform.Find("Maintenance Drone") != null &&
                                game.transform.Find("Shortcut Gate Assembly/Signal Shortcut Gate") != null &&
                                game.transform.Find("Tower Signal Lines/Signal Trunk West") != null &&
                                game.HasGeneratedAudio &&
                                game.HasMaintenanceDeckAssets &&
                                game.HasMaintenanceRoomShellAssets &&
                                game.HasSignalTowerAssets &&
                                game.HasExtractionPadAssets &&
                                game.HasShortcutGateAssets &&
                                game.HasSignalRoutingAssets &&
                                game.HasStationMachineAssets &&
                                game.HasSalvageCacheAssets &&
                                game.SalvageCacheInstanceCount == RunModel.SalvageRequired + 1 &&
                                game.AuthoredSalvageSocketCount == 2 &&
                                game.HasSalvagePresentationTuning &&
                                game.HasPlayerDroneAssets &&
                                game.HasPlayerMovementTuning &&
                                game.HasPlayerSignalWake &&
                                game.HasPlayerCameraTuning &&
                                game.IsPlayerCameraFollowing &&
                                Resources.Load<GameObject>("Actors/MaintenanceDroneModel") != null &&
                                Resources.Load<Texture2D>("Actors/MaintenanceDroneHullAlbedo") != null &&
                                Resources.Load<Material>("Materials/MaintenanceDroneHull") != null &&
                                Resources.Load<Material>("Materials/MaintenanceDroneSignal") != null &&
                                Resources.Load<Material>("Materials/MaintenanceDroneCore") != null &&
                                Resources.Load<Material>("Materials/MaintenanceDroneTool") != null &&
                                Resources.Load<PlayerDroneMovementTuning>("Tuning/PlayerDroneMovementTuning") != null &&
                                Resources.Load<Texture2D>("VFX/PlayerDroneSignalWake") != null &&
                                Resources.Load<PlayerCameraTuning>("Tuning/PlayerCameraTuning") != null &&
                                Resources.Load<SalvagePresentationTuning>("Tuning/SalvagePresentationTuning") != null &&
                                Resources.Load<GameObject>("Actors/SecurityWardenAssembly") != null &&
                                Resources.Load<GameObject>("Actors/SecurityWardenModel") != null &&
                                Resources.Load<Texture2D>("Actors/SecurityWardenArmorAlbedo") != null &&
                                Resources.Load<Material>("Materials/SecurityWardenArmor") != null &&
                                Resources.Load<Material>("Materials/SecurityWardenEye") != null &&
                                Resources.Load<Material>("Materials/SecurityWardenCrown") != null &&
                                game.HasWardenWarningTexture &&
                                Resources.Load<Texture2D>("VFX/WardenStrikeWarning") != null &&
                                Resources.Load<WardenThreatTelegraphTuning>("Tuning/WardenThreatTelegraphTuning") != null &&
                                game.HasSecurityInterceptorAssets &&
                                game.HasSecurityInterceptorPresentation &&
                                game.SecurityInterceptorPartCount == 4 &&
                                game.AuthoredInterceptorEntranceCount == 9 &&
                                Resources.Load<GameObject>("Actors/SecurityInterceptorAssembly") != null &&
                                Resources.Load<Texture2D>("Actors/SecurityInterceptorAlbedo") != null &&
                                game.HasSecuritySuppressorAssets &&
                                game.HasSecuritySuppressorPresentation &&
                                game.SecuritySuppressorPartCount == 4 &&
                                Resources.Load<GameObject>("Actors/SecuritySuppressorAssembly") != null &&
                                Resources.Load<Texture2D>("Actors/SecuritySuppressorAlbedo") != null &&
                                game.HasSuppressorFieldTexture &&
                                Resources.Load<Texture2D>("VFX/SuppressorFieldActive") != null &&
                                Resources.Load<GameObject>("Environment/InterceptorEntryGate") != null &&
                                game.AuthoredMapObstacleCount == EXPECTED_AUTHORED_OBSTACLE_COUNT &&
                                securityTrialSceneReady && securityTrialPrefabReady && securityTrialReadabilityReady &&
                                Resources.Load<GameObject>("Environment/CoolantManifoldAssembly") != null &&
                                Resources.Load<GameObject>("Environment/TowerApproachJunction") != null &&
                                Resources.Load<Texture2D>("Environment/CoolantManifoldAlbedo") != null &&
                                Resources.Load<GameObject>("Environment/SalvageAnnexBarrier") != null &&
                                Resources.Load<GameObject>("Environment/SalvageAnnex") != null &&
                                Resources.Load<Texture2D>("Environment/SalvageAnnexAlbedo") != null &&
                                Resources.Load<Texture2D>("Environment/CargoAnnexHeroAtlas") != null &&
                                Resources.Load<Mesh>("Environment/CargoAnnexHeroFinish") != null &&
                                Resources.Load<GameObject>("Environment/EastSalvageVaultModel") != null &&
                                Resources.Load<GameObject>("Environment/EastSalvageVault") != null &&
                                Resources.Load<Texture2D>("Environment/EastSalvageVaultAlbedo") != null &&
                                game.transform.Find("Relay Foundry Region/Relay Tower Assembly") != null &&
                                game.transform.Find("Relay Foundry Region/Relay Induction Turbine") != null &&
                                Resources.Load<GameObject>("Environment/RelayFoundryRegion") != null &&
                                Resources.Load<GameObject>("Environment/RelayFoundryTurbineModel") != null &&
                                Resources.Load<Texture2D>("Environment/RelayFoundryTurbineAlbedo") != null &&
                                Resources.Load<Mesh>("Environment/RelayFoundryHeroStructure") != null &&
                                Resources.Load<Mesh>("Environment/RelayFoundryHeroPower") != null &&
                                game.transform.Find("Relay Foundry Region/Relay Foundry Hero Structure") != null &&
                                game.transform.Find("Relay Foundry Region/Relay Foundry Hero Power") != null &&
                                Resources.Load<Texture2D>("Environment/RelayFoundryRouteDecal") != null &&
                                game.transform.Find(
                                    "Relay Foundry Region/Relay Cooling Gantry Region/Relay Heat Exchanger") != null &&
                                game.transform.Find(
                                    "Relay Foundry Region/Relay Cooling Gantry Region/Cooling Gantry Reinforcement Gate") != null &&
                                game.transform.Find("Relay Foundry Region/Relay Payload Calibration Anchor") != null &&
                                game.transform.Find("Relay Foundry Region/Relay Calibration Status Panel") != null &&
                                game.transform.Find("Relay Foundry Region/Relay Calibration Selector") != null &&
                                game.transform.Find("Relay Foundry Region/Relay Return Threshold") != null &&
                                game.transform.Find(
                                    "Relay Foundry Region/Relay Cooling Gantry Region/" +
                                    "Cooling Gantry Relay Payload Socket") != null &&
                                Resources.Load<GameObject>("Environment/RelayCoolingGantryRegion") != null &&
                                Resources.Load<GameObject>("Environment/RelayHeatExchanger") != null &&
                                Resources.Load<Texture2D>("Environment/CoolingGantryHeroAtlas") != null &&
                                Resources.Load<Mesh>("Environment/CoolingGantryHeroFinish") != null &&
                                game.transform.Find(
                                    "Relay Foundry Region/Relay Cooling Gantry Region/Cooling Gantry Hero Finish") != null &&
                                Resources.Load<Texture2D>("Environment/RelayCoolingGantryRouteDecal") != null &&
                                Resources.Load<Material>(
                                    "Materials/RelayCoolingGantry/RelayCoolingGantryRouteDecal") != null &&
                                game.transform.Find("Capacitor Spine Region/Capacitor Transfer Bank") != null &&
                                game.transform.Find("Capacitor Spine Region/Third Tower Berth") != null &&
                                game.transform.Find("Capacitor Spine Region/Capacitor Spine Route Decal") != null &&
                                game.transform.Find("Capacitor Spine Region/Capacitor Spine Activation Decal") != null &&
                                game.transform.Find("Capacitor Spine Region/Capacitor Spine Return Decal") != null &&
                                game.transform.Find("Capacitor Spine Region/Spine Return Threshold") != null &&
                                game.transform.Find("Capacitor Spine Region/Spine Signal Lines") != null &&
                                Resources.Load<GameObject>("Environment/CapacitorSpineRegion") != null &&
                                Resources.Load<Texture2D>("Environment/CapacitorSpineRouteDecal") != null &&
                                Resources.Load<Material>("Materials/CapacitorSpine/CapacitorSpineRouteDecal") != null &&
                                Resources.Load<Texture2D>("Environment/CapacitorSpineActivationDecal") != null &&
                                Resources.Load<Material>("Materials/CapacitorSpine/CapacitorSpineActivationDecal") != null &&
                                Resources.Load<Texture2D>("Environment/CapacitorSpineReturnDecal") != null &&
                                Resources.Load<Material>("Materials/CapacitorSpine/CapacitorSpineReturnDecal") != null &&
                                game.transform.Find(
                                    "Capacitor Spine Region/Spine Discharge Trench Region/Central Discharge Coil") != null &&
                                game.transform.Find(
                                    "Capacitor Spine Region/Spine Discharge Trench Region/" +
                                    "Discharge Trench Reinforcement Gate") != null &&
                                Resources.Load<GameObject>("Environment/SpineDischargeTrenchRegion") != null &&
                                Resources.Load<Texture2D>("Environment/SpineDischargeTrenchRouteDecal") != null &&
                                Resources.Load<Material>(
                                    "Materials/SpineDischargeTrench/SpineDischargeTrenchRouteDecal") != null &&
                                Resources.Load<Texture2D>("Environment/SpineHeroAtlas") != null &&
                                Resources.Load<Mesh>("Environment/CapacitorSpineHeroFinish") != null &&
                                Resources.Load<Mesh>("Environment/DischargeTrenchHeroFinish") != null &&
                                game.transform.Find("Capacitor Spine Region/Capacitor Spine Hero Finish") != null &&
                                game.transform.Find(
                                    "Capacitor Spine Region/Spine Discharge Trench Region/" +
                                    "Discharge Trench Hero Finish") != null &&
                                game.transform.Find("Spine Induction Gallery Region/Induction Coil") != null &&
                                game.transform.Find("Spine Induction Gallery Region/Induction Gallery Signal Lines") != null &&
                                game.transform.Find("Spine Induction Gallery Region/Induction Gallery Route Decal") != null &&
                                Resources.Load<GameObject>("Environment/SpineInductionGalleryRegion") != null &&
                                Resources.Load<Texture2D>("Environment/SpineInductionGalleryRouteDecal") != null &&
                                Resources.Load<Material>(
                                    "Materials/SpineInductionGallery/SpineInductionGalleryRouteDecal") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Induction Lattice Objective/" +
                                    "Induction Charge Status") != null &&
                                Resources.Load<Texture2D>("Environment/DeepCoreMachineryStatusPanel") != null &&
                                Resources.Load<Mesh>("Environment/InductionChargeGlyphReadability") != null &&
                                Resources.Load<Material>(
                                    "Materials/DeepCoreReadability/DeepCoreMachineryStatus") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Convergence Chamber Region/" +
                                    "Convergence Busbar Assembly") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Convergence Chamber Region/" +
                                    "Convergence Reinforcement Gate") != null &&
                                Resources.Load<GameObject>("Environment/ConvergenceChamberRegion") != null &&
                                Resources.Load<GameObject>("Environment/ConvergenceBusbar") != null &&
                                Resources.Load<Texture2D>("Environment/ConvergenceChamberRouteDecal") != null &&
                                Resources.Load<Material>(
                                    "Materials/ConvergenceChamber/ConvergenceChamberRouteDecal") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Convergence Chamber Region/" +
                                    "Convergence Breaker Gallery Region/Breaker Bank Assembly") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Convergence Chamber Region/" +
                                    "Convergence Breaker Gallery Region/Breaker Gallery Reinforcement Gate") != null &&
                                Resources.Load<GameObject>("Environment/ConvergenceBreakerGalleryRegion") != null &&
                                Resources.Load<Texture2D>("Environment/ConvergenceBreakerHeroAtlas") != null &&
                                Resources.Load<Mesh>("Environment/ConvergenceChamberHeroFinish") != null &&
                                Resources.Load<Mesh>("Environment/BreakerGalleryHeroFinish") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Convergence Chamber Region/Convergence Chamber Hero Finish") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Convergence Chamber Region/Convergence Breaker Gallery Region/Breaker Gallery Hero Finish") != null &&
                                Resources.Load<Texture2D>("Environment/ConvergenceBreakerGalleryRouteDecal") != null &&
                                Resources.Load<Material>(
                                    "Materials/ConvergenceBreakerGallery/ConvergenceBreakerGalleryRouteDecal") != null &&
                                Resources.Load<Texture2D>("Environment/BreakerDistributionStatusPanel") != null &&
                                Resources.Load<Mesh>("Environment/BreakerDistributionStatusReadability") != null &&
                                Resources.Load<Material>(
                                    "Materials/ConvergenceBreakerGallery/BreakerDistributionStatus") != null &&
                                Resources.Load<Texture2D>("Environment/CoreProcessingStatusPanel") != null &&
                                Resources.Load<Mesh>("Environment/FurnaceForgeStatusReadability") != null &&
                                Resources.Load<Mesh>("Environment/QuenchStabilizationStatusReadability") != null &&
                                Resources.Load<Texture2D>("Environment/FurnaceQuenchHeroAtlas") != null &&
                                Resources.Load<Mesh>("Environment/ArcFurnaceHeroFinish") != null &&
                                Resources.Load<Mesh>("Environment/QuenchLoopHeroFinish") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Convergence Chamber Region/" +
                                    "Arc Furnace Region/Arc Furnace Hero Finish") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Convergence Chamber Region/" +
                                    "Arc Furnace Region/Quench Loop Region/Quench Loop Hero Finish") != null &&
                                Resources.Load<Material>(
                                    "Materials/CoreProcessingReadability/CoreProcessingStatus") != null &&
                                Resources.Load<Texture2D>(
                                    "Environment/SecurityTrialCommitmentStatusPanel") != null &&
                                Resources.Load<Mesh>(
                                    "Environment/SecurityTrialCommitmentStatusReadability") != null &&
                                Resources.Load<Texture2D>("Environment/SecurityTrialHeroAtlas") != null &&
                                Resources.Load<Mesh>("Environment/SecurityTrialCommitmentHeroFinish") != null &&
                                Resources.Load<Mesh>("Environment/SecurityTrialLockdownHeroFinish") != null &&
                                Resources.Load<Mesh>("Environment/SecurityTrialVaultHeroFinish") != null &&
                                Resources.Load<Texture2D>("Environment/DepartureDockHeroAtlas") != null &&
                                Resources.Load<Mesh>("Environment/DepartureChannelHeroFinish") != null &&
                                Resources.Load<Mesh>("Environment/ExtractionDockHeroFinish") != null &&
                                Resources.Load<Material>(
                                    "Materials/SecurityTrialReadability/SecurityTrialCommitmentStatus") != null &&
                                Resources.Load<Texture2D>(
                                    "Environment/SecurityTrialLockdownStatusAtlas") != null &&
                                Resources.Load<Mesh>(
                                    "Environment/SecurityTrialLockdownStatusReadability") != null &&
                                Resources.Load<Mesh>(
                                    "Environment/SecurityTrialDoorStatusReadability") != null &&
                                Resources.Load<Mesh>(
                                    "Environment/SecurityTrialCapacitorStatusReadability") != null &&
                                Resources.Load<Material>(
                                    "Materials/SecurityTrialReadability/SecurityTrialLockdownStatus") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Flux Bypass Region/Flux Shunt Regulator") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Flux Bypass Region/Flux Bypass Signal Lines") != null &&
                                Resources.Load<GameObject>("Environment/FluxBypassRegion") != null &&
                                Resources.Load<Texture2D>("Environment/FluxBypassRouteDecal") != null &&
                                Resources.Load<Material>("Materials/FluxBypass/FluxBypassRouteDecal") != null &&
                                Resources.Load<Texture2D>("Environment/InductionFluxHeroAtlas") != null &&
                                Resources.Load<Mesh>("Environment/InductionGalleryHeroFinish") != null &&
                                Resources.Load<Mesh>("Environment/FluxBypassHeroFinish") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Induction Gallery Hero Finish") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Flux Bypass Region/Flux Bypass Hero Finish") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Flux Bypass Region/Flux Shunt Route Status") != null &&
                                Resources.Load<Mesh>("Environment/FluxShuntGlyphReadability") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Convergence Chamber Region/" +
                                    "Arc Furnace Region/Arc Furnace Assembly") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Convergence Chamber Region/" +
                                    "Arc Furnace Region/Arc Furnace Salvage Socket") != null &&
                                Resources.Load<GameObject>("Environment/ArcFurnaceRegion") != null &&
                                Resources.Load<GameObject>("Environment/ArcFurnace") != null &&
                                Resources.Load<Texture2D>("Environment/ArcFurnaceRouteDecal") != null &&
                                Resources.Load<Material>("Materials/ArcFurnace/ArcFurnaceRouteDecal") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Convergence Chamber Region/" +
                                    "Arc Furnace Region/Quench Loop Region/Quench Condenser Assembly") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Convergence Chamber Region/" +
                                    "Arc Furnace Region/Quench Loop Region/Quench Loop Signal Lines") != null &&
                                Resources.Load<GameObject>("Environment/QuenchLoopRegion") != null &&
                                Resources.Load<GameObject>("Environment/QuenchCondenser") != null &&
                                Resources.Load<Texture2D>("Environment/QuenchLoopRouteDecal") != null &&
                                Resources.Load<Material>("Materials/QuenchLoop/QuenchLoopRouteDecal") != null &&
                                Resources.Load<Texture2D>("Environment/QuenchCacheReturnDecal") != null &&
                                Resources.Load<Material>("Materials/QuenchLoop/QuenchCacheReturnDecal") != null &&
                                weaponDecalReady &&
                                weaponTextureReady &&
                                weaponMaterialReady &&
                                lockdownTextureReady &&
                                lockdownMaterialReady &&
                                lockdownDecalsReady &&
                                departureCapacitorReady &&
                                departureChannelReady &&
                                departureAlbedoReady &&
                                departureReturnTextureReady &&
                                departureReturnMaterialReady &&
                                departureSurgeTextureReady &&
                                departureSurgeMaterialReady &&
                                combatLabTextureReady &&
                                combatLabMaterialReady &&
                                combatLabAnchorsReady &&
                                swarmerPrefabReady &&
                                swarmerAlbedoReady &&
                                swarmerTuningReady &&
                                withdrawalPursuitReady &&
                                game.HasSwarmerAssets &&
                                stationBackdropTextureReady &&
                                stationBackdropMaterialReady &&
                                stationBackdropPrefabReady &&
                                stationBackdropSceneReady &&
                                actOneCompositionPrefabReady &&
                                actOneCompositionSceneReady &&
                                actThreeCompositionPrefabReady &&
                                actThreeCompositionSceneReady &&
                                securityTrialCompositionPrefabReady &&
                                securityTrialCompositionSceneReady &&
                                withdrawalDockCompositionPrefabReady &&
                                withdrawalDockCompositionSceneReady &&
                                foregroundCutawayTextureReady &&
                                foregroundCutawayMaterialReady &&
                                authoredCutawayTextureReady &&
                                authoredCutawayMaterialReady &&
                                wideCutawayTextureReady &&
                                wideCutawayMaterialReady &&
                                authoredCutawayBindingCount >= 9 &&
                                Resources.Load<GameObject>("Environment/SignalSpineInlay") != null &&
                                Resources.Load<GameObject>("Environment/OpeningSignalSpine") != null &&
                                Resources.Load<Texture2D>("Environment/SignalSpineInlay") != null &&
                                Resources.Load<Material>("Materials/SignalSpineInlay") != null &&
                                Resources.Load<GameObject>("Environment/SignalBoundaryThreshold") != null &&
                                Resources.Load<Texture2D>("Environment/SignalBoundaryThreshold") != null &&
                                Resources.Load<Material>("Materials/SignalBoundaryThreshold") != null &&
                                Resources.Load<GameObject>("Environment/CoolantBaffle") != null &&
                                Resources.Load<GameObject>("Environment/SoutheastCoolantGauntlet") != null &&
                                Resources.Load<Texture2D>("Environment/CoolantGauntletAlbedo") != null &&
                                Resources.Load<Texture2D>("Environment/CoolantReclamationHeroAtlas") != null &&
                                Resources.Load<Mesh>("Environment/CoolantReclamationHeroFinish") != null &&
                                Resources.Load<GameObject>("Environment/RelayBank") != null &&
                                Resources.Load<GameObject>("Environment/NorthwestRelayFork") != null &&
                                Resources.Load<Texture2D>("Environment/RelayForkAlbedo") != null &&
                                Resources.Load<Texture2D>("Environment/RelayForkStatusPanel") != null &&
                                Resources.Load<Mesh>("Environment/RelayForkConsoleReadability") != null &&
                                Resources.Load<Mesh>("Environment/RelayForkPanelReadability") != null &&
                                Resources.Load<Mesh>("Environment/RelayForkSelectorReadability") != null &&
                                Resources.Load<Texture2D>("Environment/RelayTransferHeroAtlas") != null &&
                                Resources.Load<Mesh>("Environment/RelayForkHeroFinish") != null &&
                                Resources.Load<Mesh>("Environment/TransferVaultHeroFinish") != null &&
                                Resources.Load<Mesh>("Environment/RouteDoorThresholdReadability") != null &&
                                Resources.Load<Material>("Materials/RelayForkStatus") != null &&
                                Resources.Load<Material>("Materials/RelayTransferFinish/RelayTransferGraphite") != null &&
                                Resources.Load<Material>("Materials/RelayTransferFinish/RelayTransferCeramic") != null &&
                                Resources.Load<Material>("Materials/RelayTransferFinish/RelayTransferCopper") != null &&
                                Resources.Load<Material>("Materials/RelayTransferFinish/RelayTransferDeck") != null &&
                                Resources.Load<Material>("Materials/RouteDoorThresholdStatus") != null &&
                                Resources.Load<GameObject>("Environment/SecurityBlastShield") != null &&
                                Resources.Load<GameObject>("Environment/SecurityBayRouteMarkerModel") != null &&
                                Resources.Load<GameObject>("Environment/SecurityBayRouteMarker") != null &&
                                Resources.Load<GameObject>("Environment/WardenStagingBay") != null &&
                                Resources.Load<Texture2D>("Environment/WardenBayAlbedo") != null &&
                                Resources.Load<Mesh>("Environment/WardenBayHeroFinish") != null &&
                                Resources.Load<GameObject>("Environment/SapperSiphonPylon") != null &&
                                Resources.Load<GameObject>("Environment/SignalSapperCradle") != null &&
                                Resources.Load<Texture2D>("Environment/SapperCradleAlbedo") != null &&
                                Resources.Load<Mesh>("Environment/SapperCradleHeroFinish") != null &&
                                Resources.Load<Material>(
                                    "Materials/WithdrawalLandmarkFinish/WardenContainmentArmor") != null &&
                                Resources.Load<Material>(
                                    "Materials/WithdrawalLandmarkFinish/SapperCradleConduit") != null &&
                                Resources.Load<GameObject>("Actors/SignalSapperAssembly") != null &&
                                Resources.Load<GameObject>("Actors/SignalSapperModel") != null &&
                                Resources.Load<Texture2D>("Actors/SignalSapperArmorAlbedo") != null &&
                                Resources.Load<Material>("Materials/SignalSapperArmor") != null &&
                                Resources.Load<Material>("Materials/SignalSapperFork") != null &&
                                Resources.Load<Material>("Materials/SignalSapperCore") != null &&
                                game.HasSignalBoltAssets &&
                                Resources.Load<GameObject>("Projectiles/SignalBoltModel") != null &&
                                Resources.Load<Texture2D>("Projectiles/SignalBoltAlbedo") != null &&
                                Resources.Load<Material>("Materials/SignalBoltShell") != null &&
                                Resources.Load<Material>("Materials/SignalBoltEnergy") != null &&
                                Resources.Load<Texture2D>("Projectiles/SignalBoltTrail") != null &&
                                Resources.Load<Material>("Materials/SignalBoltTrail") != null &&
                                Resources.Load<Texture2D>("Projectiles/SignalBoltBulkheadImpact") != null &&
                                Resources.Load<Material>("Materials/SignalBoltBulkheadImpact") != null &&
                                game.HasSignalBoltBulkheadImpact &&
                                Resources.Load<SignalBoltPresentationTuning>("Tuning/SignalBoltPresentationTuning") != null &&
                                Resources.Load<Texture2D>("VFX/SapperDrainGlyph") != null &&
                                Resources.Load<Texture2D>("VFX/SapperTetherFlow") != null &&
                                Resources.Load<SignalSapperTelegraphTuning>("Tuning/SignalSapperTelegraphTuning") != null &&
                                game.HasLowSignalWarningTexture &&
                                game.HasDirectionalDamageIndicator &&
                                Resources.Load<ScreenFeedbackTuning>("Tuning/ScreenFeedbackTuning") != null &&
                                game.HasTowerActivationSweepTexture &&
                                game.HasStationStateFeedbackTexture &&
                                game.StationStateFeedbackPoolSize == 4 &&
                                Resources.Load<StationStateFeedbackTuning>("Tuning/StationStateFeedbackTuning") != null &&
                                game.HasWeaponTransformationFeedbackTextures &&
                                game.WeaponTransformationFeedbackPoolSize == 2 &&
                                Resources.Load<WeaponTransformationFeedbackTuning>(
                                    "Tuning/WeaponTransformationFeedbackTuning") != null &&
                                game.HasExtractionOutcomeFeedbackTexture &&
                                game.ExtractionOutcomeFeedbackPoolSize == 2 &&
                                Resources.Load<ExtractionOutcomeFeedbackTuning>(
                                    "Tuning/ExtractionOutcomeFeedbackTuning") != null &&
                                game.HasBindingMatrixIcon &&
                                Resources.Load<Texture2D>("UI/BindingMatrixIcon") != null &&
                                game.HasBindingConflictIcon &&
                                Resources.Load<Texture2D>("UI/BindingConflictIcon") != null &&
                                game.HasMovementRoutingIcon &&
                                Resources.Load<Texture2D>("UI/MovementRoutingIcon") != null &&
                                game.HasControlGlyphSet &&
                                Resources.Load<Texture2D>("UI/MovementControlGlyph") != null &&
                                Resources.Load<Texture2D>("UI/AimControlGlyph") != null &&
                                Resources.Load<Texture2D>("UI/FireControlGlyph") != null &&
                                Resources.Load<Texture2D>("UI/UseControlGlyph") != null &&
                                Resources.Load<Texture2D>("UI/SystemControlGlyph") != null &&
                                game.HasSignalReserveArt &&
                                Resources.Load<Sprite>("UI/SignalReserveConduit") != null &&
                                game.HasRunDebriefArt &&
                                Resources.Load<Texture2D>("UI/RunDebriefInsignia") != null &&
                                Resources.Load<SignalHudTuning>("Tuning/SignalHudTuning") != null &&
                                Resources.Load<ThreatBalanceTuning>("Tuning/ThreatBalanceTuning") != null &&
                                Resources.Load<SignalOverclockTuning>("Tuning/SignalOverclockTuning") != null &&
                                game.HasSignalRecoveryBurst &&
                                Resources.Load<Texture2D>("VFX/SignalRecoveryBurst") != null &&
                                game.HasSalvageChainBurst &&
                                Resources.Load<Texture2D>("VFX/SalvageChainBurst") != null;
            if (!runtimeReady)
            {
                Debug.LogError("[DEAD SIGNAL STANDALONE SMOKE] FAIL | Runtime composition is incomplete.");
                UnityEngine.Application.Quit(2);
                yield break;
            }

            Debug.Log($"{PASS_MARKER} | Runtime composition and core Resources loaded.");
            UnityEngine.Application.Quit(0);
        }
    }
}
