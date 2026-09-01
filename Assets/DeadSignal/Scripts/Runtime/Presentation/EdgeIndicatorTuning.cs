using UnityEngine;

namespace DeadSignal.Presentation
{
    [CreateAssetMenu(menuName = "DEAD SIGNAL/Presentation/Edge Indicator Tuning")]
    public sealed class EdgeIndicatorTuning : ScriptableObject
    {
        [SerializeField] private Vector2 m_viewportMargin = new(0.08f, 0.11f);
        [SerializeField] private Vector2 m_visibleInset = new(0.12f, 0.15f);
        [SerializeField] private Vector2 m_objectiveSize = new(500f, 94f);
        [SerializeField] private Vector2 m_threatSize = new(176f, 54f);
        [SerializeField] private float m_objectiveTransitionSpeed = 12f;
        [SerializeField] private float m_objectiveRevealSpeed = 9f;
        [SerializeField] private float m_separation = 64f;
        [SerializeField] private float m_imminentPulseSpeed = 8f;
        [SerializeField] private int m_maximumThreatIndicators = 3;

        public Vector2 ViewportMargin => m_viewportMargin;
        public Vector2 VisibleInset => m_visibleInset;
        public Vector2 ObjectiveSize => m_objectiveSize;
        public Vector2 ThreatSize => m_threatSize;
        public float ObjectiveTransitionSpeed => m_objectiveTransitionSpeed;
        public float ObjectiveRevealSpeed => m_objectiveRevealSpeed;
        public float Separation => m_separation;
        public float ImminentPulseSpeed => m_imminentPulseSpeed;
        public int MaximumThreatIndicators => m_maximumThreatIndicators;

        private void OnValidate()
        {
            m_viewportMargin.x = Mathf.Clamp(m_viewportMargin.x, 0.02f, 0.3f);
            m_viewportMargin.y = Mathf.Clamp(m_viewportMargin.y, 0.02f, 0.3f);
            m_visibleInset.x = Mathf.Clamp(m_visibleInset.x, m_viewportMargin.x, 0.4f);
            m_visibleInset.y = Mathf.Clamp(m_visibleInset.y, m_viewportMargin.y, 0.4f);
            m_objectiveSize = Vector2.Max(m_objectiveSize, new Vector2(160f, 40f));
            m_threatSize = Vector2.Max(m_threatSize, new Vector2(84f, 26f));
            m_objectiveTransitionSpeed = Mathf.Clamp(m_objectiveTransitionSpeed, 1f, 30f);
            m_objectiveRevealSpeed = Mathf.Clamp(m_objectiveRevealSpeed, 1f, 30f);
            m_separation = Mathf.Clamp(m_separation, 20f, 100f);
            m_imminentPulseSpeed = Mathf.Clamp(m_imminentPulseSpeed, 1f, 20f);
            m_maximumThreatIndicators = Mathf.Clamp(m_maximumThreatIndicators, 1, 3);
        }
    }
}
