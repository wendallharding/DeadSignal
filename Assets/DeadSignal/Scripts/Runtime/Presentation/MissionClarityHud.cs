using UnityEngine;
using System.Collections.Generic;
using DeadSignal.Missions;
using DeadSignal.World;

namespace DeadSignal.Presentation
{
    /// <summary>Draws high-priority, runtime-only coaching above the authored HUD.</summary>
    public sealed class MissionClarityHud : MonoBehaviour
    {
        private RunModel m_model;
        private RunMetrics m_metrics;
        private DeadSignalWorld m_world;
        private SignalOverclockChoice m_overclocks;
        private Camera m_camera;
        private GUIStyle m_panelStyle;
        private GUIStyle m_primaryStyle;
        private GUIStyle m_smallStyle;
        private readonly Queue<SignalEvent> m_signalEvents = new();

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
        }

        private void OnGUI()
        {
            if (m_model == null || m_model.Outcome != RunOutcome.Running)
            {
                return;
            }

            _ensureStyles();
            if (Time.timeScale <= 0f)
            {
                _drawTacticalMap();
                return;
            }

            _drawSignalEconomy();
            if (m_model.IsCriticalRecovery)
            {
                _drawCriticalRecovery();
                _drawPlayerMarker();
                return;
            }

            _drawAbilityStatus();
            _drawThreatRewardMarkers();
            _drawSignalEvents();
            _drawPlayerMarker();
        }

        internal void NotifySignalEvent(string message)
        {
            if (string.IsNullOrWhiteSpace(message) ||
                (!message.Contains("SIGNAL") && !message.Contains("SAPPER") && !message.Contains("DASH")))
            {
                return;
            }

            while (m_signalEvents.Count >= 3)
            {
                m_signalEvents.Dequeue();
            }
            m_signalEvents.Enqueue(new SignalEvent(message, Time.unscaledTime + 2.5f));
        }

        private void _drawSignalEconomy()
        {
            var passive = RunModel.PassiveDrainRate(IsPowered);
            var movement = RunModel.MovementDrainRate(IsMoving, IsPowered);
            var total = (passive + movement) * DrainMultiplier;
            var source = IsPowered ? (IsMoving ? "POWERED MOVEMENT" : "POWERED SAFE ZONE") :
                IsMoving ? "DEAD ZONE + MOVEMENT" : "DEAD ZONE EXPOSURE";
            GUI.Label(new Rect(18f, 198f, 310f, 48f),
                total > 0f ? $"TRAVERSAL  −{total:0.0}/s\n{source}" : "TRAVERSAL  STABLE\nPOWERED SAFE ZONE", m_smallStyle);
        }

        private void _drawAbilityStatus()
        {
            var primary = m_overclocks.Selected == SignalOverclock.None
                ? "PRIMARY  LOCKED — CENTRAL COMPONENTS"
                : $"PRIMARY  {m_overclocks.Selected.ToString().ToUpperInvariant()}";
            var auxiliary = m_overclocks.SelectedAuxiliary == SignalAuxiliaryOverclock.None
                ? "AUXILIARY  LOCKED — RELAY PAYLOAD"
                : $"AUXILIARY  {m_overclocks.SelectedAuxiliary.ToString().ToUpperInvariant()}";
            var weapon = m_overclocks.SelectedWeapon == SignalWeaponOverclock.None
                ? "WEAPON  LOCKED — RELAY"
                : $"WEAPON  {m_overclocks.SelectedWeapon.ToString().ToUpperInvariant()}";
            var dash = DashCooldown <= 0f ? "DASH  READY  [SHIFT / A]" : $"DASH  {DashCooldown:0.0}s";
            GUI.Label(new Rect(Screen.width - 326f, Screen.height - 132f, 308f, 114f),
                $"{primary}\n{auxiliary}\n{weapon}\n{dash}", m_smallStyle);
        }

        private void _drawThreatRewardMarkers()
        {
            _drawThreatReward(m_world.Warden, "+12 SIGNAL");
            _drawThreatReward(m_world.Sapper, "INTERRUPT: SHOOT  +16");
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
            var safety = m_world.GetNearestPoweredTarget(
                m_world.Player.position, m_model.TowerOnline, m_model.RelayTowerOnline, m_model.SpineTowerOnline);
            var waypoint = m_world.GetNavigationWaypoint(m_world.Player.position, safety, 0.48f, m_model.ShortcutOpen);
            var point = m_camera.WorldToScreenPoint(waypoint + Vector3.up * 1.2f);
            GUI.Label(new Rect((Screen.width - width) * 0.5f, Screen.height * 0.34f, width, 100f),
                $"EMERGENCY LINK  {m_model.CriticalRecoveryRemaining:0.0}s\nFREE DASH TO CYAN ROUTE — TOWER ACTIVATION CAN RESCUE", m_primaryStyle);
            if (point.z > 0f)
            {
                GUI.Label(new Rect(Mathf.Clamp(point.x - 80f, 80f, Screen.width - 240f),
                    Mathf.Clamp(Screen.height - point.y, 150f, Screen.height - 160f), 180f, 36f), "▲  SAFE TURN", m_primaryStyle);
            }
        }

        private void _drawPlayerMarker()
        {
            var point = m_camera.WorldToScreenPoint(m_world.Player.position + Vector3.up * 1.25f);
            if (point.z > 0f)
            {
                GUI.Label(new Rect(point.x - 45f, Screen.height - point.y - 16f, 90f, 28f), "▼ YOU", m_primaryStyle);
            }
        }

        private void _drawSignalEvents()
        {
            while (m_signalEvents.Count > 0 && m_signalEvents.Peek().ExpiresAt <= Time.unscaledTime)
            {
                m_signalEvents.Dequeue();
            }

            var index = 0;
            foreach (var signalEvent in m_signalEvents)
            {
                GUI.Label(new Rect(18f, 250f + index * 22f, 440f, 24f), signalEvent.Message, m_smallStyle);
                index++;
            }
        }

        private void _drawTacticalMap()
        {
            var card = new Rect(20f, 170f, 390f, 245f);
            var map = new Rect(card.x + 12f, card.y + 44f, card.width - 24f, card.height - 56f);
            var oldColor = GUI.color;
            GUI.color = new Color(0.01f, 0.04f, 0.06f, 0.95f);
            GUI.DrawTexture(card, Texture2D.whiteTexture);
            GUI.color = oldColor;
            GUI.Label(new Rect(card.x + 8f, card.y + 5f, card.width - 16f, 34f),
                "TACTICAL MAP  //  CYAN SAFE  AMBER OBJECTIVE  RED THREAT", m_smallStyle);
            _drawMapPoint(map, m_world.ExtractionPosition, new Color(0.2f, 0.95f, 1f), 18f, "EXTRACT");
            _drawMapPoint(map, m_world.TowerPosition, new Color(0.2f, 0.95f, 1f), 18f, "TOWER");
            _drawMapPoint(map, m_world.RelayTowerPosition,
                m_model.RelayTowerOnline ? new Color(0.2f, 0.95f, 1f) : new Color(0.35f, 0.48f, 0.52f), 16f, "RELAY");
            _drawMapPoint(map, m_world.SpineTowerPosition,
                m_model.SpineTowerOnline ? new Color(0.2f, 0.95f, 1f) : new Color(0.35f, 0.48f, 0.52f), 16f, "SPINE");
            foreach (var cache in m_world.SalvagePickups)
            {
                if (cache.activeSelf)
                {
                    var optional = m_world.IsOptionalCache(cache);
                    var available = optional
                        ? m_model.CanExtract
                        : m_model.CanCollectPayload(m_world.GetPayloadRegion(cache), m_world.GetCentralComponent(cache));
                    var color = available ? new Color(1f, 0.66f, 0.12f) : new Color(0.32f, 0.27f, 0.2f);
                    _drawMapPoint(map, cache.transform.position, color, optional ? 10f : 12f,
                        optional ? "GREED" : available ? "PAYLOAD" : "LOCKED");
                }
            }
            _drawMapPoint(map, m_world.Warden.position, Color.red, 10f, "W");
            _drawMapPoint(map, m_world.Sapper.position, new Color(1f, 0.15f, 0.65f), 10f, "S");
            _drawMapPoint(map, m_world.Player.position, Color.white, 14f, "YOU");
            var waypoint = m_world.GetObjectiveGuidanceWaypoint(m_model, 0.48f);
            _drawMapPoint(map, waypoint, new Color(1f, 0.66f, 0.12f), 8f, "NEXT TURN");
        }

        private void _drawMapPoint(Rect map, Vector3 position, Color color, float size, string label)
        {
            var x = map.x + (position.x / (m_world.ArenaHalfExtents.x * 2f) + 0.5f) * map.width;
            var y = map.y + (0.5f - position.z / (m_world.ArenaHalfExtents.y * 2f)) * map.height;
            var oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x - size * 0.5f, y - size * 0.5f, size, size), Texture2D.whiteTexture);
            GUI.color = oldColor;
            GUI.Label(new Rect(x + 6f, y - 11f, 80f, 22f), label, m_smallStyle);
        }

        private readonly struct SignalEvent
        {
            public SignalEvent(string message, float expiresAt)
            {
                Message = message;
                ExpiresAt = expiresAt;
            }

            public string Message { get; }
            public float ExpiresAt { get; }
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
