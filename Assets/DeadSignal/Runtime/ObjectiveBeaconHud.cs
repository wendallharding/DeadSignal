using Reflex.Attributes;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private GameObject m_panel;
        [SerializeField] private RawImage m_icon;
        [SerializeField] private Text m_label;
        [SerializeField] private Text m_hint;
        [SerializeField] private Text m_distance;

        public bool HasIcon => m_icon != null && m_icon.texture != null;
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
            if (!HasIcon)
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
                _refreshPresentation();
            }
        }

        private void _refreshPresentation()
        {
            bool visible = m_model != null && m_world != null && m_model.Outcome == RunOutcome.Running && !m_combatFeedback.IsPaused;
            m_panel.SetActive(visible);
            if (!visible)
            {
                return;
            }

            m_icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _directionAngle());
            m_label.text = $"NEXT  {_currentLabel()}";
            m_hint.text = _currentHint();
            m_distance.text = $"{Mathf.CeilToInt(DeadSignalWorld.FlatDistance(m_world.Player.position, CurrentTarget))}m";
        }

        private void _refreshTarget()
        {
            if (!m_model.TowerOnline)
            {
                CurrentPhase = ObjectiveBeaconPhase.Tower;
                CurrentTarget = m_world.TowerPosition;
                return;
            }

            if (m_model.CanExtract)
            {
                CurrentPhase = ObjectiveBeaconPhase.Extraction;
                CurrentTarget = m_world.ExtractionPosition;
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

    }
}
