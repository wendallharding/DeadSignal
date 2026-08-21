using UnityEngine;

namespace DeadSignal
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

        public float RotationSpeed => m_rotationSpeed;
        public float HoverHeight => m_hoverHeight;
        public float HoverAmplitude => m_hoverAmplitude;
        public float HoverFrequency => m_hoverFrequency;
        public float CollectionRadius => m_collectionRadius;

        private void OnValidate()
        {
            m_rotationSpeed = Mathf.Max(0f, m_rotationSpeed);
            m_hoverHeight = Mathf.Max(0f, m_hoverHeight);
            m_hoverAmplitude = Mathf.Clamp(m_hoverAmplitude, 0f, m_hoverHeight);
            m_hoverFrequency = Mathf.Max(0f, m_hoverFrequency);
            m_collectionRadius = Mathf.Max(0.1f, m_collectionRadius);
        }
    }
}
