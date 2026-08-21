using UnityEngine;

namespace DeadSignal
{
    [CreateAssetMenu(fileName = "SignalSapperTelegraphTuning", menuName = "DEAD SIGNAL/Signal Sapper Telegraph Tuning")]
    public sealed class SignalSapperTelegraphTuning : ScriptableObject
    {
        [Header("Targeting")]
        [SerializeField] private float m_approachRadius = 1.65f;
        [SerializeField] private float m_countdownStartRadius = 2.25f;
        [SerializeField] private float m_countdownEndRadius = 0.82f;
        [SerializeField] private float m_approachRotationSpeed = 58f;
        [SerializeField] private float m_latchedRotationSpeed = 34f;

        [Header("Target Tether")]
        [SerializeField] private float m_tetherScrollSpeed = 1.4f;
        [SerializeField] private float m_tetherRepeatWorldLength = 0.9f;

        [Header("Drain Pulse")]
        [SerializeField] private float m_flashDuration = 0.42f;
        [SerializeField] private float m_flashStartingDiameter = 0.8f;
        [SerializeField] private float m_flashEndingDiameter = 5.2f;
        [SerializeField] private float m_flashMaximumAlpha = 0.82f;

        public float ApproachRadius => m_approachRadius;
        public float CountdownStartRadius => m_countdownStartRadius;
        public float CountdownEndRadius => m_countdownEndRadius;
        public float ApproachRotationSpeed => m_approachRotationSpeed;
        public float LatchedRotationSpeed => m_latchedRotationSpeed;
        public float TetherScrollSpeed => m_tetherScrollSpeed;
        public float TetherRepeatWorldLength => m_tetherRepeatWorldLength;
        public float FlashDuration => m_flashDuration;
        public float FlashStartingDiameter => m_flashStartingDiameter;
        public float FlashEndingDiameter => m_flashEndingDiameter;
        public float FlashMaximumAlpha => m_flashMaximumAlpha;

        private void OnValidate()
        {
            m_approachRadius = Mathf.Max(0.1f, m_approachRadius);
            m_countdownStartRadius = Mathf.Max(0.1f, m_countdownStartRadius);
            m_countdownEndRadius = Mathf.Clamp(m_countdownEndRadius, 0.1f, m_countdownStartRadius);
            m_approachRotationSpeed = Mathf.Max(0f, m_approachRotationSpeed);
            m_latchedRotationSpeed = Mathf.Max(0f, m_latchedRotationSpeed);
            m_tetherScrollSpeed = Mathf.Max(0f, m_tetherScrollSpeed);
            m_tetherRepeatWorldLength = Mathf.Max(0.1f, m_tetherRepeatWorldLength);
            m_flashDuration = Mathf.Max(0.05f, m_flashDuration);
            m_flashStartingDiameter = Mathf.Max(0.1f, m_flashStartingDiameter);
            m_flashEndingDiameter = Mathf.Max(m_flashStartingDiameter, m_flashEndingDiameter);
            m_flashMaximumAlpha = Mathf.Clamp01(m_flashMaximumAlpha);
        }
    }
}
