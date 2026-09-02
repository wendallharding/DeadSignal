using UnityEngine;

namespace DeadSignal.Combat
{
    [CreateAssetMenu(fileName = "CombatFeedbackTuning", menuName = "Dead Signal/Tuning/Combat Feedback")]
    public sealed class CombatFeedbackTuning : ScriptableObject
    {
        [Header("Impact Pool")]
        [SerializeField, Min(1)] private int m_impactPrewarmCount = 12;
        [SerializeField, Min(1)] private int m_impactMaximumCount = 16;
        [SerializeField, Min(0.05f)] private float m_impactDuration = 0.22f;
        [SerializeField, Range(0.02f, 0.08f)] private float m_impactGlyphWidth = 0.045f;

        [Header("Spark Pool")]
        [SerializeField, Min(1)] private int m_sparkPrewarmCount = 12;
        [SerializeField, Min(1)] private int m_sparkMaximumCount = 16;
        [SerializeField, Min(0.05f)] private float m_sparkDuration = 0.22f;

        [Header("Chain Pool")]
        [SerializeField, Min(1)] private int m_chainPrewarmCount = 6;
        [SerializeField, Min(1)] private int m_chainMaximumCount = 8;
        [SerializeField, Min(0.05f)] private float m_chainDuration = 0.18f;

        [Header("Comfort")]
        [SerializeField, Range(0.05f, 0.3f)] private float m_reducedFlashesMaximumAlpha = 0.3f;

        public int ImpactPrewarmCount => m_impactPrewarmCount;
        public int ImpactMaximumCount => m_impactMaximumCount;
        public float ImpactDuration => m_impactDuration;
        public float ImpactGlyphWidth => m_impactGlyphWidth;
        public int SparkPrewarmCount => m_sparkPrewarmCount;
        public int SparkMaximumCount => m_sparkMaximumCount;
        public float SparkDuration => m_sparkDuration;
        public int ChainPrewarmCount => m_chainPrewarmCount;
        public int ChainMaximumCount => m_chainMaximumCount;
        public float ChainDuration => m_chainDuration;
        public float ReducedFlashesMaximumAlpha => m_reducedFlashesMaximumAlpha;

        private void OnValidate()
        {
            m_impactPrewarmCount = Mathf.Max(1, m_impactPrewarmCount);
            m_impactMaximumCount = Mathf.Max(m_impactPrewarmCount, m_impactMaximumCount);
            m_impactDuration = Mathf.Max(0.05f, m_impactDuration);
            m_impactGlyphWidth = Mathf.Clamp(m_impactGlyphWidth, 0.02f, 0.08f);
            m_sparkPrewarmCount = Mathf.Max(1, m_sparkPrewarmCount);
            m_sparkMaximumCount = Mathf.Max(m_sparkPrewarmCount, m_sparkMaximumCount);
            m_sparkDuration = Mathf.Max(0.05f, m_sparkDuration);
            m_chainPrewarmCount = Mathf.Max(1, m_chainPrewarmCount);
            m_chainMaximumCount = Mathf.Max(m_chainPrewarmCount, m_chainMaximumCount);
            m_chainDuration = Mathf.Max(0.05f, m_chainDuration);
            m_reducedFlashesMaximumAlpha = Mathf.Clamp(m_reducedFlashesMaximumAlpha, 0.05f, 0.3f);
        }
    }
}
