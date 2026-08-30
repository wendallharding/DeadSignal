using UnityEngine;

namespace DeadSignal.World
{
    public enum ConvergenceCalibrationPresentationState
    {
        Dormant,
        PrerequisiteLocked,
        Available,
        Active,
        Complete
    }

    public sealed class AuthoredConvergenceCalibrationObjective : MonoBehaviour
    {
        private const float STATE_TRANSITION_SECONDS = 0.75f;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform m_interactionAnchor;
        [SerializeField] private Transform m_calibrationVolume;
        [SerializeField] private Transform m_pressureAnchor;
        [SerializeField] private GameObject m_availableState;
        [SerializeField] private GameObject m_activeState;
        [SerializeField] private GameObject m_completeState;
        [SerializeField] private Renderer[] m_readabilityRenderers;
        [SerializeField] private Transform m_calibrationSelector;
        [SerializeField] private Vector2 m_calibrationHalfExtents = new(5.6f, 2.9f);

        public Vector3 Position => m_interactionAnchor != null ? m_interactionAnchor.position : transform.position;
        public Transform PressureAnchor => m_pressureAnchor;
        public bool IsConfigured => m_interactionAnchor != null && m_calibrationVolume != null &&
                                    m_pressureAnchor != null && m_availableState != null && m_activeState != null &&
                                    m_completeState != null;
        public bool HasReadabilityAssets => m_readabilityRenderers is { Length: > 0 } && m_calibrationSelector != null;
        public ConvergenceCalibrationPresentationState PresentationState { get; private set; } =
            ConvergenceCalibrationPresentationState.Dormant;

        private void Awake()
        {
            SetState(false, false, false, false);
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

        public void Configure(
            Transform interactionAnchor,
            Transform calibrationVolume,
            Transform pressureAnchor,
            GameObject availableState,
            GameObject activeState,
            GameObject completeState,
            Vector2 calibrationHalfExtents)
        {
            m_interactionAnchor = interactionAnchor;
            m_calibrationVolume = calibrationVolume;
            m_pressureAnchor = pressureAnchor;
            m_availableState = availableState;
            m_activeState = activeState;
            m_completeState = completeState;
            m_calibrationHalfExtents = new Vector2(
                Mathf.Max(0.5f, calibrationHalfExtents.x),
                Mathf.Max(0.5f, calibrationHalfExtents.y));
            SetState(false, false, false, false);
        }

        public void ConfigureReadability(Renderer[] readabilityRenderers, Transform calibrationSelector)
        {
            m_readabilityRenderers = readabilityRenderers;
            m_calibrationSelector = calibrationSelector;
            m_transitionRemaining = 0f;
            m_wasActive = false;
            m_hasAppliedPresentation = false;
            _refreshPresentation();
        }

        public bool Contains(Vector3 worldPosition)
        {
            if (m_calibrationVolume == null)
            {
                return false;
            }

            var localPosition = m_calibrationVolume.InverseTransformPoint(worldPosition);
            return Mathf.Abs(localPosition.x) <= m_calibrationHalfExtents.x &&
                   Mathf.Abs(localPosition.z) <= m_calibrationHalfExtents.y;
        }

        public void SetState(bool deepNetworkPowered, bool available, bool active, bool complete)
        {
            if (active && !m_wasActive)
            {
                m_transitionRemaining = STATE_TRANSITION_SECONDS;
            }

            m_deepNetworkPowered = deepNetworkPowered;
            m_available = available;
            m_active = active;
            m_complete = complete;
            m_wasActive = active;
            if (m_availableState != null) m_availableState.SetActive(available && !active && !complete);
            if (m_activeState != null) m_activeState.SetActive(active && !complete);
            if (m_completeState != null) m_completeState.SetActive(complete);
            _refreshPresentation();
        }

        private void _refreshPresentation()
        {
            var state = m_complete
                ? ConvergenceCalibrationPresentationState.Complete
                : m_active
                    ? ConvergenceCalibrationPresentationState.Active
                    : m_available
                        ? ConvergenceCalibrationPresentationState.Available
                        : m_deepNetworkPowered
                            ? ConvergenceCalibrationPresentationState.PrerequisiteLocked
                            : ConvergenceCalibrationPresentationState.Dormant;
            if (!m_hasAppliedPresentation || state != PresentationState)
            {
                m_hasAppliedPresentation = true;
                PresentationState = state;
                var color = state switch
                {
                    ConvergenceCalibrationPresentationState.Dormant => new Color(0.14f, 0.16f, 0.18f),
                    ConvergenceCalibrationPresentationState.PrerequisiteLocked => new Color(0.34f, 0.045f, 0.04f),
                    ConvergenceCalibrationPresentationState.Available => new Color(1f, 0.46f, 0.055f),
                    ConvergenceCalibrationPresentationState.Active => new Color(0.96f, 0.18f, 0.06f),
                    _ => new Color(0.04f, 0.94f, 1f)
                };
                var emissionMultiplier = state switch
                {
                    ConvergenceCalibrationPresentationState.Dormant => 0.04f,
                    ConvergenceCalibrationPresentationState.PrerequisiteLocked => 0.16f,
                    _ => 1.15f
                };
                _setColors(color, emissionMultiplier);
            }

            if (m_calibrationSelector != null)
            {
                var targetAngle = state switch
                {
                    ConvergenceCalibrationPresentationState.Dormant => -24f,
                    ConvergenceCalibrationPresentationState.PrerequisiteLocked => 0f,
                    ConvergenceCalibrationPresentationState.Available => 24f,
                    ConvergenceCalibrationPresentationState.Active => 72f,
                    _ => 120f
                };
                var progress = m_transitionRemaining > 0f
                    ? 1f - m_transitionRemaining / STATE_TRANSITION_SECONDS
                    : 1f;
                m_calibrationSelector.localRotation = Quaternion.Euler(0f, targetAngle * progress, 0f);
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

        private bool m_deepNetworkPowered;
        private bool m_available;
        private bool m_active;
        private bool m_complete;
        private bool m_wasActive;
        private bool m_hasAppliedPresentation;
        private float m_transitionRemaining;
    }
}
