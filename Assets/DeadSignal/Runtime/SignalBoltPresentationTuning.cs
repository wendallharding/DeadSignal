using UnityEngine;

namespace DeadSignal
{
    [CreateAssetMenu(fileName = "SignalBoltPresentationTuning", menuName = "DEAD SIGNAL/Signal Bolt Presentation Tuning")]
    public sealed class SignalBoltPresentationTuning : ScriptableObject
    {
        [Header("Authored Trail")]
        [SerializeField] private float m_trailDuration = 0.12f;
        [SerializeField] private float m_startingWidth = 0.18f;
        [SerializeField] private float m_endingWidth = 0.015f;
        [SerializeField] private float m_minimumVertexDistance = 0.03f;
        [SerializeField] private float m_maximumAlpha = 0.62f;

        [Header("Projectile Rules")]
        [SerializeField] private float m_speed = 13.5f;
        [SerializeField] private float m_lifetime = 1.5f;
        [SerializeField] private float m_fireCooldown = 0.16f;
        [SerializeField] private float m_collisionRadius = 0.08f;

        public float TrailDuration => m_trailDuration;
        public float StartingWidth => m_startingWidth;
        public float EndingWidth => m_endingWidth;
        public float MinimumVertexDistance => m_minimumVertexDistance;
        public float MaximumAlpha => m_maximumAlpha;
        public float Speed => m_speed;
        public float Lifetime => m_lifetime;
        public float FireCooldown => m_fireCooldown;
        public float CollisionRadius => m_collisionRadius;

        private void OnValidate()
        {
            m_trailDuration = Mathf.Max(0.01f, m_trailDuration);
            m_startingWidth = Mathf.Max(0.01f, m_startingWidth);
            m_endingWidth = Mathf.Clamp(m_endingWidth, 0f, m_startingWidth);
            m_minimumVertexDistance = Mathf.Max(0.005f, m_minimumVertexDistance);
            m_maximumAlpha = Mathf.Clamp01(m_maximumAlpha);
            m_speed = Mathf.Max(0.1f, m_speed);
            m_lifetime = Mathf.Max(0.1f, m_lifetime);
            m_fireCooldown = Mathf.Max(0.01f, m_fireCooldown);
            m_collisionRadius = Mathf.Max(0.01f, m_collisionRadius);
        }
    }
}
