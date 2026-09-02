using UnityEngine;

namespace DeadSignal.Player
{
    [CreateAssetMenu(fileName = "PlayerCameraTuning", menuName = "DEAD SIGNAL/Player Camera Tuning")]
    public sealed class PlayerCameraTuning : ScriptableObject
    {
        [Header("Three-Quarter Framing")]
        [SerializeField] private float m_fieldOfView = 38f;
        [SerializeField] private float m_pitch = 55f;
        [SerializeField] private float m_yaw = 35f;
        [SerializeField] private float m_height = 12f;
        [SerializeField] private float m_followDistance = 7.4f;
        [SerializeField] private float m_followSharpness = 8f;
        [SerializeField] private float m_lookAheadDistance = 0.9f;
        [SerializeField] private float m_lookAheadSharpness = 10f;
        [SerializeField] private float m_aimLookAheadDistance = 0.45f;
        [SerializeField] private float m_arenaEdgePadding = 0.35f;
        [SerializeField] private float m_maximumTargetFocusOffset = 3.2f;

        [Header("Room B Combat Framing")]
        [SerializeField] private float m_combatFieldOfView = 45f;
        [SerializeField] private float m_combatPitch = 65f;
        [SerializeField] private float m_combatHeight = 24f;
        [SerializeField] private float m_combatFollowDistance = 11.2f;
        [SerializeField] private float m_combatTransitionDuration = 1.1f;
        [SerializeField, Range(0f, 1f)] private float m_combatArenaFocusWeight = 0.35f;

        public float FieldOfView => m_fieldOfView;
        public float Pitch => m_pitch;
        public float Yaw => m_yaw;
        public float Height => m_height;
        public float FollowDistance => m_followDistance;
        public float FollowSharpness => m_followSharpness;
        public float LookAheadDistance => m_lookAheadDistance;
        public float LookAheadSharpness => m_lookAheadSharpness;
        public float AimLookAheadDistance => m_aimLookAheadDistance;
        public float ArenaEdgePadding => m_arenaEdgePadding;
        public float MaximumTargetFocusOffset => m_maximumTargetFocusOffset;
        public float CombatFieldOfView => m_combatFieldOfView;
        public float CombatPitch => m_combatPitch;
        public float CombatHeight => m_combatHeight;
        public float CombatFollowDistance => m_combatFollowDistance;
        public float CombatTransitionDuration => m_combatTransitionDuration;
        public float CombatArenaFocusWeight => m_combatArenaFocusWeight;

        private void OnValidate()
        {
            m_fieldOfView = Mathf.Clamp(m_fieldOfView, 20f, 70f);
            m_pitch = Mathf.Clamp(m_pitch, 35f, 80f);
            m_yaw = Mathf.Clamp(m_yaw, -90f, 90f);
            m_height = Mathf.Max(2f, m_height);
            m_followDistance = Mathf.Max(0f, m_followDistance);
            m_followSharpness = Mathf.Max(0.1f, m_followSharpness);
            m_lookAheadDistance = Mathf.Max(0f, m_lookAheadDistance);
            m_lookAheadSharpness = Mathf.Max(0.1f, m_lookAheadSharpness);
            m_aimLookAheadDistance = Mathf.Max(0f, m_aimLookAheadDistance);
            m_arenaEdgePadding = Mathf.Max(0f, m_arenaEdgePadding);
            m_maximumTargetFocusOffset = Mathf.Max(1f, m_maximumTargetFocusOffset);
            m_combatFieldOfView = Mathf.Clamp(m_combatFieldOfView, 20f, 70f);
            m_combatPitch = Mathf.Clamp(m_combatPitch, 35f, 80f);
            m_combatHeight = Mathf.Max(m_height, m_combatHeight);
            m_combatFollowDistance = Mathf.Max(0f, m_combatFollowDistance);
            m_combatTransitionDuration = Mathf.Max(0.1f, m_combatTransitionDuration);
            m_combatArenaFocusWeight = Mathf.Clamp01(m_combatArenaFocusWeight);
        }
    }
}
