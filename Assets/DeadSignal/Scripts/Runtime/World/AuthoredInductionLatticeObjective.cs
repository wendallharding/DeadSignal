using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Scene-authored interaction anchor and persistent charge-state markers for the core lattice.
    /// </summary>
    public sealed class AuthoredInductionLatticeObjective : MonoBehaviour
    {
        [SerializeField] private Transform m_chargeAnchor;
        [SerializeField] private GameObject m_availableMarker;
        [SerializeField] private GameObject m_chargedMarker;

        public Vector3 Position => m_chargeAnchor != null ? m_chargeAnchor.position : transform.position;
        public bool IsConfigured => m_chargeAnchor != null && m_availableMarker != null && m_chargedMarker != null;

        private void Awake()
        {
            SetState(false, false);
        }

        public void Configure(Transform chargeAnchor, GameObject availableMarker, GameObject chargedMarker)
        {
            m_chargeAnchor = chargeAnchor;
            m_availableMarker = availableMarker;
            m_chargedMarker = chargedMarker;
            SetState(false, false);
        }

        public void SetState(bool available, bool charged)
        {
            if (m_availableMarker != null)
            {
                m_availableMarker.SetActive(available && !charged);
            }

            if (m_chargedMarker != null)
            {
                m_chargedMarker.SetActive(charged);
            }
        }
    }
}
