using UnityEngine;

namespace DeadSignal.World
{
    public enum SpineBerthPresentationState
    {
        DormantPressurized,
        VentAvailable,
        VentingActive,
        Vented
    }

    /// <summary>
    /// Scene-authored discharge control and persistent safe-pressure state for the Spine berth.
    /// </summary>
    public sealed class AuthoredSpineVentingObjective : MonoBehaviour
    {
        private const float STATE_TRANSITION_SECONDS = 0.75f;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform m_controlAnchor;
        [SerializeField] private GameObject m_ventAvailableMarker;
        [SerializeField] private GameObject m_ventedMarker;
        [SerializeField] private Renderer[] m_readabilityRenderers;
        [SerializeField] private Transform m_pressureSelector;

        public Vector3 Position => m_controlAnchor != null ? m_controlAnchor.position : transform.position;
        public bool IsConfigured => m_controlAnchor != null && m_ventAvailableMarker != null && m_ventedMarker != null;
        public bool HasReadabilityAssets => m_readabilityRenderers is { Length: > 0 } && m_pressureSelector != null;
        public SpineBerthPresentationState PresentationState { get; private set; } =
            SpineBerthPresentationState.DormantPressurized;

        private void Awake()
        {
            SetState(false, false);
        }

        private void Update()
        {
            if (m_ventTransitionRemaining > 0f)
            {
                m_ventTransitionRemaining = Mathf.Max(0f, m_ventTransitionRemaining - Time.unscaledDeltaTime);
            }

            _refreshPresentation();
        }

        public void Configure(Transform controlAnchor, GameObject ventAvailableMarker, GameObject ventedMarker)
        {
            m_controlAnchor = controlAnchor;
            m_ventAvailableMarker = ventAvailableMarker;
            m_ventedMarker = ventedMarker;
            SetState(false, false);
        }

        public void ConfigureReadability(Renderer[] readabilityRenderers, Transform pressureSelector)
        {
            m_readabilityRenderers = readabilityRenderers;
            m_pressureSelector = pressureSelector;
            m_lastVented = false;
            m_ventTransitionRemaining = 0f;
            m_hasAppliedPresentation = false;
            SetState(false, false);
        }

        public void SetState(bool ventAvailable, bool vented)
        {
            if (vented && !m_lastVented)
            {
                m_ventTransitionRemaining = STATE_TRANSITION_SECONDS;
            }

            m_lastVented = vented;
            m_ventAvailable = ventAvailable;
            m_vented = vented;
            if (m_ventAvailableMarker != null)
            {
                m_ventAvailableMarker.SetActive(ventAvailable && !vented);
            }

            if (m_ventedMarker != null)
            {
                m_ventedMarker.SetActive(vented);
            }

            _refreshPresentation();
        }

        private void _refreshPresentation()
        {
            var state = m_ventTransitionRemaining > 0f
                ? SpineBerthPresentationState.VentingActive
                : m_vented
                    ? SpineBerthPresentationState.Vented
                    : m_ventAvailable
                        ? SpineBerthPresentationState.VentAvailable
                        : SpineBerthPresentationState.DormantPressurized;
            _applyPresentation(state);
        }

        private void _applyPresentation(SpineBerthPresentationState state)
        {
            if (m_hasAppliedPresentation && PresentationState == state)
            {
                return;
            }

            m_hasAppliedPresentation = true;
            PresentationState = state;
            var color = state switch
            {
                SpineBerthPresentationState.DormantPressurized => new Color(0.38f, 0.055f, 0.045f),
                SpineBerthPresentationState.VentAvailable => new Color(1f, 0.46f, 0.055f),
                SpineBerthPresentationState.VentingActive => new Color(1f, 0.72f, 0.16f),
                _ => new Color(0.04f, 0.92f, 1f)
            };
            var emissionMultiplier = state == SpineBerthPresentationState.DormantPressurized ? 0.16f : 1.15f;
            var properties = new MaterialPropertyBlock();
            if (m_readabilityRenderers != null)
            {
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

            if (m_pressureSelector != null)
            {
                var angle = state switch
                {
                    SpineBerthPresentationState.VentAvailable => 55f,
                    SpineBerthPresentationState.VentingActive => 125f,
                    SpineBerthPresentationState.Vented => 180f,
                    _ => 0f
                };
                m_pressureSelector.localRotation = Quaternion.Euler(0f, angle, 0f);
            }
        }

        private bool m_ventAvailable;
        private bool m_vented;
        private bool m_lastVented;
        private bool m_hasAppliedPresentation;
        private float m_ventTransitionRemaining;
    }
}
