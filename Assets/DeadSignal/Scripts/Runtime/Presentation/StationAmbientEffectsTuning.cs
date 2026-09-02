using UnityEngine;

namespace DeadSignal.Presentation
{
    [CreateAssetMenu(fileName = "StationAmbientEffectsTuning", menuName = "DEAD SIGNAL/Station Ambient Effects Tuning")]
    public sealed class StationAmbientEffectsTuning : ScriptableObject
    {
        [Header("Budget")]
        [SerializeField, Min(4f)] private float m_cullingDistance = 11f;
        [SerializeField, Range(0.1f, 1f)] private float m_reducedFlashesEmissionMultiplier = 0.45f;
        [SerializeField, Range(0.1f, 1f)] private float m_reducedFlashesAlphaMultiplier = 0.55f;
        [SerializeField, Min(1)] private int m_maximumParticlesPerEmitter = 12;

        [Header("Cadence")]
        [SerializeField, Min(0f)] private float m_continuousEmissionRate = 3.6f;
        [SerializeField, Min(0f)] private float m_sparseEmissionRate = 1.1f;
        [SerializeField, Min(0f)] private float m_sparkEmissionRate = 0.6f;

        public float CullingDistance => m_cullingDistance;
        public float ReducedFlashesEmissionMultiplier => m_reducedFlashesEmissionMultiplier;
        public float ReducedFlashesAlphaMultiplier => m_reducedFlashesAlphaMultiplier;
        public int MaximumParticlesPerEmitter => m_maximumParticlesPerEmitter;
        public float ContinuousEmissionRate => m_continuousEmissionRate;
        public float SparseEmissionRate => m_sparseEmissionRate;
        public float SparkEmissionRate => m_sparkEmissionRate;

        private void OnValidate()
        {
            m_cullingDistance = Mathf.Max(4f, m_cullingDistance);
            m_reducedFlashesEmissionMultiplier = Mathf.Clamp(m_reducedFlashesEmissionMultiplier, 0.1f, 1f);
            m_reducedFlashesAlphaMultiplier = Mathf.Clamp(m_reducedFlashesAlphaMultiplier, 0.1f, 1f);
            m_maximumParticlesPerEmitter = Mathf.Max(1, m_maximumParticlesPerEmitter);
            m_continuousEmissionRate = Mathf.Max(0f, m_continuousEmissionRate);
            m_sparseEmissionRate = Mathf.Max(0f, m_sparseEmissionRate);
            m_sparkEmissionRate = Mathf.Max(0f, m_sparkEmissionRate);
        }
    }
}
