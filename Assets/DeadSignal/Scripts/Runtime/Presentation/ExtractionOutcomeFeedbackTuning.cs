using UnityEngine;

namespace DeadSignal.Presentation
{
    [CreateAssetMenu(fileName = "ExtractionOutcomeFeedbackTuning",
        menuName = "Dead Signal/Tuning/Extraction Outcome Feedback")]
    public sealed class ExtractionOutcomeFeedbackTuning : ScriptableObject
    {
        [SerializeField, Range(2, 4)] private int m_eventPoolSize = 2;
        [SerializeField, Min(0.1f)] private float m_eventDuration = 0.92f;
        [SerializeField, Range(0f, 1f)] private float m_eventMaximumAlpha = 0.58f;
        [SerializeField, Range(0f, 1f)] private float m_reducedFlashesEventMaximumAlpha = 0.26f;
        [SerializeField, Range(0f, 1f)] private float m_progressMaximumAlpha = 0.22f;
        [SerializeField, Range(0f, 1f)] private float m_reducedFlashesProgressMaximumAlpha = 0.14f;
        [SerializeField, Min(0.1f)] private float m_progressDiameter = 3.2f;
        [SerializeField, Min(0.1f)] private float m_completionDiameter = 4.2f;
        [SerializeField, Min(0.1f)] private float m_victoryDiameter = 5f;
        [SerializeField, Min(0.1f)] private float m_defeatDiameter = 3.4f;

        public int EventPoolSize => m_eventPoolSize;
        public float EventDuration => m_eventDuration;
        public float EventMaximumAlpha => m_eventMaximumAlpha;
        public float ReducedFlashesEventMaximumAlpha => m_reducedFlashesEventMaximumAlpha;
        public float ProgressMaximumAlpha => m_progressMaximumAlpha;
        public float ReducedFlashesProgressMaximumAlpha => m_reducedFlashesProgressMaximumAlpha;
        public float ProgressDiameter => m_progressDiameter;
        public float CompletionDiameter => m_completionDiameter;
        public float VictoryDiameter => m_victoryDiameter;
        public float DefeatDiameter => m_defeatDiameter;

        private void OnValidate()
        {
            m_eventPoolSize = Mathf.Clamp(m_eventPoolSize, 2, 4);
            m_eventDuration = Mathf.Max(0.1f, m_eventDuration);
            m_eventMaximumAlpha = Mathf.Clamp01(m_eventMaximumAlpha);
            m_reducedFlashesEventMaximumAlpha = Mathf.Clamp(
                m_reducedFlashesEventMaximumAlpha, 0f, m_eventMaximumAlpha);
            m_progressMaximumAlpha = Mathf.Clamp01(m_progressMaximumAlpha);
            m_reducedFlashesProgressMaximumAlpha = Mathf.Clamp(
                m_reducedFlashesProgressMaximumAlpha, 0f, m_progressMaximumAlpha);
            m_progressDiameter = Mathf.Max(0.1f, m_progressDiameter);
            m_completionDiameter = Mathf.Max(m_progressDiameter, m_completionDiameter);
            m_victoryDiameter = Mathf.Max(m_completionDiameter, m_victoryDiameter);
            m_defeatDiameter = Mathf.Max(0.1f, m_defeatDiameter);
        }
    }
}
