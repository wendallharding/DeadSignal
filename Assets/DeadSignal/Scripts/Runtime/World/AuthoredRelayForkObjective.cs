using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Scene-authored interaction and persistent presentation for routing the two Central feeds.
    /// </summary>
    public sealed class AuthoredRelayForkObjective : MonoBehaviour
    {
        [SerializeField] private Transform m_routingAnchor;
        [SerializeField] private GameObject m_availableMarker;
        [SerializeField] private GameObject m_routedMarker;

        public Vector3 Position => m_routingAnchor != null ? m_routingAnchor.position : transform.position;
        public bool IsConfigured => m_routingAnchor != null && m_availableMarker != null && m_routedMarker != null;

        private void Awake()
        {
            SetState(false, false);
        }

        public void Configure(Transform routingAnchor, GameObject availableMarker, GameObject routedMarker)
        {
            m_routingAnchor = routingAnchor;
            m_availableMarker = availableMarker;
            m_routedMarker = routedMarker;
            SetState(false, false);
        }

        public void SetState(bool available, bool complete)
        {
            if (m_availableMarker != null)
            {
                m_availableMarker.SetActive(available && !complete);
            }

            if (m_routedMarker != null)
            {
                m_routedMarker.SetActive(complete);
            }
        }
    }
}
