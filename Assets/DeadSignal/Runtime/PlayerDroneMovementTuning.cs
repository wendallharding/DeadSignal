using UnityEngine;

namespace DeadSignal
{
    [CreateAssetMenu(fileName = "PlayerDroneMovementTuning", menuName = "DEAD SIGNAL/Player Drone Movement Tuning")]
    public sealed class PlayerDroneMovementTuning : ScriptableObject
    {
        [Header("Flight Response")]
        [SerializeField] private float m_maximumSpeed = 6.4f;
        [SerializeField] private float m_acceleration = 32f;
        [SerializeField] private float m_braking = 21.333334f;
        [SerializeField] private float m_reversalAccelerationMultiplier = 0.7f;

        [Header("Presentation")]
        [SerializeField] private float m_maximumBankDegrees = 10f;
        [SerializeField] private float m_bankSharpness = 10f;
        [SerializeField] private float m_bodyTurnSharpness = 12f;
        [SerializeField] private float m_turretTurnSharpness = 30f;
        [SerializeField] private float m_turretMountHeight = 0.14f;
        [SerializeField] private float m_hoverAmplitude = 0.025f;
        [SerializeField] private float m_hoverFrequency = 1.4f;

        [Header("Signal Wake")]
        [SerializeField] private float m_wakeMinimumSpeed = 0.8f;
        [SerializeField] private float m_wakeDuration = 0.22f;
        [SerializeField] private float m_wakeEmitterSpacing = 0.22f;
        [SerializeField] private float m_wakeMinimumWidth = 0.06f;
        [SerializeField] private float m_wakeMaximumWidth = 0.18f;

        public float MaximumSpeed => m_maximumSpeed;
        public float Acceleration => m_acceleration;
        public float Braking => m_braking;
        public float ReversalAccelerationMultiplier => m_reversalAccelerationMultiplier;
        public float MaximumBankDegrees => m_maximumBankDegrees;
        public float BankSharpness => m_bankSharpness;
        public float BodyTurnSharpness => m_bodyTurnSharpness;
        public float TurretTurnSharpness => m_turretTurnSharpness;
        public float TurretMountHeight => m_turretMountHeight;
        public float HoverAmplitude => m_hoverAmplitude;
        public float HoverFrequency => m_hoverFrequency;
        public float WakeMinimumSpeed => m_wakeMinimumSpeed;
        public float WakeDuration => m_wakeDuration;
        public float WakeEmitterSpacing => m_wakeEmitterSpacing;
        public float WakeMinimumWidth => m_wakeMinimumWidth;
        public float WakeMaximumWidth => m_wakeMaximumWidth;

        private void OnValidate()
        {
            m_maximumSpeed = Mathf.Max(0.1f, m_maximumSpeed);
            m_acceleration = Mathf.Max(0.1f, m_acceleration);
            m_braking = Mathf.Max(0.1f, m_braking);
            m_reversalAccelerationMultiplier = Mathf.Clamp(m_reversalAccelerationMultiplier, 0.1f, 1f);
            m_maximumBankDegrees = Mathf.Clamp(m_maximumBankDegrees, 0f, 30f);
            m_bankSharpness = Mathf.Max(0.1f, m_bankSharpness);
            m_bodyTurnSharpness = Mathf.Max(0.1f, m_bodyTurnSharpness);
            m_turretTurnSharpness = Mathf.Max(0.1f, m_turretTurnSharpness);
            m_turretMountHeight = Mathf.Clamp(m_turretMountHeight, 0f, 0.4f);
            m_hoverAmplitude = Mathf.Clamp(m_hoverAmplitude, 0f, 0.15f);
            m_hoverFrequency = Mathf.Max(0f, m_hoverFrequency);
            m_wakeMinimumSpeed = Mathf.Clamp(m_wakeMinimumSpeed, 0f, m_maximumSpeed);
            m_wakeDuration = Mathf.Clamp(m_wakeDuration, 0.05f, 1f);
            m_wakeEmitterSpacing = Mathf.Clamp(m_wakeEmitterSpacing, 0.05f, 0.5f);
            m_wakeMinimumWidth = Mathf.Clamp(m_wakeMinimumWidth, 0.01f, 0.5f);
            m_wakeMaximumWidth = Mathf.Clamp(m_wakeMaximumWidth, m_wakeMinimumWidth, 0.75f);
        }
    }
}
