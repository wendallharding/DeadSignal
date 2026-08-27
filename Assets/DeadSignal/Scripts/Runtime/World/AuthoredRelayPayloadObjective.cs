using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Scene-authored Relay payload processing and Foundry installation markers.
    /// </summary>
    public sealed class AuthoredRelayPayloadObjective : MonoBehaviour
    {
        [SerializeField] private Transform m_installAnchor;
        [SerializeField] private GameObject m_stabilizedMarker;
        [SerializeField] private GameObject m_installAvailableMarker;
        [SerializeField] private GameObject m_installedMarker;

        public Vector3 Position => m_installAnchor != null ? m_installAnchor.position : transform.position;
        public bool IsConfigured => m_installAnchor != null && m_stabilizedMarker != null &&
                                    m_installAvailableMarker != null && m_installedMarker != null;

        private void Awake()
        {
            SetState(false, false, false);
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

        public void SetState(bool stabilized, bool installAvailable, bool installed)
        {
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
        }
    }
}
