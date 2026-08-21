using UnityEngine;

namespace DeadSignal
{
    public sealed class AuthoredMapObstacle : MonoBehaviour
    {
        [SerializeField] private Vector2 m_localHalfSize = new(2.2f, 0.55f);

        public Vector2 Center => new(transform.position.x, transform.position.z);
        public Vector2 RightAxis => _horizontalAxis(transform.TransformVector(Vector3.right));
        public Vector2 ForwardAxis => _horizontalAxis(transform.TransformVector(Vector3.forward));

        public Vector2 ScaledHalfSize => new(
            _horizontalMagnitude(transform.TransformVector(Vector3.right)) * m_localHalfSize.x,
            _horizontalMagnitude(transform.TransformVector(Vector3.forward)) * m_localHalfSize.y);

        private void OnValidate()
        {
            Configure(m_localHalfSize);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.85f, 1f, 0.4f);
            var previousMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                new Vector3(transform.position.x, 0.15f, transform.position.z),
                transform.rotation,
                new Vector3(transform.lossyScale.x, 1f, transform.lossyScale.z));
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(m_localHalfSize.x * 2f, 0.3f, m_localHalfSize.y * 2f));
            Gizmos.matrix = previousMatrix;
        }

        public void Configure(Vector2 localHalfSize)
        {
            m_localHalfSize = new Vector2(Mathf.Max(0.05f, localHalfSize.x), Mathf.Max(0.05f, localHalfSize.y));
        }

        public bool OverlapsCircle(Vector3 worldPosition, float radius)
        {
            var delta = new Vector2(worldPosition.x - Center.x, worldPosition.z - Center.y);
            var halfSize = ScaledHalfSize;
            return Mathf.Abs(Vector2.Dot(delta, RightAxis)) < halfSize.x + radius &&
                   Mathf.Abs(Vector2.Dot(delta, ForwardAxis)) < halfSize.y + radius;
        }

        private static Vector2 _horizontalAxis(Vector3 vector)
        {
            var horizontal = new Vector2(vector.x, vector.z);
            return horizontal.sqrMagnitude > Mathf.Epsilon ? horizontal.normalized : Vector2.right;
        }

        private static float _horizontalMagnitude(Vector3 vector)
        {
            return new Vector2(vector.x, vector.z).magnitude;
        }
    }
}
