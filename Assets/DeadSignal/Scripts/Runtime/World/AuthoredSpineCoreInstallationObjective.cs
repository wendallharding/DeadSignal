using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Scene-authored final core installation anchor and persistent completion state at the Spine Tower.
    /// </summary>
    public sealed class AuthoredSpineCoreInstallationObjective : MonoBehaviour
    {
        [SerializeField] private Transform m_installAnchor;
        [SerializeField] private GameObject m_availableMarker;
        [SerializeField] private GameObject m_installedMarker;

        public Vector3 Position => m_installAnchor != null ? m_installAnchor.position : transform.position;
        public bool IsConfigured => m_installAnchor != null && m_availableMarker != null && m_installedMarker != null;

        private void Awake()
        {
            SetState(false, false);
        }

        public void Configure(Transform installAnchor, GameObject availableMarker, GameObject installedMarker)
        {
            m_installAnchor = installAnchor;
            m_availableMarker = availableMarker;
            m_installedMarker = installedMarker;
            SetState(false, false);
        }

        public void SetState(bool available, bool installed)
        {
            if (m_availableMarker != null)
            {
                m_availableMarker.SetActive(available && !installed);
            }

            if (m_installedMarker != null)
            {
                m_installedMarker.SetActive(installed);
            }
        }
    }
}
