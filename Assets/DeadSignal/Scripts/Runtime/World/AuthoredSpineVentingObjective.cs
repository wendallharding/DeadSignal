using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Scene-authored discharge control and persistent safe-pressure state for the Spine berth.
    /// </summary>
    public sealed class AuthoredSpineVentingObjective : MonoBehaviour
    {
        [SerializeField] private Transform m_controlAnchor;
        [SerializeField] private GameObject m_ventAvailableMarker;
        [SerializeField] private GameObject m_ventedMarker;

        public Vector3 Position => m_controlAnchor != null ? m_controlAnchor.position : transform.position;
        public bool IsConfigured => m_controlAnchor != null && m_ventAvailableMarker != null && m_ventedMarker != null;

        private void Awake()
        {
            SetState(false, false);
        }

        public void Configure(Transform controlAnchor, GameObject ventAvailableMarker, GameObject ventedMarker)
        {
            m_controlAnchor = controlAnchor;
            m_ventAvailableMarker = ventAvailableMarker;
            m_ventedMarker = ventedMarker;
            SetState(false, false);
        }

        public void SetState(bool ventAvailable, bool vented)
        {
            if (m_ventAvailableMarker != null)
            {
                m_ventAvailableMarker.SetActive(ventAvailable && !vented);
            }

            if (m_ventedMarker != null)
            {
                m_ventedMarker.SetActive(vented);
            }
        }
    }
}
