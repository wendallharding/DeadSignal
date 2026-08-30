using UnityEngine;

namespace DeadSignal.World
{
    public enum BreakerResetPresentationState
    {
        DistributionLocked,
        ResetAvailable,
        ResetActive,
        ResetComplete
    }

    public sealed class AuthoredBreakerResetObjective : MonoBehaviour
    {
        private const float RESET_TRANSITION_SECONDS = 0.75f;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform m_resetAnchor;
        [SerializeField] private GameObject m_availableMarker;
        [SerializeField] private GameObject m_completeMarker;
        [SerializeField] private Renderer[] m_readabilityRenderers;
        [SerializeField] private Transform m_resetSelector;

        public bool IsConfigured => m_resetAnchor != null && m_availableMarker != null && m_completeMarker != null;
        public bool HasReadabilityAssets => m_readabilityRenderers is { Length: > 0 } && m_resetSelector != null;
        public Vector3 Position => m_resetAnchor != null ? m_resetAnchor.position : transform.position;
        public BreakerResetPresentationState PresentationState { get; private set; } =
            BreakerResetPresentationState.DistributionLocked;

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

        public void Configure(Transform resetAnchor, GameObject availableMarker, GameObject completeMarker)
        {
            m_resetAnchor = resetAnchor;
            m_availableMarker = availableMarker;
            m_completeMarker = completeMarker;
            SetState(false, false);
        }

        public void ConfigureReadability(Renderer[] readabilityRenderers, Transform resetSelector)
        {
            m_readabilityRenderers = readabilityRenderers;
            m_resetSelector = resetSelector;
            m_transitionRemaining = 0f;
            m_wasComplete = false;
            m_hasAppliedPresentation = false;
            _refreshPresentation();
        }

        public void SetState(bool available, bool complete)
        {
            if (complete && !m_wasComplete)
            {
                m_transitionRemaining = RESET_TRANSITION_SECONDS;
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
                    ? BreakerResetPresentationState.ResetActive
                    : BreakerResetPresentationState.ResetComplete
                : m_available
                    ? BreakerResetPresentationState.ResetAvailable
                    : BreakerResetPresentationState.DistributionLocked;
            if (!m_hasAppliedPresentation || state != PresentationState)
            {
                m_hasAppliedPresentation = true;
                PresentationState = state;
                var color = state switch
                {
                    BreakerResetPresentationState.DistributionLocked => new Color(0.34f, 0.045f, 0.04f),
                    BreakerResetPresentationState.ResetAvailable => new Color(1f, 0.46f, 0.055f),
                    BreakerResetPresentationState.ResetActive => new Color(0.96f, 0.18f, 0.06f),
                    _ => new Color(0.04f, 0.94f, 1f)
                };
                var emissionMultiplier = state == BreakerResetPresentationState.DistributionLocked ? 0.16f : 1.15f;
                _setColors(color, emissionMultiplier);
            }

            if (m_resetSelector != null)
            {
                var targetAngle = state switch
                {
                    BreakerResetPresentationState.DistributionLocked => -34f,
                    BreakerResetPresentationState.ResetAvailable => 0f,
                    BreakerResetPresentationState.ResetActive =>
                        Mathf.Lerp(0f, 110f, 1f - m_transitionRemaining / RESET_TRANSITION_SECONDS),
                    _ => 110f
                };
                m_resetSelector.localRotation = Quaternion.Euler(0f, targetAngle, 0f);
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
