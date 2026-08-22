using UnityEngine;

namespace DeadSignal
{
    [CreateAssetMenu(fileName = "ThreatBalanceTuning", menuName = "DEAD SIGNAL/Threat Balance Tuning")]
    public sealed class ThreatBalanceTuning : ScriptableObject
    {
        [Header("Security Warden")]
        [SerializeField] private int m_wardenHealth = 3;
        [SerializeField] private float m_wardenSpeed = 2.15f;
        [SerializeField] private float m_wardenAttackDistance = 1.05f;
        [SerializeField] private float m_wardenAttackCooldown = 0.9f;
        [SerializeField] private float m_wardenSignalReward = 12f;

        [Header("Signal Sapper")]
        [SerializeField] private int m_sapperHealth = 2;
        [SerializeField] private float m_sapperSpeed = 1.8f;
        [SerializeField] private float m_sapperLatchDistance = 1.25f;
        [SerializeField] private float m_sapperFirstPulseDelay = 1.6f;
        [SerializeField] private float m_sapperPulseInterval = 1.35f;
        [SerializeField] private float m_sapperSignalReward = 16f;

        [Header("Security Escalation")]
        [SerializeField] private float m_reinforcementEntryDelay = 2.5f;
        [SerializeField] private float m_reinforcementSafeDistance = 6f;

        public int WardenHealth => m_wardenHealth;
        public float WardenSpeed => m_wardenSpeed;
        public float WardenAttackDistance => m_wardenAttackDistance;
        public float WardenAttackCooldown => m_wardenAttackCooldown;
        public float WardenSignalReward => m_wardenSignalReward;
        public int SapperHealth => m_sapperHealth;
        public float SapperSpeed => m_sapperSpeed;
        public float SapperLatchDistance => m_sapperLatchDistance;
        public float SapperFirstPulseDelay => m_sapperFirstPulseDelay;
        public float SapperPulseInterval => m_sapperPulseInterval;
        public float SapperSignalReward => m_sapperSignalReward;
        public float ReinforcementEntryDelay => m_reinforcementEntryDelay;
        public float ReinforcementSafeDistance => m_reinforcementSafeDistance;

        private void OnValidate()
        {
            m_wardenHealth = Mathf.Max(1, m_wardenHealth);
            m_wardenSpeed = Mathf.Max(0.1f, m_wardenSpeed);
            m_wardenAttackDistance = Mathf.Max(0.1f, m_wardenAttackDistance);
            m_wardenAttackCooldown = Mathf.Max(0.1f, m_wardenAttackCooldown);
            m_wardenSignalReward = Mathf.Max(0f, m_wardenSignalReward);
            m_sapperHealth = Mathf.Max(1, m_sapperHealth);
            m_sapperSpeed = Mathf.Max(0.1f, m_sapperSpeed);
            m_sapperLatchDistance = Mathf.Max(0.1f, m_sapperLatchDistance);
            m_sapperFirstPulseDelay = Mathf.Max(0.1f, m_sapperFirstPulseDelay);
            m_sapperPulseInterval = Mathf.Max(0.1f, m_sapperPulseInterval);
            m_sapperSignalReward = Mathf.Max(0f, m_sapperSignalReward);
            m_reinforcementEntryDelay = Mathf.Max(0f, m_reinforcementEntryDelay);
            m_reinforcementSafeDistance = Mathf.Max(0f, m_reinforcementSafeDistance);
        }
    }
}
