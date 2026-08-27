using UnityEngine;

namespace DeadSignal.Combat
{
    [CreateAssetMenu(fileName = "SwarmerPressureTuning", menuName = "DEAD SIGNAL/Swarmer Pressure Tuning")]
    public sealed class SwarmerPressureTuning : ScriptableObject
    {
        [SerializeField] private int m_maximumAlive = 6;
        [SerializeField] private int m_waveSize = 3;
        [SerializeField] private float m_secondWaveDelay = 4f;
        [SerializeField] private float m_safeSpawnDistance = 4.5f;
        [SerializeField] private float m_speed = 3.4f;
        [SerializeField] private float m_collisionRadius = 0.28f;
        [SerializeField] private float m_contactDistance = 0.65f;
        [SerializeField] private float m_contactCooldown = 1.25f;
        [SerializeField] private float m_contactSignalDrain = 10f;
        [SerializeField] private float m_purgeSignalReward = 3f;
        [SerializeField] private float m_spawnSpacing = 0.8f;

        public int MaximumAlive => m_maximumAlive;
        public int WaveSize => m_waveSize;
        public float SecondWaveDelay => m_secondWaveDelay;
        public float SafeSpawnDistance => m_safeSpawnDistance;
        public float Speed => m_speed;
        public float CollisionRadius => m_collisionRadius;
        public float ContactDistance => m_contactDistance;
        public float ContactCooldown => m_contactCooldown;
        public float ContactSignalDrain => m_contactSignalDrain;
        public float PurgeSignalReward => m_purgeSignalReward;
        public float SpawnSpacing => m_spawnSpacing;

        private void OnValidate()
        {
            m_maximumAlive = Mathf.Max(1, m_maximumAlive);
            m_waveSize = Mathf.Clamp(m_waveSize, 1, m_maximumAlive);
            m_secondWaveDelay = Mathf.Max(0.1f, m_secondWaveDelay);
            m_safeSpawnDistance = Mathf.Max(1f, m_safeSpawnDistance);
            m_speed = Mathf.Max(0.1f, m_speed);
            m_collisionRadius = Mathf.Max(0.1f, m_collisionRadius);
            m_contactDistance = Mathf.Max(m_collisionRadius, m_contactDistance);
            m_contactCooldown = Mathf.Max(0.1f, m_contactCooldown);
            m_contactSignalDrain = Mathf.Max(0f, m_contactSignalDrain);
            m_purgeSignalReward = Mathf.Clamp(m_purgeSignalReward, 0f, m_contactSignalDrain);
            m_spawnSpacing = Mathf.Max(m_collisionRadius * 2f, m_spawnSpacing);
        }
    }
}
