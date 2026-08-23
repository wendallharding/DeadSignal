using System.Linq;
using Reflex.Attributes;
using Reflex.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadSignal.Combat;
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

        public float CurrentSignal => m_model?.Signal ?? 0f;
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
        public int ActiveSignalBoltCount => transform.Cast<Transform>().Count(child => child.name == "Signal Bolt");

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
            m_overclockChoice = new SignalOverclockChoice();
            m_routeGuidanceStrength = Mathf.Clamp01(PlayerPrefs.GetFloat("DeadSignal.RouteGuidance", 0.7f));
            m_difficultyDrainMultiplier = Mathf.Clamp(PlayerPrefs.GetFloat("DeadSignal.DifficultyDrain", 1f), 0.75f, 1.2f);
            var routeVariant = (PlayerPrefs.GetInt("DeadSignal.RouteVariant", 0) + 1) % 3;
            PlayerPrefs.SetInt("DeadSignal.RouteVariant", routeVariant);
            PlayerPrefs.Save();

            m_world = new DeadSignalWorld(transform, m_comfortSettings);
            m_world.ConfigurePlayerSignalWake(m_playerMovementTuning);
            m_combatFeedback.Configure(m_world.Camera);
            var signalBoltTuning = Resources.Load<SignalBoltPresentationTuning>("Tuning/SignalBoltPresentationTuning");
            var threatTuning = Resources.Load<ThreatBalanceTuning>("Tuning/ThreatBalanceTuning");
            m_overclockTuning = Resources.Load<SignalOverclockTuning>("Tuning/SignalOverclockTuning");
            if (signalBoltTuning == null || threatTuning == null || m_overclockTuning == null)
            {
                Debug.LogError("Signal bolt, threat balance, or overclock tuning is missing from Resources/Tuning.", this);
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
            m_lastPoweredState = m_world.IsPowered(m_world.Player.position, m_model.TowerOnline);
            m_signalDust.Configure();
            m_signalDust.Tick(m_lastPoweredState, m_model.TowerOnline, m_model.Signal / RunModel.MaximumSignal);
            m_lowSignalWarning.Configure(m_model);
            m_lowSignalWarning.Tick(0f);
            m_towerActivationSweep.Configure(m_world.TowerPosition, DeadSignalWorld.TOWER_POWER_RADIUS);
        }

        private void Update()
        {
            if (m_focusInputBlockFrames > 0)
            {
                m_focusInputBlockFrames--;
                return;
            }

            _handlePauseInput();
            var dt = Mathf.Min(Time.deltaTime, 0.05f);
            m_hud.Tick(dt);

            if (m_combatFeedback.IsFrozen)
            {
                if (!IsPaused && m_input.PressedFire())
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
                if (m_input.PressedRestart())
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }

                return;
            }

            var movement = _updatePlayer(dt);
            var aimDirection = _applyAimAssist(m_input.ReadAimDirection(m_world.Camera, m_world.Player));
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
                if ((m_fireBuffered || m_input.PressedFire()) && m_threats.CanFire)
                {
                    m_fireBuffered = false;
                    m_threats.TryFire(aimDirection);
                }

                if (m_input.PressedInteract())
                {
                    _handleInteraction();
                }
            }

            m_world.TickTower(dt, m_model.TowerOnline);
            m_threats.Tick(dt, powered);
            _tryTriggerEmergencyCapacitor();
            m_salvage.Tick(dt);
            m_world.TickExtraction(dt, m_model.CanExtract);
            if (m_extractionUplink.Tick(dt))
            {
                _completeExtraction();
            }
        }

        private void OnDestroy()
        {
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

            var moveInput = m_input.ReadMovement();
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
            if (m_input.PressedDash() && m_dashCooldown <= 0f && moveInput.sqrMagnitude > 0.1f)
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
            return m_world.IsPowered(m_world.Player.position, m_model.TowerOnline) ||
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
            if (!m_model.TowerOnline && DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.TowerPosition) < 1.8f)
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
                _showFeedback($"EXTRACTION LOCKED — {RunModel.SalvageRequired - m_model.Salvage} SALVAGE MISSING");
            }
        }

        private bool _isExtractionUplinkChoiceAvailable()
        {
            return m_model.CanExtract &&
                   !m_extractionUplink.IsActive &&
                   !m_extractionUplink.IsComplete &&
                   DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.ExtractionPosition) < 1.65f;
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
            _showFeedback(feedback);
        }

        private void _handleOverclockChoice()
        {
            if (m_overclockChoice.IsAuxiliaryPending)
            {
                _handleAuxiliaryOverclockChoice();
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
