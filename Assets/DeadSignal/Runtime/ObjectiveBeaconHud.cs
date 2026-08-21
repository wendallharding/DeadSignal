using Reflex.Attributes;
using UnityEngine;

namespace DeadSignal
{
    public enum ObjectiveBeaconPhase
    {
        Tower,
        Salvage,
        Extraction
    }

    internal interface IObjectiveBeacon
    {
        bool HasIcon { get; }
        ObjectiveBeaconPhase CurrentPhase { get; }
        Vector3 CurrentTarget { get; }

        void Configure(RunModel model, DeadSignalWorld world);
    }

    /// <summary>
    /// Presents the next critical run objective as a directional HUD beacon.
    /// </summary>
    public sealed class ObjectiveBeaconHud : MonoBehaviour, IObjectiveBeacon
    {
        private const string ICON_PATH = "UI/ObjectiveBeaconIcon";

        private RunModel m_model;
        private DeadSignalWorld m_world;
        private ICombatFeedback m_combatFeedback;
        private Texture2D m_icon;
        private GUIStyle m_labelStyle;
        private GUIStyle m_distanceStyle;

        public bool HasIcon => m_icon != null;
        public ObjectiveBeaconPhase CurrentPhase { get; private set; }
        public Vector3 CurrentTarget { get; private set; }

        [Inject]
        private void _construct(ICombatFeedback combatFeedback)
        {
            m_combatFeedback = combatFeedback;
        }

        void IObjectiveBeacon.Configure(RunModel model, DeadSignalWorld world)
        {
            m_model = model;
            m_world = world;
            m_icon = Resources.Load<Texture2D>(ICON_PATH);
            if (m_icon == null)
            {
                Debug.LogWarning($"Objective beacon icon was not found at Resources/{ICON_PATH}.", this);
            }

            _refreshTarget();
        }

        private void Update()
        {
            if (m_model != null)
            {
                _refreshTarget();
            }
        }

        private void OnGUI()
        {
            if (m_model == null || m_world == null || m_model.Outcome != RunOutcome.Running || m_combatFeedback.IsPaused)
            {
                return;
            }

            _ensureGuiStyles();

            var panel = new Rect(Screen.width * 0.5f - 210f, Screen.height - 154f, 420f, 58f);
            GUI.color = new Color(0.015f, 0.025f, 0.035f, 0.93f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;

            var iconRect = new Rect(panel.x + 8f, panel.y + 5f, 48f, 48f);
            if (m_icon != null)
            {
                Matrix4x4 previousMatrix = GUI.matrix;
                GUIUtility.RotateAroundPivot(_directionAngle(), iconRect.center);
                GUI.DrawTexture(iconRect, m_icon, ScaleMode.ScaleToFit, true);
                GUI.matrix = previousMatrix;
            }

            GUI.Label(new Rect(panel.x + 66f, panel.y + 7f, 275f, 23f), $"NEXT  {_currentLabel()}", m_labelStyle);
            float distance = DeadSignalWorld.FlatDistance(m_world.Player.position, CurrentTarget);
            GUI.Label(new Rect(panel.x + 66f, panel.y + 30f, 275f, 20f), _currentHint(), m_distanceStyle);
            GUI.Label(new Rect(panel.x + 338f, panel.y + 14f, 68f, 28f), $"{Mathf.CeilToInt(distance)}m", m_labelStyle);
            GUI.color = Color.white;
        }

        private void _refreshTarget()
        {
            if (!m_model.TowerOnline)
            {
                CurrentPhase = ObjectiveBeaconPhase.Tower;
                CurrentTarget = m_world.TowerPosition;
                return;
            }

            GameObject nearestSalvage = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (var salvage in m_world.SalvagePickups)
            {
                if (!salvage.activeSelf)
                {
                    continue;
                }

                float sqrDistance = (salvage.transform.position - m_world.Player.position).sqrMagnitude;
                if (sqrDistance < nearestDistance)
                {
                    nearestDistance = sqrDistance;
                    nearestSalvage = salvage;
                }
            }

            if (nearestSalvage != null)
            {
                CurrentPhase = ObjectiveBeaconPhase.Salvage;
                CurrentTarget = nearestSalvage.transform.position;
                return;
            }

            CurrentPhase = ObjectiveBeaconPhase.Extraction;
            CurrentTarget = m_world.ExtractionPosition;
        }

        private float _directionAngle()
        {
            var direction = CurrentTarget - m_world.Player.position;
            return Vector2.SignedAngle(Vector2.down, new Vector2(direction.x, -direction.z));
        }

        private string _currentLabel()
        {
            return CurrentPhase switch
            {
                ObjectiveBeaconPhase.Tower => "ACTIVATE TOWER",
                ObjectiveBeaconPhase.Salvage => "RECOVER SALVAGE",
                ObjectiveBeaconPhase.Extraction => "RETURN TO EXTRACTION",
                _ => string.Empty
            };
        }

        private string _currentHint()
        {
            return CurrentPhase switch
            {
                ObjectiveBeaconPhase.Tower => "Bring the network online",
                ObjectiveBeaconPhase.Salvage => $"Nearest cache  |  {m_model.Salvage}/{RunModel.SalvageRequired} secured",
                ObjectiveBeaconPhase.Extraction => "All salvage secured",
                _ => string.Empty
            };
        }

        private void _ensureGuiStyles()
        {
            if (m_labelStyle != null)
            {
                return;
            }

            m_labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            m_labelStyle.normal.textColor = new Color(0.12f, 0.96f, 1f);
            m_distanceStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };
            m_distanceStyle.normal.textColor = new Color(0.72f, 0.82f, 0.86f);
        }
    }
}
