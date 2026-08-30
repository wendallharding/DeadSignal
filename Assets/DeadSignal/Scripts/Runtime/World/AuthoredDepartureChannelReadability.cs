using UnityEngine;

namespace DeadSignal.World
{
    public enum DepartureChannelPresentationState
    {
        DormantLocked,
        ReleaseAvailable,
        ReleaseActive,
        OpenSurgeAvailable,
        OpenSurgeConsumed
    }

    /// <summary>
    /// Presents the Departure Channel release and its one-shot recovery without owning route or reward authority.
    /// </summary>
    public sealed class AuthoredDepartureChannelReadability : MonoBehaviour
    {
        private const float RELEASE_TRANSITION_SECONDS = 0.75f;
        private const float OPEN_SHUTTER_HEIGHT = 1.2f;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform m_shutterPresentation;
        [SerializeField] private GameObject m_returnSignal;
        [SerializeField] private GameObject m_surgeSignal;
        [SerializeField] private Renderer[] m_lockRenderers;
        [SerializeField] private Renderer[] m_thresholdRenderers;

        public bool IsConfigured => m_shutterPresentation != null && m_returnSignal != null && m_surgeSignal != null &&
                                    m_lockRenderers is { Length: > 0 } && m_thresholdRenderers is { Length: > 0 };
        public bool IsOpen => m_isOpen;
        public DepartureChannelPresentationState PresentationState { get; private set; } =
            DepartureChannelPresentationState.DormantLocked;

        private void Awake()
        {
            ResetPresentation();
        }

        private void Update()
        {
            if (m_releaseTransitionRemaining <= 0f)
            {
                return;
            }

            m_releaseTransitionRemaining = Mathf.Max(0f, m_releaseTransitionRemaining - Time.unscaledDeltaTime);
            _refreshPresentation();
        }

        public void Configure(
            Transform shutterPresentation,
            GameObject returnSignal,
            GameObject surgeSignal,
            Renderer[] lockRenderers,
            Renderer[] thresholdRenderers)
        {
            m_shutterPresentation = shutterPresentation;
            m_returnSignal = returnSignal;
            m_surgeSignal = surgeSignal;
            m_lockRenderers = lockRenderers;
            m_thresholdRenderers = thresholdRenderers;
            ResetPresentation();
        }

        public void ResetPresentation()
        {
            m_releaseAvailable = false;
            m_isOpen = false;
            m_surgeConsumed = false;
            m_releaseTransitionRemaining = 0f;
            m_hasAppliedState = false;
            _refreshPresentation();
        }

        public void SetReleaseAvailable(bool available)
        {
            if (m_isOpen)
            {
                return;
            }

            m_releaseAvailable = available;
            _refreshPresentation();
        }

        public void BeginRelease()
        {
            if (m_isOpen)
            {
                return;
            }

            m_releaseAvailable = false;
            m_isOpen = true;
            m_releaseTransitionRemaining = RELEASE_TRANSITION_SECONDS;
            _refreshPresentation();
        }

        public void SetSurgeConsumed()
        {
            if (!m_isOpen)
            {
                return;
            }

            m_surgeConsumed = true;
            _refreshPresentation();
        }

        private void _refreshPresentation()
        {
            var state = !m_isOpen
                ? m_releaseAvailable
                    ? DepartureChannelPresentationState.ReleaseAvailable
                    : DepartureChannelPresentationState.DormantLocked
                : m_releaseTransitionRemaining > 0f
                    ? DepartureChannelPresentationState.ReleaseActive
                    : m_surgeConsumed
                        ? DepartureChannelPresentationState.OpenSurgeConsumed
                        : DepartureChannelPresentationState.OpenSurgeAvailable;

            var progress = m_isOpen
                ? 1f - m_releaseTransitionRemaining / RELEASE_TRANSITION_SECONDS
                : 0f;
            if (m_shutterPresentation != null)
            {
                m_shutterPresentation.localPosition = Vector3.up * (OPEN_SHUTTER_HEIGHT * Mathf.Clamp01(progress));
                m_shutterPresentation.gameObject.SetActive(!m_isOpen || m_releaseTransitionRemaining > 0f);
            }

            if (m_returnSignal != null)
            {
                m_returnSignal.SetActive(m_isOpen);
            }

            if (m_surgeSignal != null)
            {
                m_surgeSignal.SetActive(m_isOpen && !m_surgeConsumed);
            }

            if (m_hasAppliedState && PresentationState == state)
            {
                return;
            }

            m_hasAppliedState = true;
            PresentationState = state;
            var color = state switch
            {
                DepartureChannelPresentationState.DormantLocked => new Color(0.42f, 0.04f, 0.03f),
                DepartureChannelPresentationState.ReleaseAvailable => new Color(1f, 0.45f, 0.035f),
                DepartureChannelPresentationState.ReleaseActive => new Color(0.95f, 0.08f, 0.72f),
                DepartureChannelPresentationState.OpenSurgeAvailable => new Color(0.02f, 0.9f, 1f),
                _ => new Color(0.08f, 0.36f, 0.42f)
            };
            var emission = state == DepartureChannelPresentationState.DormantLocked ? 0.18f :
                state == DepartureChannelPresentationState.OpenSurgeConsumed ? 0.42f : 1.25f;
            _setRendererColors(m_lockRenderers, color, emission);
            _setRendererColors(m_thresholdRenderers, color, emission);
        }

        private static void _setRendererColors(Renderer[] renderers, Color color, float emissionMultiplier)
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
            }
        }

        private bool m_releaseAvailable;
        private bool m_isOpen;
        private bool m_surgeConsumed;
        private bool m_hasAppliedState;
        private float m_releaseTransitionRemaining;
    }
}
