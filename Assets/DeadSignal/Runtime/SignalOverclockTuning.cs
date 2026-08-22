using UnityEngine;

namespace DeadSignal
{
    [CreateAssetMenu(fileName = "SignalOverclockTuning", menuName = "DEAD SIGNAL/Signal Overclock Tuning")]
    public sealed class SignalOverclockTuning : ScriptableObject
    {
        [Header("Chain Arc")]
        [SerializeField] private float m_chainArcRadius = 4.5f;

        [Header("Overdrive Thrusters")]
        [SerializeField] private float m_thrusterSpeedMultiplier = 1.25f;
        [SerializeField] private float m_thrusterAccelerationMultiplier = 1.2f;

        [Header("Emergency Capacitor")]
        [SerializeField] private float m_emergencyCapacitorThreshold = 25f;
        [SerializeField] private float m_emergencyCapacitorRestore = 22f;

        public float ChainArcRadius => m_chainArcRadius;
        public float ThrusterSpeedMultiplier => m_thrusterSpeedMultiplier;
        public float ThrusterAccelerationMultiplier => m_thrusterAccelerationMultiplier;
        public float EmergencyCapacitorThreshold => m_emergencyCapacitorThreshold;
        public float EmergencyCapacitorRestore => m_emergencyCapacitorRestore;

        private void OnValidate()
        {
            m_chainArcRadius = Mathf.Clamp(m_chainArcRadius, 1f, 8f);
            m_thrusterSpeedMultiplier = Mathf.Clamp(m_thrusterSpeedMultiplier, 1f, 1.6f);
            m_thrusterAccelerationMultiplier = Mathf.Clamp(m_thrusterAccelerationMultiplier, 1f, 1.6f);
            m_emergencyCapacitorThreshold = Mathf.Clamp(m_emergencyCapacitorThreshold, 1f, RunModel.MaximumSignal - 1f);
            m_emergencyCapacitorRestore = Mathf.Clamp(m_emergencyCapacitorRestore, 1f, RunModel.MaximumSignal);
        }
    }
}
