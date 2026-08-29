using UnityEngine;

namespace DeadSignal.Presentation
{
    [CreateAssetMenu(fileName = "WeaponTransformationFeedbackTuning",
        menuName = "Dead Signal/Tuning/Weapon Transformation Feedback")]
    public sealed class WeaponTransformationFeedbackTuning : ScriptableObject
    {
        [SerializeField, Range(2, 4)] private int m_poolSize = 2;
        [SerializeField, Min(0.1f)] private float m_duration = 0.9f;
        [SerializeField, Range(0f, 1f)] private float m_maximumAlpha = 0.66f;
        [SerializeField, Range(0f, 1f)] private float m_reducedFlashesMaximumAlpha = 0.28f;
        [SerializeField, Min(0.1f)] private float m_transformationDiameter = 2.35f;
        [SerializeField, Min(0.1f)] private float m_evolutionDiameter = 3.15f;
        [SerializeField, Min(0.1f)] private float m_startingDiameterMultiplier = 0.58f;
        [SerializeField, Min(0.1f)] private float m_endingDiameterMultiplier = 1.1f;

        public int PoolSize => m_poolSize;
        public float Duration => m_duration;
        public float MaximumAlpha => m_maximumAlpha;
        public float ReducedFlashesMaximumAlpha => m_reducedFlashesMaximumAlpha;
        public float TransformationDiameter => m_transformationDiameter;
        public float EvolutionDiameter => m_evolutionDiameter;
        public float StartingDiameterMultiplier => m_startingDiameterMultiplier;
        public float EndingDiameterMultiplier => m_endingDiameterMultiplier;

        private void OnValidate()
        {
            m_poolSize = Mathf.Clamp(m_poolSize, 2, 4);
            m_duration = Mathf.Max(0.1f, m_duration);
            m_maximumAlpha = Mathf.Clamp01(m_maximumAlpha);
            m_reducedFlashesMaximumAlpha = Mathf.Clamp(m_reducedFlashesMaximumAlpha, 0f, m_maximumAlpha);
            m_transformationDiameter = Mathf.Max(0.1f, m_transformationDiameter);
            m_evolutionDiameter = Mathf.Max(m_transformationDiameter, m_evolutionDiameter);
            m_startingDiameterMultiplier = Mathf.Max(0.1f, m_startingDiameterMultiplier);
            m_endingDiameterMultiplier = Mathf.Max(m_startingDiameterMultiplier, m_endingDiameterMultiplier);
        }
    }
}
