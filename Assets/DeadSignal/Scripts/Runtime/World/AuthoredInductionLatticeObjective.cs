using UnityEngine;

namespace DeadSignal.World
{
    public enum InductionLatticePresentationState
    {
        PrerequisiteLocked,
        ChargeAvailable,
        Charging,
        Charged
    }

    /// <summary>
    /// Owns the scene-authored lattice interaction and its persistent charging presentation.
    /// </summary>
    public sealed class AuthoredInductionLatticeObjective : MonoBehaviour
    {
        private const float STATE_TRANSITION_SECONDS = 0.75f;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform m_chargeAnchor;
        [SerializeField] private GameObject m_availableMarker;
        [SerializeField] private GameObject m_chargedMarker;
        [SerializeField] private Renderer[] m_readabilityRenderers;
        [SerializeField] private Transform m_chargeRotor;

        public Vector3 Position => m_chargeAnchor != null ? m_chargeAnchor.position : transform.position;
        public bool IsConfigured => m_chargeAnchor != null && m_availableMarker != null && m_chargedMarker != null;
        public bool HasReadabilityAssets => m_readabilityRenderers is { Length: > 0 } && m_chargeRotor != null;
        public InductionLatticePresentationState PresentationState { get; private set; } =
            InductionLatticePresentationState.PrerequisiteLocked;

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

        public void Configure(Transform chargeAnchor, GameObject availableMarker, GameObject chargedMarker)
        {
            m_chargeAnchor = chargeAnchor;
            m_availableMarker = availableMarker;
            m_chargedMarker = chargedMarker;
            SetState(false, false);
        }

        public void ConfigureReadability(Renderer[] readabilityRenderers, Transform chargeRotor)
        {
            m_readabilityRenderers = readabilityRenderers;
            m_chargeRotor = chargeRotor;
            m_lastCharged = false;
            m_transitionRemaining = 0f;
            m_hasAppliedPresentation = false;
            _refreshPresentation();
        }

        public void SetState(bool available, bool charged)
        {
            if (charged && !m_lastCharged)
            {
                m_transitionRemaining = STATE_TRANSITION_SECONDS;
            }

            m_lastCharged = charged;
            m_available = available;
            m_charged = charged;
            if (m_availableMarker != null)
            {
                m_availableMarker.SetActive(available && !charged);
            }

            if (m_chargedMarker != null)
            {
                m_chargedMarker.SetActive(charged);
            }

            _refreshPresentation();
        }

        private void _refreshPresentation()
        {
            var state = m_transitionRemaining > 0f
                ? InductionLatticePresentationState.Charging
                : m_charged
                    ? InductionLatticePresentationState.Charged
                    : m_available
                        ? InductionLatticePresentationState.ChargeAvailable
                        : InductionLatticePresentationState.PrerequisiteLocked;
            var progress = m_transitionRemaining > 0f
                ? 1f - m_transitionRemaining / STATE_TRANSITION_SECONDS
                : state == InductionLatticePresentationState.Charged ? 1f : 0f;
            _applyPresentation(state, progress);
        }

        private void _applyPresentation(InductionLatticePresentationState state, float progress)
        {
            if (!m_hasAppliedPresentation || PresentationState != state)
            {
                m_hasAppliedPresentation = true;
                PresentationState = state;
                var color = state switch
                {
                    InductionLatticePresentationState.PrerequisiteLocked => new Color(0.34f, 0.045f, 0.04f),
                    InductionLatticePresentationState.ChargeAvailable => new Color(1f, 0.46f, 0.055f),
                    InductionLatticePresentationState.Charging => new Color(0.96f, 0.76f, 0.22f),
                    _ => new Color(0.04f, 0.94f, 1f)
                };
                _setColors(color, state == InductionLatticePresentationState.PrerequisiteLocked ? 0.16f : 1.15f);
            }

            if (m_chargeRotor != null)
            {
                m_chargeRotor.localRotation = Quaternion.Euler(0f, progress * 120f, 0f);
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
        private bool m_charged;
        private bool m_lastCharged;
        private bool m_hasAppliedPresentation;
        private float m_transitionRemaining;
    }
}
