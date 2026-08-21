using UnityEngine;

namespace DeadSignal
{
    public sealed class AuthoredMapObstacle : MonoBehaviour
    {
        [SerializeField] private Vector2 m_localHalfSize = new(2.2f, 0.55f);

        public Vector2 Center => new(transform.position.x, transform.position.z);

        public Vector2 WorldHalfSize
        {
            get
            {
                var rightExtent = transform.TransformVector(new Vector3(m_localHalfSize.x, 0f, 0f));
                var forwardExtent = transform.TransformVector(new Vector3(0f, 0f, m_localHalfSize.y));
                return new Vector2(
                    Mathf.Abs(rightExtent.x) + Mathf.Abs(forwardExtent.x),
                    Mathf.Abs(rightExtent.z) + Mathf.Abs(forwardExtent.z));
            }
        }

        private void OnValidate()
        {
            Configure(m_localHalfSize);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.85f, 1f, 0.4f);
            var center = new Vector3(transform.position.x, 0.15f, transform.position.z);
            var halfSize = WorldHalfSize;
            Gizmos.DrawWireCube(center, new Vector3(halfSize.x * 2f, 0.3f, halfSize.y * 2f));
        }

        public void Configure(Vector2 localHalfSize)
        {
            m_localHalfSize = new Vector2(Mathf.Max(0.05f, localHalfSize.x), Mathf.Max(0.05f, localHalfSize.y));
        }
    }
}
