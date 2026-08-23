using UnityEngine;

namespace DeadSignal.Combat
{
    [CreateAssetMenu(fileName = "WardenThreatTelegraphTuning", menuName = "DEAD SIGNAL/Warden Threat Telegraph Tuning")]
    public sealed class WardenThreatTelegraphTuning : ScriptableObject
    {
        [Header("Proximity Warning")]
        [SerializeField] private float m_warningDistance = 2.6f;
        [SerializeField] private float m_ringDiameter = 2.1f;
        [SerializeField] private float m_minimumAlpha = 0.16f;
        [SerializeField] private float m_maximumAlpha = 0.72f;
        [SerializeField] private float m_rotationSpeed = 24f;
        [SerializeField] private float m_pulseSpeed = 7f;
        [SerializeField] private float m_pulseScale = 0.045f;

        public float WarningDistance => m_warningDistance;
        public float RingDiameter => m_ringDiameter;
        public float MinimumAlpha => m_minimumAlpha;
        public float MaximumAlpha => m_maximumAlpha;
        public float RotationSpeed => m_rotationSpeed;
        public float PulseSpeed => m_pulseSpeed;
        public float PulseScale => m_pulseScale;

        private void OnValidate()
        {
            m_warningDistance = Mathf.Max(0.1f, m_warningDistance);
            m_ringDiameter = Mathf.Max(0.1f, m_ringDiameter);
            m_minimumAlpha = Mathf.Clamp01(m_minimumAlpha);
            m_maximumAlpha = Mathf.Clamp(m_maximumAlpha, m_minimumAlpha, 1f);
            m_rotationSpeed = Mathf.Max(0f, m_rotationSpeed);
            m_pulseSpeed = Mathf.Max(0f, m_pulseSpeed);
            m_pulseScale = Mathf.Clamp(m_pulseScale, 0f, 0.25f);
        }
    }
}
