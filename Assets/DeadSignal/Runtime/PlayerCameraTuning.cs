using UnityEngine;

namespace DeadSignal
{
    [CreateAssetMenu(fileName = "PlayerCameraTuning", menuName = "DEAD SIGNAL/Player Camera Tuning")]
    public sealed class PlayerCameraTuning : ScriptableObject
    {
        [Header("Tactical Framing")]
        [SerializeField] private float m_orthographicSize = 5.8f;
        [SerializeField] private float m_followSharpness = 8f;
        [SerializeField] private float m_lookAheadDistance = 0.9f;
        [SerializeField] private float m_lookAheadSharpness = 10f;
        [SerializeField] private float m_arenaEdgePadding = 0.35f;

        public float OrthographicSize => m_orthographicSize;
        public float FollowSharpness => m_followSharpness;
        public float LookAheadDistance => m_lookAheadDistance;
        public float LookAheadSharpness => m_lookAheadSharpness;
        public float ArenaEdgePadding => m_arenaEdgePadding;

        private void OnValidate()
        {
            m_orthographicSize = Mathf.Max(1f, m_orthographicSize);
            m_followSharpness = Mathf.Max(0.1f, m_followSharpness);
            m_lookAheadDistance = Mathf.Max(0f, m_lookAheadDistance);
            m_lookAheadSharpness = Mathf.Max(0.1f, m_lookAheadSharpness);
            m_arenaEdgePadding = Mathf.Max(0f, m_arenaEdgePadding);
        }
    }
}
