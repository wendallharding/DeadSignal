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
            var missionObjectivesReady = missionObjectives != null && missionObjectives.ObjectiveCount == 15;
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
            var inductionLatticeObjective =
                FindFirstObjectByType<AuthoredInductionLatticeObjective>(FindObjectsInactive.Include);
            var fluxShuntObjective =
                FindFirstObjectByType<AuthoredFluxShuntObjective>(FindObjectsInactive.Include);
            var centralTransferReady = relayForkObjective != null && relayForkObjective.IsConfigured &&
                                       transferVaultObjective != null && transferVaultObjective.IsConfigured &&
                                       centralInstallationObjective != null && centralInstallationObjective.IsConfigured &&
                                       transferVaultObjective.IsRouteConfigured && !transferVaultObjective.IsRelayRouteOpen;
            var relayPayloadReady = relayPayloadObjective != null && relayPayloadObjective.IsConfigured;
            var spineVentingReady = spineVentingObjective != null && spineVentingObjective.IsConfigured &&
                                    game != null && !game.IsSpineBerthVented;
            var inductionLatticeReady = inductionLatticeObjective != null && inductionLatticeObjective.IsConfigured &&
                                        game != null && !game.IsInductionLatticeCharged;
            var fluxShuntReady = fluxShuntObjective != null && fluxShuntObjective.IsConfigured &&
                                 game != null && !game.IsFluxShuntRouted;
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
            var swarmerTuningReady = Resources.Load<SwarmerPressureTuning>("Tuning/SwarmerPressureTuning") != null;
            var securityTrialPrefabReady =
                Resources.Load<GameObject>("Environment/SecurityTrialWingRegion") != null;
            var securityTrialSceneReady = game != null && game.HasAuthoredCombatChamber;
            var stationBackdropTextureReady =
                Resources.Load<Texture2D>("Environment/StationUnderdeckAlbedo") != null;
            var stationBackdropMaterialReady = Resources.Load<Material>("Materials/StationUnderdeck") != null;
            var stationBackdropPrefabReady = Resources.Load<GameObject>("Environment/StationUnderdeckBackdrop") != null;
            var stationBackdropSceneReady = FindFirstObjectByType<AuthoredStationBackdrop>() != null;
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
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] FOREGROUND CUTAWAY | " +
                      $"runtimeEnabled={game?.HasForegroundOcclusion ?? false} texture={foregroundCutawayTextureReady} " +
                      $"material={foregroundCutawayMaterialReady} authoredTexture={authoredCutawayTextureReady} " +
                      $"authoredMaterial={authoredCutawayMaterialReady} wideTexture={wideCutawayTextureReady} " +
                      $"wideMaterial={wideCutawayMaterialReady} bindings={authoredCutawayBindingCount}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] MISSION OBJECTIVES | " +
                      $"resource={missionObjectives != null} objectives={missionObjectives?.ObjectiveCount ?? 0} " +
                      $"consumers={objectiveConsumersReady} current={game?.CurrentMissionObjectiveId}");
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
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] INDUCTION LATTICE | " +
                      $"configured={inductionLatticeObjective?.IsConfigured ?? false} " +
                      $"charged={game?.IsInductionLatticeCharged ?? false}");
            Debug.Log($"[DEAD SIGNAL STANDALONE SMOKE] FLUX SHUNT | " +
                      $"configured={fluxShuntObjective?.IsConfigured ?? false} " +
                      $"routed={game?.IsFluxShuntRouted ?? false}");
            var runtimeReady = game != null &&
                                missionObjectivesReady &&
                                objectiveConsumersReady &&
                                cargoAnnexReady &&
                                coolantReady &&
                                centralTransferReady &&
                                relayPayloadReady &&
                                spineVentingReady &&
                                inductionLatticeReady &&
                                fluxShuntReady &&
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
                                game.SalvageCacheInstanceCount == RunModel.SalvageRequired * 2 &&
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
                                game.SecurityInterceptorPartCount == 4 &&
                                game.AuthoredInterceptorEntranceCount == 9 &&
                                Resources.Load<GameObject>("Actors/SecurityInterceptorAssembly") != null &&
                                game.HasSecuritySuppressorAssets &&
                                game.SecuritySuppressorPartCount == 4 &&
                                Resources.Load<GameObject>("Actors/SecuritySuppressorAssembly") != null &&
                                game.HasSuppressorFieldTexture &&
                                Resources.Load<Texture2D>("VFX/SuppressorFieldActive") != null &&
                                Resources.Load<GameObject>("Environment/InterceptorEntryGate") != null &&
                                game.AuthoredMapObstacleCount == EXPECTED_AUTHORED_OBSTACLE_COUNT &&
                                securityTrialSceneReady && securityTrialPrefabReady &&
                                Resources.Load<GameObject>("Environment/CoolantManifoldAssembly") != null &&
                                Resources.Load<GameObject>("Environment/TowerApproachJunction") != null &&
                                Resources.Load<Texture2D>("Environment/CoolantManifoldAlbedo") != null &&
                                Resources.Load<GameObject>("Environment/SalvageAnnexBarrier") != null &&
                                Resources.Load<GameObject>("Environment/SalvageAnnex") != null &&
                                Resources.Load<Texture2D>("Environment/SalvageAnnexAlbedo") != null &&
                                Resources.Load<GameObject>("Environment/EastSalvageVaultModel") != null &&
                                Resources.Load<GameObject>("Environment/EastSalvageVault") != null &&
                                Resources.Load<Texture2D>("Environment/EastSalvageVaultAlbedo") != null &&
                                game.transform.Find("Relay Foundry Region/Relay Tower Assembly") != null &&
                                game.transform.Find("Relay Foundry Region/Relay Induction Turbine") != null &&
                                Resources.Load<GameObject>("Environment/RelayFoundryRegion") != null &&
                                Resources.Load<GameObject>("Environment/RelayFoundryTurbineModel") != null &&
                                Resources.Load<Texture2D>("Environment/RelayFoundryTurbineAlbedo") != null &&
                                Resources.Load<Texture2D>("Environment/RelayFoundryRouteDecal") != null &&
                                game.transform.Find(
                                    "Relay Foundry Region/Relay Cooling Gantry Region/Relay Heat Exchanger") != null &&
                                game.transform.Find(
                                    "Relay Foundry Region/Relay Cooling Gantry Region/Cooling Gantry Reinforcement Gate") != null &&
                                game.transform.Find("Relay Foundry Region/Relay Payload Calibration Anchor") != null &&
                                game.transform.Find(
                                    "Relay Foundry Region/Relay Cooling Gantry Region/" +
                                    "Cooling Gantry Relay Payload Socket") != null &&
                                Resources.Load<GameObject>("Environment/RelayCoolingGantryRegion") != null &&
                                Resources.Load<GameObject>("Environment/RelayHeatExchanger") != null &&
                                Resources.Load<Texture2D>("Environment/RelayCoolingGantryRouteDecal") != null &&
                                Resources.Load<Material>(
                                    "Materials/RelayCoolingGantry/RelayCoolingGantryRouteDecal") != null &&
                                game.transform.Find("Capacitor Spine Region/Capacitor Transfer Bank") != null &&
                                game.transform.Find("Capacitor Spine Region/Third Tower Berth") != null &&
                                game.transform.Find("Capacitor Spine Region/Capacitor Spine Route Decal") != null &&
                                game.transform.Find("Capacitor Spine Region/Capacitor Spine Activation Decal") != null &&
                                game.transform.Find("Capacitor Spine Region/Capacitor Spine Return Decal") != null &&
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
                                game.transform.Find("Spine Induction Gallery Region/Induction Coil") != null &&
                                game.transform.Find("Spine Induction Gallery Region/Induction Gallery Signal Lines") != null &&
                                game.transform.Find("Spine Induction Gallery Region/Induction Gallery Route Decal") != null &&
                                Resources.Load<GameObject>("Environment/SpineInductionGalleryRegion") != null &&
                                Resources.Load<Texture2D>("Environment/SpineInductionGalleryRouteDecal") != null &&
                                Resources.Load<Material>(
                                    "Materials/SpineInductionGallery/SpineInductionGalleryRouteDecal") != null &&
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
                                Resources.Load<Texture2D>("Environment/ConvergenceBreakerGalleryRouteDecal") != null &&
                                Resources.Load<Material>(
                                    "Materials/ConvergenceBreakerGallery/ConvergenceBreakerGalleryRouteDecal") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Flux Bypass Region/Flux Shunt Regulator") != null &&
                                game.transform.Find(
                                    "Spine Induction Gallery Region/Flux Bypass Region/Flux Bypass Signal Lines") != null &&
                                Resources.Load<GameObject>("Environment/FluxBypassRegion") != null &&
                                Resources.Load<Texture2D>("Environment/FluxBypassRouteDecal") != null &&
                                Resources.Load<Material>("Materials/FluxBypass/FluxBypassRouteDecal") != null &&
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
                                swarmerTuningReady &&
                                game.HasSwarmerAssets &&
                                stationBackdropTextureReady &&
                                stationBackdropMaterialReady &&
                                stationBackdropPrefabReady &&
                                stationBackdropSceneReady &&
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
                                Resources.Load<GameObject>("Environment/RelayBank") != null &&
                                Resources.Load<GameObject>("Environment/NorthwestRelayFork") != null &&
                                Resources.Load<Texture2D>("Environment/RelayForkAlbedo") != null &&
                                Resources.Load<GameObject>("Environment/SecurityBlastShield") != null &&
                                Resources.Load<GameObject>("Environment/SecurityBayRouteMarkerModel") != null &&
                                Resources.Load<GameObject>("Environment/SecurityBayRouteMarker") != null &&
                                Resources.Load<GameObject>("Environment/WardenStagingBay") != null &&
                                Resources.Load<Texture2D>("Environment/WardenBayAlbedo") != null &&
                                Resources.Load<GameObject>("Environment/SapperSiphonPylon") != null &&
                                Resources.Load<GameObject>("Environment/SignalSapperCradle") != null &&
                                Resources.Load<Texture2D>("Environment/SapperCradleAlbedo") != null &&
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
                                game.HasTowerActivationSweepTexture &&
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
