using UnityEngine;

namespace DeadSignal.World
{
    public enum FluxShuntPresentationState
    {
        PrerequisiteLocked,
        RoutingAvailable,
        Routing,
        Routed
    }

    /// <summary>
    /// Owns the scene-authored Flux shunt interaction and its persistent route presentation.
    /// </summary>
    public sealed class AuthoredFluxShuntObjective : MonoBehaviour
    {
        private const float STATE_TRANSITION_SECONDS = 0.75f;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform m_routingAnchor;
        [SerializeField] private GameObject m_availableMarker;
        [SerializeField] private GameObject m_routedMarker;
        [SerializeField] private Renderer[] m_readabilityRenderers;
        [SerializeField] private Transform m_shuntSelector;

        public bool IsConfigured => m_routingAnchor != null && m_availableMarker != null && m_routedMarker != null;
        public bool HasReadabilityAssets => m_readabilityRenderers is { Length: > 0 } && m_shuntSelector != null;
        public Vector3 Position => m_routingAnchor != null ? m_routingAnchor.position : transform.position;
        public FluxShuntPresentationState PresentationState { get; private set; } =
            FluxShuntPresentationState.PrerequisiteLocked;

        private void Awake()
        {
            SetState(false, false);
        }

        private void Update()
        {
            if (m_transitionRemaining <= 0f)
            {
                return;
            }

            m_transitionRemaining = Mathf.Max(0f, m_transitionRemaining - Time.unscaledDeltaTime);
            _refreshPresentation();
        }

        public void Configure(Transform routingAnchor, GameObject availableMarker, GameObject routedMarker)
        {
            m_routingAnchor = routingAnchor;
            m_availableMarker = availableMarker;
            m_routedMarker = routedMarker;
            SetState(false, false);
        }

        public void ConfigureReadability(Renderer[] readabilityRenderers, Transform shuntSelector)
        {
            m_readabilityRenderers = readabilityRenderers;
            m_shuntSelector = shuntSelector;
            m_lastRouted = false;
            m_transitionRemaining = 0f;
            m_hasAppliedPresentation = false;
            _refreshPresentation();
        }

        public void SetState(bool available, bool routed)
        {
            if (routed && !m_lastRouted)
            {
                m_transitionRemaining = STATE_TRANSITION_SECONDS;
            }

            m_lastRouted = routed;
            m_available = available;
            m_routed = routed;
            if (m_availableMarker != null)
            {
                m_availableMarker.SetActive(available && !routed);
            }

            if (m_routedMarker != null)
            {
                m_routedMarker.SetActive(routed);
            }

            _refreshPresentation();
        }

        private void _refreshPresentation()
        {
            var state = m_transitionRemaining > 0f
                ? FluxShuntPresentationState.Routing
                : m_routed
                    ? FluxShuntPresentationState.Routed
                    : m_available
                        ? FluxShuntPresentationState.RoutingAvailable
                        : FluxShuntPresentationState.PrerequisiteLocked;
            var progress = m_transitionRemaining > 0f
                ? 1f - m_transitionRemaining / STATE_TRANSITION_SECONDS
                : state == FluxShuntPresentationState.Routed ? 1f : 0f;
            _applyPresentation(state, progress);
        }

        private void _applyPresentation(FluxShuntPresentationState state, float progress)
        {
            if (!m_hasAppliedPresentation || PresentationState != state)
            {
                m_hasAppliedPresentation = true;
                PresentationState = state;
                var color = state switch
                {
                    FluxShuntPresentationState.PrerequisiteLocked => new Color(0.34f, 0.045f, 0.04f),
                    FluxShuntPresentationState.RoutingAvailable => new Color(1f, 0.46f, 0.055f),
                    FluxShuntPresentationState.Routing => new Color(0.96f, 0.76f, 0.22f),
                    _ => new Color(0.04f, 0.94f, 1f)
                };
                _setColors(color, state == FluxShuntPresentationState.PrerequisiteLocked ? 0.16f : 1.15f);
            }

            if (m_shuntSelector != null)
            {
                m_shuntSelector.localRotation = Quaternion.Euler(0f, progress * -68f, 0f);
            }
        }

        private void _setColors(Color color, float emissionMultiplier)
        {
            if (m_readabilityRenderers == null)
            {
                return;
            }

            var properties = new MaterialPropertyBlock();
            foreach (var renderer in m_readabilityRenderers)
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

        private bool m_available;
        private bool m_routed;
        private bool m_lastRouted;
        private bool m_hasAppliedPresentation;
        private float m_transitionRemaining;
    }
}
