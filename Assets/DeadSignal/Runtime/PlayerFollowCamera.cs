using UnityEngine;

namespace DeadSignal
{
    /// <summary>
    /// Moves a camera rig after gameplay updates while leaving the child camera free for local combat impulse.
    /// </summary>
    public sealed class PlayerFollowCamera : MonoBehaviour
    {
        private Camera m_targetCamera;
        private Transform m_target;
        private PlayerCameraTuning m_tuning;
        private Vector2 m_arenaHalfExtents;
        private Vector3 m_currentFocus;
        private Vector3 m_currentLookAhead;
        private Vector3 m_lastTargetPosition;
        private bool m_isConfigured;

        public Vector3 CurrentFocus => m_currentFocus;
        public Vector3 CurrentLookAhead => m_currentLookAhead;
        public bool IsConfigured => m_isConfigured;

        private void LateUpdate()
        {
            if (!m_isConfigured || m_target == null || m_targetCamera == null || Time.deltaTime <= 0f)
            {
                return;
            }

            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            var targetPosition = m_target.position;
            var targetDelta = targetPosition - m_lastTargetPosition;
            targetDelta.y = 0f;
            var desiredLookAhead = targetDelta.sqrMagnitude > 0.000001f
                ? targetDelta.normalized * m_tuning.LookAheadDistance
                : Vector3.zero;
            m_currentLookAhead = Vector3.Lerp(
                m_currentLookAhead,
                desiredLookAhead,
                _exponentialBlend(m_tuning.LookAheadSharpness, dt));

            var desiredFocus = CalculateClampedFocus(
                targetPosition + m_currentLookAhead,
                m_arenaHalfExtents,
                m_targetCamera.orthographicSize,
                m_targetCamera.aspect,
                m_tuning.ArenaEdgePadding);
            m_currentFocus = Vector3.Lerp(
                m_currentFocus,
                desiredFocus,
                _exponentialBlend(m_tuning.FollowSharpness, dt));
            transform.position = m_currentFocus;
            m_lastTargetPosition = targetPosition;
        }

        public void Configure(
            Camera targetCamera,
            Transform target,
            PlayerCameraTuning tuning,
            Vector2 arenaHalfExtents)
        {
            m_targetCamera = targetCamera;
            m_target = target;
            m_tuning = tuning;
            m_arenaHalfExtents = arenaHalfExtents;
            m_isConfigured = m_targetCamera != null && m_target != null && m_tuning != null;
            if (!m_isConfigured)
            {
                return;
            }

            m_targetCamera.orthographicSize = m_tuning.OrthographicSize;
            m_currentLookAhead = Vector3.zero;
            m_lastTargetPosition = m_target.position;
            m_currentFocus = CalculateClampedFocus(
                m_target.position,
                m_arenaHalfExtents,
                m_targetCamera.orthographicSize,
                m_targetCamera.aspect,
                m_tuning.ArenaEdgePadding);
            transform.position = m_currentFocus;
        }

        public static Vector3 CalculateClampedFocus(
            Vector3 desiredFocus,
            Vector2 arenaHalfExtents,
            float orthographicSize,
            float aspect,
            float edgePadding)
        {
            float safeAspect = Mathf.Max(0.01f, aspect);
            float maximumX = Mathf.Max(0f, arenaHalfExtents.x - orthographicSize * safeAspect - edgePadding);
            float maximumZ = Mathf.Max(0f, arenaHalfExtents.y - orthographicSize - edgePadding);
            return new Vector3(
                Mathf.Clamp(desiredFocus.x, -maximumX, maximumX),
                0f,
                Mathf.Clamp(desiredFocus.z, -maximumZ, maximumZ));
        }

        private static float _exponentialBlend(float sharpness, float dt)
        {
            return 1f - Mathf.Exp(-sharpness * dt);
        }
    }
}
