using UnityEngine;

namespace DeadSignal.World
{
    public sealed class AuthoredQuenchStabilizationObjective : MonoBehaviour
    {
        private const float PROCESSING_TRANSITION_SECONDS = 0.75f;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform m_stabilizationAnchor;
        [SerializeField] private GameObject m_availableMarker;
        [SerializeField] private GameObject m_completeMarker;
        [SerializeField] private Renderer[] m_readabilityRenderers;
        [SerializeField] private Transform m_processingGlyph;

        public bool IsConfigured => m_stabilizationAnchor != null && m_availableMarker != null && m_completeMarker != null;
        public bool HasReadabilityAssets => m_readabilityRenderers is { Length: > 0 } && m_processingGlyph != null;
        public Vector3 Position => m_stabilizationAnchor != null ? m_stabilizationAnchor.position : transform.position;
        public CoreProcessingPresentationState PresentationState { get; private set; } =
            CoreProcessingPresentationState.Locked;

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

        public void Configure(Transform stabilizationAnchor, GameObject availableMarker, GameObject completeMarker)
        {
            m_stabilizationAnchor = stabilizationAnchor;
            m_availableMarker = availableMarker;
            m_completeMarker = completeMarker;
            SetState(false, false);
        }

        public void ConfigureReadability(Renderer[] readabilityRenderers, Transform processingGlyph)
        {
            m_readabilityRenderers = readabilityRenderers;
            m_processingGlyph = processingGlyph;
            m_transitionRemaining = 0f;
            m_wasComplete = false;
            m_hasAppliedPresentation = false;
            _refreshPresentation();
        }

        public void SetState(bool available, bool complete)
        {
            if (complete && !m_wasComplete)
            {
                m_transitionRemaining = PROCESSING_TRANSITION_SECONDS;
            }

            m_available = available;
            m_complete = complete;
            m_wasComplete = complete;
            if (m_availableMarker != null)
            {
                m_availableMarker.SetActive(available && !complete);
            }

            if (m_completeMarker != null)
            {
                m_completeMarker.SetActive(complete);
            }

            _refreshPresentation();
        }

        private void _refreshPresentation()
        {
            var state = m_complete
                ? m_transitionRemaining > 0f
                    ? CoreProcessingPresentationState.ProcessingActive
                    : CoreProcessingPresentationState.Complete
                : m_available
                    ? CoreProcessingPresentationState.Available
                    : CoreProcessingPresentationState.Locked;
            if (!m_hasAppliedPresentation || state != PresentationState)
            {
                m_hasAppliedPresentation = true;
                PresentationState = state;
                var color = state switch
                {
                    CoreProcessingPresentationState.Locked => new Color(0.34f, 0.045f, 0.04f),
                    CoreProcessingPresentationState.Available => new Color(1f, 0.46f, 0.055f),
                    CoreProcessingPresentationState.ProcessingActive => new Color(0.14f, 0.72f, 1f),
                    _ => new Color(0.04f, 0.94f, 1f)
                };
                _setColors(color, state == CoreProcessingPresentationState.Locked ? 0.16f : 1.15f);
            }

            if (m_processingGlyph != null)
            {
                var progress = m_transitionRemaining > 0f
                    ? 1f - m_transitionRemaining / PROCESSING_TRANSITION_SECONDS
                    : 1f;
                var angle = state == CoreProcessingPresentationState.ProcessingActive ? progress * 240f : 0f;
                m_processingGlyph.localRotation = Quaternion.Euler(0f, angle, 0f);
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
        private bool m_complete;
        private bool m_wasComplete;
        private bool m_hasAppliedPresentation;
        private float m_transitionRemaining;
    }
}
