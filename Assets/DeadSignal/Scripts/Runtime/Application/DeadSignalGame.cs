using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reflex.Attributes;
using Reflex.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadSignal.Combat;
using DeadSignal.Diagnostics;
using DeadSignal.Missions;
using DeadSignal.Player;
using DeadSignal.Presentation;
using DeadSignal.Salvage;
using DeadSignal.World;

namespace DeadSignal.Application
{
    /// <summary>
    /// Coordinates one run while delegating input, world presentation, threats, salvage, and HUD ownership.
    /// </summary>
    public sealed class DeadSignalGame : MonoBehaviour
    {
        private const float PLAYER_COLLISION_RADIUS = 0.48f;
        private const float DASH_DISTANCE = 2.4f;
        private const float DASH_SIGNAL_COST = 4f;
        private const float DASH_COOLDOWN = 2.4f;
        private const float TOWER_INTERACTION_RADIUS = 2.2f;
        private const float DEPARTURE_SURGE_SIGNAL_RESTORE = 12f;
        private static readonly Vector3[] s_liveBalanceInterceptDirections =
        {
            Vector3.forward, Vector3.right, Vector3.back, Vector3.left,
            new Vector3(1f, 0f, 1f).normalized, new Vector3(1f, 0f, -1f).normalized,
            new Vector3(-1f, 0f, -1f).normalized, new Vector3(-1f, 0f, 1f).normalized
        };

        private RunModel m_model;
        private RunMetrics m_metrics;
        private DeadSignalWorld m_world;
        private DeadSignalThreatController m_threats;
        private DeadSignalSalvageController m_salvage;
        private IDeadSignalInput m_input;
        private IDeadSignalAudio m_audio;
        private ICombatFeedback m_combatFeedback;
        private IComfortSettings m_comfortSettings;
        private IDeadSignalHud m_hud;
        private IObjectiveBeacon m_objectiveBeacon;
        private ISignalDust m_signalDust;
        private SalvagePresentationTuning m_salvageTuning;
        private PlayerDroneMovementTuning m_playerMovementTuning;
        private PlayerDroneMovement m_playerMovement;
        private SignalOverclockChoice m_overclockChoice;
        private SignalOverclockTuning m_overclockTuning;
        private ExtractionUplink m_extractionUplink;
        private ILowSignalWarning m_lowSignalWarning;
        private ITowerActivationSweep m_towerActivationSweep;
        private MissionClarityHud m_missionClarityHud;
        private Container m_container;
        private Vector3 m_playerPresentationAcceleration;
        private bool m_lastPoweredState;
        private bool m_fireBuffered;
        private int m_focusInputBlockFrames;
        private int m_onboardingStep;
        private float m_routeGuidanceStrength = 0.7f;
        private float m_difficultyDrainMultiplier = 1f;
        private float m_dashCooldown;
        private float m_blockedFeedbackCooldown;
        private bool m_debugMenuOpen;
        private bool m_debugInfiniteSignal;
        private bool m_debugFireHeld;
        private bool m_debugRouteDriving;
        private DebugLocation m_debugRouteDestination;
        private DebugRouteSequencer m_debugRouteSequencer;
        private bool m_debugRouteReportWritten;
        private int m_lastDebugInputFrame = -1;
        private float m_debugRouteBlockedSeconds;
        private int m_debugObservedRecoveryCount;
        private int m_debugSalvageBeforeRouteAction;
        private bool m_debugOptionalBeforeRouteAction;
        private int m_debugObservedSequenceStep = -1;
        private Vector3 m_debugSequenceTarget;
        private bool m_debugCaptureEachRouteStep;
        private float m_debugSignalDeltaRate;
        private float m_debugPreviousSignal = -1f;
        private readonly Queue<string> m_debugEventLog = new();
        private DeadSignalDebugVisualization m_debugVisualization;
        private DeadSignalDebugCamera m_debugCamera;
        private string m_lastDebugCapturePath = "None";
        private AuthoredCombatScenario m_debugCombatScenario;
        private AuthoredCombatChamber m_combatChamber;
        private bool m_debugCombatScenarioIncludesSwarmers;
        private float m_debugCombatScenarioSeconds;
        private bool m_debugCombatScenarioActive;
        private DebugScenario m_debugTacticalWindowScenario = DebugScenario.FreshRun;
        private Vector2 m_debugTacticalWindowMoveInput;
        private bool m_debugTacticalWindowSweepActive;
        private bool m_debugTacticalWindowSweepPassed;
        private int m_debugTacticalWindowSweepSamples;
        private int m_debugTacticalWindowSweepUnsafeActorSamples;
        private float m_debugTacticalWindowSweepDistance;
        private float m_debugTacticalWindowSweepMaximumCoverage;
        private readonly LiveBalanceCombatPolicy m_liveBalanceCombatPolicy = new();
        private LiveBalanceCombatDecision m_liveBalanceCombatDecision;

        public float CurrentSignal => m_model?.Signal ?? 0f;
        public int CurrentSalvage => m_model?.Salvage ?? 0;
        public bool IsTowerOnline => m_model?.TowerOnline ?? false;
        public RunOutcome CurrentRunOutcome => m_model?.Outcome ?? RunOutcome.Destroyed;
        public bool IsRelayTowerOnline => m_model?.RelayTowerOnline ?? false;
        public Vector3 RelayTowerPosition => m_world?.RelayTowerPosition ?? Vector3.zero;
        public bool IsSpineTowerOnline => m_model?.SpineTowerOnline ?? false;
        public bool IsCentralPayloadSecured => m_model?.CentralPayloadSecured ?? false;
        public bool IsRelayPayloadSecured => m_model?.RelayPayloadSecured ?? false;
        public bool IsSpinePayloadSecured => m_model?.SpinePayloadSecured ?? false;
        public bool IsExtractionReady => m_model?.CanExtract ?? false;
        public bool IsDepartureSurgeConsumed => m_world?.IsDepartureSurgeConsumed ?? false;
        public MissionStage CurrentMissionStage => m_model?.CurrentMissionStage ?? MissionStage.CentralTower;
        public Vector3 SpineTowerPosition => m_world?.SpineTowerPosition ?? Vector3.zero;
        public bool IsWeaponEvolved => m_overclockChoice?.IsWeaponEvolved ?? false;
        public Vector3 SafestReinforcementEntryPosition => m_world == null
            ? Vector3.zero
            : m_world.GetReinforcementEntryPosition(
                SecurityReinforcement.Interceptor, m_world.GetSafestInterceptorEntryIndex(m_world.Player.position));
        public bool IsSapperLatched => m_threats?.IsSapperLatched ?? false;
        public bool IsWardenScreeningSapper => m_threats?.IsWardenScreeningSapper ?? false;
        public Vector3 WardenTacticalTarget => m_threats?.WardenTacticalTarget ?? Vector3.zero;
        public float SapperHealth => m_threats?.SapperHealth ?? 0f;
        public float WardenHealth => m_threats?.WardenHealth ?? 0f;
        public float InterceptorHealth => m_threats?.InterceptorHealth ?? 0f;
        public bool IsInterceptorCharging => m_threats?.IsInterceptorCharging ?? false;
        public bool IsInterceptorRecovering => m_threats?.IsInterceptorRecovering ?? false;
        public float InterceptorRecoverySecondsRemaining => m_threats?.InterceptorRecoverySecondsRemaining ?? 0f;
        public bool IsInterceptorCuttingSapperFlank => m_threats?.IsInterceptorCuttingSapperFlank ?? false;
        public Vector3 InterceptorCutoffTarget => m_threats?.InterceptorCutoffTarget ?? Vector3.zero;
        public float SuppressorHealth => m_threats?.SuppressorHealth ?? 0f;
        public bool IsSuppressorFieldActive => m_threats?.IsSuppressorFieldActive ?? false;
        public bool IsSuppressorFieldWarningActive => m_threats?.IsSuppressorFieldWarningActive ?? false;
        public Vector3 SuppressorFieldCenter => m_threats?.SuppressorFieldCenter ?? Vector3.zero;
        public bool IsPlayerSuppressed => m_threats?.IsPlayerSuppressed ?? false;
        public int SecurityEscalationTier => m_threats?.EscalationTier ?? 0;
        public int SecurityReinforcementsRemaining => m_threats?.ReinforcementsRemaining ?? 0;
        public bool IsDeadZoneSecurityTraceActive => m_threats?.IsDeadZoneTraceActive ?? false;
        public float DeadZoneSecurityTraceSecondsRemaining => m_threats?.DeadZoneTraceSecondsRemaining ?? 0f;
        public SecurityReinforcement PendingSecurityReinforcement =>
            m_threats?.PendingReinforcement ?? SecurityReinforcement.None;
        public float ReinforcementEntryCountdown => m_threats?.ReinforcementEntryCountdown ?? 0f;
        public bool IsReinforcementEntryBlocked => m_threats?.IsReinforcementEntryBlocked ?? false;
        public bool IsExtractionUplinkActive => m_extractionUplink?.IsActive ?? false;
        public float ExtractionUplinkSecondsRemaining => m_extractionUplink?.SecondsRemaining ?? 0f;
        public ExtractionUplinkMode CurrentExtractionUplinkMode => m_extractionUplink?.Mode ?? ExtractionUplinkMode.None;
        public float CurrentExtractionPurgeAcceleration => m_extractionUplink?.CurrentPurgeAcceleration ?? 0f;
        public bool LastSignalBoltBlockedByEnvironment => m_threats?.LastShotBlockedByEnvironment ?? false;
        public bool IsPaused => m_combatFeedback?.IsPaused ?? false;
        public bool HasPauseInsignia => m_hud?.HasPauseInsignia ?? false;
        public bool HasBindingMatrixIcon => m_hud?.HasBindingMatrixIcon ?? false;
        public bool HasBindingConflictIcon => m_hud?.HasBindingConflictIcon ?? false;
        public bool HasMovementRoutingIcon => m_hud?.HasMovementRoutingIcon ?? false;
        public bool HasControlGlyphSet => m_hud?.HasControlGlyphSet ?? false;
        public bool HasSignalReserveArt => m_hud?.HasSignalReserveArt ?? false;
        public bool HasRunDebriefArt => m_hud?.HasRunDebriefArt ?? false;
        public SignalReserveState CurrentSignalReserveState =>
            m_hud?.CurrentSignalReserveState ?? SignalReserveState.Critical;
        public bool HasCameraComfortIcon => m_hud?.HasCameraComfortIcon ?? false;
        public bool HasReducedFlashesIcon => m_hud?.HasReducedFlashesIcon ?? false;
        public bool HasHighContrastIcon => m_hud?.HasHighContrastIcon ?? false;
        public bool HasObjectiveBeaconIcon => m_objectiveBeacon?.HasIcon ?? false;
        public bool HasInputLinkIcon => m_hud?.HasInputLinkIcon ?? false;
        public bool HasAudioLinkIcon => m_hud?.HasAudioLinkIcon ?? false;
        public bool HasGeneratedAudio => m_audio?.HasGeneratedClips ?? false;
        public bool HasSignalDustTexture => m_signalDust?.HasTexture ?? false;
        public bool HasLowSignalWarningTexture => m_lowSignalWarning?.HasTexture ?? false;
        public bool HasMaintenanceDeckAssets => m_world?.HasMaintenanceDeckAssets ?? false;
        public int MaintenanceDeckModuleCount => m_world?.MaintenanceDeckModuleCount ?? 0;
        public bool HasMaintenanceRoomShellAssets => m_world?.HasMaintenanceRoomShellAssets ?? false;
        public int RoomShellBulkheadCount => m_world?.RoomShellBulkheadCount ?? 0;
        public int MachineSocketCount => m_world?.MachineSocketCount ?? 0;
        public bool HasSignalTowerAssets => m_world?.HasSignalTowerAssets ?? false;
        public int SignalTowerPartCount => m_world?.SignalTowerPartCount ?? 0;
        public bool HasExtractionPadAssets => m_world?.HasExtractionPadAssets ?? false;
        public int ExtractionPadPartCount => m_world?.ExtractionPadPartCount ?? 0;
        public bool HasShortcutGateAssets => m_world?.HasShortcutGateAssets ?? false;
        public int ShortcutGatePartCount => m_world?.ShortcutGatePartCount ?? 0;
        public bool HasSignalRoutingAssets => m_world?.HasSignalRoutingAssets ?? false;
        public int SignalRoutingPartCount => m_world?.SignalRoutingPartCount ?? 0;
        public bool HasStationMachineAssets => m_world?.HasStationMachineAssets ?? false;
        public int StationMachineInstanceCount => m_world?.StationMachineInstanceCount ?? 0;
        public int StationMachinePartCount => m_world?.StationMachinePartCount ?? 0;
        public bool HasSalvageCacheAssets => m_world?.HasSalvageCacheAssets ?? false;
        public int SalvageCacheInstanceCount => m_world?.SalvageCacheInstanceCount ?? 0;
        public int SalvageCachePartCount => m_world?.SalvageCachePartCount ?? 0;
        public bool HasPlayerDroneAssets => m_world?.HasPlayerDroneAssets ?? false;
        public int PlayerDronePartCount => m_world?.PlayerDronePartCount ?? 0;
        public bool HasSignalBoltAssets => m_world?.HasSignalBoltAssets ?? false;
        public bool LastSignalBoltUsedAuthoredPrefab => m_world?.LastSignalBoltUsedAuthoredPrefab ?? false;
        public bool HasWardenWarningTexture => m_world?.WardenTelegraph?.HasTexture ?? false;
        public bool IsWardenWarningVisible => m_world?.WardenTelegraph?.IsWarningVisible ?? false;
        public bool IsWardenWarningMotionSuppressed => m_world?.WardenTelegraph?.IsMotionSuppressed ?? false;
        public bool HasSignalSapperAssets => m_world?.HasSignalSapperAssets ?? false;
        public int SignalSapperPartCount => m_world?.SignalSapperPartCount ?? 0;
        public bool HasSecurityInterceptorAssets => m_world?.HasSecurityInterceptorAssets ?? false;
        public int SecurityInterceptorPartCount => m_world?.SecurityInterceptorPartCount ?? 0;
        public bool HasSecuritySuppressorAssets => m_world?.HasSecuritySuppressorAssets ?? false;
        public int SecuritySuppressorPartCount => m_world?.SecuritySuppressorPartCount ?? 0;
        public bool HasSuppressorFieldTexture => m_world?.SuppressorFieldTelegraph?.HasTexture ?? false;
        public int AuthoredInterceptorEntranceCount => m_world?.AuthoredInterceptorEntranceCount ?? 0;
        public int AuthoredMapObstacleCount => m_world?.AuthoredMapObstacleCount ?? 0;
        public int AuthoredSalvageSocketCount => m_world?.AuthoredSalvageSocketCount ?? 0;
        public bool HasPlayerCameraTuning => m_world?.HasPlayerCameraTuning ?? false;
        public bool IsPlayerCameraFollowing => m_world?.PlayerCamera?.IsConfigured ?? false;
        public float LowSignalWarningIntensity => m_lowSignalWarning?.CurrentIntensity ?? 0f;
        public bool IsSignalDustPowered => m_signalDust?.IsPowered ?? false;
        public int SignalDustMaximumParticles => m_signalDust?.MaximumParticles ?? 0;
        public float SignalDustEmissionRate => m_signalDust?.EmissionRate ?? 0f;
        public bool HasTowerActivationSweepTexture => m_towerActivationSweep?.HasTexture ?? false;
        public bool IsTowerActivationSweepPlaying => m_towerActivationSweep?.IsPlaying ?? false;
        public float TowerActivationSweepAlpha => m_towerActivationSweep?.CurrentAlpha ?? 0f;
        public float TowerActivationSweepDiameter => m_towerActivationSweep?.CurrentDiameter ?? 0f;
        public float TowerActivationSweepMaximumDiameter => m_towerActivationSweep?.MaximumDiameter ?? 0f;
        public bool IsAudioEnabled => m_comfortSettings?.AudioEnabled ?? true;
        public InputPromptDevice ActiveInputPromptDevice => m_input?.ActivePromptDevice ?? InputPromptDevice.KeyboardMouse;
        public string FireKeyboardBinding => m_input?.FireKeyboardBinding ?? string.Empty;
        public string InteractKeyboardBinding => m_input?.InteractKeyboardBinding ?? string.Empty;
        public string MoveUpKeyboardBinding => m_input?.MoveUpKeyboardBinding ?? string.Empty;
        public string MoveDownKeyboardBinding => m_input?.MoveDownKeyboardBinding ?? string.Empty;
        public string MoveLeftKeyboardBinding => m_input?.MoveLeftKeyboardBinding ?? string.Empty;
        public string MoveRightKeyboardBinding => m_input?.MoveRightKeyboardBinding ?? string.Empty;
        public string RebindStatusMessage => m_input?.RebindStatusMessage ?? string.Empty;
        public ObjectiveBeaconPhase CurrentObjectiveBeaconPhase => m_objectiveBeacon?.CurrentPhase ?? ObjectiveBeaconPhase.Tower;
        public Vector3 CurrentObjectiveBeaconTarget => m_objectiveBeacon?.CurrentTarget ?? Vector3.zero;
        public int CurrentMissionPhase => m_hud?.CurrentMissionPhase ?? 0;
        public string CurrentMissionObjective => m_hud?.CurrentMissionObjective ?? string.Empty;
        public bool IsCameraImpulseEnabled => m_comfortSettings?.CameraImpulseEnabled ?? true;
        public bool IsReducedFlashesEnabled => m_comfortSettings?.ReducedFlashesEnabled ?? false;
        public bool IsHighContrastEnabled => m_comfortSettings?.HighContrastEnabled ?? false;
        public bool HasSalvagePresentationTuning => m_salvageTuning != null;
        public bool HasPlayerMovementTuning => m_playerMovementTuning != null;
        public bool HasPlayerSignalWake => m_world?.PlayerSignalWake?.HasTexture ?? false;
        public bool HasPlayerCombatPresentation => m_world?.PlayerCombatPresentation != null;
        public bool HasForegroundOcclusion => m_world?.ForegroundOcclusion != null;
        public bool HasSignalBoltBulkheadImpact => m_combatFeedback?.HasEnvironmentImpactTexture ?? false;
        public bool HasSignalRecoveryBurst => m_combatFeedback?.HasSignalRecoveryTexture ?? false;
        public bool HasSalvageChainBurst => m_combatFeedback?.HasSalvageChainTexture ?? false;
        public int CurrentSalvageChain => m_salvage?.ChainCount ?? 0;
        public float SalvageChainSecondsRemaining => m_salvage?.ChainSecondsRemaining ?? 0f;
        public bool IsOptionalSalvageAvailable => m_salvage?.IsOptionalCacheAvailable ?? false;
        public bool IsOptionalSalvageSecured => m_model?.OptionalSalvageSecured ?? false;
        public float OptionalSalvageSignalReward => m_salvage?.OptionalCacheSignalReward ?? 0f;
        public bool IsOverclockChoicePending => m_overclockChoice?.IsPending ?? false;
        public bool IsAuxiliaryOverclockChoicePending => m_overclockChoice?.IsAuxiliaryPending ?? false;
        public SignalOverclock SelectedOverclock => m_overclockChoice?.Selected ?? SignalOverclock.None;
        public SignalAuxiliaryOverclock SelectedAuxiliaryOverclock =>
            m_overclockChoice?.SelectedAuxiliary ?? SignalAuxiliaryOverclock.None;
        public bool IsWeaponOverclockChoicePending => m_overclockChoice?.IsWeaponPending ?? false;
        public SignalWeaponOverclock SelectedWeaponOverclock =>
            m_overclockChoice?.SelectedWeapon ?? SignalWeaponOverclock.None;
        public ExtractionSuppressionProfile CurrentExtractionSuppressionProfile =>
            m_threats?.CurrentExtractionSuppressionProfile ?? ExtractionSuppressionProfile.Standard;
        public bool IsEmergencyCapacitorAvailable => m_overclockChoice?.IsEmergencyCapacitorAvailable ?? false;
        public bool IsFeedbackShieldCharged => m_overclockChoice?.IsFeedbackShieldCharged ?? false;
        public bool IsChainArcOverloadReady => m_overclockChoice?.IsChainArcOverloadReady ?? false;
        public bool IsOverdriveSynergySurgeActive => m_overclockChoice?.IsOverdriveSurgeActive ?? false;
        public SignalOverclockSynergy CurrentOverclockSynergy => m_overclockChoice?.Synergy ?? SignalOverclockSynergy.None;
        public int ChainArcsPlayed => (m_combatFeedback as CombatFeedbackController)?.ChainArcsPlayed ?? 0;
        public float CurrentPlayerMaximumSpeed => m_playerMovementTuning == null
            ? 0f
            : m_playerMovementTuning.MaximumSpeed *
              (SelectedOverclock == SignalOverclock.OverdriveThrusters
                  ? m_overclockTuning.ThrusterSpeedMultiplier *
                    (m_overclockChoice.IsOverdriveSurgeActive ? m_overclockTuning.OverdriveSynergySpeedMultiplier : 1f)
                  : 1f);
        public int ThreatsPurged => m_metrics?.ThreatsPurged ?? 0;
        public float SignalRecovered => m_metrics?.SignalRecovered ?? 0f;
        public int ShotsFired => m_metrics?.ShotsFired ?? 0;
        public int ActiveSignalBoltCount => transform.Cast<Transform>().Count(child =>
            child.name == "Signal Bolt" && child.gameObject.activeInHierarchy);
        public int PiercingPulseFollowThroughs => m_threats?.PiercingPulseFollowThroughs ?? 0;
        public int ControlledRicochets => m_threats?.ControlledRicochets ?? 0;
        public bool HasSwarmerAssets => m_threats?.HasSwarmerAssets ?? false;
        public int ActiveSwarmerCount => m_threats?.ActiveSwarmerCount ?? 0;
        public int PeakSwarmerCount => m_threats?.PeakSwarmerCount ?? 0;
        public int SwarmersSpawned => m_threats?.SwarmersSpawned ?? 0;
        public int SwarmersPurged => m_threats?.SwarmersPurged ?? 0;
        public int SwarmerContacts => m_threats?.SwarmerContacts ?? 0;
        public CombatChamberState CurrentCombatChamberState =>
            m_combatChamber?.State ?? DeadSignal.World.CombatChamberState.Dormant;
        public int CombatChamberPhase => m_combatChamber?.Phase ?? 0;
        public bool HasAuthoredCombatChamber => m_combatChamber != null && m_combatChamber.IsComplete;
        public int PeakThreatConcurrency => m_metrics?.PeakThreatConcurrency ?? 0;
        public string DebugOverview =>
            $"DEBUG ACTIVE\n" +
            $"Run: {m_model?.Outcome}  //  Phase {CurrentMissionPhase}\n" +
            $"Signal: {CurrentSignal:0.0}  {CurrentSignalReserveState}\n" +
            $"Tower: {(m_model?.TowerOnline == true ? "ONLINE" : "DORMANT")}  Relay: {(IsRelayTowerOnline ? "ONLINE" : "DORMANT")}\n" +
            $"Salvage: {m_model?.Salvage ?? 0}/{RunModel.SalvageRequired}  Chain: {CurrentSalvageChain} ({SalvageChainSecondsRemaining:0.0}s)\n" +
            $"Threats: W {WardenHealth:0}  S {SapperHealth:0}  I {InterceptorHealth:0}  X {SuppressorHealth:0}\n" +
            $"Security: Tier {SecurityEscalationTier}  Reserve {SecurityReinforcementsRemaining}\n" +
            $"Upgrades: {SelectedOverclock} / {SelectedAuxiliaryOverclock} / {SelectedWeaponOverclock}\n" +
            $"Extraction: {CurrentExtractionUplinkMode} {ExtractionUplinkSecondsRemaining:0.0}s";
        public string DebugComposition =>
            $"Assets — deck:{HasMaintenanceDeckAssets}, shell:{HasMaintenanceRoomShellAssets}, tower:{HasSignalTowerAssets}, " +
            $"drone:{HasPlayerDroneAssets}, bolt:{HasSignalBoltAssets}\n" +
            $"Authored — obstacles:{AuthoredMapObstacleCount}, salvage sockets:{AuthoredSalvageSocketCount}, " +
            $"entries:{AuthoredInterceptorEntranceCount}\n" +
            $"Runtime — projectiles:{ActiveSignalBoltCount}, objective:{CurrentObjectiveBeaconPhase}, audio:{HasGeneratedAudio}";
        public string DebugTelemetry => m_world == null
            ? "Runtime unavailable"
            : $"Position {m_world.Player.position.x:0.00}, {m_world.Player.position.z:0.00}  " +
              $"Velocity {m_playerMovement.Velocity.magnitude:0.00}  FPS {(Time.unscaledDeltaTime > 0f ? 1f / Time.unscaledDeltaTime : 0f):0}\n" +
              $"Focus {(UnityEngine.Application.isFocused ? "OWNED" : "LOST")}  Input {(Time.frameCount - m_lastDebugInputFrame <= 1 ? "RECEIVED" : "IDLE")}  " +
              $"Time {Time.timeScale:0.00}x  Signal Δ {m_debugSignalDeltaRate:+0.0;-0.0;0.0}/s\nCamera {m_world.Camera.transform.position.x:0.0}, " +
              $"{m_world.Camera.transform.position.y:0.0}, {m_world.Camera.transform.position.z:0.0}  " +
              $"Route {(m_debugRouteDriving ? m_debugRouteDestination.ToString() : "MANUAL")}" +
              $"{(m_debugRouteBlockedSeconds > 0.1f ? $" BLOCKED {m_debugRouteBlockedSeconds:0.0}s" : string.Empty)}\n" +
              $"Interaction {_debugInteractionTelemetry()}\n" +
              $"NavMesh {m_world.NavMeshStatus}  Corners {m_world.GetRemainingNavMeshCorners(m_world.Player)}";
        public string DebugRouteSequenceStatus => m_debugRouteSequencer == null
            ? "ROUTE SEQUENCE  Unavailable"
            : $"ROUTE SEQUENCE  {m_debugRouteSequencer.State}  " +
              $"{m_debugRouteSequencer.StepNumber}/{m_debugRouteSequencer.StepCount}\n" +
              $"{m_debugRouteSequencer.CurrentStep?.Name ?? "No active step"}  " +
              $"{m_debugRouteSequencer.StepSeconds:0.0}s  Recoveries {m_debugRouteSequencer.RecoveryCount}";
        public string DebugRouteSequenceReport => m_debugRouteSequencer?.Report ?? "No route report available.";
        public DebugRouteRunState DebugRouteSequenceState => m_debugRouteSequencer?.State ?? DebugRouteRunState.Idle;
        public string DebugLastCapturePath => m_lastDebugCapturePath;
        public string DebugEventLog => string.Join("\n", m_debugEventLog);
        public string DebugReplayInfo =>
            $"Route seed {PlayerPrefs.GetInt("DeadSignal.RouteVariant", 0)}  Frame {Time.frameCount}  Capture {m_lastDebugCapturePath}";
        public Vector3 DebugPlayerPosition => m_world?.Player.position ?? Vector3.zero;
        public bool IsEasternCombatScenarioActive => m_debugCombatScenarioActive;
        public bool DebugCombatScenarioIncludesSwarmers => m_debugCombatScenarioIncludesSwarmers;
        public float DebugCombatScenarioSeconds => m_debugCombatScenarioSeconds;
        public int DebugCombatScenarioAttackCount => m_threats?.DebugScenarioAttackCount ?? 0;
        public bool AreDebugCombatActorsInSafeViewport => m_debugCombatScenarioActive &&
            _isInSafeViewport(m_world.Player) && _isInSafeViewport(m_world.Warden) &&
            _isInSafeViewport(m_world.Sapper) && _isInSafeViewport(m_world.Interceptor) &&
            _isInSafeViewport(m_world.Suppressor) && _areActiveSwarmersInSafeViewport();
        public bool AreTacticalWindowActorsInSafeViewport =>
            _isInSafeViewport(m_world?.Player) && _isInSafeViewport(m_world?.Sapper);
        public bool IsTacticalWindowSweepActive => m_debugTacticalWindowSweepActive;
        public bool DidTacticalWindowSweepPass => m_debugTacticalWindowSweepPassed;
        public int TacticalWindowSweepSamples => m_debugTacticalWindowSweepSamples;
        public int TacticalWindowSweepUnsafeActorSamples => m_debugTacticalWindowSweepUnsafeActorSamples;
        public float TacticalWindowSweepDistance => m_debugTacticalWindowSweepDistance;
        public float TacticalWindowSweepMaximumCoverage => m_debugTacticalWindowSweepMaximumCoverage;
        public string DebugCombatScenarioStatus => !m_debugCombatScenarioActive
            ? "COMBAT LAB  Inactive"
            : $"COMBAT LAB  {(m_debugCombatScenarioIncludesSwarmers ? "SWARMERS ON" : "SWARMERS OFF")}  " +
              $"{m_debugCombatScenarioSeconds:0.0}s  Signal {CurrentSignal:0.0}  " +
              $"Threats {_activeDebugThreatCount()}/{(m_debugCombatScenarioIncludesSwarmers ? 10 : 4)}  " +
              $"Attacks {DebugCombatScenarioAttackCount}/{(m_debugCombatScenarioIncludesSwarmers ? 5 : 4)}  " +
              $"Swarmers {ActiveSwarmerCount}/6 peak {PeakSwarmerCount}\n" +
              $"Viewport P:{_viewportState(m_world.Player)} W:{_viewportState(m_world.Warden)} " +
              $"S:{_viewportState(m_world.Sapper)} I:{_viewportState(m_world.Interceptor)} " +
              $"X:{_viewportState(m_world.Suppressor)}";
        public bool IsDebugRouteDriving => m_debugRouteDriving;
        public int DebugLiveBalanceEvasionResponses => m_liveBalanceCombatPolicy.EvasionResponses;
        public int DebugLiveBalanceDirectedShots => m_liveBalanceCombatPolicy.DirectedShots;
        public bool IsDebugMenuOpen => m_debugMenuOpen;
        public bool HasRuntimeNavMesh => m_world?.HasRuntimeNavMesh ?? false;
        public string DebugNavMeshStatus => m_world?.NavMeshStatus ?? "Unavailable";

        public static bool TryParseCombatLabScenario(string[] arguments, out DebugScenario scenario)
        {
            const string PREFIX = "-DEADSIGNALCOMBATLAB=";
            scenario = DebugScenario.FreshRun;
            if (arguments == null)
            {
                return false;
            }

            foreach (var argument in arguments)
            {
                if (argument == null || !argument.StartsWith(PREFIX, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var population = argument.Substring(PREFIX.Length);
                if (population.Equals("SwarmersOn", System.StringComparison.OrdinalIgnoreCase))
                {
                    scenario = DebugScenario.EasternRoomCombat;
                    return true;
                }
                if (population.Equals("SwarmersOff", System.StringComparison.OrdinalIgnoreCase))
                {
                    scenario = DebugScenario.EasternRoomCombatNoSwarmers;
                    return true;
                }
            }
            return false;
        }

        public static bool TryParseTacticalWindowScenario(string[] arguments, out DebugScenario scenario)
        {
            const string PREFIX = "-DEADSIGNALTACTICALWINDOW=";
            scenario = DebugScenario.FreshRun;
            if (arguments == null)
            {
                return false;
            }

            foreach (var argument in arguments)
            {
                if (argument == null || !argument.StartsWith(PREFIX, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var location = argument.Substring(PREFIX.Length);
                if (location.Equals("Opening", System.StringComparison.OrdinalIgnoreCase))
                {
                    scenario = DebugScenario.OpeningTacticalWindow;
                    return true;
                }
                if (location.Equals("SpineReturn", System.StringComparison.OrdinalIgnoreCase))
                {
                    scenario = DebugScenario.SpineReturnTacticalWindow;
                    return true;
                }
            }
            return false;
        }

        public static bool HasTacticalWindowCaptureArgument(string[] arguments)
        {
            const string ARGUMENT = "-DEADSIGNALTACTICALWINDOWCAPTURE";
            return arguments != null && arguments.Any(argument =>
                argument != null && argument.Equals(ARGUMENT, System.StringComparison.OrdinalIgnoreCase));
        }

        public static bool HasTacticalWindowSweepArgument(string[] arguments)
        {
            const string ARGUMENT = "-DEADSIGNALTACTICALWINDOWSWEEP";
            return arguments != null && arguments.Any(argument =>
                argument != null && argument.Equals(ARGUMENT, System.StringComparison.OrdinalIgnoreCase));
        }

        public float DebugDistanceToLocation(DebugLocation location)
        {
            return m_world == null
                ? float.PositiveInfinity
                : DeadSignalWorld.FlatDistance(m_world.Player.position, _debugLocationPosition(location));
        }

        public Vector3 DebugThreatPosition(SecurityReinforcement reinforcement)
        {
            return reinforcement switch
            {
                SecurityReinforcement.Warden => m_world?.Warden.position ?? Vector3.zero,
                SecurityReinforcement.Sapper => m_world?.Sapper.position ?? Vector3.zero,
                SecurityReinforcement.Interceptor => m_world?.Interceptor.position ?? Vector3.zero,
                SecurityReinforcement.Suppressor => m_world?.Suppressor.position ?? Vector3.zero,
                _ => Vector3.zero
            };
        }

        public void SetDebugMenuState(bool open, bool runWhileOpen)
        {
            if (!DeadSignalDebugMenu.IsAvailable)
            {
                return;
            }

            m_debugMenuOpen = open;
            m_hud?.SetDebugMenuVisible(open);
            if (m_missionClarityHud != null)
            {
                m_missionClarityHud.enabled = !open;
            }
            if (m_objectiveBeacon is MonoBehaviour objectiveBeacon)
            {
                objectiveBeacon.enabled = !open;
            }
            _setPaused(open && !runWhileOpen);
        }

        public void DebugConfirm(string message) => _showFeedback($"DEBUG — {message}");

        public void DebugSetSignal(float signal) => m_model?.SetSignalForDebug(signal);

        public void DebugSetInfiniteSignal(bool enabled)
        {
            m_debugInfiniteSignal = enabled;
            if (enabled)
            {
                m_model?.SetSignalForDebug(RunModel.MaximumSignal);
            }
        }

        public void DebugSetInvulnerable(bool enabled) => m_threats?.SetPlayerInvulnerableForDebug(enabled);

        public void DebugSetThreatsFrozen(bool frozen) => m_threats?.SetFrozenForDebug(frozen);

        public void DebugSetFireHeld(bool held) => m_debugFireHeld = held;

        public void DebugFireAt(Vector3 position)
        {
            if (m_world == null || m_threats == null || !m_threats.CanFire)
            {
                return;
            }

            var direction = position - m_world.Player.position;
            direction.y = 0f;
            m_threats.TryFire(direction);
        }

        public void DebugSetTimeScale(float scale)
        {
            scale = Mathf.Clamp(scale, 0f, 2f);
            m_combatFeedback.SetPaused(scale <= 0f);
            if (scale > 0f)
            {
                Time.timeScale = scale;
            }
        }

        public void DebugStepFrame() => StartCoroutine(_debugStepFrame());

        public void DebugDamageThreat(SecurityReinforcement reinforcement) => m_threats?.DamageForDebug(reinforcement);

        public void DebugForceThreatAttack(SecurityReinforcement reinforcement) => m_threats?.ForceAttackForDebug(reinforcement);

        public void DebugStartRouteDriver(DebugLocation destination)
        {
            m_debugRouteDestination = destination;
            m_debugRouteBlockedSeconds = 0f;
            m_debugRouteDriving = true;
        }

        public void DebugStopRouteDriver()
        {
            m_debugRouteDriving = false;
            m_debugRouteBlockedSeconds = 0f;
        }

        public void DebugStartRouteSequence(DebugRoutePreset preset, DebugAutomationMode mode, DebugAutomationProfile profile)
        {
            _resetDebugTransientState();
            m_liveBalanceCombatPolicy.Reset();
            m_liveBalanceCombatDecision = default;
            m_debugRouteReportWritten = false;
            m_debugRouteSequencer.Start(preset, mode, profile, CurrentSignal);
            m_debugObservedSequenceStep = -1;
            m_debugRouteDriving = m_debugRouteSequencer.IsRunning;
            _applyDebugAutomationProfile(profile);
            _showFeedback($"DEBUG ROUTE SEQUENCE — {preset.ToString().ToUpperInvariant()}");
        }

        public void DebugPauseRouteSequence()
        {
            m_debugRouteSequencer?.Pause();
            m_debugRouteDriving = false;
        }

        public void DebugResumeRouteSequence()
        {
            m_debugRouteSequencer?.Resume();
            m_debugRouteDriving = m_debugRouteSequencer?.IsRunning == true;
        }

        public void DebugSkipRouteStep() => m_debugRouteSequencer?.Skip();

        public void DebugRetryRouteStep()
        {
            m_debugRouteSequencer?.Retry();
            m_debugRouteDriving = m_debugRouteSequencer?.IsRunning == true;
        }

        public void DebugAbortRouteSequence()
        {
            m_debugRouteSequencer?.Abort("Aborted by operator.");
            m_debugRouteDriving = false;
            _writeDebugRouteReport();
        }

        public void DebugRecordRouteNode()
        {
            if (m_world != null)
            {
                m_debugRouteSequencer.Record(m_world.Player.position);
                _showFeedback($"DEBUG ROUTE NODE RECORDED — {m_world.Player.position.x:0.0}, {m_world.Player.position.z:0.0}");
            }
        }

        public void DebugClearRecordedRoute() => m_debugRouteSequencer?.ClearRecording();

        public void DebugCopyRouteReport() => GUIUtility.systemCopyBuffer = _finishDebugRouteReport();

        public void DebugSetRouteStepMode(bool enabled) => m_debugRouteSequencer.PauseAfterEachStep = enabled;

        public void DebugSetRouteCaptureEachStep(bool enabled) => m_debugCaptureEachRouteStep = enabled;

        public void DebugToggleWorldVisualization()
        {
            if (m_debugVisualization == null)
            {
                m_debugVisualization = gameObject.AddComponent<DeadSignalDebugVisualization>();
                m_debugVisualization.Configure(m_world.Camera, m_world.Player);
            }

            m_debugVisualization.SetVisible(!m_debugVisualization.IsVisible);
        }

        public void DebugToggleFreeCamera()
        {
            if (m_debugCamera == null)
            {
                m_debugCamera = m_world.Camera.gameObject.AddComponent<DeadSignalDebugCamera>();
                m_debugCamera.Configure(m_world.PlayerCamera);
            }

            m_debugCamera.SetFree(!m_debugCamera.IsFree);
        }

        public void DebugCameraOverview()
        {
            if (m_debugCamera == null)
            {
                m_debugCamera = m_world.Camera.gameObject.AddComponent<DeadSignalDebugCamera>();
                m_debugCamera.Configure(m_world.PlayerCamera);
            }

            m_debugCamera.ShowOverview();
        }

        public void DebugCaptureScreenshot()
        {
            _debugCaptureScreenshot(null);
        }

        public void DebugCaptureCombatSequence() => StartCoroutine(_debugCaptureCombatSequence());

        public void DebugStartTacticalWindowSweep()
        {
            if (m_debugTacticalWindowScenario is not (DebugScenario.OpeningTacticalWindow or
                DebugScenario.SpineReturnTacticalWindow) || m_debugTacticalWindowSweepActive)
            {
                return;
            }

            StartCoroutine(_captureTacticalWindowSweepSequence(m_debugTacticalWindowScenario, false, false));
        }

        public void DebugExerciseCombatFeedback()
        {
            DebugPlaySignalImpact();
            m_combatFeedback?.PlayShieldImpact(m_world.Player.position + Vector3.left);
            m_combatFeedback?.PlayEnvironmentImpact(m_world.Player.position + Vector3.right);
            DebugPlaySignalRecovery();
            DebugPlaySalvageChain();
        }

        public void DebugExerciseThreatTelegraphs()
        {
            foreach (var reinforcement in new[] { SecurityReinforcement.Warden, SecurityReinforcement.Sapper,
                         SecurityReinforcement.Interceptor, SecurityReinforcement.Suppressor })
            {
                DebugForceThreatAttack(reinforcement);
            }
            _showFeedback("DEBUG — ALL THREAT TELEGRAPHS ARMED");
        }

        public void DebugVisitCameraBoundaries() => StartCoroutine(_debugVisitCameraBoundaries());

        public void DebugCopyReplayData()
        {
            GUIUtility.systemCopyBuffer = $"{DebugReplayInfo}\n{DebugTelemetry}\n{DebugEventLog}";
        }

        public void DebugResetDashCooldown() => m_dashCooldown = 0f;

        public bool DebugIsPoweredAt(Vector3 position) => m_world != null && m_world.IsPowered(
            position, m_model.TowerOnline, m_model.RelayTowerOnline, m_model.SpineTowerOnline);

        public void DebugActivateTower()
        {
            if (m_model == null || m_model.TowerOnline)
            {
                return;
            }

            if (m_model.Signal < RunModel.TowerCost)
            {
                m_model.SetSignalForDebug(RunModel.TowerCost);
            }

            if (m_model.TryActivateTower())
            {
                m_world.ActivateTower(m_threats.SapperPulseInterval);
                m_towerActivationSweep.Play();
                _showFeedback("DEBUG — CENTRAL TOWER ACTIVATED");
            }
        }

        public void DebugActivateRelayTower()
        {
            DebugActivateTower();
            if (!m_model.CentralPayloadSecured)
            {
                DebugCollectNextCache();
            }
            if (m_model.RelayTowerOnline)
            {
                return;
            }

            m_model.SetSignalForDebug(Mathf.Max(m_model.Signal, RunModel.RelayTowerCost + 1f));
            if (m_model.TryActivateRelayTower())
            {
                m_world.ActivateRelayTower();
                m_overclockChoice.NotifyRelayActivated();
                _showFeedback("DEBUG — RELAY TOWER ACTIVATED");
            }
        }

        public void DebugActivateSpineTower()
        {
            DebugActivateRelayTower();
            if (!m_model.RelayPayloadSecured)
            {
                DebugCollectNextCache();
            }
            if (m_overclockChoice.SelectedWeapon == SignalWeaponOverclock.None)
            {
                DebugSelectWeapon(SignalWeaponOverclock.PiercingPulse);
            }
            if (m_model.SpineTowerOnline)
            {
                return;
            }

            m_model.SetSignalForDebug(Mathf.Max(m_model.Signal, RunModel.SpineTowerCost + 1f));
            if (m_model.TryActivateSpineTower())
            {
                m_world.ActivateSpineTower();
                m_overclockChoice.NotifySpineActivated();
                _showFeedback("DEBUG — SPINE TOWER ACTIVATED");
            }
        }

        public void DebugOpenShortcut()
        {
            DebugActivateTower();
            m_model.SetSignalForDebug(Mathf.Max(m_model.Signal, RunModel.ShortcutCost + 1f));
            if (m_model.TryOpenShortcut())
            {
                m_world.OpenShortcut();
            }
        }

        public void DebugTeleport(DebugLocation location)
        {
            if (m_world?.Player == null)
            {
                return;
            }

            m_world.Player.position = _debugLocationPosition(location);
            m_playerMovement = new PlayerDroneMovement();
            _showFeedback($"DEBUG TELEPORT — {location.ToString().ToUpperInvariant()}");
        }

        public void DebugCollectNextCache()
        {
            if (m_model == null || m_model.Outcome != RunOutcome.Running)
            {
                return;
            }

            foreach (var pickup in m_world.SalvagePickups)
            {
                if (!pickup.activeSelf || m_world.IsOptionalCache(pickup) ||
                    !m_model.CanCollectPayload(m_world.GetPayloadRegion(pickup)))
                {
                    continue;
                }

                m_world.Player.position = pickup.transform.position;
                m_salvage.Tick(0f);
                return;
            }

            if (!m_model.CanExtract || m_model.OptionalSalvageSecured)
            {
                return;
            }

            foreach (var pickup in m_world.SalvagePickups)
            {
                if (!pickup.activeSelf || !m_world.IsOptionalCache(pickup))
                {
                    continue;
                }

                m_world.Player.position = pickup.transform.position;
                m_salvage.Tick(0f);
                return;
            }
        }

        public void DebugMakeExtractionReady()
        {
            DebugActivateTower();
            DebugCollectNextCache();
            if (m_overclockChoice.IsPrimaryPending)
            {
                m_overclockChoice.TrySelect(SignalOverclock.ChainArc);
            }
            DebugActivateRelayTower();
            if (m_overclockChoice.IsWeaponPending)
            {
                m_overclockChoice.TrySelect(SignalWeaponOverclock.PiercingPulse);
            }
            DebugCollectNextCache();
            if (m_overclockChoice.IsAuxiliaryPending)
            {
                m_overclockChoice.TrySelect(SignalAuxiliaryOverclock.FeedbackShield);
            }
            DebugActivateSpineTower();
            DebugCollectNextCache();
        }

        public void DebugSpawnThreat(SecurityReinforcement reinforcement)
        {
            DebugActivateTower();
            m_threats.SpawnForDebug(reinforcement);
        }

        public void DebugRepositionThreat(SecurityReinforcement reinforcement)
        {
            DebugSpawnThreat(reinforcement);
            var position = m_world.Player.position + m_world.Player.forward * 3f;
            var target = reinforcement switch
            {
                SecurityReinforcement.Warden => m_world.Warden,
                SecurityReinforcement.Sapper => m_world.Sapper,
                SecurityReinforcement.Interceptor => m_world.Interceptor,
                SecurityReinforcement.Suppressor => m_world.Suppressor,
                _ => null
            };
            if (target != null)
            {
                target.position = m_world.ClampToArena(position, 0.8f);
            }
        }

        public void DebugPurgeThreat(SecurityReinforcement reinforcement) => m_threats?.PurgeForDebug(reinforcement);

        public void DebugPurgeSwarmers() => m_threats?.PurgeSwarmersForDebug();

        public void DebugSelectOverclock(SignalOverclock overclock)
        {
            m_overclockChoice.NotifySalvageCollected(1);
            m_overclockChoice.TrySelect(overclock);
        }

        public void DebugSelectAuxiliary(SignalAuxiliaryOverclock overclock)
        {
            if (m_overclockChoice.Selected == SignalOverclock.None)
            {
                DebugSelectOverclock(SignalOverclock.ChainArc);
            }
            m_overclockChoice.NotifySalvageCollected(2);
            m_overclockChoice.TrySelect(overclock);
        }

        public void DebugSelectWeapon(SignalWeaponOverclock overclock)
        {
            m_overclockChoice.NotifyRelayActivated();
            m_overclockChoice.TrySelect(overclock);
        }

        public void DebugBeginExtraction(ExtractionUplinkMode mode)
        {
            if (!m_model.CanExtract)
            {
                DebugMakeExtractionReady();
            }
            if (mode == ExtractionUplinkMode.Overdrive)
            {
                m_model.SetSignalForDebug(Mathf.Max(m_model.Signal, m_extractionUplink.OverdriveSignalCost + 1f));
                m_model.TrySpend(m_extractionUplink.OverdriveSignalCost);
            }
            _beginExtractionUplink(mode, $"DEBUG — {mode.ToString().ToUpperInvariant()} UPLINK");
        }

        public void DebugCompleteExtraction()
        {
            DebugMakeExtractionReady();
            if (!m_extractionUplink.IsActive && !m_extractionUplink.IsComplete)
            {
                DebugBeginExtraction(ExtractionUplinkMode.Stable);
            }
            m_extractionUplink.Tick(999f);
            _completeExtraction();
        }

        public void DebugPlayTowerSweep() => m_towerActivationSweep?.Play();
        public void DebugPlaySignalImpact() => m_combatFeedback?.PlaySignalImpact(m_world.Player.position + Vector3.forward, false);
        public void DebugPlaySignalRecovery() => m_combatFeedback?.PlaySignalRecovery(m_world.Player.position + Vector3.forward);
        public void DebugPlaySalvageChain() => m_combatFeedback?.PlaySalvageChain(m_world.Player.position + Vector3.forward, 3);

        public void DebugToggleCameraImpulse() => m_comfortSettings?.ToggleCameraImpulse();
        public void DebugToggleReducedFlashes() => m_comfortSettings?.ToggleReducedFlashes();
        public void DebugToggleHighContrast()
        {
            m_comfortSettings?.ToggleHighContrast();
            m_world?.ApplyHighContrast(m_comfortSettings?.HighContrastEnabled ?? false);
        }
        public void DebugToggleAudio() => m_comfortSettings?.ToggleAudio();

        public void DebugApplyScenario(DebugScenario scenario)
        {
            _resetDebugTransientState();
            if (scenario == DebugScenario.FreshRun)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }

            switch (scenario)
            {
                case DebugScenario.TowerActivation: DebugTeleport(DebugLocation.CentralTower); DebugActivateTower(); break;
                case DebugScenario.FirstOverclock: DebugActivateTower(); DebugCollectNextCache(); break;
                case DebugScenario.ActiveSalvageChain: DebugActivateTower(); DebugCollectNextCache(); DebugCollectNextCache(); break;
                case DebugScenario.SapperPulse: DebugSpawnThreat(SecurityReinforcement.Sapper); DebugTeleport(DebugLocation.CentralTower); break;
                case DebugScenario.InterceptorCharge: DebugSpawnThreat(SecurityReinforcement.Interceptor); break;
                case DebugScenario.SuppressorExtraction: DebugBeginExtraction(ExtractionUplinkMode.Stable); DebugSpawnThreat(SecurityReinforcement.Suppressor); break;
                case DebugScenario.CriticalRecovery: m_model.SetSignalForDebug(0f); break;
                case DebugScenario.OptionalCache: DebugMakeExtractionReady(); break;
                case DebugScenario.StableExtraction: DebugBeginExtraction(ExtractionUplinkMode.Stable); DebugTeleport(DebugLocation.Extraction); break;
                case DebugScenario.OverdriveExtraction: DebugBeginExtraction(ExtractionUplinkMode.Overdrive); DebugTeleport(DebugLocation.Extraction); break;
                case DebugScenario.Victory: DebugCompleteExtraction(); break;
                case DebugScenario.Failure: m_model.SetSignalForDebug(0f); m_model.Advance(RunModel.CriticalRecoveryDuration, false, false); break;
                case DebugScenario.OpeningTacticalWindow:
                    _applyTacticalWindowScenario(DebugLocation.Extraction); break;
                case DebugScenario.SpineReturnTacticalWindow:
                    _applyTacticalWindowScenario(DebugLocation.SpineTower); break;
                case DebugScenario.EasternRoomCombat:
                    _applyEasternRoomCombatScenario(true); break;
                case DebugScenario.EasternRoomCombatNoSwarmers:
                    _applyEasternRoomCombatScenario(false); break;
                case DebugScenario.AllEffects:
                    DebugActivateTower(); DebugTeleport(DebugLocation.CentralTower); DebugExerciseCombatFeedback(); break;
            }
        }

        public void BeginFireKeyboardRebind()
        {
            if (IsPaused)
            {
                m_input.BeginFireKeyboardRebind();
            }
        }

        public void BeginInteractKeyboardRebind()
        {
            if (IsPaused)
            {
                m_input.BeginInteractKeyboardRebind();
            }
        }

        public void ResetKeyboardBindings()
        {
            if (IsPaused)
            {
                m_input.ResetKeyboardBindings();
            }
        }

        /// <summary>
        /// Toggles the persisted camera-impulse preference while the pause overlay is authoritative.
        /// </summary>
        public void ToggleCameraImpulse()
        {
            if (IsPaused)
            {
                m_comfortSettings.ToggleCameraImpulse();
            }
        }

        /// <summary>
        /// Toggles the persisted reduced-flashes preference while the pause overlay is authoritative.
        /// </summary>
        public void ToggleReducedFlashes()
        {
            if (IsPaused)
            {
                m_comfortSettings.ToggleReducedFlashes();
            }
        }

        /// <summary>
        /// Toggles the persisted high-contrast preference while the pause overlay is authoritative.
        /// </summary>
        public void ToggleHighContrast()
        {
            if (!IsPaused)
            {
                return;
            }

            m_comfortSettings.ToggleHighContrast();
            m_world.ApplyHighContrast(m_comfortSettings.HighContrastEnabled);
        }

        public void ToggleAudio()
        {
            if (IsPaused)
            {
                m_comfortSettings.ToggleAudio();
            }
        }

        [Inject]
        private void _construct(
            ICombatFeedback combatFeedback,
            IComfortSettings comfortSettings,
            IDeadSignalInput input,
            IDeadSignalAudio audio,
            IDeadSignalHud hud,
            IObjectiveBeacon objectiveBeacon,
            ISignalDust signalDust,
            ILowSignalWarning lowSignalWarning,
            ITowerActivationSweep towerActivationSweep,
            Container container)
        {
            m_combatFeedback = combatFeedback;
            m_comfortSettings = comfortSettings;
            m_input = input;
            m_audio = audio;
            m_hud = hud;
            m_objectiveBeacon = objectiveBeacon;
            m_signalDust = signalDust;
            m_lowSignalWarning = lowSignalWarning;
            m_towerActivationSweep = towerActivationSweep;
            m_container = container;
        }

        private void Awake()
        {
            Time.timeScale = 1f;
            UnityEngine.Application.targetFrameRate = 120;

            m_model = new RunModel();
            m_metrics = new RunMetrics();
            m_salvageTuning = Resources.Load<SalvagePresentationTuning>("Tuning/SalvagePresentationTuning");
            if (m_salvageTuning == null)
            {
                Debug.LogError("Salvage presentation tuning is missing from Resources/Tuning.", this);
                enabled = false;
                return;
            }

            m_playerMovementTuning = Resources.Load<PlayerDroneMovementTuning>("Tuning/PlayerDroneMovementTuning");
            if (m_playerMovementTuning == null)
            {
                Debug.LogError("Player drone movement tuning is missing from Resources/Tuning.", this);
                enabled = false;
                return;
            }

            m_playerMovement = new PlayerDroneMovement();
            m_debugRouteSequencer = new DebugRouteSequencer();
            m_overclockChoice = new SignalOverclockChoice();
            m_routeGuidanceStrength = Mathf.Clamp01(PlayerPrefs.GetFloat("DeadSignal.RouteGuidance", 0.7f));
            m_difficultyDrainMultiplier = Mathf.Clamp(PlayerPrefs.GetFloat("DeadSignal.DifficultyDrain", 1f), 0.75f, 1.2f);
            var routeVariant = (PlayerPrefs.GetInt("DeadSignal.RouteVariant", 0) + 1) % 3;
            PlayerPrefs.SetInt("DeadSignal.RouteVariant", routeVariant);
            PlayerPrefs.Save();

            m_world = new DeadSignalWorld(transform, m_comfortSettings);
            m_combatChamber = Object.FindFirstObjectByType<AuthoredCombatChamber>(FindObjectsInactive.Include);
            m_combatChamber?.ResetState();
            m_world.ConfigurePlayerSignalWake(m_playerMovementTuning);
            m_combatFeedback.Configure(m_world.Camera);
            var signalBoltTuning = Resources.Load<SignalBoltPresentationTuning>("Tuning/SignalBoltPresentationTuning");
            var threatTuning = Resources.Load<ThreatBalanceTuning>("Tuning/ThreatBalanceTuning");
            var swarmerTuning = Resources.Load<SwarmerPressureTuning>("Tuning/SwarmerPressureTuning");
            m_overclockTuning = Resources.Load<SignalOverclockTuning>("Tuning/SignalOverclockTuning");
            if (signalBoltTuning == null || threatTuning == null || swarmerTuning == null || m_overclockTuning == null)
            {
                Debug.LogError("Signal bolt, threat, Swarmer, or overclock tuning is missing from Resources/Tuning.", this);
                enabled = false;
                return;
            }

            m_extractionUplink = new ExtractionUplink(
                threatTuning.ExtractionUplinkDuration,
                threatTuning.ExtractionOverdriveDuration,
                threatTuning.ExtractionOverdriveSignalCost,
                threatTuning.StableExtractionPurgeAcceleration,
                threatTuning.OverdriveExtractionPurgeAcceleration);

            m_threats = new DeadSignalThreatController(
                m_model,
                m_metrics,
                m_world,
                m_combatFeedback,
                m_audio,
                signalBoltTuning,
                threatTuning,
                swarmerTuning,
                m_overclockChoice,
                m_overclockTuning,
                _showFeedback,
                _rewardExtractionPurge);
            m_salvage = new DeadSignalSalvageController(
                m_model, m_metrics, m_world, m_audio, m_combatFeedback, m_salvageTuning, m_overclockChoice, _showFeedback);
            m_hud.Configure(m_model, m_metrics, m_world, m_threats, m_salvage, m_extractionUplink, m_overclockChoice);
            m_missionClarityHud = gameObject.AddComponent<MissionClarityHud>();
            m_missionClarityHud.Configure(m_model, m_metrics, m_world, m_overclockChoice);
            m_objectiveBeacon.Configure(m_model, m_world);
            m_lastPoweredState = m_world.IsPowered(
                m_world.Player.position, m_model.TowerOnline, m_model.RelayTowerOnline, m_model.SpineTowerOnline);
            m_signalDust.Configure();
            m_signalDust.Tick(m_lastPoweredState, m_model.TowerOnline, m_model.Signal / RunModel.MaximumSignal);
            m_lowSignalWarning.Configure(m_model);
            m_lowSignalWarning.Tick(0f);
            m_towerActivationSweep.Configure(m_world.TowerPosition, DeadSignalWorld.TOWER_POWER_RADIUS);
            _tryStartCommandLineDebugRoute();
        }

        private void Update()
        {
            _sampleDebugSignalRate();
            if (m_debugInfiniteSignal)
            {
                m_model?.SetSignalForDebug(RunModel.MaximumSignal);
            }

            if (m_focusInputBlockFrames > 0)
            {
                m_focusInputBlockFrames--;
                return;
            }

            _handlePauseInput();
            var dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (m_debugCombatScenarioActive && !IsPaused)
            {
                m_debugCombatScenarioSeconds += dt;
            }
            m_hud.Tick(dt);

            if (m_combatFeedback.IsFrozen)
            {
                if (!IsPaused && m_input.IsFireHeld())
                {
                    m_fireBuffered = true;
                }

                return;
            }

            m_threats.TickCooldown(dt);
            m_overclockChoice.Tick(dt);
            m_dashCooldown = Mathf.Max(0f, m_dashCooldown - dt);
            m_blockedFeedbackCooldown = Mathf.Max(0f, m_blockedFeedbackCooldown - dt);

            if (m_model.Outcome != RunOutcome.Running)
            {
                _finalizeDebugRouteAfterOutcome();
                if (m_input.PressedRestart())
                {
                    _resetDebugTransientState();
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }

                return;
            }

            _updateLiveBalanceCombatDecision(dt);
            var movement = m_debugMenuOpen && !m_debugRouteDriving ? Vector3.zero : _updatePlayer(dt);
            var aimDirection = m_liveBalanceCombatDecision.AimDirection.sqrMagnitude > 0.01f
                ? m_liveBalanceCombatDecision.AimDirection
                : m_debugMenuOpen
                    ? m_world.Player.forward
                    : _applyAimAssist(m_input.ReadAimDirection(m_world.Camera, m_world.Player));
            m_world.TickPlayerPresentation(
                dt,
                m_playerPresentationAcceleration,
                m_playerMovement.Velocity,
                aimDirection,
                m_playerMovementTuning);
            var powered = _isPlayerPowered();
            m_world.TickEnvironmentPresentation(dt, m_model.TowerOnline, powered);
            m_world.PlayerSignalWake.Tick(m_playerMovement.Velocity);
            m_world.TickGameplayAssists(dt, m_model, m_threats, aimDirection, m_routeGuidanceStrength);

            var moving = movement.sqrMagnitude > 0.01f;
            m_audio.Tick(powered, m_model.TowerOnline, m_model.Signal / RunModel.MaximumSignal);
            m_model.Advance(dt * m_difficultyDrainMultiplier, moving, powered);
            m_metrics.RecordTraversalDrain(
                RunModel.PassiveDrainRate(powered) * dt * m_difficultyDrainMultiplier,
                RunModel.MovementDrainRate(moving, powered) * dt * m_difficultyDrainMultiplier);
            m_missionClarityHud.DrainMultiplier = m_difficultyDrainMultiplier;
            m_missionClarityHud.DashCooldown = m_dashCooldown;
            m_missionClarityHud.IsMoving = moving;
            m_missionClarityHud.IsPowered = powered;
            _tickDebugRouteSequence(dt);
            _tryTriggerEmergencyCapacitor();
            m_signalDust.Tick(powered, m_model.TowerOnline, m_model.Signal / RunModel.MaximumSignal);
            m_lowSignalWarning.Tick(dt);
            m_metrics.Advance(dt, powered);
            _tickOnboarding();
            if (powered != m_lastPoweredState)
            {
                _showFeedback(powered ? "NETWORK LINK RESTORED" : "DEAD ZONE — SIGNAL BLEED");
                m_world.PlayBoundaryTransition();
                m_lastPoweredState = powered;
            }

            if (m_debugMenuOpen)
            {
                _tickRunSystems(dt, powered);
                return;
            }

            if (m_overclockChoice.IsPending)
            {
                _handleOverclockChoice();
            }
            else if (_isExtractionUplinkChoiceAvailable())
            {
                _handleExtractionUplinkChoice();
            }
            else
            {
                if ((m_fireBuffered || m_debugFireHeld || m_input.IsFireHeld()) && m_threats.CanFire)
                {
                    m_fireBuffered = false;
                    m_threats.TryFire(aimDirection);
                }
                else if (m_liveBalanceCombatDecision.ShouldFire)
                {
                    m_threats.TryFire(aimDirection);
                }

                if (m_input.PressedInteract())
                {
                    _handleInteraction();
                }
            }

            _tickRunSystems(dt, powered);
            if (m_debugInfiniteSignal)
            {
                m_model.SetSignalForDebug(RunModel.MaximumSignal);
            }
        }

        private void OnDestroy()
        {
            m_world?.Dispose();
            if (m_container != null)
            {
                m_container.Dispose();
                m_container = null;
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (m_model == null || m_model.Outcome != RunOutcome.Running)
            {
                return;
            }

            if (!hasFocus)
            {
                _setPaused(true);
                return;
            }

            m_focusInputBlockFrames = 2;
        }

        private Vector3 _updatePlayer(float dt)
        {
            if (dt <= 0f)
            {
                m_playerPresentationAcceleration = Vector3.zero;
                return Vector3.zero;
            }

            var moveInput = m_debugTacticalWindowSweepActive
                ? m_debugTacticalWindowMoveInput
                : m_debugRouteDriving ? _debugRouteInput() : m_input.ReadMovement();
            if (_isLiveBalanceAutomationActive())
            {
                moveInput = LiveBalanceCombatPolicy.BlendMovement(
                    moveInput, m_liveBalanceCombatDecision.EvasionDirection);
            }
            if (moveInput.sqrMagnitude > 0.01f)
            {
                m_lastDebugInputFrame = Time.frameCount;
            }
            var previousVelocity = m_playerMovement.Velocity;
            var hasThrusterOverclock = m_overclockChoice.Selected == SignalOverclock.OverdriveThrusters;
            var suppressionMultiplier = m_threats.PlayerMovementMultiplier;
            var synergyMultiplier = m_overclockChoice.IsOverdriveSurgeActive
                ? m_overclockTuning.OverdriveSynergySpeedMultiplier
                : 1f;
            var speedMultiplier = (hasThrusterOverclock ? m_overclockTuning.ThrusterSpeedMultiplier * synergyMultiplier : 1f) *
                                  suppressionMultiplier;
            if (m_model.IsCriticalRecovery)
            {
                speedMultiplier *= 1.25f;
            }
            var accelerationMultiplier =
                (hasThrusterOverclock ? m_overclockTuning.ThrusterAccelerationMultiplier : 1f) * suppressionMultiplier;
            var velocity = m_playerMovement.Tick(
                moveInput, dt, m_playerMovementTuning, speedMultiplier, accelerationMultiplier);
            var previousPosition = m_world.Player.position;
            var desired = previousPosition + velocity * dt;
            var dashRequested = false;
            var dashSpentSignal = false;
            if (!m_debugRouteDriving && m_input.PressedDash() && m_dashCooldown <= 0f && moveInput.sqrMagnitude > 0.1f)
            {
                if (m_model.IsCriticalRecovery || m_model.TrySpend(DASH_SIGNAL_COST))
                {
                    dashRequested = true;
                    dashSpentSignal = !m_model.IsCriticalRecovery;
                    var dashDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
                    desired += dashDirection * DASH_DISTANCE;
                    m_dashCooldown = DASH_COOLDOWN;
                    _showFeedback(m_model.IsCriticalRecovery
                        ? "EMERGENCY DASH — NO SIGNAL COST"
                        : $"SIGNAL DASH  −{DASH_SIGNAL_COST:0}  //  EVADE AND REPOSITION");
                }
                else
                {
                    _showFeedback($"DASH REQUIRES {DASH_SIGNAL_COST:0} SIGNAL");
                }
            }
            desired = m_world.ClampToArena(desired, 0.6f);
            m_world.Player.position = m_world.ResolveMovement(
                previousPosition,
                desired,
                PLAYER_COLLISION_RADIUS,
                m_model.ShortcutOpen);
            if (m_debugRouteDriving && m_world.LastMovementBlocked)
            {
                m_debugRouteBlockedSeconds += dt;
            }
            else
            {
                m_debugRouteBlockedSeconds = 0f;
            }
            var travelled = DeadSignalWorld.FlatDistance(previousPosition, m_world.Player.position);
            if (dashRequested && travelled < DASH_DISTANCE * 0.5f)
            {
                if (dashSpentSignal)
                {
                    m_model.RestoreSignal(DASH_SIGNAL_COST);
                }
                m_dashCooldown = 0.4f;
                _showFeedback("DASH BLOCKED — SIGNAL REFUNDED  //  FOLLOW AMBER TURN");
            }
            else if (dashRequested)
            {
                m_world.PlayPlayerDash(previousPosition + Vector3.up * 0.16f, m_world.Player.position + Vector3.up * 0.16f);
            }
            else if (m_world.LastMovementBlocked && moveInput.sqrMagnitude > 0.1f && m_blockedFeedbackCooldown <= 0f)
            {
                m_blockedFeedbackCooldown = 1.1f;
                _showFeedback("ROUTE BLOCKED — FOLLOW AMBER TURN");
            }
            var resolvedVelocity = (m_world.Player.position - previousPosition) / dt;
            m_playerMovement.ApplyResolvedVelocity(resolvedVelocity);
            var acceleration = (m_playerMovement.Velocity - previousVelocity) / dt;
            m_playerPresentationAcceleration = acceleration;
            return resolvedVelocity;
        }

        private bool _isPlayerPowered()
        {
            return m_world.IsPowered(
                       m_world.Player.position, m_model.TowerOnline, m_model.RelayTowerOnline, m_model.SpineTowerOnline) ||
                   (m_salvage.IsRecoveryFieldActive && DeadSignalWorld.FlatDistance(
                       m_world.Player.position, m_salvage.RecoveryFieldPosition) <= m_salvage.RecoveryFieldRadius);
        }

        private Vector3 _applyAimAssist(Vector3 aimDirection)
        {
            var strength = Mathf.Clamp01(PlayerPrefs.GetFloat("DeadSignal.AimAssist", 0.35f));
            if (strength <= 0f || m_input.ActivePromptDevice != InputPromptDevice.Gamepad)
            {
                return aimDirection;
            }

            var candidates = new[] { m_world.Warden, m_world.Sapper, m_world.Interceptor, m_world.Suppressor };
            Transform best = null;
            var bestDot = 0.82f;
            foreach (var candidate in candidates)
            {
                if (candidate == null || !candidate.gameObject.activeSelf)
                {
                    continue;
                }

                var direction = candidate.position - m_world.Player.position;
                direction.y = 0f;
                var dot = Vector3.Dot(aimDirection.normalized, direction.normalized);
                if (dot > bestDot && direction.sqrMagnitude <= 64f)
                {
                    best = candidate;
                    bestDot = dot;
                }
            }

            if (best == null)
            {
                return aimDirection;
            }

            var assisted = best.position - m_world.Player.position;
            assisted.y = 0f;
            return Vector3.Slerp(aimDirection.normalized, assisted.normalized, strength);
        }

        private void _tickOnboarding()
        {
            if (m_onboardingStep == 0 && m_metrics.ElapsedSeconds >= 1f)
            {
                _showFeedback("TRAINING LINK — MOVE, AIM, AND FIRE WHILE SIGNAL DRAINS");
                m_onboardingStep++;
            }
            else if (m_onboardingStep == 1 && m_metrics.ElapsedSeconds >= 5f)
            {
                _showFeedback("CYAN TERRITORY STOPS ACTIVE SIGNAL DRAIN");
                m_onboardingStep++;
            }
            else if (m_onboardingStep == 2 && m_model.TowerOnline)
            {
                _showFeedback("NETWORK ONLINE — FOLLOW THE PULSE TO THREE SALVAGE CORES");
                m_onboardingStep++;
            }
            else if (m_onboardingStep == 3 && m_model.CanExtract)
            {
                _showFeedback("TRAINING COMPLETE — RETURN THROUGH THE CYAN APPROACH LANE");
                m_onboardingStep++;
            }
        }

        private void _handlePauseInput()
        {
            if (m_debugMenuOpen)
            {
                return;
            }

            if (m_model.Outcome == RunOutcome.Running && m_input.PressedPause())
            {
                _setPaused(!IsPaused);
            }

            if (!IsPaused)
            {
                return;
            }

            if (m_input.PressedCameraImpulseToggle())
            {
                ToggleCameraImpulse();
            }

            if (m_input.PressedReducedFlashesToggle())
            {
                ToggleReducedFlashes();
            }

            if (m_input.PressedHighContrastToggle())
            {
                ToggleHighContrast();
            }

            if (m_input.PressedAudioToggle())
            {
                ToggleAudio();
            }

            if (m_input.PressedGuidanceToggle())
            {
                m_routeGuidanceStrength = m_routeGuidanceStrength < 0.1f ? 0.7f : m_routeGuidanceStrength < 0.9f ? 1f : 0f;
                PlayerPrefs.SetFloat("DeadSignal.RouteGuidance", m_routeGuidanceStrength);
                PlayerPrefs.Save();
            }

            if (m_input.PressedDifficultyToggle())
            {
                m_difficultyDrainMultiplier = m_difficultyDrainMultiplier < 0.9f ? 1f :
                    m_difficultyDrainMultiplier < 1.1f ? 1.2f : 0.75f;
                PlayerPrefs.SetFloat("DeadSignal.DifficultyDrain", m_difficultyDrainMultiplier);
                PlayerPrefs.Save();
            }
        }

        private void _handleInteraction()
        {
            if (m_combatChamber != null)
            {
                if (m_combatChamber.TryCollectReward(m_world.Player.position))
                {
                    var restored = m_model.RestoreSignal(m_combatChamber.RewardSignal);
                    m_combatFeedback.PlaySignalRecovery(m_world.Player.position + Vector3.up * 0.45f);
                    m_audio.Play(DeadSignalAudioCue.TowerOnline);
                    _showFeedback($"SECURITY VAULT RECOVERED  +{restored:0} SIGNAL");
                    return;
                }

                if (m_combatChamber.CanInteract(m_world.Player.position))
                {
                    if (!m_threats.CanBeginCombatChamber)
                    {
                        _showFeedback("SECURITY TRIAL BLOCKED — CLEAR ACTIVE THREATS");
                        return;
                    }

                    if (m_combatChamber.TryArm(m_world.Player.position))
                    {
                        m_world.RefreshNavigation();
                        m_audio.Play(DeadSignalAudioCue.Shortcut);
                        _showFeedback("SECURITY TRIAL ARMED — CROSSING THE RED THRESHOLD SEALS THE ROOM");
                    }
                    return;
                }
            }

            if (!m_model.SpineTowerOnline &&
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.SpineTowerPosition) < TOWER_INTERACTION_RADIUS)
            {
                if (m_model.TryActivateSpineTower())
                {
                    m_world.ActivateSpineTower();
                    m_overclockChoice.NotifySpineActivated();
                    m_audio.Play(DeadSignalAudioCue.TowerOnline);
                    var evolution = m_overclockChoice.SelectedWeapon == SignalWeaponOverclock.PiercingPulse
                        ? "PIERCING PULSE EVOLVED — THREE TARGETS"
                        : "CONTROLLED RICOCHET EVOLVED — TWO BANKS";
                    _showFeedback($"SPINE ONLINE — {evolution}");
                }
                else if (!m_model.RelayTowerOnline)
                {
                    _showFeedback("SPINE LOCKED — RESTORE RELAY FOUNDRY");
                }
                else if (!m_model.RelayPayloadSecured)
                {
                    _showFeedback("SPINE LOCKED — SECURE A RELAY PAYLOAD");
                }
                else
                {
                    _showFeedback($"KEEP 1 SIGNAL AFTER {RunModel.SpineTowerCost:0} COST");
                }

                return;
            }

            if (!m_model.RelayTowerOnline &&
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.RelayTowerPosition) < TOWER_INTERACTION_RADIUS)
            {
                if (m_model.TryActivateRelayTower())
                {
                    m_world.ActivateRelayTower();
                    m_overclockChoice.NotifyRelayActivated();
                    m_audio.Play(DeadSignalAudioCue.TowerOnline);
                    _showFeedback("RELAY ONLINE — RETURN BULKHEAD OPEN  //  WEAPON CALIBRATION READY");
                }
                else if (!m_model.TowerOnline)
                {
                    _showFeedback("RELAY LOCKED — RESTORE CENTRAL TOWER");
                }
                else if (!m_model.CentralPayloadSecured)
                {
                    _showFeedback("RELAY LOCKED — SECURE A CENTRAL PAYLOAD");
                }
                else
                {
                    _showFeedback($"KEEP 1 SIGNAL AFTER {RunModel.RelayTowerCost:0} COST");
                }

                return;
            }

            if (!m_model.TowerOnline &&
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.TowerPosition) < TOWER_INTERACTION_RADIUS)
            {
                if (m_model.TryActivateTower())
                {
                    m_world.ActivateTower(m_threats.SapperPulseInterval);
                    m_towerActivationSweep.Play();
                    m_audio.Play(DeadSignalAudioCue.TowerOnline);
                    _showFeedback("TOWER ONLINE - TWO THREATS AWAKENED");
                }
                else
                {
                    _showFeedback("TOWER REQUIRES 10 SIGNAL");
                }

                return;
            }

            if (!m_model.ShortcutOpen && DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.ShortcutPosition) < 1.9f)
            {
                if (m_model.TryOpenShortcut())
                {
                    m_world.OpenShortcut();
                    m_audio.Play(DeadSignalAudioCue.Shortcut);
                    _showFeedback($"SHORTCUT OPEN  -{RunModel.ShortcutCost:0} SIGNAL");
                }
                else if (!m_model.TowerOnline)
                {
                    _showFeedback("SHORTCUT OFFLINE - ACTIVATE TOWER");
                }
                else
                {
                    _showFeedback($"KEEP 1 SIGNAL AFTER {RunModel.ShortcutCost:0} COST");
                }

                return;
            }

            if (DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.ExtractionPosition) >= 1.65f)
            {
                return;
            }

            if (!m_model.CanExtract)
            {
                _showFeedback($"EXTRACTION LOCKED — {_extractionRequirement()}");
            }
        }

        private bool _isExtractionUplinkChoiceAvailable()
        {
            return m_model.CanExtract &&
                   !m_extractionUplink.IsActive &&
                   !m_extractionUplink.IsComplete &&
                   DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.ExtractionPosition) < 1.65f;
        }

        private string _extractionRequirement()
        {
            return m_model.CurrentMissionStage switch
            {
                MissionStage.CentralTower => "RESTORE CENTRAL TOWER",
                MissionStage.CentralPayload => "SECURE CENTRAL PAYLOAD",
                MissionStage.RelayTower => "RESTORE RELAY TOWER",
                MissionStage.RelayPayload => "SECURE RELAY PAYLOAD",
                MissionStage.SpineTower => "RESTORE SPINE TOWER",
                MissionStage.SpinePayload => "SECURE SPINE PAYLOAD",
                _ => "COMPLETE NETWORK JOURNEY"
            };
        }

        private void _handleExtractionUplinkChoice()
        {
            if (m_fireBuffered || m_input.PressedFire())
            {
                m_fireBuffered = false;
                if (!m_extractionUplink.CanAffordOverdrive(m_model.Signal))
                {
                    _showFeedback($"KEEP 1 SIGNAL AFTER {m_extractionUplink.OverdriveSignalCost:0} OVERDRIVE COST");
                    return;
                }

                if (m_model.TrySpend(m_extractionUplink.OverdriveSignalCost))
                {
                    _beginExtractionUplink(ExtractionUplinkMode.Overdrive,
                        $"UPLINK OVERDRIVEN  −{m_extractionUplink.OverdriveSignalCost:0} SIGNAL — PURSUIT INBOUND");
                }

                return;
            }

            if (m_input.PressedInteract())
            {
                _beginExtractionUplink(ExtractionUplinkMode.Stable, "STABLE UPLINK STARTED — SECURITY PURSUIT INBOUND");
            }
        }

        private void _beginExtractionUplink(ExtractionUplinkMode mode, string feedback)
        {
            if (!m_extractionUplink.Begin(mode))
            {
                return;
            }

            m_threats.BeginExtractionPressure(mode);
            var countermeasure = m_threats.CurrentExtractionSuppressionProfile switch
            {
                ExtractionSuppressionProfile.PiercingCrossLane => " // QUENCH CROSS-LANE COUNTERTRACE",
                ExtractionSuppressionProfile.RicochetCoverFlush => " // QUENCH COVER-FLUSH COUNTERTRACE",
                _ => string.Empty
            };
            _showFeedback(feedback + countermeasure);
        }

        private void _handleOverclockChoice()
        {
            if (m_overclockChoice.IsAuxiliaryPending)
            {
                _handleAuxiliaryOverclockChoice();
                return;
            }

            if (m_overclockChoice.IsWeaponPending)
            {
                _handleWeaponOverclockChoice();
                return;
            }

            if (m_fireBuffered || m_input.PressedFire())
            {
                m_fireBuffered = false;
                if (m_overclockChoice.TrySelect(SignalOverclock.ChainArc))
                {
                    _showFeedback("CHAIN ARC ONLINE — BOLTS JUMP TO A NEARBY THREAT");
                }

                return;
            }

            if (m_input.PressedInteract() && m_overclockChoice.TrySelect(SignalOverclock.OverdriveThrusters))
            {
                _showFeedback("OVERDRIVE THRUSTERS ONLINE — SPEED AND RESPONSE BOOSTED");
            }
        }

        private void _handleWeaponOverclockChoice()
        {
            if (m_fireBuffered || m_input.PressedFire())
            {
                m_fireBuffered = false;
                if (m_overclockChoice.TrySelect(SignalWeaponOverclock.PiercingPulse))
                {
                    _showFeedback("PIERCING PULSE ONLINE — EACH BOLT CAN STRIKE TWO THREATS");
                }

                return;
            }

            if (m_input.PressedInteract() && m_overclockChoice.TrySelect(SignalWeaponOverclock.ControlledRicochet))
            {
                _showFeedback("CONTROLLED RICOCHET ONLINE — COVER CAN REDIRECT ONE BOLT");
            }
        }

        private float _rewardExtractionPurge()
        {
            var accelerated = m_extractionUplink.RewardPurge();
            if (m_extractionUplink.IsComplete)
            {
                _completeExtraction();
            }

            return accelerated;
        }

        private void _completeExtraction()
        {
            if (!m_model.TryExtract())
            {
                return;
            }

            m_audio.Play(DeadSignalAudioCue.Extraction);
            _showFeedback("EXTRACTION COMPLETE");
            _finalizeDebugRouteAfterOutcome();
        }

        private void _handleAuxiliaryOverclockChoice()
        {
            if (m_fireBuffered || m_input.PressedFire())
            {
                m_fireBuffered = false;
                if (m_overclockChoice.TrySelect(SignalAuxiliaryOverclock.EmergencyCapacitor))
                {
                    _showFeedback("EMERGENCY CAPACITOR ARMED — LOW SIGNAL TRIGGERS ONE REFILL");
                    _tryTriggerEmergencyCapacitor();
                }

                return;
            }

            if (m_input.PressedInteract() && m_overclockChoice.TrySelect(SignalAuxiliaryOverclock.FeedbackShield))
            {
                _showFeedback("FEEDBACK SHIELD CHARGED — PURGE A THREAT TO RECHARGE");
            }
        }

        private void _tryTriggerEmergencyCapacitor()
        {
            var restored = m_overclockChoice.TryTriggerEmergencyCapacitor(m_model, m_overclockTuning);
            if (restored > 0f)
            {
                var synergyText = m_overclockChoice.Synergy switch
                {
                    SignalOverclockSynergy.ArcOverload => "  //  ARC OVERLOAD PRIMED",
                    SignalOverclockSynergy.CapacitorSurge =>
                        $"  //  THRUSTER SURGE {m_overclockTuning.OverdriveSynergySurgeDuration:0.#} SEC",
                    _ => string.Empty
                };
                _showFeedback($"EMERGENCY CAPACITOR FIRED  +{restored:0} SIGNAL{synergyText}");
            }
        }

        private void _showFeedback(string message)
        {
            m_hud.ShowFeedback(message);
            m_missionClarityHud?.NotifySignalEvent(message);
            if (DeadSignalDebugMenu.IsAvailable)
            {
                while (m_debugEventLog.Count >= 12)
                {
                    m_debugEventLog.Dequeue();
                }
                m_debugEventLog.Enqueue($"{Time.unscaledTime:000.00}s  {message}");
            }
        }

        private Vector2 _debugRouteInput()
        {
            var sequenceStep = m_debugRouteSequencer?.IsRunning == true ? m_debugRouteSequencer.CurrentStep : null;
            var destination = sequenceStep == null
                ? _debugLocationPosition(m_debugRouteDestination)
                : _debugRouteSequenceDestination(sequenceStep);
            var completionRadius = sequenceStep?.ArrivalRadius ?? _debugRouteCompletionRadius(m_debugRouteDestination);
            if (m_debugObservedRecoveryCount != (m_debugRouteSequencer?.RecoveryCount ?? 0))
            {
                m_debugObservedRecoveryCount = m_debugRouteSequencer?.RecoveryCount ?? 0;
                m_world.InvalidateNavMeshRoute(m_world.Player);
            }
            var targetDelta = destination - m_world.Player.position;
            targetDelta.y = 0f;
            if (targetDelta.sqrMagnitude <= completionRadius * completionRadius)
            {
                if (sequenceStep != null)
                {
                    return Vector2.zero;
                }
                m_debugRouteDriving = false;
                m_debugRouteBlockedSeconds = 0f;
                _showFeedback($"DEBUG ROUTE COMPLETE — {m_debugRouteDestination.ToString().ToUpperInvariant()}");
                return Vector2.zero;
            }

            var navigationDestination = destination;
            if (_isLiveBalanceAutomationActive() &&
                m_liveBalanceCombatDecision.Target == SecurityReinforcement.Sapper &&
                m_threats.IsSapperAlive &&
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.Sapper.position) > 2.5f)
            {
                navigationDestination = _liveBalanceSapperInterceptPosition();
            }
            var waypoint = m_world.GetNavMeshWaypoint(
                m_world.Player, navigationDestination, PLAYER_COLLISION_RADIUS, m_model.ShortcutOpen);
            var delta = waypoint - m_world.Player.position;
            delta.y = 0f;
            delta.Normalize();
            var remainingDistance = Mathf.Max(
                0f, DeadSignalWorld.FlatDistance(m_world.Player.position, navigationDestination) - completionRadius);
            var speed = Mathf.Clamp(remainingDistance / 2.5f, 0.2f, 1f);
            return new Vector2(delta.x, delta.z) * speed;
        }

        private void _tickDebugRouteSequence(float dt)
        {
            if (m_debugRouteSequencer == null || !m_debugRouteSequencer.IsRunning || m_world == null)
            {
                return;
            }

            var step = m_debugRouteSequencer.CurrentStep;
            var destination = _debugRouteSequenceDestination(step);
            var distance = DeadSignalWorld.FlatDistance(m_world.Player.position, destination);
            var hasCompleteRoute = m_world.GetRemainingNavMeshCorners(m_world.Player) > 0;
            var arrived = m_debugRouteSequencer.TickNavigation(
                distance, dt, m_world.LastMovementBlocked, hasCompleteRoute);
            if (arrived && m_debugCaptureEachRouteStep)
            {
                DebugCaptureScreenshot();
            }
            if (m_debugRouteSequencer.ShouldIssueAction())
            {
                m_debugSalvageBeforeRouteAction = m_model.Salvage;
                m_debugOptionalBeforeRouteAction = m_model.OptionalSalvageSecured;
                _executeDebugRouteAction(step.Action);
            }
            if (m_debugRouteSequencer.State == DebugRouteRunState.Verifying)
            {
                var verified = _verifyDebugRouteAction(step.Action);
                var assertionPassed = _verifyDebugRouteAssertion(step.Assertion, distance);
                m_debugRouteSequencer.TickVerification(dt, verified, assertionPassed,
                    $"distance {distance:0.00}m, assertion {step.Assertion}", CurrentSignal);
            }

            m_debugRouteDriving = m_debugRouteSequencer.State == DebugRouteRunState.Navigating;
            if (m_debugRouteSequencer.State == DebugRouteRunState.Completed ||
                m_debugRouteSequencer.State == DebugRouteRunState.Failed)
            {
                _showFeedback($"DEBUG ROUTE {m_debugRouteSequencer.State.ToString().ToUpperInvariant()}");
                if (!m_debugRouteSequencer.AwaitsRunOutcomeReport)
                {
                    _writeDebugRouteReport();
                }
            }
        }

        private void _executeDebugRouteAction(DebugRouteAction action)
        {
            switch (action)
            {
                case DebugRouteAction.ActivateCentralTower: DebugActivateTower(); break;
                case DebugRouteAction.CollectCache: _debugCollectNearestCacheForRoute(); break;
                case DebugRouteAction.SelectPrimaryOverclock: DebugSelectOverclock(SignalOverclock.ChainArc); break;
                case DebugRouteAction.ActivateRelayTower: DebugActivateRelayTower(); break;
                case DebugRouteAction.SelectWeaponOverclock: DebugSelectWeapon(SignalWeaponOverclock.PiercingPulse); break;
                case DebugRouteAction.ActivateSpineTower: DebugActivateSpineTower(); break;
                case DebugRouteAction.BeginStableExtraction: DebugBeginExtraction(ExtractionUplinkMode.Stable); break;
                case DebugRouteAction.CaptureScreenshot: DebugCaptureScreenshot(); break;
            }
        }

        private Vector3 _debugRouteSequenceDestination(DebugRouteStep step)
        {
            if (m_debugObservedSequenceStep != m_debugRouteSequencer.StepNumber)
            {
                m_debugObservedSequenceStep = m_debugRouteSequencer.StepNumber;
                m_debugSequenceTarget = step.UsesCustomPosition ? step.CustomPosition : _debugLocationPosition(step.Location);
            }
            return m_debugSequenceTarget;
        }

        private bool _verifyDebugRouteAction(DebugRouteAction action)
        {
            return action switch
            {
                DebugRouteAction.ActivateCentralTower => m_model.TowerOnline,
                DebugRouteAction.CollectCache => m_model.Salvage > m_debugSalvageBeforeRouteAction ||
                                                 m_model.OptionalSalvageSecured != m_debugOptionalBeforeRouteAction,
                DebugRouteAction.SelectPrimaryOverclock => m_overclockChoice.Selected != SignalOverclock.None,
                DebugRouteAction.ActivateRelayTower => m_model.RelayTowerOnline,
                DebugRouteAction.SelectWeaponOverclock => m_overclockChoice.SelectedWeapon != SignalWeaponOverclock.None,
                DebugRouteAction.ActivateSpineTower => m_model.SpineTowerOnline,
                DebugRouteAction.BeginStableExtraction => m_extractionUplink.IsActive,
                _ => true
            };
        }

        private void _debugCollectNearestCacheForRoute()
        {
            var nearest = m_world.SalvagePickups
                .Where(pickup => pickup.activeSelf)
                .OrderBy(pickup => DeadSignalWorld.FlatDistance(m_world.Player.position, pickup.transform.position))
                .FirstOrDefault();
            if (nearest == null)
            {
                return;
            }
            var routePosition = m_world.Player.position;
            m_world.Player.position = nearest.transform.position;
            m_salvage.Tick(0f);
            m_world.Player.position = routePosition;
            if (m_overclockChoice.IsPending)
            {
                var primary = m_debugRouteSequencer?.Profile == DebugAutomationProfile.LiveBalance
                    ? SignalOverclock.OverdriveThrusters
                    : SignalOverclock.ChainArc;
                DebugSelectOverclock(primary);
            }
            if (m_overclockChoice.IsAuxiliaryPending)
            {
                DebugSelectAuxiliary(SignalAuxiliaryOverclock.EmergencyCapacitor);
            }
        }

        private bool _verifyDebugRouteAssertion(DebugRouteAssertion assertion, float distance)
        {
            if (assertion == DebugRouteAssertion.SignalAboveTwenty)
            {
                return CurrentSignal >= 20f;
            }
            if (assertion == DebugRouteAssertion.InteractionInRange)
            {
                return distance <= TOWER_INTERACTION_RADIUS;
            }
            if (assertion == DebugRouteAssertion.CameraContainsPlayer)
            {
                var viewport = m_world.Camera.WorldToViewportPoint(m_world.Player.position);
                return viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
            }
            return true;
        }

        private void _updateLiveBalanceCombatDecision(float dt)
        {
            if (!_isLiveBalanceAutomationActive())
            {
                m_liveBalanceCombatDecision = default;
                return;
            }

            var sapperAimPosition = _liveBalanceSapperAimPosition();
            var sapperLineBlocked = m_threats.IsSapperAlive && m_world.TryGetProjectileObstacleHit(
                m_world.Player.position + Vector3.up * 0.18f,
                sapperAimPosition + Vector3.up * 0.5f,
                0.08f,
                m_model.ShortcutOpen,
                out _);
            m_liveBalanceCombatDecision = m_liveBalanceCombatPolicy.Tick(
                dt,
                m_world.Player.position,
                CurrentSignal,
                m_model.TowerOnline,
                m_threats.CanFire && !sapperLineBlocked &&
                !m_overclockChoice.IsPending && !m_overclockChoice.IsAuxiliaryPending &&
                !m_overclockChoice.IsWeaponPending && !_isExtractionUplinkChoiceAvailable(),
                ActiveSignalBoltCount > 0,
                m_threats.IsInterceptorCharging,
                m_threats.IsSuppressorFieldWarningActive,
                m_threats.IsPlayerSuppressed,
                m_threats.SuppressorFieldCenter,
                _liveBalanceThreat(SecurityReinforcement.Warden, m_threats.IsWardenAlive, m_world.Warden,
                    m_world.WardenTelegraph?.IsWarningVisible ?? false),
                new LiveBalanceThreatSnapshot(
                    SecurityReinforcement.Sapper,
                    m_threats.IsSapperAlive && m_world.Sapper.gameObject.activeInHierarchy,
                    sapperAimPosition,
                    m_threats.IsSapperAlive),
                _liveBalanceThreat(SecurityReinforcement.Interceptor, m_threats.IsInterceptorAlive, m_world.Interceptor,
                    m_threats.IsInterceptorCharging || m_threats.IsInterceptorRecovering),
                _liveBalanceThreat(SecurityReinforcement.Suppressor, m_threats.IsSuppressorAlive, m_world.Suppressor,
                    m_threats.IsSuppressorFieldWarningActive || m_threats.IsSuppressorFieldActive));
        }

        private static LiveBalanceThreatSnapshot _liveBalanceThreat(
            SecurityReinforcement role, bool alive, Transform threat, bool urgent)
        {
            return new LiveBalanceThreatSnapshot(
                role,
                alive && threat != null && threat.gameObject.activeInHierarchy,
                threat != null ? threat.position : Vector3.zero,
                urgent);
        }

        private Vector3 _liveBalanceSapperAimPosition()
        {
            var position = m_world.Sapper.position;
            if (m_threats.IsSapperLatched)
            {
                return position;
            }

            var toTower = m_world.TowerPosition - position;
            toTower.y = 0f;
            if (toTower.sqrMagnitude <= 0.01f || m_threats.SignalBoltSpeed <= 0f)
            {
                return position;
            }

            var flightSeconds = DeadSignalWorld.FlatDistance(m_world.Player.position, position) /
                                m_threats.SignalBoltSpeed;
            var leadDistance = Mathf.Min(toTower.magnitude, m_threats.SapperSpeed * flightSeconds);
            return position + toTower.normalized * leadDistance;
        }

        private Vector3 _liveBalanceSapperInterceptPosition()
        {
            var target = m_world.Sapper.position;
            var best = target;
            var bestDistance = float.PositiveInfinity;
            foreach (var direction in s_liveBalanceInterceptDirections)
            {
                var candidate = m_world.ClampToArena(target + direction * 3.2f, 0.6f);
                if (m_world.TryGetProjectileObstacleHit(
                        candidate + Vector3.up * 0.18f,
                        target + Vector3.up * 0.5f,
                        0.08f,
                        m_model.ShortcutOpen,
                        out _))
                {
                    continue;
                }

                var distance = DeadSignalWorld.FlatDistance(m_world.Player.position, candidate);
                if (distance >= bestDistance)
                {
                    continue;
                }

                best = candidate;
                bestDistance = distance;
            }

            return best;
        }

        private bool _isLiveBalanceAutomationActive()
        {
            return m_debugRouteSequencer != null &&
                   m_debugRouteSequencer.Mode == DebugAutomationMode.AssistedPlaythrough &&
                   m_debugRouteSequencer.Profile == DebugAutomationProfile.LiveBalance &&
                   m_debugRouteSequencer.State is DebugRouteRunState.Navigating or
                       DebugRouteRunState.Verifying or DebugRouteRunState.Completed;
        }

        private void _applyDebugAutomationProfile(DebugAutomationProfile profile)
        {
            DebugSetInfiniteSignal(profile != DebugAutomationProfile.LiveBalance);
            var safe = profile == DebugAutomationProfile.SafeNavigation &&
                       m_debugRouteSequencer.Mode == DebugAutomationMode.DeterministicValidation;
            DebugSetInvulnerable(safe);
            DebugSetThreatsFrozen(safe);
        }

        private string _finishDebugRouteReport()
        {
            return m_debugRouteSequencer?.FinishReport(CurrentSignal, m_metrics, m_model.OptionalSalvageSecured,
                m_model.ShortcutOpen, m_world?.Player.position ?? Vector3.zero,
                m_model?.Outcome ?? RunOutcome.Destroyed,
                m_liveBalanceCombatPolicy.DirectedShots,
                m_liveBalanceCombatPolicy.EvasionResponses) ?? "No route report available.";
        }

        private void _writeDebugRouteReport()
        {
            if (m_debugRouteReportWritten)
            {
                return;
            }

            var directory = Path.Combine(UnityEngine.Application.persistentDataPath, "PlaytestReports");
            Directory.CreateDirectory(directory);
            var preset = m_debugRouteSequencer?.Preset.ToString() ?? "Unknown";
            var path = Path.Combine(directory, $"route-{preset}-{System.DateTime.Now:yyyyMMdd-HHmmss}.txt");
            File.WriteAllText(path, _finishDebugRouteReport());
            m_lastDebugCapturePath = path;
            m_debugRouteReportWritten = true;
        }

        private void _finalizeDebugRouteAfterOutcome()
        {
            if (m_debugRouteSequencer == null || m_debugRouteReportWritten ||
                m_debugRouteSequencer.State == DebugRouteRunState.Idle)
            {
                return;
            }

            if (m_debugRouteSequencer.IsRunning)
            {
                m_debugRouteSequencer.Abort($"Run ended with {m_model.Outcome} before route completion.");
            }
            _writeDebugRouteReport();
        }

        private void _tryStartCommandLineDebugRoute()
        {
            if (!DeadSignalDebugMenu.IsAvailable)
            {
                return;
            }
            var arguments = System.Environment.GetCommandLineArgs();
            if (TryParseTacticalWindowScenario(arguments, out var tacticalWindowScenario))
            {
                DebugApplyScenario(tacticalWindowScenario);
                if (HasTacticalWindowSweepArgument(arguments))
                {
                    StartCoroutine(_captureTacticalWindowSweepSequence(tacticalWindowScenario, true, true));
                }
                else if (HasTacticalWindowCaptureArgument(arguments))
                {
                    StartCoroutine(_captureTacticalWindowSequence(tacticalWindowScenario));
                }
                return;
            }
            if (TryParseCombatLabScenario(arguments, out var combatLabScenario))
            {
                DebugApplyScenario(combatLabScenario);
                return;
            }

            const string PREFIX = "-DEADSIGNALROUTE=";
            const string LIVE_ROUTE_ARGUMENT = "-DEADSIGNALLIVEROUTE";
            var liveRoute = arguments.Any(argument =>
                argument.Equals(LIVE_ROUTE_ARGUMENT, System.StringComparison.OrdinalIgnoreCase));
            foreach (var argument in arguments)
            {
                if (!argument.StartsWith(PREFIX, System.StringComparison.OrdinalIgnoreCase) ||
                    !System.Enum.TryParse(argument.Substring(PREFIX.Length), true, out DebugRoutePreset preset))
                {
                    continue;
                }
                DebugStartRouteSequence(
                    preset,
                    liveRoute ? DebugAutomationMode.AssistedPlaythrough : DebugAutomationMode.DeterministicValidation,
                    liveRoute ? DebugAutomationProfile.LiveBalance : DebugAutomationProfile.SafeNavigation);
                return;
            }
        }

        private float _debugRouteCompletionRadius(DebugLocation location)
        {
            return location switch
            {
                DebugLocation.CentralTower or DebugLocation.RelayTower or DebugLocation.SpineTower =>
                    TOWER_INTERACTION_RADIUS - 0.2f,
                DebugLocation.Extraction => 1.5f,
                DebugLocation.Shortcut => 1.75f,
                _ => 0.65f
            };
        }

        private string _debugInteractionTelemetry()
        {
            var target = m_world.GetObjectiveTarget(m_model);
            var distance = DeadSignalWorld.FlatDistance(m_world.Player.position, target);
            var radius = !m_model.TowerOnline || !m_model.RelayTowerOnline || !m_model.SpineTowerOnline
                ? TOWER_INTERACTION_RADIUS
                : m_model.CanExtract ? 1.65f : 1.9f;
            return $"{distance:0.00}m / {radius:0.00}m {(distance < radius ? "IN RANGE" : "OUT OF RANGE")}";
        }

        private void _sampleDebugSignalRate()
        {
            if (m_model == null)
            {
                return;
            }

            if (m_debugPreviousSignal >= 0f && Time.unscaledDeltaTime > 0f)
            {
                var instantaneousRate = (m_model.Signal - m_debugPreviousSignal) / Time.unscaledDeltaTime;
                m_debugSignalDeltaRate = Mathf.Lerp(m_debugSignalDeltaRate, instantaneousRate, 0.2f);
            }
            m_debugPreviousSignal = m_model.Signal;
        }

        private void _applyEasternRoomCombatScenario(bool includeSwarmers)
        {
            m_debugCombatScenario = Object.FindObjectsByType<AuthoredCombatScenario>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(scenario => scenario.name == "Eastern Combat Scenario");
            if (m_debugCombatScenario == null || !m_debugCombatScenario.IsComplete)
            {
                _showFeedback("DEBUG — EASTERN COMBAT ANCHORS MISSING");
                return;
            }

            DebugActivateTower();
            if (!m_model.CentralPayloadSecured)
            {
                DebugCollectNextCache();
            }
            if (m_overclockChoice.IsPrimaryPending)
            {
                DebugSelectOverclock(SignalOverclock.ChainArc);
            }
            DebugActivateRelayTower();
            if (m_overclockChoice.IsWeaponPending)
            {
                DebugSelectWeapon(SignalWeaponOverclock.PiercingPulse);
            }
            if (!m_model.RelayPayloadSecured)
            {
                DebugCollectNextCache();
            }
            if (m_overclockChoice.IsAuxiliaryPending)
            {
                DebugSelectAuxiliary(SignalAuxiliaryOverclock.FeedbackShield);
            }
            DebugActivateSpineTower();

            m_model.SetSignalForDebug(RunModel.MaximumSignal);
            m_world.Player.SetPositionAndRotation(
                m_debugCombatScenario.PlayerAnchor.position,
                m_debugCombatScenario.PlayerAnchor.rotation);
            m_playerMovement = new PlayerDroneMovement();
            m_world.PlayerCamera.SnapToFocus(m_debugCombatScenario.CameraFocus.position);
            m_threats.ConfigureForDebugScenario(m_debugCombatScenario, includeSwarmers);
            m_debugCombatScenarioIncludesSwarmers = includeSwarmers;
            m_debugCombatScenarioSeconds = 0f;
            m_debugCombatScenarioActive = true;
            m_hud.SetDebugObjective(
                "COMBAT LAB  //  SURVIVE 30 SECONDS\n" +
                (includeSwarmers
                    ? "KEEP MOVING — PURGE TWO SWARMER TRIOS THROUGH SPECIALIST PRESSURE\n"
                    : "MATCHED CONTROL — FOUR SPECIALISTS, NO SWARMER PRESSURE\n") +
                "DEBUG SHIELD ACTIVE — FIRE AND EVADE NORMALLY");
            if (m_missionClarityHud != null)
            {
                m_missionClarityHud.enabled = false;
            }
            if (m_objectiveBeacon is MonoBehaviour objectiveBeacon)
            {
                objectiveBeacon.enabled = false;
            }
            _showFeedback("DEBUG — EASTERN COMBAT LAB READY");
        }

        private void _applyTacticalWindowScenario(DebugLocation location)
        {
            m_debugTacticalWindowScenario = location == DebugLocation.Extraction
                ? DebugScenario.OpeningTacticalWindow
                : DebugScenario.SpineReturnTacticalWindow;
            if (location == DebugLocation.Extraction)
            {
                DebugMakeExtractionReady();
            }
            else
            {
                DebugActivateSpineTower();
            }

            DebugTeleport(location);
            if (location == DebugLocation.SpineTower)
            {
                m_world.Player.position = m_world.ClampToArena(
                    m_world.SpineTowerPosition + Vector3.forward * 3.35f,
                    0.8f);
            }
            m_world.PlayerCamera.SnapToFocus(m_world.Player.position);
            m_model.SetSignalForDebug(RunModel.MaximumSignal);
            m_threats.ResetDebugScenario();
            DebugSetInvulnerable(true);
            foreach (var reinforcement in new[]
                     {
                         SecurityReinforcement.Warden, SecurityReinforcement.Sapper,
                         SecurityReinforcement.Interceptor, SecurityReinforcement.Suppressor
                     })
            {
                DebugPurgeThreat(reinforcement);
            }
            DebugForceThreatAttack(SecurityReinforcement.Sapper);
            var playerViewport = m_world.Camera.WorldToViewportPoint(m_world.Player.position + Vector3.up * 0.5f);
            var stagingRay = m_world.Camera.ViewportPointToRay(new Vector3(0.7f, playerViewport.y, 0f));
            var actorPlane = new Plane(Vector3.up, m_world.Player.position + Vector3.up * 0.5f);
            if (actorPlane.Raycast(stagingRay, out var stagingDistance))
            {
                var stagingPosition = stagingRay.GetPoint(stagingDistance) - Vector3.up * 0.5f;
                m_world.Sapper.position = m_world.ClampToArena(stagingPosition, 0.8f);
            }
            _fireTacticalWindowTrace();
            m_hud.SetDebugObjective(
                $"TACTICAL WINDOW  //  {(location == DebugLocation.Extraction ? "OPENING RETURN" : "SPINE RETURN")}\n" +
                "MOVE, FIRE, AND CAPTURE THE THREAT, BOLT PATH, AND ONE ESCAPE LANE");
            _showFeedback("DEBUG — TACTICAL WINDOW CAPTURE READY");
        }

        private IEnumerator _captureTacticalWindowSweepSequence(
            DebugScenario scenario,
            bool captureFrames,
            bool exitWhenComplete)
        {
            const float CAMERA_SETTLE_SECONDS = 1f;
            var legs = new[]
            {
                (Input: Vector2.left, Seconds: 0.35f, Label: "Left"),
                (Input: Vector2.right, Seconds: 0.7f, Label: "Right"),
                (Input: Vector2.left, Seconds: 0.35f, Label: "Return")
            };
            var scenarioLabel = scenario == DebugScenario.OpeningTacticalWindow ? "Opening" : "SpineReturn";
            m_debugTacticalWindowSweepActive = true;
            m_debugTacticalWindowSweepPassed = true;
            m_debugTacticalWindowSweepSamples = 0;
            m_debugTacticalWindowSweepUnsafeActorSamples = 0;
            m_debugTacticalWindowSweepDistance = 0f;
            m_debugTacticalWindowSweepMaximumCoverage = 0f;
            m_debugTacticalWindowMoveInput = Vector2.zero;

            yield return new WaitForSecondsRealtime(CAMERA_SETTLE_SECONDS);
            _fireTacticalWindowTrace();
            yield return captureFrames ? new WaitForEndOfFrame() : null;
            _recordTacticalWindowSweepSample(scenarioLabel, "Center", captureFrames);

            foreach (var leg in legs)
            {
                var legStart = m_world.Player.position;
                var elapsed = 0f;
                m_debugTacticalWindowMoveInput = leg.Input;
                while (elapsed < leg.Seconds)
                {
                    yield return null;
                    elapsed += Time.unscaledDeltaTime;
                }

                m_debugTacticalWindowMoveInput = Vector2.zero;
                m_debugTacticalWindowSweepDistance += DeadSignalWorld.FlatDistance(legStart, m_world.Player.position);
                _fireTacticalWindowTrace();
                yield return captureFrames ? new WaitForEndOfFrame() : null;
                _recordTacticalWindowSweepSample(scenarioLabel, leg.Label, captureFrames);
            }

            m_debugTacticalWindowMoveInput = Vector2.zero;
            m_debugTacticalWindowSweepActive = false;
            var result = m_debugTacticalWindowSweepPassed ? "PASS" : "FAIL";
            Debug.Log(
                $"[DEAD SIGNAL TACTICAL WINDOW SWEEP] {result} | {scenarioLabel} | {Screen.width}x{Screen.height} | " +
                $"samples={m_debugTacticalWindowSweepSamples} | distance={m_debugTacticalWindowSweepDistance:0.00}m | " +
                $"maxCoverage={m_debugTacticalWindowSweepMaximumCoverage:P1} | production state unchanged");
            if (exitWhenComplete && !UnityEngine.Application.isEditor)
            {
                UnityEngine.Application.Quit(m_debugTacticalWindowSweepPassed ? 0 : 1);
            }
        }

        private void _recordTacticalWindowSweepSample(string scenarioLabel, string sampleLabel, bool captureFrame)
        {
            var coverage = TacticalWindowCoverageDiagnostic.Measure(
                m_world.Camera,
                FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                    .Where(renderer => renderer is MeshRenderer &&
                                       renderer.bounds.center.y > 0.2f && renderer.bounds.size.y > 0.45f));
            var maximumCoverage = coverage.Count > 0 ? coverage[0].WindowCoverage : 1f;
            var playerViewport = m_world.Camera.WorldToViewportPoint(m_world.Player.position + Vector3.up * 0.5f);
            var sapperViewport = m_world.Camera.WorldToViewportPoint(m_world.Sapper.position + Vector3.up * 0.5f);
            var actorsSafe = AreTacticalWindowActorsInSafeViewport;
            m_debugTacticalWindowSweepSamples++;
            if (!actorsSafe)
            {
                m_debugTacticalWindowSweepUnsafeActorSamples++;
            }
            m_debugTacticalWindowSweepMaximumCoverage = Mathf.Max(
                m_debugTacticalWindowSweepMaximumCoverage,
                maximumCoverage);
            m_debugTacticalWindowSweepPassed &= actorsSafe && maximumCoverage <= 0.2f;
            Debug.Log(
                $"[DEAD SIGNAL TACTICAL WINDOW SWEEP] SAMPLE | {scenarioLabel} | {sampleLabel} | " +
                $"actorsSafe={actorsSafe} | coverage={maximumCoverage:P1} | " +
                $"playerViewport={playerViewport.x:0.00},{playerViewport.y:0.00} | " +
                $"sapperViewport={sapperViewport.x:0.00},{sapperViewport.y:0.00} | " +
                $"player={m_world.Player.position.x:0.00},{m_world.Player.position.z:0.00}");
            if (captureFrame)
            {
                _debugCaptureScreenshot(
                    $"TacticalWindowSweep-{scenarioLabel}-{Screen.width}x{Screen.height}-{sampleLabel}");
            }
        }

        private void _fireTacticalWindowTrace()
        {
            var toSapper = m_world.Sapper.position - m_world.Player.position;
            toSapper.y = 0f;
            var perpendicular = toSapper.sqrMagnitude > 0.01f
                ? Vector3.Cross(Vector3.up, toSapper.normalized)
                : Vector3.right;
            DebugFireAt(m_world.Sapper.position + perpendicular * 1.1f);
        }

        private IEnumerator _captureTacticalWindowSequence(DebugScenario scenario)
        {
            const float CAMERA_SETTLE_SECONDS = 1f;
            const float BETWEEN_CAPTURES_SECONDS = 0.5f;
            var scenarioLabel = scenario == DebugScenario.OpeningTacticalWindow ? "Opening" : "SpineReturn";

            yield return new WaitForSecondsRealtime(CAMERA_SETTLE_SECONDS);
            for (var captureIndex = 1; captureIndex <= 2; captureIndex++)
            {
                DebugFireAt(m_world.Sapper.position);
                yield return new WaitForEndOfFrame();
                var label = $"TacticalWindow-{scenarioLabel}-{Screen.width}x{Screen.height}-{captureIndex}";
                _debugCaptureScreenshot(label);
                yield return new WaitForSecondsRealtime(BETWEEN_CAPTURES_SECONDS);
            }

            yield return new WaitForSecondsRealtime(0.5f);
            Debug.Log(
                $"[DEAD SIGNAL TACTICAL WINDOW] PASS | {scenarioLabel} | {Screen.width}x{Screen.height} | " +
                "captures=2 | production state unchanged");
            if (!UnityEngine.Application.isEditor)
            {
                UnityEngine.Application.Quit(0);
            }
        }

        private void _debugCaptureScreenshot(string label)
        {
            var directory = Path.Combine(UnityEngine.Application.persistentDataPath, "PlaytestCaptures");
            Directory.CreateDirectory(directory);
            var captureLabel = string.IsNullOrWhiteSpace(label) ? string.Empty : $"-{label}";
            m_lastDebugCapturePath = Path.Combine(
                directory,
                $"DeadSignal{captureLabel}-{System.DateTime.Now:yyyyMMdd-HHmmssfff}.png");
            ScreenCapture.CaptureScreenshot(m_lastDebugCapturePath);
            _showFeedback("DEBUG — SCREENSHOT CAPTURED");
        }

        private int _activeDebugThreatCount()
        {
            var count = 0;
            if (WardenHealth > 0f) count++;
            if (SapperHealth > 0f) count++;
            if (InterceptorHealth > 0f) count++;
            if (SuppressorHealth > 0f) count++;
            return count + ActiveSwarmerCount;
        }

        private bool _areActiveSwarmersInSafeViewport()
        {
            if (m_threats == null)
            {
                return false;
            }

            foreach (var swarmer in m_threats.ActiveSwarmers)
            {
                if (!_isInSafeViewport(swarmer))
                {
                    return false;
                }
            }
            return true;
        }

        private bool _isInSafeViewport(Transform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy || m_world?.Camera == null)
            {
                return false;
            }

            var viewport = m_world.Camera.WorldToViewportPoint(target.position + Vector3.up * 0.5f);
            return viewport.z > 0f && viewport.x >= 0.15f && viewport.x <= 0.85f &&
                   viewport.y >= 0.15f && viewport.y <= 0.85f;
        }

        private string _viewportState(Transform target)
        {
            if (target == null || m_world?.Camera == null)
            {
                return "MISSING";
            }

            var viewport = m_world.Camera.WorldToViewportPoint(target.position + Vector3.up * 0.5f);
            return $"{(_isInSafeViewport(target) ? "SAFE" : "OUT")}({viewport.x:0.00},{viewport.y:0.00})";
        }

        private void _resetDebugTransientState()
        {
            m_debugRouteDriving = false;
            m_debugRouteBlockedSeconds = 0f;
            m_debugObservedSequenceStep = -1;
            m_fireBuffered = false;
            m_debugFireHeld = false;
            m_playerMovement?.ApplyResolvedVelocity(Vector3.zero);
            if (m_debugCombatScenarioActive)
            {
                m_threats?.ResetDebugScenario();
            }
            else
            {
                m_threats?.SetPlayerInvulnerableForDebug(false);
            }
            m_debugCombatScenarioActive = false;
            m_debugCombatScenarioIncludesSwarmers = false;
            m_debugCombatScenarioSeconds = 0f;
            m_hud?.SetDebugObjective(string.Empty);
            if (m_missionClarityHud != null)
            {
                m_missionClarityHud.enabled = true;
            }
            if (m_objectiveBeacon is MonoBehaviour objectiveBeacon)
            {
                objectiveBeacon.enabled = true;
            }
        }

        private Vector3 _debugLocationPosition(DebugLocation location)
        {
            return location switch
            {
                DebugLocation.Extraction => m_world.ExtractionPosition,
                DebugLocation.CentralTower => m_world.TowerPosition,
                DebugLocation.Shortcut => m_world.ShortcutPosition,
                DebugLocation.RelayTower => m_world.RelayTowerPosition,
                DebugLocation.SpineTower => m_world.SpineTowerPosition,
                DebugLocation.CacheOne => m_world.GetSalvagePosition(0),
                DebugLocation.CacheTwo => m_world.GetSalvagePosition(1),
                DebugLocation.CacheThree => m_world.GetSalvagePosition(2),
                DebugLocation.CacheFour => _debugOptionalCachePosition(),
                DebugLocation.FarEast => new Vector3(m_world.ArenaHalfExtents.x - 1.2f, 0f, 0f),
                DebugLocation.NorthBoundary => new Vector3(0f, 0f, m_world.ArenaHalfExtents.y - 1.2f),
                DebugLocation.SouthBoundary => new Vector3(0f, 0f, -m_world.ArenaHalfExtents.y + 1.2f),
                DebugLocation.CurrentObjective => m_world.GetObjectiveTarget(m_model),
                _ => m_world.ExtractionPosition
            };
        }

        private Vector3 _debugOptionalCachePosition()
        {
            var optional = m_world.SalvagePickups.FirstOrDefault(pickup =>
                pickup.activeSelf && m_world.IsOptionalCache(pickup));
            return optional != null ? optional.transform.position : m_world.GetSalvagePosition(3);
        }

        private IEnumerator _debugStepFrame()
        {
            m_combatFeedback.SetPaused(false);
            Time.timeScale = 1f;
            yield return null;
            m_combatFeedback.SetPaused(true);
        }

        private IEnumerator _debugCaptureCombatSequence()
        {
            DebugExerciseCombatFeedback();
            yield return new WaitForEndOfFrame();
            DebugCaptureScreenshot();
        }

        private IEnumerator _debugVisitCameraBoundaries()
        {
            var locations = new[] { DebugLocation.Extraction, DebugLocation.NorthBoundary, DebugLocation.FarEast,
                DebugLocation.SouthBoundary };
            foreach (var location in locations)
            {
                DebugTeleport(location);
                yield return new WaitForEndOfFrame();
            }
        }

        private void _tickRunSystems(float dt, bool powered)
        {
            _tickCombatChamber();
            m_world.TickTower(dt, m_model.TowerOnline);
            m_threats.Tick(dt, powered);
            _tryTriggerEmergencyCapacitor();
            m_salvage.Tick(dt);
            if (m_model.OptionalSalvageSecured)
            {
                m_world.OpenQuenchReturn();
            }
            if (m_model.CanExtract)
            {
                m_world.OpenDepartureReturn();
                if (m_world.TryConsumeDepartureSurge(m_world.Player.position))
                {
                    var restored = m_model.RestoreSignal(DEPARTURE_SURGE_SIGNAL_RESTORE);
                    m_metrics.RecordDepartureSurge(restored);
                    m_combatFeedback.PlaySignalRecovery(m_world.Player.position + Vector3.up * 0.45f);
                    m_audio.Play(DeadSignalAudioCue.TowerOnline);
                    _showFeedback($"DEPARTURE CAPACITOR DISCHARGED  +{restored:0} SIGNAL");
                }
            }
            m_world.TickExtraction(dt, m_model.CanExtract);
            if (m_extractionUplink.Tick(dt))
            {
                _completeExtraction();
            }
            m_metrics.RecordSignal(m_model.Signal);
        }

        private void _tickCombatChamber()
        {
            if (m_combatChamber == null)
            {
                return;
            }

            if (m_combatChamber.TryBeginLockdown(m_world.Player.position))
            {
                m_world.RefreshNavigation();
                m_threats.BeginCombatChamberPhase(m_combatChamber.CombatScenario, m_combatChamber.Phase);
                m_audio.Play(DeadSignalAudioCue.SecurityImpact);
                _showFeedback("SECURITY LOCKDOWN — PHASE 1  //  PURGE THE SWARM");
                return;
            }

            if (m_combatChamber.State != CombatChamberState.Lockdown ||
                !m_threats.IsCombatChamberPhaseCleared())
            {
                return;
            }

            if (m_combatChamber.AdvancePhase())
            {
                m_threats.BeginCombatChamberPhase(m_combatChamber.CombatScenario, m_combatChamber.Phase);
                var objective = m_combatChamber.Phase == 2
                    ? "PHASE 2 — SWARM + WARDEN  //  CREATE SPACE"
                    : "PHASE 3 — SWARM + SAPPER  //  PROTECT THE SIGNAL";
                _showFeedback(objective);
                return;
            }

            m_threats.EndCombatChamber();
            m_combatChamber.Complete();
            m_world.RefreshNavigation();
            m_audio.Play(DeadSignalAudioCue.TowerOnline);
            _showFeedback("SECURITY TRIAL CLEARED — VAULT OPEN  //  RETURN ROUTE POWERED");
        }

        private void _setPaused(bool paused)
        {
            m_combatFeedback.SetPaused(paused);
            m_audio.SetPaused(paused);
            m_signalDust.SetPaused(paused);
            m_world.PlayerSignalWake.SetPaused(paused);
        }
    }
}
