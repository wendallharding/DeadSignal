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
        private GUIStyle m_mapTitleStyle;
        private GUIStyle m_mapLabelStyle;
        private GUIStyle m_mapLegendStyle;
        private readonly Queue<SignalEvent> m_signalEvents = new();
        private readonly List<Rect> m_mapLabelRects = new();

        private static readonly Color s_mapCyan = new(0.15f, 0.9f, 1f);
        private static readonly Color s_mapAmber = new(1f, 0.66f, 0.12f);
        private static readonly Color s_mapRed = new(1f, 0.18f, 0.16f);
        private static readonly Color s_mapOffline = new(0.28f, 0.42f, 0.46f);
        private static readonly Vector2[] s_mapLabelOffsets =
        {
            new(9f, -10f), new(9f, 5f), new(-9f, -10f), new(-9f, 5f),
            new(0f, -25f), new(0f, 12f)
        };

        public float DashCooldown { get; set; }
        public Rect LastTacticalMapCard { get; private set; }
        public Rect LastTacticalMapWorldBounds { get; private set; }
        public int LastTacticalMapObstacleCount { get; private set; }

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
            var cardWidth = Mathf.Min(560f, Screen.width - 36f);
            var cardY = Mathf.Clamp(Screen.height * 0.19f, 88f, 150f);
            var cardHeight = Mathf.Min(360f, Screen.height - cardY - 18f);
            var card = new Rect(18f, cardY, cardWidth, cardHeight);
            var map = new Rect(card.x + 12f, card.y + 66f, card.width - 24f, card.height - 96f);
            LastTacticalMapCard = card;
            var oldColor = GUI.color;
            GUI.color = new Color(0.008f, 0.025f, 0.035f, 0.98f);
            GUI.DrawTexture(card, Texture2D.whiteTexture);
            GUI.color = new Color(0.08f, 0.34f, 0.4f, 0.95f);
            GUI.DrawTexture(new Rect(card.x, card.y, card.width, 2f), Texture2D.whiteTexture);
            GUI.color = oldColor;

            GUI.Label(new Rect(card.x + 14f, card.y + 8f, card.width - 120f, 24f), "STATION NETWORK", m_mapTitleStyle);
            GUI.Label(new Rect(card.x + card.width - 110f, card.y + 10f, 96f, 22f),
                "PAUSED  //  LIVE", m_mapLegendStyle);
            _drawLegendItem(new Rect(card.x + 14f, card.y + 38f, 92f, 18f), s_mapCyan, "POWERED");
            _drawLegendItem(new Rect(card.x + 110f, card.y + 38f, 104f, 18f), s_mapAmber, "OBJECTIVE");
            _drawLegendItem(new Rect(card.x + 218f, card.y + 38f, 82f, 18f), s_mapRed, "THREAT");
            _drawLegendItem(new Rect(card.x + 304f, card.y + 38f, 94f, 18f), Color.white, "YOU");

            var waypoint = m_world.GetObjectiveGuidanceWaypoint(m_model, 0.48f);
            var worldBounds = _calculateMapBounds(map, waypoint);
            LastTacticalMapWorldBounds = worldBounds;
            LastTacticalMapObstacleCount = m_world.AuthoredMapObstacles.Count;
            GUI.BeginGroup(map);
            var localMap = new Rect(0f, 0f, map.width, map.height);
            _drawMapBackground(localMap, worldBounds);
            m_mapLabelRects.Clear();
            _drawMapPoint(localMap, worldBounds, m_world.ExtractionPosition, s_mapCyan, 12f, "DOCK");
            _drawMapPoint(localMap, worldBounds, m_world.TowerPosition,
                m_model.TowerOnline ? s_mapCyan : s_mapOffline, 12f, "CENTRAL");
            _drawMapPoint(localMap, worldBounds, m_world.RelayTowerPosition,
                m_model.RelayTowerOnline ? s_mapCyan : s_mapOffline, 11f, "RELAY");
            _drawMapPoint(localMap, worldBounds, m_world.SpineTowerPosition,
                m_model.SpineTowerOnline ? s_mapCyan : s_mapOffline, 11f, "SPINE");
            foreach (var cache in m_world.SalvagePickups)
            {
                if (cache.activeSelf)
                {
                    var optional = m_world.IsOptionalCache(cache);
                    var available = optional
                        ? m_model.CanExtract
                        : m_model.CanCollectPayload(m_world.GetPayloadRegion(cache), m_world.GetCentralComponent(cache));
                    var color = available ? new Color(1f, 0.66f, 0.12f) : new Color(0.32f, 0.27f, 0.2f);
                    _drawMapPoint(localMap, worldBounds, cache.transform.position, color, optional ? 8f : 9f,
                        optional ? "GREED" : available ? "PAYLOAD" : "LOCKED");
                }
            }
            if (m_world.Warden.gameObject.activeSelf)
            {
                _drawMapPoint(localMap, worldBounds, m_world.Warden.position, s_mapRed, 9f, "WARDEN");
            }
            if (m_world.Sapper.gameObject.activeSelf)
            {
                _drawMapPoint(localMap, worldBounds, m_world.Sapper.position,
                    new Color(1f, 0.15f, 0.65f), 9f, "SAPPER");
            }
            _drawMapPoint(localMap, worldBounds, waypoint, s_mapAmber, 9f, "NEXT");
            _drawMapPoint(localMap, worldBounds, m_world.Player.position, Color.white, 12f, "YOU");
            GUI.Label(new Rect(localMap.width - 30f, 6f, 20f, 20f), "N", m_mapLegendStyle);
            _drawLine(new Vector2(localMap.width - 20f, 25f), new Vector2(localMap.width - 20f, 38f), 2f, s_mapCyan);
            GUI.EndGroup();

            GUI.Label(new Rect(card.x + 14f, card.yMax - 25f, card.width - 28f, 20f),
                "LIVE ROUTE  //  STRUCTURES SHOWN IN BLUE  //  ESC / MENU TO RESUME", m_mapLegendStyle);
        }

        private Rect _calculateMapBounds(Rect map, Vector3 waypoint)
        {
            var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            _encapsulateMapPosition(ref minimum, ref maximum, m_world.ExtractionPosition);
            _encapsulateMapPosition(ref minimum, ref maximum, m_world.TowerPosition);
            _encapsulateMapPosition(ref minimum, ref maximum, m_world.RelayTowerPosition);
            _encapsulateMapPosition(ref minimum, ref maximum, m_world.SpineTowerPosition);
            _encapsulateMapPosition(ref minimum, ref maximum, m_world.Player.position);
            _encapsulateMapPosition(ref minimum, ref maximum, waypoint);
            foreach (var cache in m_world.SalvagePickups)
            {
                _encapsulateMapPosition(ref minimum, ref maximum, cache.transform.position);
            }

            minimum -= Vector2.one * 3.5f;
            maximum += Vector2.one * 3.5f;
            var size = maximum - minimum;
            var mapAspect = map.width / Mathf.Max(1f, map.height);
            if (size.x / Mathf.Max(0.01f, size.y) < mapAspect)
            {
                var expansion = (size.y * mapAspect - size.x) * 0.5f;
                minimum.x -= expansion;
                maximum.x += expansion;
            }
            else
            {
                var expansion = (size.x / mapAspect - size.y) * 0.5f;
                minimum.y -= expansion;
                maximum.y += expansion;
            }

            return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
        }

        private void _drawMapBackground(Rect map, Rect worldBounds)
        {
            var oldColor = GUI.color;
            GUI.color = new Color(0.015f, 0.06f, 0.075f, 1f);
            GUI.DrawTexture(map, Texture2D.whiteTexture);
            GUI.color = oldColor;

            const float gridSpacing = 5f;
            for (var x = Mathf.Ceil(worldBounds.xMin / gridSpacing) * gridSpacing; x < worldBounds.xMax; x += gridSpacing)
            {
                var start = _projectMapPosition(map, worldBounds, new Vector3(x, 0f, worldBounds.yMin));
                var end = _projectMapPosition(map, worldBounds, new Vector3(x, 0f, worldBounds.yMax));
                _drawLine(start, end, 1f, new Color(0.08f, 0.22f, 0.25f, 0.42f));
            }
            for (var z = Mathf.Ceil(worldBounds.yMin / gridSpacing) * gridSpacing; z < worldBounds.yMax; z += gridSpacing)
            {
                var start = _projectMapPosition(map, worldBounds, new Vector3(worldBounds.xMin, 0f, z));
                var end = _projectMapPosition(map, worldBounds, new Vector3(worldBounds.xMax, 0f, z));
                _drawLine(start, end, 1f, new Color(0.08f, 0.22f, 0.25f, 0.42f));
            }

            _drawMapRoute(map, worldBounds, m_world.ExtractionPosition, m_world.TowerPosition);
            _drawMapRoute(map, worldBounds, m_world.TowerPosition, m_world.RelayTowerPosition);
            _drawMapRoute(map, worldBounds, m_world.RelayTowerPosition, m_world.SpineTowerPosition);
            foreach (var cache in m_world.SalvagePickups)
            {
                var source = _nearestMapHub(cache.transform.position);
                _drawMapRoute(map, worldBounds, source, cache.transform.position, false);
            }

            foreach (var obstacle in m_world.AuthoredMapObstacles)
            {
                _drawMapObstacle(map, worldBounds, obstacle);
            }

            GUI.color = new Color(0.18f, 0.64f, 0.72f, 0.75f);
            GUI.DrawTexture(new Rect(map.x, map.y, map.width, 1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(map.x, map.yMax - 1f, map.width, 1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(map.x, map.y, 1f, map.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(map.xMax - 1f, map.y, 1f, map.height), Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        private void _drawMapRoute(Rect map, Rect worldBounds, Vector3 start, Vector3 end, bool primary = true)
        {
            var mapStart = _projectMapPosition(map, worldBounds, start);
            var mapEnd = _projectMapPosition(map, worldBounds, end);
            _drawLine(mapStart, mapEnd, primary ? 13f : 7f,
                primary ? new Color(0.035f, 0.18f, 0.22f, 0.95f) : new Color(0.05f, 0.13f, 0.15f, 0.8f));
            _drawLine(mapStart, mapEnd, primary ? 2f : 1f,
                primary ? new Color(0.12f, 0.48f, 0.54f, 0.85f) : new Color(0.2f, 0.3f, 0.31f, 0.7f));
        }

        private void _drawMapObstacle(Rect map, Rect worldBounds, AuthoredMapObstacle obstacle)
        {
            var center = new Vector3(obstacle.Center.x, 0f, obstacle.Center.y);
            if (!worldBounds.Contains(obstacle.Center))
            {
                return;
            }

            var projectedCenter = _projectMapPosition(map, worldBounds, center);
            var projectedRight = _projectMapPosition(map, worldBounds,
                center + new Vector3(obstacle.RightAxis.x, 0f, obstacle.RightAxis.y) * obstacle.ScaledHalfSize.x);
            var projectedForward = _projectMapPosition(map, worldBounds,
                center + new Vector3(obstacle.ForwardAxis.x, 0f, obstacle.ForwardAxis.y) * obstacle.ScaledHalfSize.y);
            var right = projectedRight - projectedCenter;
            var forward = projectedForward - projectedCenter;
            var width = Mathf.Max(1.5f, right.magnitude * 2f);
            var height = Mathf.Max(1.5f, forward.magnitude * 2f);
            var oldMatrix = GUI.matrix;
            var oldColor = GUI.color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(right.y, right.x) * Mathf.Rad2Deg, projectedCenter);
            GUI.color = new Color(0.08f, 0.2f, 0.23f, 0.92f);
            GUI.DrawTexture(new Rect(projectedCenter.x - width * 0.5f, projectedCenter.y - height * 0.5f, width, height),
                Texture2D.whiteTexture);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private void _drawMapPoint(Rect map, Rect worldBounds, Vector3 position, Color color, float size, string label)
        {
            var point = _projectMapPosition(map, worldBounds, position);
            var oldColor = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, 0.2f);
            GUI.DrawTexture(new Rect(point.x - size * 0.8f, point.y - size * 0.8f, size * 1.6f, size * 1.6f),
                Texture2D.whiteTexture);
            GUI.color = color;
            var markerRect = new Rect(point.x - size * 0.5f, point.y - size * 0.5f, size, size);
            GUI.DrawTexture(markerRect, Texture2D.whiteTexture);
            GUI.color = oldColor;
            var labelRect = _findMapLabelRect(map, point, label);
            GUI.color = new Color(0.005f, 0.025f, 0.035f, 0.9f);
            GUI.DrawTexture(labelRect, Texture2D.whiteTexture);
            GUI.color = oldColor;
            GUI.Label(labelRect, label, m_mapLabelStyle);
            m_mapLabelRects.Add(markerRect);
            m_mapLabelRects.Add(labelRect);
        }

        private Rect _findMapLabelRect(Rect map, Vector2 point, string label)
        {
            var content = new GUIContent(label);
            var measured = m_mapLabelStyle.CalcSize(content);
            var width = Mathf.Clamp(measured.x + 8f, 34f, 88f);
            var height = 17f;
            var fallback = new Rect(point.x + 9f, point.y - 10f, width, height);
            foreach (var offset in s_mapLabelOffsets)
            {
                var x = offset.x < 0f ? point.x + offset.x - width : point.x + offset.x;
                var candidate = new Rect(x, point.y + offset.y, width, height);
                candidate.x = Mathf.Clamp(candidate.x, map.x + 3f, map.xMax - candidate.width - 3f);
                candidate.y = Mathf.Clamp(candidate.y, map.y + 3f, map.yMax - candidate.height - 3f);
                fallback = candidate;
                var overlaps = false;
                foreach (var occupied in m_mapLabelRects)
                {
                    if (candidate.Overlaps(occupied))
                    {
                        overlaps = true;
                        break;
                    }
                }
                if (!overlaps)
                {
                    return candidate;
                }
            }

            return fallback;
        }

        private Vector3 _nearestMapHub(Vector3 position)
        {
            var nearest = m_world.TowerPosition;
            var nearestDistance = DeadSignalWorld.FlatDistance(position, nearest);
            _considerNearestMapHub(position, m_world.ExtractionPosition, ref nearest, ref nearestDistance);
            _considerNearestMapHub(position, m_world.RelayTowerPosition, ref nearest, ref nearestDistance);
            _considerNearestMapHub(position, m_world.SpineTowerPosition, ref nearest, ref nearestDistance);
            return nearest;
        }

        private static void _considerNearestMapHub(
            Vector3 position,
            Vector3 candidate,
            ref Vector3 nearest,
            ref float nearestDistance)
        {
            var distance = DeadSignalWorld.FlatDistance(position, candidate);
            if (distance < nearestDistance)
            {
                nearest = candidate;
                nearestDistance = distance;
            }
        }

        private static Vector2 _projectMapPosition(Rect map, Rect worldBounds, Vector3 position)
        {
            var normalizedX = Mathf.InverseLerp(worldBounds.xMin, worldBounds.xMax, position.x);
            var normalizedY = Mathf.InverseLerp(worldBounds.yMin, worldBounds.yMax, position.z);
            return new Vector2(map.x + normalizedX * map.width, map.y + (1f - normalizedY) * map.height);
        }

        private static void _encapsulateMapPosition(ref Vector2 minimum, ref Vector2 maximum, Vector3 position)
        {
            var point = new Vector2(position.x, position.z);
            minimum = Vector2.Min(minimum, point);
            maximum = Vector2.Max(maximum, point);
        }

        private static void _drawLine(Vector2 start, Vector2 end, float thickness, Color color)
        {
            var delta = end - start;
            if (delta.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var oldMatrix = GUI.matrix;
            var oldColor = GUI.color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
            GUI.color = color;
            GUI.DrawTexture(new Rect(start.x, start.y - thickness * 0.5f, delta.magnitude, thickness), Texture2D.whiteTexture);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private void _drawLegendItem(Rect rect, Color color, string label)
        {
            var oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y + 4f, 8f, 8f), Texture2D.whiteTexture);
            GUI.color = oldColor;
            GUI.Label(new Rect(rect.x + 14f, rect.y, rect.width - 14f, rect.height), label, m_mapLegendStyle);
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
            m_mapTitleStyle = new GUIStyle(m_primaryStyle)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 17
            };
            m_mapTitleStyle.normal.textColor = new Color(0.7f, 0.96f, 1f);
            m_mapLabelStyle = new GUIStyle(m_smallStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                clipping = TextClipping.Clip
            };
            m_mapLabelStyle.normal.textColor = new Color(0.78f, 0.94f, 0.96f);
            m_mapLegendStyle = new GUIStyle(m_smallStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10
            };
            m_mapLegendStyle.normal.textColor = new Color(0.48f, 0.72f, 0.76f);
        }
    }
}
