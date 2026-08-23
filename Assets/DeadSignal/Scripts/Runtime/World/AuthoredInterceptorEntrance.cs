using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Scene-authored deployment point for the Security Interceptor.
    /// </summary>
    public sealed class AuthoredInterceptorEntrance : MonoBehaviour
    {
        [SerializeField] private int m_priority;

        public int Priority => m_priority;
        public Vector3 Position => transform.position;

        public void Configure(int priority)
        {
            m_priority = Mathf.Max(0, priority);
        }
    }
}
