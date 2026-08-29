using UnityEngine;

namespace DeadSignal.World
{
    public enum RelayForkPresentationState
    {
        Locked,
        Available,
        Routing,
        Routed
    }

    /// <summary>
    /// Scene-authored interaction and persistent presentation for routing the two Central feeds.
    /// </summary>
    public sealed class AuthoredRelayForkObjective : MonoBehaviour
    {
        private const float ROUTING_PRESENTATION_SECONDS = 0.75f;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform m_routingAnchor;
        [SerializeField] private GameObject m_availableMarker;
        [SerializeField] private GameObject m_routedMarker;
        [SerializeField] private Renderer[] m_statusRenderers;
        [SerializeField] private Transform m_routingSelector;

        public Vector3 Position => m_routingAnchor != null ? m_routingAnchor.position : transform.position;
        public bool IsConfigured => m_routingAnchor != null && m_availableMarker != null && m_routedMarker != null;
        public bool HasReadabilityAssets => m_statusRenderers != null && m_statusRenderers.Length == 2 &&
                                            m_statusRenderers[0] != null && m_statusRenderers[1] != null &&
                                            m_routingSelector != null;
        public RelayForkPresentationState PresentationState { get; private set; } = RelayForkPresentationState.Locked;

        private void Awake()
        {
            SetState(false, false);
        }

        private void Update()
        {
            if (m_routingRemaining <= 0f)
            {
                return;
            }

            m_routingRemaining = Mathf.Max(0f, m_routingRemaining - Time.unscaledDeltaTime);
            if (m_routingSelector != null)
            {
                var progress = 1f - m_routingRemaining / ROUTING_PRESENTATION_SECONDS;
                m_routingSelector.localRotation = Quaternion.Euler(0f, Mathf.SmoothStep(0f, 90f, progress), 0f);
            }

            _refreshPresentation();
        }

        public void Configure(Transform routingAnchor, GameObject availableMarker, GameObject routedMarker)
        {
            m_routingAnchor = routingAnchor;
            m_availableMarker = availableMarker;
            m_routedMarker = routedMarker;
            SetState(false, false);
        }

        public void ConfigureReadability(Renderer[] statusRenderers, Transform routingSelector)
        {
            m_statusRenderers = statusRenderers;
            m_routingSelector = routingSelector;
            m_routingRemaining = 0f;
            m_wasComplete = false;
            m_hasAppliedPresentation = false;
            if (m_routingSelector != null)
            {
                m_routingSelector.localRotation = Quaternion.identity;
            }

            _applyPresentation(RelayForkPresentationState.Locked);
        }

        public void SetState(bool available, bool complete)
        {
            if (complete && !m_wasComplete)
            {
                m_routingRemaining = ROUTING_PRESENTATION_SECONDS;
            }

            m_available = available;
            m_complete = complete;
            m_wasComplete = complete;
            if (m_availableMarker != null)
            {
                m_availableMarker.SetActive(available && !complete);
            }

            if (m_routedMarker != null)
            {
                m_routedMarker.SetActive(complete);
            }

            _refreshPresentation();
        }

        private void _refreshPresentation()
        {
            _applyPresentation(m_routingRemaining > 0f
                ? RelayForkPresentationState.Routing
                : m_complete
                    ? RelayForkPresentationState.Routed
                    : m_available
                        ? RelayForkPresentationState.Available
                        : RelayForkPresentationState.Locked);
        }

        private void _applyPresentation(RelayForkPresentationState state)
        {
            if (m_hasAppliedPresentation && PresentationState == state)
            {
                return;
            }

            m_hasAppliedPresentation = true;
            PresentationState = state;
            if (m_statusRenderers == null)
            {
                return;
            }

            var color = state switch
            {
                RelayForkPresentationState.Locked => new Color(0.16f, 0.06f, 0.055f),
                RelayForkPresentationState.Available => new Color(1f, 0.42f, 0.035f),
                RelayForkPresentationState.Routing => new Color(0.95f, 0.72f, 0.18f),
                _ => new Color(0.015f, 0.86f, 1f)
            };
            var emission = state == RelayForkPresentationState.Locked ? 0.06f : 1.2f;
            foreach (var statusRenderer in m_statusRenderers)
            {
                if (statusRenderer == null)
                {
                    continue;
                }

                var properties = new MaterialPropertyBlock();
                statusRenderer.GetPropertyBlock(properties);
                properties.SetColor(s_baseColor, color);
                properties.SetColor(s_emissionColor, color * emission);
                statusRenderer.SetPropertyBlock(properties);
            }
        }

        private bool m_available;
        private bool m_complete;
        private bool m_wasComplete;
        private bool m_hasAppliedPresentation;
        private float m_routingRemaining;
    }
}
