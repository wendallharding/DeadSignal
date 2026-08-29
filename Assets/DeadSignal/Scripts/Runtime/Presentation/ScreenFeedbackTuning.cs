using UnityEngine;

namespace DeadSignal.Presentation
{
    [CreateAssetMenu(fileName = "ScreenFeedbackTuning", menuName = "Dead Signal/Tuning/Screen Feedback")]
    public sealed class ScreenFeedbackTuning : ScriptableObject
    {
        [Header("Signal Reserve")]
        [SerializeField, Range(0f, 1f)] private float m_warningThreshold = 0.3f;
        [SerializeField, Range(0f, 1f)] private float m_criticalThreshold = 0.25f;
        [SerializeField, Min(0f)] private float m_signalPulseSpeed = 2.4f;
        [SerializeField, Range(0f, 1f)] private float m_warningMinimumAlpha = 0.07f;
        [SerializeField, Range(0f, 1f)] private float m_warningMaximumAlpha = 0.14f;
        [SerializeField, Range(0f, 1f)] private float m_criticalMaximumAlpha = 0.2f;
        [SerializeField, Range(0f, 1f)] private float m_reducedFlashesSignalAlpha = 0.1f;

        [Header("Directional Damage")]
        [SerializeField, Min(0.05f)] private float m_directionalDuration = 0.52f;
        [SerializeField, Range(0f, 1f)] private float m_directionalMaximumAlpha = 0.48f;
        [SerializeField, Range(0f, 1f)] private float m_reducedFlashesDirectionalAlpha = 0.24f;
        [SerializeField, Range(0.1f, 0.49f)] private float m_horizontalAnchorRadius = 0.43f;
        [SerializeField, Range(0.1f, 0.49f)] private float m_verticalAnchorRadius = 0.4f;
        [SerializeField] private Color m_securityDamageColor = new(1f, 0.12f, 0.08f, 1f);
        [SerializeField] private Color m_sapperDamageColor = new(1f, 0.08f, 0.65f, 1f);

        public float WarningThreshold => m_warningThreshold;
        public float CriticalThreshold => m_criticalThreshold;
        public float SignalPulseSpeed => m_signalPulseSpeed;
        public float WarningMinimumAlpha => m_warningMinimumAlpha;
        public float WarningMaximumAlpha => m_warningMaximumAlpha;
        public float CriticalMaximumAlpha => m_criticalMaximumAlpha;
        public float ReducedFlashesSignalAlpha => m_reducedFlashesSignalAlpha;
        public float DirectionalDuration => m_directionalDuration;
        public float DirectionalMaximumAlpha => m_directionalMaximumAlpha;
        public float ReducedFlashesDirectionalAlpha => m_reducedFlashesDirectionalAlpha;
        public float HorizontalAnchorRadius => m_horizontalAnchorRadius;
        public float VerticalAnchorRadius => m_verticalAnchorRadius;
        public Color SecurityDamageColor => m_securityDamageColor;
        public Color SapperDamageColor => m_sapperDamageColor;

        private void OnValidate()
        {
            m_warningThreshold = Mathf.Clamp01(m_warningThreshold);
            m_criticalThreshold = Mathf.Clamp(m_criticalThreshold, 0f, m_warningThreshold);
            m_signalPulseSpeed = Mathf.Max(0f, m_signalPulseSpeed);
            m_warningMinimumAlpha = Mathf.Clamp01(m_warningMinimumAlpha);
            m_warningMaximumAlpha = Mathf.Clamp(m_warningMaximumAlpha, m_warningMinimumAlpha, 1f);
            m_criticalMaximumAlpha = Mathf.Clamp(m_criticalMaximumAlpha, m_warningMaximumAlpha, 1f);
            m_reducedFlashesSignalAlpha = Mathf.Clamp01(m_reducedFlashesSignalAlpha);
            m_directionalDuration = Mathf.Max(0.05f, m_directionalDuration);
            m_directionalMaximumAlpha = Mathf.Clamp01(m_directionalMaximumAlpha);
            m_reducedFlashesDirectionalAlpha = Mathf.Clamp(
                m_reducedFlashesDirectionalAlpha, 0f, m_directionalMaximumAlpha);
            m_horizontalAnchorRadius = Mathf.Clamp(m_horizontalAnchorRadius, 0.1f, 0.49f);
            m_verticalAnchorRadius = Mathf.Clamp(m_verticalAnchorRadius, 0.1f, 0.49f);
        }
    }
}
