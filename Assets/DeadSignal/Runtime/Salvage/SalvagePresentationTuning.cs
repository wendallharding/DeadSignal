using UnityEngine;

namespace DeadSignal.Salvage
{
    [CreateAssetMenu(fileName = "SalvagePresentationTuning", menuName = "DEAD SIGNAL/Salvage Presentation Tuning")]
    public sealed class SalvagePresentationTuning : ScriptableObject
    {
        [Header("World Presentation")]
        [SerializeField] private float m_rotationSpeed = 70f;
        [SerializeField] private float m_hoverHeight = 0.06f;
        [SerializeField] private float m_hoverAmplitude = 0.04f;
        [SerializeField] private float m_hoverFrequency = 3f;

        [Header("Collection")]
        [SerializeField] private float m_collectionRadius = 0.85f;
        [SerializeField, Min(0f)] private float m_requiredCacheSignalReward = 12f;
        [SerializeField, Min(0f)] private float m_recoveryFieldDuration = 3f;
        [SerializeField, Min(0.1f)] private float m_recoveryFieldRadius = 2.4f;

        [Header("Salvage Chain")]
        [SerializeField, Min(1f)] private float m_chainWindow = 12f;
        [SerializeField, Min(0f)] private float m_secondCacheSignalReward = 4f;
        [SerializeField, Min(0f)] private float m_thirdCacheSignalReward = 8f;

        [Header("Optional Cache")]
        [SerializeField, Min(0f)] private float m_optionalCacheSignalReward = 18f;

        public float RotationSpeed => m_rotationSpeed;
        public float HoverHeight => m_hoverHeight;
        public float HoverAmplitude => m_hoverAmplitude;
        public float HoverFrequency => m_hoverFrequency;
        public float CollectionRadius => m_collectionRadius;
        public float RequiredCacheSignalReward => m_requiredCacheSignalReward;
        public float RecoveryFieldDuration => m_recoveryFieldDuration;
        public float RecoveryFieldRadius => m_recoveryFieldRadius;
        public float ChainWindow => m_chainWindow;
        public float SecondCacheSignalReward => m_secondCacheSignalReward;
        public float ThirdCacheSignalReward => m_thirdCacheSignalReward;
        public float OptionalCacheSignalReward => m_optionalCacheSignalReward;

        private void OnValidate()
        {
            m_rotationSpeed = Mathf.Max(0f, m_rotationSpeed);
            m_hoverHeight = Mathf.Max(0f, m_hoverHeight);
            m_hoverAmplitude = Mathf.Clamp(m_hoverAmplitude, 0f, m_hoverHeight);
            m_hoverFrequency = Mathf.Max(0f, m_hoverFrequency);
            m_collectionRadius = Mathf.Max(0.1f, m_collectionRadius);
            m_requiredCacheSignalReward = Mathf.Max(0f, m_requiredCacheSignalReward);
            m_recoveryFieldDuration = Mathf.Max(0f, m_recoveryFieldDuration);
            m_recoveryFieldRadius = Mathf.Max(0.1f, m_recoveryFieldRadius);
            m_chainWindow = Mathf.Max(1f, m_chainWindow);
            m_secondCacheSignalReward = Mathf.Max(0f, m_secondCacheSignalReward);
            m_thirdCacheSignalReward = Mathf.Max(m_secondCacheSignalReward, m_thirdCacheSignalReward);
            m_optionalCacheSignalReward = Mathf.Max(0f, m_optionalCacheSignalReward);
        }
    }
}
