using UnityEngine;

namespace DeadSignal.World
{
    public sealed class AuthoredConvergenceCalibrationObjective : MonoBehaviour
    {
        [SerializeField] private Transform m_interactionAnchor;
        [SerializeField] private Transform m_calibrationVolume;
        [SerializeField] private Transform m_pressureAnchor;
        [SerializeField] private GameObject m_availableState;
        [SerializeField] private GameObject m_activeState;
        [SerializeField] private GameObject m_completeState;
        [SerializeField] private Vector2 m_calibrationHalfExtents = new(5.6f, 2.9f);

        public Vector3 Position => m_interactionAnchor != null ? m_interactionAnchor.position : transform.position;
        public Transform PressureAnchor => m_pressureAnchor;
        public bool IsConfigured => m_interactionAnchor != null && m_calibrationVolume != null &&
                                    m_pressureAnchor != null && m_availableState != null && m_activeState != null &&
                                    m_completeState != null;

        public void Configure(
            Transform interactionAnchor,
            Transform calibrationVolume,
            Transform pressureAnchor,
            GameObject availableState,
            GameObject activeState,
            GameObject completeState,
            Vector2 calibrationHalfExtents)
        {
            m_interactionAnchor = interactionAnchor;
            m_calibrationVolume = calibrationVolume;
            m_pressureAnchor = pressureAnchor;
            m_availableState = availableState;
            m_activeState = activeState;
            m_completeState = completeState;
            m_calibrationHalfExtents = new Vector2(
                Mathf.Max(0.5f, calibrationHalfExtents.x),
                Mathf.Max(0.5f, calibrationHalfExtents.y));
            SetState(false, false, false);
        }

        public bool Contains(Vector3 worldPosition)
        {
            if (m_calibrationVolume == null)
            {
                return false;
            }

            var localPosition = m_calibrationVolume.InverseTransformPoint(worldPosition);
            return Mathf.Abs(localPosition.x) <= m_calibrationHalfExtents.x &&
                   Mathf.Abs(localPosition.z) <= m_calibrationHalfExtents.y;
        }

        public void SetState(bool available, bool active, bool complete)
        {
            if (m_availableState != null) m_availableState.SetActive(available && !active && !complete);
            if (m_activeState != null) m_activeState.SetActive(active && !complete);
            if (m_completeState != null) m_completeState.SetActive(complete);
        }
    }
}
