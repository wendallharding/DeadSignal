using UnityEngine;

namespace DeadSignal.World
{
    public sealed class AuthoredQuenchStabilizationObjective : MonoBehaviour
    {
        [SerializeField] private Transform m_stabilizationAnchor;
        [SerializeField] private GameObject m_availableMarker;
        [SerializeField] private GameObject m_completeMarker;

        public bool IsConfigured => m_stabilizationAnchor != null && m_availableMarker != null && m_completeMarker != null;
        public Vector3 Position => m_stabilizationAnchor != null ? m_stabilizationAnchor.position : transform.position;

        public void Configure(Transform stabilizationAnchor, GameObject availableMarker, GameObject completeMarker)
        {
            m_stabilizationAnchor = stabilizationAnchor;
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
