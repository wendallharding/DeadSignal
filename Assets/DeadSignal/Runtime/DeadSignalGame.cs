using Reflex.Attributes;
using Reflex.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSignal
{
    /// <summary>
    /// Coordinates one run while delegating input, world presentation, threats, salvage, and HUD ownership.
    /// </summary>
    public sealed class DeadSignalGame : MonoBehaviour
    {
        private const float PLAYER_SPEED = 6.4f;
        private const float PLAYER_COLLISION_RADIUS = 0.48f;

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
        private ILowSignalWarning m_lowSignalWarning;
        private ITowerActivationSweep m_towerActivationSweep;
        private Container m_container;
        private bool m_lastPoweredState;
        private bool m_fireBuffered;

        public float CurrentSignal => m_model?.Signal ?? 0f;
        public bool IsSapperLatched => m_threats?.IsSapperLatched ?? false;
        public bool IsPaused => m_combatFeedback?.IsPaused ?? false;
        public bool HasPauseInsignia => m_hud?.HasPauseInsignia ?? false;
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
        public ObjectiveBeaconPhase CurrentObjectiveBeaconPhase => m_objectiveBeacon?.CurrentPhase ?? ObjectiveBeaconPhase.Tower;
        public Vector3 CurrentObjectiveBeaconTarget => m_objectiveBeacon?.CurrentTarget ?? Vector3.zero;
        public bool IsCameraImpulseEnabled => m_comfortSettings?.CameraImpulseEnabled ?? true;
        public bool IsReducedFlashesEnabled => m_comfortSettings?.ReducedFlashesEnabled ?? false;
        public bool IsHighContrastEnabled => m_comfortSettings?.HighContrastEnabled ?? false;

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
            Application.targetFrameRate = 120;

            m_model = new RunModel();
            m_metrics = new RunMetrics();
            m_world = new DeadSignalWorld(transform, m_comfortSettings);
            m_combatFeedback.Configure(m_world.Camera);
            m_threats = new DeadSignalThreatController(m_model, m_metrics, m_world, m_combatFeedback, m_audio, _showFeedback);
            m_salvage = new DeadSignalSalvageController(m_model, m_world, m_audio, _showFeedback);
            m_hud.Configure(m_model, m_metrics, m_world, m_threats);
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
            _handlePauseInput();

            if (m_combatFeedback.IsFrozen)
            {
                if (!IsPaused && m_input.PressedFire())
                {
                    m_fireBuffered = true;
                }

                return;
            }

            var dt = Mathf.Min(Time.deltaTime, 0.05f);
            m_hud.Tick(dt);
            m_threats.TickCooldown(dt);

            if (m_model.Outcome != RunOutcome.Running)
            {
                if (m_input.PressedRestart())
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }

                return;
            }

            var movement = _updatePlayer(dt);
            var aimDirection = m_input.ReadAimDirection(m_world.Camera, m_world.Player);
            if (aimDirection.sqrMagnitude > 0.01f)
            {
                m_world.Player.rotation = Quaternion.LookRotation(aimDirection, Vector3.up);
            }

            var powered = m_world.IsPowered(m_world.Player.position, m_model.TowerOnline);
            m_audio.Tick(powered, m_model.TowerOnline, m_model.Signal / RunModel.MaximumSignal);
            m_model.Advance(dt, movement.sqrMagnitude > 0.01f, powered);
            m_signalDust.Tick(powered, m_model.TowerOnline, m_model.Signal / RunModel.MaximumSignal);
            m_lowSignalWarning.Tick(dt);
            m_metrics.Advance(dt, powered);
            if (powered != m_lastPoweredState)
            {
                _showFeedback(powered ? "NETWORK LINK RESTORED" : "DEAD ZONE — SIGNAL BLEED");
                m_lastPoweredState = powered;
            }

            if ((m_fireBuffered || m_input.PressedFire()) && m_threats.CanFire)
            {
                m_fireBuffered = false;
                m_threats.TryFire(aimDirection);
            }

            if (m_input.PressedInteract())
            {
                _handleInteraction();
            }

            m_world.TickTower(dt, m_model.TowerOnline);
            m_threats.Tick(dt);
            m_salvage.Tick(dt);
            m_world.TickExtraction(dt, m_model.CanExtract);
        }

        private void OnDestroy()
        {
            if (m_container != null)
            {
                m_container.Dispose();
                m_container = null;
            }
        }

        private Vector3 _updatePlayer(float dt)
        {
            var moveInput = m_input.ReadMovement();
            var movement = new Vector3(moveInput.x, 0f, moveInput.y);
            if (movement.sqrMagnitude > 1f)
            {
                movement.Normalize();
            }

            var desired = m_world.Player.position + movement * (PLAYER_SPEED * dt);
            desired = m_world.ClampToArena(desired, 0.6f);
            m_world.Player.position = m_world.ResolveMovement(
                m_world.Player.position,
                desired,
                PLAYER_COLLISION_RADIUS,
                m_model.ShortcutOpen);
            return movement;
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
        }

        private void _handleInteraction()
        {
            if (!m_model.TowerOnline && DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.TowerPosition) < 1.8f)
            {
                if (m_model.TryActivateTower())
                {
                    m_world.ActivateTower(DeadSignalThreatController.SAPPER_PULSE_INTERVAL);
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

            if (m_model.TryExtract())
            {
                m_audio.Play(DeadSignalAudioCue.Extraction);
                _showFeedback("EXTRACTION COMPLETE");
            }
            else
            {
                _showFeedback($"EXTRACTION LOCKED — {RunModel.SalvageRequired - m_model.Salvage} SALVAGE MISSING");
            }
        }

        private void _showFeedback(string message)
        {
            m_hud.ShowFeedback(message);
        }

        private void _setPaused(bool paused)
        {
            m_combatFeedback.SetPaused(paused);
            m_audio.SetPaused(paused);
            m_signalDust.SetPaused(paused);
        }
    }
}
