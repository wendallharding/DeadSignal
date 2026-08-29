using UnityEngine;

namespace DeadSignal.World
{
    public enum SpineTowerPresentationState
    {
        PressurizedLocked,
        ActivationAvailable,
        Activating,
        Powered
    }

    /// <summary>
    /// Owns persistent Spine Tower presentation while activation authority remains in RunModel.
    /// </summary>
    public sealed class AuthoredSpineTowerReadability : MonoBehaviour
    {
        private const float STATE_TRANSITION_SECONDS = 0.75f;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer[] m_readabilityRenderers;
        [SerializeField] private Transform m_networkSelector;

        public bool IsConfigured => m_readabilityRenderers is { Length: > 0 } && m_networkSelector != null;
        public SpineTowerPresentationState PresentationState { get; private set; } =
            SpineTowerPresentationState.PressurizedLocked;

        private void Awake()
        {
            SetState(false, false);
        }

        private void Update()
        {
            if (m_activationTransitionRemaining > 0f)
            {
                m_activationTransitionRemaining = Mathf.Max(
                    0f, m_activationTransitionRemaining - Time.unscaledDeltaTime);
            }

            _refreshPresentation();
        }

        public void Configure(Renderer[] readabilityRenderers, Transform networkSelector)
        {
            m_readabilityRenderers = readabilityRenderers;
            m_networkSelector = networkSelector;
            m_lastPowered = false;
            m_activationTransitionRemaining = 0f;
            m_hasAppliedPresentation = false;
            SetState(false, false);
        }

        public void SetState(bool activationAvailable, bool powered)
        {
            if (powered && !m_lastPowered)
            {
                m_activationTransitionRemaining = STATE_TRANSITION_SECONDS;
            }

            m_lastPowered = powered;
            m_activationAvailable = activationAvailable;
            m_powered = powered;
            _refreshPresentation();
        }

        private void _refreshPresentation()
        {
            var state = m_activationTransitionRemaining > 0f
                ? SpineTowerPresentationState.Activating
                : m_powered
                    ? SpineTowerPresentationState.Powered
                    : m_activationAvailable
                        ? SpineTowerPresentationState.ActivationAvailable
                        : SpineTowerPresentationState.PressurizedLocked;
            _applyPresentation(state);
        }

        private void _applyPresentation(SpineTowerPresentationState state)
        {
            if (m_hasAppliedPresentation && PresentationState == state)
            {
                return;
            }

            m_hasAppliedPresentation = true;
            PresentationState = state;
            var color = state switch
            {
                SpineTowerPresentationState.PressurizedLocked => new Color(0.38f, 0.055f, 0.045f),
                SpineTowerPresentationState.ActivationAvailable => new Color(1f, 0.46f, 0.055f),
                SpineTowerPresentationState.Activating => new Color(1f, 0.72f, 0.16f),
                _ => new Color(0.02f, 0.92f, 1f)
            };
            var emissionMultiplier = state == SpineTowerPresentationState.PressurizedLocked ? 0.16f : 1.2f;
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

            if (m_networkSelector != null)
            {
                var angle = state switch
                {
                    SpineTowerPresentationState.ActivationAvailable => 65f,
                    SpineTowerPresentationState.Activating => 135f,
                    SpineTowerPresentationState.Powered => 180f,
                    _ => 0f
                };
                m_networkSelector.localRotation = Quaternion.Euler(0f, angle, 0f);
            }
        }

        private bool m_activationAvailable;
        private bool m_powered;
        private bool m_lastPowered;
        private bool m_hasAppliedPresentation;
        private float m_activationTransitionRemaining;
    }
}
