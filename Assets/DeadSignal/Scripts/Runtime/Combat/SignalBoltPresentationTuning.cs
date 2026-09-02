using UnityEngine;

namespace DeadSignal.Combat
{
    [CreateAssetMenu(fileName = "SignalBoltPresentationTuning", menuName = "DEAD SIGNAL/Signal Bolt Presentation Tuning")]
    public sealed class SignalBoltPresentationTuning : ScriptableObject
    {
        [Header("Authored Trail")]
        [SerializeField] private float m_trailDuration = 0.16f;
        [SerializeField] private float m_startingWidth = 0.24f;
        [SerializeField] private float m_endingWidth = 0.01f;
        [SerializeField] private float m_minimumVertexDistance = 0.03f;
        [SerializeField] private float m_maximumAlpha = 0.86f;

        [Header("Muzzle and Launch")]
        [SerializeField] private float m_recoilDuration = 0.09f;
        [SerializeField] private float m_recoilDistance = 0.18f;
        [SerializeField] private float m_burstDuration = 0.08f;
        [SerializeField] private int m_burstParticleCount = 7;
        [SerializeField] private int m_reducedFlashesParticleCount = 3;
        [SerializeField] private float m_launchStreakDuration = 0.065f;
        [SerializeField] private float m_launchStreakLength = 0.46f;
        [SerializeField] private float m_launchStreakWidth = 0.12f;
        [SerializeField] private float m_muzzleLightDuration = 0.06f;
        [SerializeField] private float m_muzzleLightRange = 2.4f;
        [SerializeField] private float m_muzzleLightIntensity = 2.6f;

        [Header("Evolved Weapon Language")]
        [SerializeField] private float m_piercingTrailWidth = 0.18f;
        [SerializeField] private float m_ricochetTrailWidth = 0.21f;
        [SerializeField] private float m_evolvedTrailMultiplier = 1.16f;
        [SerializeField] private float m_weaponEventDuration = 0.18f;
        [SerializeField] private float m_weaponEventLength = 0.72f;
        [SerializeField] private float m_weaponEventWidth = 0.075f;
        [SerializeField] private int m_weaponEventPoolSize = 8;

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
        public float RecoilDuration => m_recoilDuration;
        public float RecoilDistance => m_recoilDistance;
        public float BurstDuration => m_burstDuration;
        public int BurstParticleCount => m_burstParticleCount;
        public int ReducedFlashesParticleCount => m_reducedFlashesParticleCount;
        public float LaunchStreakDuration => m_launchStreakDuration;
        public float LaunchStreakLength => m_launchStreakLength;
        public float LaunchStreakWidth => m_launchStreakWidth;
        public float MuzzleLightDuration => m_muzzleLightDuration;
        public float MuzzleLightRange => m_muzzleLightRange;
        public float MuzzleLightIntensity => m_muzzleLightIntensity;
        public float PiercingTrailWidth => m_piercingTrailWidth;
        public float RicochetTrailWidth => m_ricochetTrailWidth;
        public float EvolvedTrailMultiplier => m_evolvedTrailMultiplier;
        public float WeaponEventDuration => m_weaponEventDuration;
        public float WeaponEventLength => m_weaponEventLength;
        public float WeaponEventWidth => m_weaponEventWidth;
        public int WeaponEventPoolSize => m_weaponEventPoolSize;
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
            m_recoilDuration = Mathf.Max(0.01f, m_recoilDuration);
            m_recoilDistance = Mathf.Max(0f, m_recoilDistance);
            m_burstDuration = Mathf.Max(0.01f, m_burstDuration);
            m_burstParticleCount = Mathf.Max(1, m_burstParticleCount);
            m_reducedFlashesParticleCount = Mathf.Clamp(m_reducedFlashesParticleCount, 1, m_burstParticleCount);
            m_launchStreakDuration = Mathf.Max(0.01f, m_launchStreakDuration);
            m_launchStreakLength = Mathf.Max(0.01f, m_launchStreakLength);
            m_launchStreakWidth = Mathf.Max(0.01f, m_launchStreakWidth);
            m_muzzleLightDuration = Mathf.Max(0.01f, m_muzzleLightDuration);
            m_muzzleLightRange = Mathf.Max(0.1f, m_muzzleLightRange);
            m_muzzleLightIntensity = Mathf.Max(0f, m_muzzleLightIntensity);
            m_piercingTrailWidth = Mathf.Max(0.01f, m_piercingTrailWidth);
            m_ricochetTrailWidth = Mathf.Max(0.01f, m_ricochetTrailWidth);
            m_evolvedTrailMultiplier = Mathf.Max(1f, m_evolvedTrailMultiplier);
            m_weaponEventDuration = Mathf.Max(0.05f, m_weaponEventDuration);
            m_weaponEventLength = Mathf.Max(0.1f, m_weaponEventLength);
            m_weaponEventWidth = Mathf.Max(0.01f, m_weaponEventWidth);
            m_weaponEventPoolSize = Mathf.Clamp(m_weaponEventPoolSize, 4, 16);
            m_speed = Mathf.Max(0.1f, m_speed);
            m_lifetime = Mathf.Max(0.1f, m_lifetime);
            m_fireCooldown = Mathf.Max(0.01f, m_fireCooldown);
            m_collisionRadius = Mathf.Max(0.01f, m_collisionRadius);
        }
    }
}
