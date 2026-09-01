using UnityEngine;

namespace DeadSignal.World
{
    public enum RelayCalibrationPresentationState
    {
        PrerequisiteLocked,
        PayloadStabilized,
        InstallationAvailable,
        InstallationActive,
        Installed
    }

    /// <summary>
    /// Scene-authored Relay payload processing and Foundry installation markers.
    /// </summary>
    public sealed class AuthoredRelayPayloadObjective : MonoBehaviour
    {
        private const float STATE_TRANSITION_SECONDS = 0.75f;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform m_installAnchor;
        [SerializeField] private GameObject m_stabilizedMarker;
        [SerializeField] private GameObject m_installAvailableMarker;
        [SerializeField] private GameObject m_installedMarker;
        [SerializeField] private Renderer[] m_calibrationRenderers;
        [SerializeField] private Transform m_calibrationSelector;

        public Vector3 Position => m_installAnchor != null ? m_installAnchor.position : transform.position;
        public bool IsConfigured => m_installAnchor != null && m_stabilizedMarker != null &&
                                    m_installAvailableMarker != null && m_installedMarker != null;
        public bool HasReadabilityAssets => m_calibrationRenderers is { Length: > 0 } &&
                                            m_calibrationSelector != null;
        public RelayCalibrationPresentationState PresentationState { get; private set; } =
            RelayCalibrationPresentationState.PrerequisiteLocked;

        private void Awake()
        {
            SetState(false, false, false);
        }

        private void Update()
        {
            if (m_stabilizedTransitionRemaining > 0f)
            {
                m_stabilizedTransitionRemaining = Mathf.Max(
                    0f, m_stabilizedTransitionRemaining - Time.unscaledDeltaTime);
            }

            if (m_installationTransitionRemaining > 0f)
            {
                m_installationTransitionRemaining = Mathf.Max(
                    0f, m_installationTransitionRemaining - Time.unscaledDeltaTime);
            }

            _refreshPresentation();
        }

        public void Configure(
            Transform installAnchor,
            GameObject stabilizedMarker,
            GameObject installAvailableMarker,
            GameObject installedMarker)
        {
            m_installAnchor = installAnchor;
            m_stabilizedMarker = stabilizedMarker;
            m_installAvailableMarker = installAvailableMarker;
            m_installedMarker = installedMarker;
            SetState(false, false, false);
        }

        public void ConfigureReadability(
            Renderer[] calibrationRenderers,
            Transform calibrationSelector)
        {
            m_calibrationRenderers = calibrationRenderers;
            m_calibrationSelector = calibrationSelector;
            m_hasAppliedPresentation = false;
            m_lastStabilized = false;
            m_lastInstalled = false;
            m_stabilizedTransitionRemaining = 0f;
            m_installationTransitionRemaining = 0f;
            SetState(false, false, false);
        }

        public void SetState(bool stabilized, bool installAvailable, bool installed)
        {
            if (stabilized && !m_lastStabilized)
            {
                m_stabilizedTransitionRemaining = STATE_TRANSITION_SECONDS;
            }

            if (installed && !m_lastInstalled)
            {
                m_installationTransitionRemaining = STATE_TRANSITION_SECONDS;
            }

            m_lastStabilized = stabilized;
            m_lastInstalled = installed;
            m_installAvailable = installAvailable;
            m_installed = installed;

            if (m_stabilizedMarker != null)
            {
                m_stabilizedMarker.SetActive(stabilized || installed);
            }

            if (m_installAvailableMarker != null)
            {
                m_installAvailableMarker.SetActive(installAvailable && !installed);
            }

            if (m_installedMarker != null)
            {
                m_installedMarker.SetActive(installed);
            }

            _refreshPresentation();
        }

        private void _refreshPresentation()
        {
            var state = m_installationTransitionRemaining > 0f
                ? RelayCalibrationPresentationState.InstallationActive
                : m_installed
                    ? RelayCalibrationPresentationState.Installed
                    : m_stabilizedTransitionRemaining > 0f
                        ? RelayCalibrationPresentationState.PayloadStabilized
                        : m_installAvailable
                            ? RelayCalibrationPresentationState.InstallationAvailable
                            : RelayCalibrationPresentationState.PrerequisiteLocked;
            _applyPresentation(state);
        }

        private void _applyPresentation(RelayCalibrationPresentationState state)
        {
            if (m_hasAppliedPresentation && PresentationState == state)
            {
                return;
            }

            m_hasAppliedPresentation = true;
            PresentationState = state;
            var color = state switch
            {
                RelayCalibrationPresentationState.PrerequisiteLocked => new Color(0.34f, 0.055f, 0.045f),
                RelayCalibrationPresentationState.PayloadStabilized => new Color(0.08f, 0.82f, 1f),
                RelayCalibrationPresentationState.InstallationAvailable => new Color(1f, 0.46f, 0.055f),
                RelayCalibrationPresentationState.InstallationActive => new Color(1f, 0.74f, 0.18f),
                _ => new Color(0.02f, 0.92f, 1f)
            };
            var emissionMultiplier = state == RelayCalibrationPresentationState.PrerequisiteLocked ? 0.16f : 1.15f;
            var properties = new MaterialPropertyBlock();
            if (m_calibrationRenderers != null)
            {
                foreach (var renderer in m_calibrationRenderers)
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

            if (m_calibrationSelector != null)
            {
                var angle = state switch
                {
                    RelayCalibrationPresentationState.PayloadStabilized => 35f,
                    RelayCalibrationPresentationState.InstallationAvailable => 90f,
                    RelayCalibrationPresentationState.InstallationActive => 145f,
                    RelayCalibrationPresentationState.Installed => 180f,
                    _ => 0f
                };
                m_calibrationSelector.localRotation = Quaternion.Euler(0f, angle, 0f);
            }
        }

        private bool m_installAvailable;
        private bool m_installed;
        private bool m_lastStabilized;
        private bool m_lastInstalled;
        private bool m_hasAppliedPresentation;
        private float m_stabilizedTransitionRemaining;
        private float m_installationTransitionRemaining;
    }
}
