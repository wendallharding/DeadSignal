using UnityEngine;

namespace DeadSignal.Presentation
{
    public enum SignalReserveState
    {
        Stable,
        Strained,
        Critical
    }

    [CreateAssetMenu(fileName = "SignalHudTuning", menuName = "Dead Signal/Tuning/Signal HUD")]
    public sealed class SignalHudTuning : ScriptableObject
    {
        [SerializeField, Range(0f, 1f)] private float m_strainedThreshold = 0.6f;
        [SerializeField, Range(0f, 1f)] private float m_criticalThreshold = 0.25f;
        [SerializeField, Min(0f)] private float m_criticalPulseSpeed = 3.2f;
        [SerializeField, Range(0f, 1f)] private float m_criticalMinimumAlpha = 0.58f;
        [SerializeField] private Color m_stableColor = new(0.02f, 0.9f, 1f, 1f);
        [SerializeField] private Color m_strainedColor = new(1f, 0.58f, 0.08f, 1f);
        [SerializeField] private Color m_criticalColor = new(1f, 0.06f, 0.05f, 1f);

        public float StrainedThreshold => m_strainedThreshold;
        public float CriticalThreshold => m_criticalThreshold;
        public float CriticalPulseSpeed => m_criticalPulseSpeed;
        public float CriticalMinimumAlpha => m_criticalMinimumAlpha;
        public Color StableColor => m_stableColor;
        public Color StrainedColor => m_strainedColor;
        public Color CriticalColor => m_criticalColor;

        private void OnValidate()
        {
            m_strainedThreshold = Mathf.Clamp01(m_strainedThreshold);
            m_criticalThreshold = Mathf.Clamp(m_criticalThreshold, 0f, m_strainedThreshold);
            m_criticalPulseSpeed = Mathf.Max(0f, m_criticalPulseSpeed);
            m_criticalMinimumAlpha = Mathf.Clamp01(m_criticalMinimumAlpha);
        }
    }
}
