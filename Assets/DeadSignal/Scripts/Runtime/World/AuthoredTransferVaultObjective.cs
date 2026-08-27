using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Scene-authored interaction and persistent presentation for assembling the Central payload.
    /// </summary>
    public sealed class AuthoredTransferVaultObjective : MonoBehaviour
    {
        [SerializeField] private Transform m_assemblyAnchor;
        [SerializeField] private GameObject m_availableMarker;
        [SerializeField] private GameObject m_assembledMarker;
        [SerializeField] private GameObject m_relayRouteGate;
        [SerializeField] private GameObject m_relayRouteOpenMarker;

        public Vector3 Position => m_assemblyAnchor != null ? m_assemblyAnchor.position : transform.position;
        public bool IsConfigured => m_assemblyAnchor != null && m_availableMarker != null && m_assembledMarker != null;
        public bool IsRouteConfigured => m_relayRouteGate != null && m_relayRouteOpenMarker != null;
        public bool IsRelayRouteOpen => m_relayRouteGate != null && !m_relayRouteGate.activeSelf;

        private void Awake()
        {
            SetState(false, false);
            SetRouteOpen(false);
        }

        public void Configure(Transform assemblyAnchor, GameObject availableMarker, GameObject assembledMarker)
        {
            m_assemblyAnchor = assemblyAnchor;
            m_availableMarker = availableMarker;
            m_assembledMarker = assembledMarker;
            SetState(false, false);
        }

        public void SetState(bool available, bool complete)
        {
            if (m_availableMarker != null)
            {
                m_availableMarker.SetActive(available && !complete);
            }

            if (m_assembledMarker != null)
            {
                m_assembledMarker.SetActive(complete);
            }
        }

        public void ConfigureRouteGate(GameObject relayRouteGate, GameObject relayRouteOpenMarker)
        {
            m_relayRouteGate = relayRouteGate;
            m_relayRouteOpenMarker = relayRouteOpenMarker;
            SetRouteOpen(false);
        }

        public void SetRouteOpen(bool open)
        {
            if (m_relayRouteGate != null)
            {
                m_relayRouteGate.SetActive(!open);
            }

            if (m_relayRouteOpenMarker != null)
            {
                m_relayRouteOpenMarker.SetActive(open);
            }
        }
    }
}
