using UnityEngine;

namespace DeadSignal.World
{
    public sealed class AuthoredFluxShuntObjective : MonoBehaviour
    {
        [SerializeField] private Transform m_routingAnchor;
        [SerializeField] private GameObject m_availableMarker;
        [SerializeField] private GameObject m_routedMarker;

        public bool IsConfigured => m_routingAnchor != null && m_availableMarker != null && m_routedMarker != null;
        public Vector3 Position => m_routingAnchor != null ? m_routingAnchor.position : transform.position;

        public void Configure(Transform routingAnchor, GameObject availableMarker, GameObject routedMarker)
        {
            m_routingAnchor = routingAnchor;
            m_availableMarker = availableMarker;
            m_routedMarker = routedMarker;
            SetState(false, false);
        }

        public void SetState(bool available, bool routed)
        {
            if (m_availableMarker != null)
            {
                m_availableMarker.SetActive(available && !routed);
            }

            if (m_routedMarker != null)
            {
                m_routedMarker.SetActive(routed);
            }
        }
    }
}
