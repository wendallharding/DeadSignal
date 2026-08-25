using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>Scene-authored positions and framing target for a repeatable development combat laboratory.</summary>
    public sealed class AuthoredCombatScenario : MonoBehaviour
    {
        [SerializeField] private Transform m_playerAnchor;
        [SerializeField] private Transform m_cameraFocus;
        [SerializeField] private Transform m_wardenAnchor;
        [SerializeField] private Transform m_sapperAnchor;
        [SerializeField] private Transform m_interceptorAnchor;
        [SerializeField] private Transform m_suppressorAnchor;
        [SerializeField] private Vector2 m_minimumLocalPosition = new(-4.6f, -2.6f);
        [SerializeField] private Vector2 m_maximumLocalPosition = new(4.6f, 0.5f);

        public Transform PlayerAnchor => m_playerAnchor;
        public Transform CameraFocus => m_cameraFocus;
        public Transform WardenAnchor => m_wardenAnchor;
        public Transform SapperAnchor => m_sapperAnchor;
        public Transform InterceptorAnchor => m_interceptorAnchor;
        public Transform SuppressorAnchor => m_suppressorAnchor;
        public bool IsComplete => m_playerAnchor != null && m_cameraFocus != null && m_wardenAnchor != null &&
                                  m_sapperAnchor != null && m_interceptorAnchor != null && m_suppressorAnchor != null;

        public void Configure(
            Transform playerAnchor,
            Transform cameraFocus,
            Transform wardenAnchor,
            Transform sapperAnchor,
            Transform interceptorAnchor,
            Transform suppressorAnchor,
            Vector2 minimumLocalPosition,
            Vector2 maximumLocalPosition)
        {
            m_playerAnchor = playerAnchor;
            m_cameraFocus = cameraFocus;
            m_wardenAnchor = wardenAnchor;
            m_sapperAnchor = sapperAnchor;
            m_interceptorAnchor = interceptorAnchor;
            m_suppressorAnchor = suppressorAnchor;
            m_minimumLocalPosition = minimumLocalPosition;
            m_maximumLocalPosition = maximumLocalPosition;
        }

        public Vector3 ClampToSafeArea(Vector3 position)
        {
            var localPosition = transform.InverseTransformPoint(position);
            localPosition.x = Mathf.Clamp(localPosition.x, m_minimumLocalPosition.x, m_maximumLocalPosition.x);
            localPosition.z = Mathf.Clamp(localPosition.z, m_minimumLocalPosition.y, m_maximumLocalPosition.y);
            return transform.TransformPoint(localPosition);
        }
    }
}
