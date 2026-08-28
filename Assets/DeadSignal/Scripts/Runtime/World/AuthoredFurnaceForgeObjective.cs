using UnityEngine;

namespace DeadSignal.World
{
    public sealed class AuthoredFurnaceForgeObjective : MonoBehaviour
    {
        [SerializeField] private Transform m_forgeAnchor;
        [SerializeField] private GameObject m_availableMarker;
        [SerializeField] private GameObject m_completeMarker;

        public bool IsConfigured => m_forgeAnchor != null && m_availableMarker != null && m_completeMarker != null;
        public Vector3 Position => m_forgeAnchor != null ? m_forgeAnchor.position : transform.position;

        public void Configure(Transform forgeAnchor, GameObject availableMarker, GameObject completeMarker)
        {
            m_forgeAnchor = forgeAnchor;
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
