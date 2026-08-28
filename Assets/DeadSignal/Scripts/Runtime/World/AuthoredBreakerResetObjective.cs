using UnityEngine;

namespace DeadSignal.World
{
    public sealed class AuthoredBreakerResetObjective : MonoBehaviour
    {
        [SerializeField] private Transform m_resetAnchor;
        [SerializeField] private GameObject m_availableMarker;
        [SerializeField] private GameObject m_completeMarker;

        public bool IsConfigured => m_resetAnchor != null && m_availableMarker != null && m_completeMarker != null;
        public Vector3 Position => m_resetAnchor != null ? m_resetAnchor.position : transform.position;

        public void Configure(Transform resetAnchor, GameObject availableMarker, GameObject completeMarker)
        {
            m_resetAnchor = resetAnchor;
            m_availableMarker = availableMarker;
            m_completeMarker = completeMarker;
            SetState(false, false);
        }

        public void SetState(bool available, bool complete)
        {
            if (m_availableMarker != null)
            {
                m_availableMarker.SetActive(available && !complete);
            }

            if (m_completeMarker != null)
            {
                m_completeMarker.SetActive(complete);
            }
        }
    }
}
