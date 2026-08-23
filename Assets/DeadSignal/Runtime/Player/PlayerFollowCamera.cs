using UnityEngine;

namespace DeadSignal.Player
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
        private Vector3 m_aimDirection;
        private Vector2 m_groundFootprintMinimum;
        private Vector2 m_groundFootprintMaximum;
        private float m_lastAspect;
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
            if (!Mathf.Approximately(m_lastAspect, m_targetCamera.aspect))
            {
                _refreshGroundFootprint();
            }

            var targetPosition = m_target.position;
            var targetDelta = targetPosition - m_lastTargetPosition;
            targetDelta.y = 0f;
            var desiredLookAhead = targetDelta.sqrMagnitude > 0.000001f
                ? targetDelta.normalized * m_tuning.LookAheadDistance
                : Vector3.zero;
            if (m_aimDirection.sqrMagnitude > 0.01f)
            {
                desiredLookAhead += m_aimDirection.normalized * m_tuning.AimLookAheadDistance;
            }
            m_currentLookAhead = Vector3.Lerp(
                m_currentLookAhead,
                desiredLookAhead,
                _exponentialBlend(m_tuning.LookAheadSharpness, dt));

            var desiredFocus = CalculateClampedFocus(
                targetPosition + m_currentLookAhead,
                m_arenaHalfExtents,
                m_groundFootprintMinimum,
                m_groundFootprintMaximum,
                m_tuning.ArenaEdgePadding);
            desiredFocus = EnsureTargetVisibleFocus(
                desiredFocus,
                targetPosition,
                m_tuning.MaximumTargetFocusOffset);
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

            m_targetCamera.orthographic = false;
            m_targetCamera.fieldOfView = m_tuning.FieldOfView;
            m_targetCamera.transform.localPosition = new Vector3(0f, m_tuning.Height, -m_tuning.FollowDistance);
            m_targetCamera.transform.localRotation = Quaternion.Euler(m_tuning.Pitch, 0f, 0f);
            _refreshGroundFootprint();
            m_currentLookAhead = Vector3.zero;
            m_lastTargetPosition = m_target.position;
            m_currentFocus = CalculateClampedFocus(
                m_target.position,
                m_arenaHalfExtents,
                m_groundFootprintMinimum,
                m_groundFootprintMaximum,
                m_tuning.ArenaEdgePadding);
            m_currentFocus = EnsureTargetVisibleFocus(
                m_currentFocus,
                m_target.position,
                m_tuning.MaximumTargetFocusOffset);
            transform.position = m_currentFocus;
        }

        public void SetAimDirection(Vector3 aimDirection)
        {
            aimDirection.y = 0f;
            m_aimDirection = aimDirection;
        }

        public static Vector3 CalculateClampedFocus(
            Vector3 desiredFocus,
            Vector2 arenaHalfExtents,
            Vector2 groundFootprintMinimum,
            Vector2 groundFootprintMaximum,
            float edgePadding)
        {
            float minimumX = -arenaHalfExtents.x - groundFootprintMinimum.x + edgePadding;
            float maximumX = arenaHalfExtents.x - groundFootprintMaximum.x - edgePadding;
            float minimumZ = -arenaHalfExtents.y - groundFootprintMinimum.y + edgePadding;
            float maximumZ = arenaHalfExtents.y - groundFootprintMaximum.y - edgePadding;
            return new Vector3(
                minimumX <= maximumX ? Mathf.Clamp(desiredFocus.x, minimumX, maximumX) : 0f,
                0f,
                minimumZ <= maximumZ ? Mathf.Clamp(desiredFocus.z, minimumZ, maximumZ) : 0f);
        }

        public static void CalculateGroundFootprint(Camera targetCamera, out Vector2 minimum, out Vector2 maximum)
        {
            minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            var deck = new Plane(Vector3.up, Vector3.zero);
            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    var ray = targetCamera.ViewportPointToRay(new Vector3(x, y, 0f));
                    if (!deck.Raycast(ray, out float distance))
                    {
                        continue;
                    }

                    var point = ray.GetPoint(distance) - targetCamera.transform.parent.position;
                    minimum = Vector2.Min(minimum, new Vector2(point.x, point.z));
                    maximum = Vector2.Max(maximum, new Vector2(point.x, point.z));
                }
            }

            if (float.IsInfinity(minimum.x))
            {
                minimum = Vector2.zero;
                maximum = Vector2.zero;
            }
        }

        public static Vector3 EnsureTargetVisibleFocus(Vector3 focus, Vector3 target, float maximumOffset)
        {
            maximumOffset = Mathf.Max(0f, maximumOffset);
            focus.x = Mathf.Clamp(focus.x, target.x - maximumOffset, target.x + maximumOffset);
            focus.z = Mathf.Clamp(focus.z, target.z - maximumOffset, target.z + maximumOffset);
            return focus;
        }

        private static float _exponentialBlend(float sharpness, float dt)
        {
            return 1f - Mathf.Exp(-sharpness * dt);
        }

        private void _refreshGroundFootprint()
        {
            CalculateGroundFootprint(m_targetCamera, out m_groundFootprintMinimum, out m_groundFootprintMaximum);
            m_lastAspect = m_targetCamera.aspect;
        }
    }
}
