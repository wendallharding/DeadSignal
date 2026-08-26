using Reflex.Attributes;
using UnityEngine;
using UnityEngine.UI;
using DeadSignal.Combat;
using DeadSignal.Missions;
using DeadSignal.World;

namespace DeadSignal.Presentation
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
            CurrentTarget = m_world.GetObjectiveTarget(m_model);
            CurrentPhase = m_model.CurrentMissionStage switch
            {
                MissionStage.CentralTower or MissionStage.RelayTower or MissionStage.SpineTower => ObjectiveBeaconPhase.Tower,
                MissionStage.Extraction => ObjectiveBeaconPhase.Extraction,
                _ => ObjectiveBeaconPhase.Salvage
            };
        }

        private float _directionAngle()
        {
            var direction = CurrentTarget - m_world.Player.position;
            return Vector2.SignedAngle(Vector2.down, new Vector2(direction.x, -direction.z));
        }

        private string _currentLabel()
        {
            return m_model.CurrentMissionStage switch
            {
                MissionStage.CentralTower => "ACTIVATE CENTRAL TOWER",
                MissionStage.CentralPayload => "RECOVER CENTRAL PAYLOAD",
                MissionStage.RelayTower => "RESTORE RELAY TOWER",
                MissionStage.RelayPayload => "RECOVER RELAY PAYLOAD",
                MissionStage.SpineTower => "RESTORE SPINE TOWER",
                MissionStage.SpinePayload => "RECOVER SPINE PAYLOAD",
                MissionStage.Extraction => "RETURN TO EXTRACTION",
                _ => string.Empty
            };
        }

        private string _currentHint()
        {
            return m_model.CurrentMissionStage switch
            {
                MissionStage.CentralTower => "Bring the opening network online",
                MissionStage.CentralPayload => "Choose one of two local payload routes",
                MissionStage.RelayTower => "Push east and establish the Relay foothold",
                MissionStage.RelayPayload => "Choose inner Foundry cover or the Cooling Gantry loop",
                MissionStage.SpineTower => "Carry the network into the Capacitor Spine",
                MissionStage.SpinePayload => "Secure the final extraction payload",
                MissionStage.Extraction => "Three towers and regional payloads secured",
                _ => string.Empty
            };
        }

    }
}
