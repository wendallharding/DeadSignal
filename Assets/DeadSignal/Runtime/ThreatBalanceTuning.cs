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

        [Header("Security Interceptor")]
        [SerializeField] private int m_interceptorHealth = 3;
        [SerializeField] private float m_interceptorApproachSpeed = 3.1f;
        [SerializeField] private float m_interceptorCutoffFraction = 0.48f;
        [SerializeField] private float m_interceptorChargeDistance = 1.35f;
        [SerializeField] private float m_interceptorChargeDuration = 0.8f;
        [SerializeField] private float m_interceptorDashSpeed = 8.5f;
        [SerializeField] private float m_interceptorDashDuration = 0.65f;
        [SerializeField] private float m_interceptorHitDistance = 0.9f;
        [SerializeField] private float m_interceptorHitCooldown = 1.2f;
        [SerializeField] private float m_interceptorSignalReward = 14f;

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
        public int InterceptorHealth => m_interceptorHealth;
        public float InterceptorApproachSpeed => m_interceptorApproachSpeed;
        public float InterceptorCutoffFraction => m_interceptorCutoffFraction;
        public float InterceptorChargeDistance => m_interceptorChargeDistance;
        public float InterceptorChargeDuration => m_interceptorChargeDuration;
        public float InterceptorDashSpeed => m_interceptorDashSpeed;
        public float InterceptorDashDuration => m_interceptorDashDuration;
        public float InterceptorHitDistance => m_interceptorHitDistance;
        public float InterceptorHitCooldown => m_interceptorHitCooldown;
        public float InterceptorSignalReward => m_interceptorSignalReward;
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
            m_interceptorHealth = Mathf.Max(1, m_interceptorHealth);
            m_interceptorApproachSpeed = Mathf.Max(0.1f, m_interceptorApproachSpeed);
            m_interceptorCutoffFraction = Mathf.Clamp01(m_interceptorCutoffFraction);
            m_interceptorChargeDistance = Mathf.Max(0.1f, m_interceptorChargeDistance);
            m_interceptorChargeDuration = Mathf.Max(0.1f, m_interceptorChargeDuration);
            m_interceptorDashSpeed = Mathf.Max(0.1f, m_interceptorDashSpeed);
            m_interceptorDashDuration = Mathf.Max(0.1f, m_interceptorDashDuration);
            m_interceptorHitDistance = Mathf.Max(0.1f, m_interceptorHitDistance);
            m_interceptorHitCooldown = Mathf.Max(0.1f, m_interceptorHitCooldown);
            m_interceptorSignalReward = Mathf.Max(0f, m_interceptorSignalReward);
            m_reinforcementEntryDelay = Mathf.Max(0f, m_reinforcementEntryDelay);
            m_reinforcementSafeDistance = Mathf.Max(0f, m_reinforcementSafeDistance);
        }
    }
}
