using UnityEngine;

namespace DeadSignal
{
    /// <summary>Draws high-priority, runtime-only coaching above the authored HUD.</summary>
    public sealed class MissionClarityHud : MonoBehaviour
    {
        private const float TARGET_PULSE_DURATION = 2.25f;

        private RunModel m_model;
        private RunMetrics m_metrics;
        private DeadSignalWorld m_world;
        private SignalOverclockChoice m_overclocks;
        private Camera m_camera;
        private GUIStyle m_panelStyle;
        private GUIStyle m_primaryStyle;
        private GUIStyle m_smallStyle;
        private int m_lastSalvage;
        private float m_targetPulse;

        public float DrainMultiplier { get; set; } = 1f;
        public float DashCooldown { get; set; }
        public bool IsMoving { get; set; }
        public bool IsPowered { get; set; }

        internal void Configure(
            RunModel model,
            RunMetrics metrics,
            DeadSignalWorld world,
            SignalOverclockChoice overclocks)
        {
            m_model = model;
            m_metrics = metrics;
            m_world = world;
            m_overclocks = overclocks;
            m_camera = world.Camera;
            m_lastSalvage = model.Salvage;
        }

        private void Update()
        {
            if (m_model == null)
            {
                return;
            }

            if (m_model.Salvage != m_lastSalvage)
            {
                m_lastSalvage = m_model.Salvage;
                m_targetPulse = TARGET_PULSE_DURATION;
            }

            m_targetPulse = Mathf.Max(0f, m_targetPulse - Time.unscaledDeltaTime);
        }

        private void OnGUI()
        {
            if (m_model == null || m_model.Outcome != RunOutcome.Running || Time.timeScale <= 0f)
            {
                return;
            }

            _ensureStyles();
            _drawSignalEconomy();
            _drawAbilityStatus();
            _drawObjectiveMarker();
            _drawThreatRewardMarkers();
            if (m_model.IsCriticalRecovery)
            {
                _drawCriticalRecovery();
            }
        }

        private void _drawSignalEconomy()
        {
            var passive = RunModel.PassiveDrainRate(IsPowered);
            var movement = RunModel.MovementDrainRate(IsMoving, IsPowered);
            var total = (passive + movement) * DrainMultiplier;
            var source = IsPowered ? (IsMoving ? "POWERED MOVEMENT" : "POWERED SAFE ZONE") :
                IsMoving ? "DEAD ZONE + MOVEMENT" : "DEAD ZONE EXPOSURE";
            GUI.Label(new Rect(18f, 102f, 310f, 48f),
                total > 0f ? $"SIGNAL  −{total:0.0}/s\n{source}" : "SIGNAL  STABLE\nPOWERED SAFE ZONE", m_smallStyle);
        }

        private void _drawAbilityStatus()
        {
            var primary = m_overclocks.Selected == SignalOverclock.None
                ? "PRIMARY  LOCKED — CACHE 1"
                : $"PRIMARY  {m_overclocks.Selected.ToString().ToUpperInvariant()}";
            var auxiliary = m_overclocks.SelectedAuxiliary == SignalAuxiliaryOverclock.None
                ? "AUXILIARY  LOCKED — CACHE 2"
                : $"AUXILIARY  {m_overclocks.SelectedAuxiliary.ToString().ToUpperInvariant()}";
            var dash = DashCooldown <= 0f ? "DASH  READY  [SHIFT / A]" : $"DASH  {DashCooldown:0.0}s";
            GUI.Label(new Rect(Screen.width - 326f, Screen.height - 112f, 308f, 94f),
                $"{primary}\n{auxiliary}\n{dash}", m_smallStyle);
        }

        private void _drawObjectiveMarker()
        {
            var target = _objectiveTarget();
            var distance = DeadSignalWorld.FlatDistance(m_world.Player.position, target);
            var point = m_camera.WorldToScreenPoint(target + Vector3.up * 1.6f);
            var behind = point.z <= 0f;
            var x = behind ? Screen.width - point.x : point.x;
            var y = Screen.height - point.y;
            // Keep the marker clear of the authored upper-corner HUD panels while retaining edge guidance.
            x = Mathf.Clamp(x, 120f, Screen.width - 360f);
            y = Mathf.Clamp(y, 175f, Screen.height - 132f);
            var pulse = m_targetPulse > 0f ? "  //  NEW ROUTE" : string.Empty;
            GUI.Label(new Rect(x - 105f, y - 22f, 210f, 44f), $"▲  {_objectiveName()}  {distance:0}m{pulse}", m_primaryStyle);
        }

        private void _drawThreatRewardMarkers()
        {
            _drawThreatReward(m_world.Warden, "+12 SIGNAL");
            _drawThreatReward(m_world.Sapper, "+16 SIGNAL");
            _drawThreatReward(m_world.Interceptor, "+14 SIGNAL");
            _drawThreatReward(m_world.Suppressor, "+15 SIGNAL");
        }

        private void _drawThreatReward(Transform threat, string reward)
        {
            if (threat == null || !threat.gameObject.activeSelf ||
                DeadSignalWorld.FlatDistance(m_world.Player.position, threat.position) > 8f)
            {
                return;
            }

            var point = m_camera.WorldToScreenPoint(threat.position + Vector3.up * 1.35f);
            if (point.z > 0f)
            {
                GUI.Label(new Rect(point.x - 55f, Screen.height - point.y, 110f, 24f), reward, m_smallStyle);
            }
        }

        private void _drawCriticalRecovery()
        {
            var scale = 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.05f;
            var width = 480f * scale;
            GUI.Label(new Rect((Screen.width - width) * 0.5f, Screen.height * 0.34f, width, 100f),
                $"EMERGENCY LINK  {m_model.CriticalRecoveryRemaining:0.0}s\nREACH CYAN POWER OR PURGE A MARKED THREAT", m_primaryStyle);
        }

        private Vector3 _objectiveTarget()
        {
            if (!m_model.TowerOnline)
            {
                return m_world.TowerPosition;
            }
            if (m_model.CanExtract)
            {
                return m_world.ExtractionPosition;
            }

            var nearest = m_world.TowerPosition;
            var nearestDistance = float.PositiveInfinity;
            foreach (var salvage in m_world.SalvagePickups)
            {
                if (!salvage.activeSelf)
                {
                    continue;
                }

                var distance = DeadSignalWorld.FlatDistance(m_world.Player.position, salvage.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = salvage.transform.position;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        private string _objectiveName()
        {
            if (!m_model.TowerOnline)
            {
                return "TOWER";
            }
            return m_model.CanExtract ? "EXTRACTION" : "CACHE";
        }

        private void _ensureStyles()
        {
            if (m_panelStyle != null)
            {
                return;
            }

            m_panelStyle = new GUIStyle(GUI.skin.box);
            m_panelStyle.normal.background = Texture2D.whiteTexture;
            m_primaryStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            m_primaryStyle.normal.textColor = new Color(1f, 0.72f, 0.16f);
            m_smallStyle = new GUIStyle(m_primaryStyle)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 12
            };
            m_smallStyle.normal.textColor = new Color(0.35f, 0.95f, 1f);
        }
    }
}
