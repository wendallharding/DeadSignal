using UnityEngine;

namespace DeadSignal.Missions
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

        [Header("Pair Synergies")]
        [SerializeField] private float m_overdriveSynergySurgeDuration = 2f;
        [SerializeField] private float m_overdriveSynergySpeedMultiplier = 1.2f;

        [Header("Relay Weapon Calibration")]
        [SerializeField] private int m_piercingPulseThreatHits = 2;
        [SerializeField] private float m_controlledRicochetTargetRadius = 7f;

        [Header("Capacitor Spine Weapon Evolution")]
        [SerializeField] private int m_evolvedPiercingPulseThreatHits = 3;
        [SerializeField] private int m_evolvedControlledRicochetBanks = 2;

        public float ChainArcRadius => m_chainArcRadius;
        public float ThrusterSpeedMultiplier => m_thrusterSpeedMultiplier;
        public float ThrusterAccelerationMultiplier => m_thrusterAccelerationMultiplier;
        public float EmergencyCapacitorThreshold => m_emergencyCapacitorThreshold;
        public float EmergencyCapacitorRestore => m_emergencyCapacitorRestore;
        public float OverdriveSynergySurgeDuration => m_overdriveSynergySurgeDuration;
        public float OverdriveSynergySpeedMultiplier => m_overdriveSynergySpeedMultiplier;
        public int PiercingPulseThreatHits => m_piercingPulseThreatHits;
        public float ControlledRicochetTargetRadius => m_controlledRicochetTargetRadius;
        public int EvolvedPiercingPulseThreatHits => m_evolvedPiercingPulseThreatHits;
        public int EvolvedControlledRicochetBanks => m_evolvedControlledRicochetBanks;

        private void OnValidate()
        {
            m_chainArcRadius = Mathf.Clamp(m_chainArcRadius, 1f, 8f);
            m_thrusterSpeedMultiplier = Mathf.Clamp(m_thrusterSpeedMultiplier, 1f, 1.6f);
            m_thrusterAccelerationMultiplier = Mathf.Clamp(m_thrusterAccelerationMultiplier, 1f, 1.6f);
            m_emergencyCapacitorThreshold = Mathf.Clamp(m_emergencyCapacitorThreshold, 1f, RunModel.MaximumSignal - 1f);
            m_emergencyCapacitorRestore = Mathf.Clamp(m_emergencyCapacitorRestore, 1f, RunModel.MaximumSignal);
            m_overdriveSynergySurgeDuration = Mathf.Clamp(m_overdriveSynergySurgeDuration, 0.5f, 5f);
            m_overdriveSynergySpeedMultiplier = Mathf.Clamp(m_overdriveSynergySpeedMultiplier, 1.05f, 1.4f);
            m_piercingPulseThreatHits = Mathf.Clamp(m_piercingPulseThreatHits, 2, 3);
            m_controlledRicochetTargetRadius = Mathf.Clamp(m_controlledRicochetTargetRadius, 2f, 10f);
            m_evolvedPiercingPulseThreatHits = Mathf.Clamp(m_evolvedPiercingPulseThreatHits, 3, 4);
            m_evolvedControlledRicochetBanks = Mathf.Clamp(m_evolvedControlledRicochetBanks, 2, 3);
        }
    }
}
