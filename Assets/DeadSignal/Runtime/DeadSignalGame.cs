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
        private ICombatFeedback m_combatFeedback;
        private IComfortSettings m_comfortSettings;
        private IDeadSignalHud m_hud;
        private IObjectiveBeacon m_objectiveBeacon;
        private Container m_container;
        private bool m_lastPoweredState;
        private bool m_fireBuffered;

        public float CurrentSignal => m_model?.Signal ?? 0f;
        public bool IsSapperLatched => m_threats?.IsSapperLatched ?? false;
        public bool IsPaused => m_combatFeedback?.IsPaused ?? false;
        public bool HasPauseInsignia => m_hud?.HasPauseInsignia ?? false;
        public bool HasCameraComfortIcon => m_hud?.HasCameraComfortIcon ?? false;
        public bool HasReducedFlashesIcon => m_hud?.HasReducedFlashesIcon ?? false;
        public bool HasObjectiveBeaconIcon => m_objectiveBeacon?.HasIcon ?? false;
        public ObjectiveBeaconPhase CurrentObjectiveBeaconPhase => m_objectiveBeacon?.CurrentPhase ?? ObjectiveBeaconPhase.Tower;
        public Vector3 CurrentObjectiveBeaconTarget => m_objectiveBeacon?.CurrentTarget ?? Vector3.zero;
        public bool IsCameraImpulseEnabled => m_comfortSettings?.CameraImpulseEnabled ?? true;
        public bool IsReducedFlashesEnabled => m_comfortSettings?.ReducedFlashesEnabled ?? false;

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

        [Inject]
        private void _construct(
            ICombatFeedback combatFeedback,
            IComfortSettings comfortSettings,
            IDeadSignalHud hud,
            IObjectiveBeacon objectiveBeacon,
            Container container)
        {
            m_combatFeedback = combatFeedback;
            m_comfortSettings = comfortSettings;
            m_hud = hud;
            m_objectiveBeacon = objectiveBeacon;
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
            m_threats = new DeadSignalThreatController(m_model, m_metrics, m_world, m_combatFeedback, _showFeedback);
            m_salvage = new DeadSignalSalvageController(m_model, m_world, _showFeedback);
            m_hud.Configure(m_model, m_metrics, m_world, m_threats);
            m_objectiveBeacon.Configure(m_model, m_world);
            m_lastPoweredState = m_world.IsPowered(m_world.Player.position, m_model.TowerOnline);
        }

        private void Update()
        {
            if (m_model.Outcome == RunOutcome.Running && DeadSignalInput.PressedPause())
            {
                _setPaused(!IsPaused);
            }

            if (IsPaused && DeadSignalInput.PressedCameraImpulseToggle())
            {
                ToggleCameraImpulse();
            }

            if (IsPaused && DeadSignalInput.PressedReducedFlashesToggle())
            {
                ToggleReducedFlashes();
            }

            if (m_combatFeedback.IsFrozen)
            {
                if (!IsPaused && DeadSignalInput.PressedFire())
                {
                    m_fireBuffered = true;
                }

                return;
            }

            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            m_hud.Tick(dt);
            m_threats.TickCooldown(dt);

            if (m_model.Outcome != RunOutcome.Running)
            {
                if (DeadSignalInput.PressedRestart())
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }

                return;
            }

            var movement = _updatePlayer(dt);
            var aimDirection = DeadSignalInput.ReadAimDirection(m_world.Camera, m_world.Player);
            if (aimDirection.sqrMagnitude > 0.01f)
            {
                m_world.Player.rotation = Quaternion.LookRotation(aimDirection, Vector3.up);
            }

            bool powered = m_world.IsPowered(m_world.Player.position, m_model.TowerOnline);
            m_model.Advance(dt, movement.sqrMagnitude > 0.01f, powered);
            m_metrics.Advance(dt, powered);
            if (powered != m_lastPoweredState)
            {
                _showFeedback(powered ? "NETWORK LINK RESTORED" : "DEAD ZONE — SIGNAL BLEED");
                m_lastPoweredState = powered;
            }

            if ((m_fireBuffered || DeadSignalInput.PressedFire()) && m_threats.CanFire)
            {
                m_fireBuffered = false;
                m_threats.TryFire(aimDirection);
            }

            if (DeadSignalInput.PressedInteract())
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
            var moveInput = DeadSignalInput.ReadMovement();
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

        private void _handleInteraction()
        {
            if (!m_model.TowerOnline && DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.TowerPosition) < 1.8f)
            {
                if (m_model.TryActivateTower())
                {
                    m_world.ActivateTower(DeadSignalThreatController.SAPPER_PULSE_INTERVAL);
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
        }
    }
}
