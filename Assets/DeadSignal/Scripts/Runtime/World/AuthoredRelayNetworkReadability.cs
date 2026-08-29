using UnityEngine;

namespace DeadSignal.World
{
    public enum RelayTowerPresentationState
    {
        Dormant,
        ActivationAvailable,
        Activating,
        Powered
    }

    public enum CoolingGantryPresentationState
    {
        PrerequisiteLocked,
        ProcessingAvailable,
        Active,
        Stabilized
    }

    /// <summary>
    /// Owns persistent Relay Foundry and Cooling Gantry presentation while mission rules remain in RunModel.
    /// </summary>
    public sealed class AuthoredRelayNetworkReadability : MonoBehaviour
    {
        private const float STATE_TRANSITION_SECONDS = 0.75f;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer[] m_relayRenderers;
        [SerializeField] private Renderer[] m_gantryRenderers;

        public bool IsConfigured => m_relayRenderers is { Length: > 0 } && m_gantryRenderers is { Length: > 0 };
        public RelayTowerPresentationState RelayState { get; private set; } = RelayTowerPresentationState.Dormant;
        public CoolingGantryPresentationState GantryState { get; private set; } =
            CoolingGantryPresentationState.PrerequisiteLocked;

        private void Awake()
        {
            _applyRelayState(RelayTowerPresentationState.Dormant);
            _applyGantryState(CoolingGantryPresentationState.PrerequisiteLocked);
        }

        private void Update()
        {
            if (m_relayTransitionRemaining > 0f)
            {
                m_relayTransitionRemaining = Mathf.Max(0f, m_relayTransitionRemaining - Time.unscaledDeltaTime);
            }

            if (m_gantryTransitionRemaining > 0f)
            {
                m_gantryTransitionRemaining = Mathf.Max(0f, m_gantryTransitionRemaining - Time.unscaledDeltaTime);
            }

            _refreshStates();
        }

        public void Configure(Renderer[] relayRenderers, Renderer[] gantryRenderers)
        {
            m_relayRenderers = relayRenderers;
            m_gantryRenderers = gantryRenderers;
            m_relayPowered = false;
            m_payloadStabilized = false;
            m_lastRelayPowered = false;
            m_lastPayloadStabilized = false;
            m_relayTransitionRemaining = 0f;
            m_gantryTransitionRemaining = 0f;
            m_hasAppliedRelayState = false;
            m_hasAppliedGantryState = false;
            _refreshStates();
        }

        public void SetState(
            bool relayActivationAvailable,
            bool relayPowered,
            bool gantryProcessingAvailable,
            bool payloadStabilized)
        {
            if (relayPowered && !m_lastRelayPowered)
            {
                m_relayTransitionRemaining = STATE_TRANSITION_SECONDS;
            }

            if (payloadStabilized && !m_lastPayloadStabilized)
            {
                m_gantryTransitionRemaining = STATE_TRANSITION_SECONDS;
            }

            m_lastRelayPowered = relayPowered;
            m_lastPayloadStabilized = payloadStabilized;
            m_relayActivationAvailable = relayActivationAvailable;
            m_relayPowered = relayPowered;
            m_gantryProcessingAvailable = gantryProcessingAvailable;
            m_payloadStabilized = payloadStabilized;
            _refreshStates();
        }

        private void _refreshStates()
        {
            var relayState = m_relayTransitionRemaining > 0f
                ? RelayTowerPresentationState.Activating
                : m_relayPowered
                    ? RelayTowerPresentationState.Powered
                    : m_relayActivationAvailable
                        ? RelayTowerPresentationState.ActivationAvailable
                        : RelayTowerPresentationState.Dormant;
            var gantryState = m_gantryTransitionRemaining > 0f
                ? CoolingGantryPresentationState.Active
                : m_payloadStabilized
                    ? CoolingGantryPresentationState.Stabilized
                    : m_gantryProcessingAvailable
                        ? CoolingGantryPresentationState.ProcessingAvailable
                        : CoolingGantryPresentationState.PrerequisiteLocked;
            _applyRelayState(relayState);
            _applyGantryState(gantryState);
        }

        private void _applyRelayState(RelayTowerPresentationState state)
        {
            if (m_hasAppliedRelayState && RelayState == state)
            {
                return;
            }

            m_hasAppliedRelayState = true;
            RelayState = state;
            var color = state switch
            {
                RelayTowerPresentationState.Dormant => new Color(0.12f, 0.16f, 0.18f),
                RelayTowerPresentationState.ActivationAvailable => new Color(1f, 0.46f, 0.055f),
                RelayTowerPresentationState.Activating => new Color(1f, 0.72f, 0.16f),
                _ => new Color(0.02f, 0.9f, 1f)
            };
            _setColors(m_relayRenderers, color, state == RelayTowerPresentationState.Dormant ? 0.08f : 1.2f);
        }

        private void _applyGantryState(CoolingGantryPresentationState state)
        {
            if (m_hasAppliedGantryState && GantryState == state)
            {
                return;
            }

            m_hasAppliedGantryState = true;
            GantryState = state;
            var color = state switch
            {
                CoolingGantryPresentationState.PrerequisiteLocked => new Color(0.38f, 0.055f, 0.045f),
                CoolingGantryPresentationState.ProcessingAvailable => new Color(1f, 0.46f, 0.055f),
                CoolingGantryPresentationState.Active => new Color(0.98f, 0.74f, 0.2f),
                _ => new Color(0.06f, 0.94f, 1f)
            };
            _setColors(m_gantryRenderers, color,
                state == CoolingGantryPresentationState.PrerequisiteLocked ? 0.18f : 1.15f);
        }

        private static void _setColors(Renderer[] renderers, Color color, float emissionMultiplier)
        {
            if (renderers == null)
            {
                return;
            }

            var properties = new MaterialPropertyBlock();
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(properties);
                properties.SetColor(s_baseColor, color);
                properties.SetColor(s_emissionColor, color * emissionMultiplier);
                renderer.SetPropertyBlock(properties);
                properties.Clear();
            }
        }

        private bool m_relayActivationAvailable;
        private bool m_relayPowered;
        private bool m_gantryProcessingAvailable;
        private bool m_payloadStabilized;
        private bool m_lastRelayPowered;
        private bool m_lastPayloadStabilized;
        private bool m_hasAppliedRelayState;
        private bool m_hasAppliedGantryState;
        private float m_relayTransitionRemaining;
        private float m_gantryTransitionRemaining;
    }
}
