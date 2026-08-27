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

        public Vector3 Position => m_assemblyAnchor != null ? m_assemblyAnchor.position : transform.position;
        public bool IsConfigured => m_assemblyAnchor != null && m_availableMarker != null && m_assembledMarker != null;

        private void Awake()
        {
            SetState(false, false);
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
    }
}
