using System;
using System.Collections;
using UnityEngine;

namespace DeadSignal
{
    /// <summary>
    /// Provides a command-line-only health check for the built player without affecting ordinary runs.
    /// </summary>
    public sealed class StandaloneBuildSmokeProbe : MonoBehaviour
    {
        public const string COMMAND_LINE_ARGUMENT = "-deadSignalBuildSmoke";
        public const string PASS_MARKER = "[DEAD SIGNAL STANDALONE SMOKE] PASS";

        private const int EXPECTED_AUTHORED_OBSTACLE_COUNT = 25;

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
            var runtimeReady = game != null &&
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
                                game.AuthoredSalvageSocketCount == 1 &&
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
                                game.AuthoredMapObstacleCount == EXPECTED_AUTHORED_OBSTACLE_COUNT &&
                                Resources.Load<GameObject>("Environment/CoolantManifoldAssembly") != null &&
                                Resources.Load<GameObject>("Environment/TowerApproachJunction") != null &&
                                Resources.Load<Texture2D>("Environment/CoolantManifoldAlbedo") != null &&
                                Resources.Load<GameObject>("Environment/SalvageAnnexBarrier") != null &&
                                Resources.Load<GameObject>("Environment/SalvageAnnex") != null &&
                                Resources.Load<Texture2D>("Environment/SalvageAnnexAlbedo") != null &&
                                Resources.Load<GameObject>("Environment/EastSalvageVaultModel") != null &&
                                Resources.Load<GameObject>("Environment/EastSalvageVault") != null &&
                                Resources.Load<Texture2D>("Environment/EastSalvageVaultAlbedo") != null &&
                                Resources.Load<GameObject>("Environment/DepartureCapacitor") != null &&
                                Resources.Load<GameObject>("Environment/ExtractionDepartureChannel") != null &&
                                Resources.Load<Texture2D>("Environment/DepartureCapacitorAlbedo") != null &&
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
                                Resources.Load<Texture2D>("UI/MovementRoutingIcon") != null;
            if (!runtimeReady)
            {
                Debug.LogError("[DEAD SIGNAL STANDALONE SMOKE] FAIL | Runtime composition is incomplete.");
                Application.Quit(2);
                yield break;
            }

            Debug.Log($"{PASS_MARKER} | Runtime composition and core Resources loaded.");
            Application.Quit(0);
        }
    }
}
