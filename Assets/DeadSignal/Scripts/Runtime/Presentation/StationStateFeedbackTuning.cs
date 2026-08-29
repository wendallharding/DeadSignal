using UnityEngine;

namespace DeadSignal.Presentation
{
    [CreateAssetMenu(fileName = "StationStateFeedbackTuning", menuName = "Dead Signal/Tuning/Station State Feedback")]
    public sealed class StationStateFeedbackTuning : ScriptableObject
    {
        [SerializeField, Range(2, 8)] private int m_poolSize = 4;
        [SerializeField, Min(0.1f)] private float m_duration = 0.82f;
        [SerializeField, Range(0f, 1f)] private float m_maximumAlpha = 0.62f;
        [SerializeField, Range(0f, 1f)] private float m_reducedFlashesMaximumAlpha = 0.28f;
        [SerializeField, Min(0.1f)] private float m_startingDiameterMultiplier = 0.58f;
        [SerializeField, Min(0.1f)] private float m_endingDiameterMultiplier = 1.15f;
        [SerializeField] private Color m_availableColor = new(1f, 0.58f, 0.08f, 1f);
        [SerializeField] private Color m_completeColor = new(0.1f, 0.88f, 1f, 1f);

        public int PoolSize => m_poolSize;
        public float Duration => m_duration;
        public float MaximumAlpha => m_maximumAlpha;
        public float ReducedFlashesMaximumAlpha => m_reducedFlashesMaximumAlpha;
        public float StartingDiameterMultiplier => m_startingDiameterMultiplier;
        public float EndingDiameterMultiplier => m_endingDiameterMultiplier;
        public Color AvailableColor => m_availableColor;
        public Color CompleteColor => m_completeColor;

        private void OnValidate()
        {
            m_poolSize = Mathf.Clamp(m_poolSize, 2, 8);
            m_duration = Mathf.Max(0.1f, m_duration);
            m_maximumAlpha = Mathf.Clamp01(m_maximumAlpha);
            m_reducedFlashesMaximumAlpha = Mathf.Clamp(
                m_reducedFlashesMaximumAlpha, 0f, m_maximumAlpha);
            m_startingDiameterMultiplier = Mathf.Max(0.1f, m_startingDiameterMultiplier);
            m_endingDiameterMultiplier = Mathf.Max(m_startingDiameterMultiplier, m_endingDiameterMultiplier);
        }
    }
}
