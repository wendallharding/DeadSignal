using UnityEngine;

namespace DeadSignal.World
{
    public enum SpineCoreInstallationPresentationState
    {
        InstallationLocked,
        CompletedCoreAvailable,
        InstallationActive,
        FinalInstalled
    }

    /// <summary>
    /// Scene-authored final core installation anchor and persistent completion state at the Spine Tower.
    /// </summary>
    public sealed class AuthoredSpineCoreInstallationObjective : MonoBehaviour
    {
        private const float INSTALLATION_TRANSITION_SECONDS = 0.75f;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform m_installAnchor;
        [SerializeField] private GameObject m_availableMarker;
        [SerializeField] private GameObject m_installedMarker;
        [SerializeField] private Renderer m_socketStatusRenderer;
        [SerializeField] private Transform m_socketSelector;

        public Vector3 Position => m_installAnchor != null ? m_installAnchor.position : transform.position;
        public bool IsConfigured => m_installAnchor != null && m_availableMarker != null && m_installedMarker != null;
        public bool HasReadabilityAssets => m_socketStatusRenderer != null && m_socketSelector != null;
        public SpineCoreInstallationPresentationState PresentationState { get; private set; } =
            SpineCoreInstallationPresentationState.InstallationLocked;

        private void Awake()
        {
            SetState(false, false);
        }

        private void Update()
        {
            if (m_installationTransitionRemaining <= 0f)
            {
                return;
            }

            m_installationTransitionRemaining = Mathf.Max(
                0f, m_installationTransitionRemaining - Time.unscaledDeltaTime);
            _refreshPresentation();
        }

        public void Configure(Transform installAnchor, GameObject availableMarker, GameObject installedMarker)
        {
            m_installAnchor = installAnchor;
            m_availableMarker = availableMarker;
            m_installedMarker = installedMarker;
            SetState(false, false);
        }

        public void ConfigureReadability(Renderer socketStatusRenderer, Transform socketSelector)
        {
            m_socketStatusRenderer = socketStatusRenderer;
            m_socketSelector = socketSelector;
            m_installationTransitionRemaining = 0f;
            m_wasInstalled = false;
            m_hasAppliedPresentation = false;
            _refreshPresentation();
        }

        public void SetState(bool available, bool installed)
        {
            if (installed && !m_wasInstalled)
            {
                m_installationTransitionRemaining = INSTALLATION_TRANSITION_SECONDS;
            }

            m_available = available;
            m_installed = installed;
            m_wasInstalled = installed;
            if (m_availableMarker != null)
            {
                m_availableMarker.SetActive(available && !installed);
            }

            if (m_installedMarker != null)
            {
                m_installedMarker.SetActive(installed);
            }

            _refreshPresentation();
        }

        private void _refreshPresentation()
        {
            var state = m_installed
                ? m_installationTransitionRemaining > 0f
                    ? SpineCoreInstallationPresentationState.InstallationActive
                    : SpineCoreInstallationPresentationState.FinalInstalled
                : m_available
                    ? SpineCoreInstallationPresentationState.CompletedCoreAvailable
                    : SpineCoreInstallationPresentationState.InstallationLocked;
            if (!m_hasAppliedPresentation || state != PresentationState)
            {
                m_hasAppliedPresentation = true;
                PresentationState = state;
                var color = state switch
                {
                    SpineCoreInstallationPresentationState.InstallationLocked => new Color(0.34f, 0.045f, 0.04f),
                    SpineCoreInstallationPresentationState.CompletedCoreAvailable => new Color(1f, 0.46f, 0.055f),
                    SpineCoreInstallationPresentationState.InstallationActive => new Color(0.95f, 0.05f, 0.72f),
                    _ => new Color(0.04f, 0.94f, 1f)
                };
                var emissionMultiplier = state == SpineCoreInstallationPresentationState.InstallationLocked ? 0.16f : 1.2f;
                _setColor(color, emissionMultiplier);
            }

            if (m_socketSelector != null)
            {
                var progress = 1f - m_installationTransitionRemaining / INSTALLATION_TRANSITION_SECONDS;
                var angle = state switch
                {
                    SpineCoreInstallationPresentationState.InstallationLocked => -18f,
                    SpineCoreInstallationPresentationState.CompletedCoreAvailable => 0f,
                    SpineCoreInstallationPresentationState.InstallationActive => Mathf.Lerp(0f, 120f, progress),
                    _ => 120f
                };
                m_socketSelector.localRotation = Quaternion.Euler(0f, angle, 0f);
            }
        }

        private void _setColor(Color color, float emissionMultiplier)
        {
            if (m_socketStatusRenderer == null)
            {
                return;
            }

            var properties = new MaterialPropertyBlock();
            m_socketStatusRenderer.GetPropertyBlock(properties);
            properties.SetColor(s_baseColor, color);
            properties.SetColor(s_emissionColor, color * emissionMultiplier);
            m_socketStatusRenderer.SetPropertyBlock(properties);
        }

        private bool m_available;
        private bool m_installed;
        private bool m_wasInstalled;
        private bool m_hasAppliedPresentation;
        private float m_installationTransitionRemaining;
    }
}
