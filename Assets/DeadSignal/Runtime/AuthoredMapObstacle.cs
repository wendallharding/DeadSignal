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
            return OrientedObstacleCollision.Overlaps(
                worldPosition,
                radius,
                Center,
                ScaledHalfSize,
                RightAxis,
                ForwardAxis);
        }

        public bool TryResolveSlide(Vector3 current, Vector3 desired, float radius, out Vector3 resolved)
        {
            if (!OrientedObstacleCollision.TryGetSweepHit(
                current,
                desired,
                radius,
                Center,
                ScaledHalfSize,
                RightAxis,
                ForwardAxis,
                out var hitFraction,
                out var hitNormal))
            {
                resolved = desired;
                return false;
            }

            resolved = OrientedObstacleCollision.ResolveSlide(current, desired, hitFraction, hitNormal);
            return true;
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

    internal static class OrientedObstacleCollision
    {
        public static bool Overlaps(
            Vector3 position,
            float radius,
            Vector2 center,
            Vector2 halfSize,
            Vector2 rightAxis,
            Vector2 forwardAxis)
        {
            var delta = new Vector2(position.x - center.x, position.z - center.y);
            var local = new Vector2(Vector2.Dot(delta, rightAxis), Vector2.Dot(delta, forwardAxis));
            var closest = new Vector2(
                Mathf.Clamp(local.x, -halfSize.x, halfSize.x),
                Mathf.Clamp(local.y, -halfSize.y, halfSize.y));
            var isInside = Mathf.Abs(local.x) < halfSize.x && Mathf.Abs(local.y) < halfSize.y;
            return isInside || (local - closest).sqrMagnitude < radius * radius;
        }

        public static bool TryGetSweepHit(
            Vector3 current,
            Vector3 desired,
            float radius,
            Vector2 center,
            Vector2 halfSize,
            Vector2 rightAxis,
            Vector2 forwardAxis,
            out float hitFraction,
            out Vector2 hitNormal)
        {
            var currentOffset = new Vector2(current.x - center.x, current.z - center.y);
            var movement = new Vector2(desired.x - current.x, desired.z - current.z);
            var localCurrent = new Vector2(
                Vector2.Dot(currentOffset, rightAxis),
                Vector2.Dot(currentOffset, forwardAxis));
            var localMovement = new Vector2(
                Vector2.Dot(movement, rightAxis),
                Vector2.Dot(movement, forwardAxis));
            hitFraction = float.PositiveInfinity;
            var localNormal = Vector2.zero;

            _considerSide(localCurrent, localMovement, halfSize, radius, true, -1f, ref hitFraction, ref localNormal);
            _considerSide(localCurrent, localMovement, halfSize, radius, true, 1f, ref hitFraction, ref localNormal);
            _considerSide(localCurrent, localMovement, halfSize, radius, false, -1f, ref hitFraction, ref localNormal);
            _considerSide(localCurrent, localMovement, halfSize, radius, false, 1f, ref hitFraction, ref localNormal);

            if (radius > Mathf.Epsilon)
            {
                _considerCorner(localCurrent, localMovement, halfSize, radius, new Vector2(-1f, -1f), ref hitFraction, ref localNormal);
                _considerCorner(localCurrent, localMovement, halfSize, radius, new Vector2(-1f, 1f), ref hitFraction, ref localNormal);
                _considerCorner(localCurrent, localMovement, halfSize, radius, new Vector2(1f, -1f), ref hitFraction, ref localNormal);
                _considerCorner(localCurrent, localMovement, halfSize, radius, new Vector2(1f, 1f), ref hitFraction, ref localNormal);
            }

            if (float.IsPositiveInfinity(hitFraction))
            {
                hitNormal = Vector2.zero;
                return false;
            }

            hitNormal = rightAxis * localNormal.x + forwardAxis * localNormal.y;
            return true;
        }

        public static Vector3 ResolveSlide(
            Vector3 current,
            Vector3 desired,
            float hitFraction,
            Vector2 hitNormal)
        {
            var movement = new Vector2(desired.x - current.x, desired.z - current.z);
            var contact = new Vector2(current.x, current.z) + movement * hitFraction;
            var remaining = movement * (1f - hitFraction);
            var inwardMovement = Vector2.Dot(remaining, hitNormal);
            if (inwardMovement < 0f)
            {
                remaining -= hitNormal * inwardMovement;
            }

            var slid = contact + hitNormal * 0.001f + remaining;
            return new Vector3(slid.x, desired.y, slid.y);
        }

        private static void _considerSide(
            Vector2 start,
            Vector2 movement,
            Vector2 halfSize,
            float radius,
            bool rightSide,
            float sign,
            ref float nearestFraction,
            ref Vector2 nearestNormal)
        {
            var primaryStart = rightSide ? start.x : start.y;
            var primaryMovement = rightSide ? movement.x : movement.y;
            if (primaryMovement * sign >= -Mathf.Epsilon)
            {
                return;
            }

            var primaryHalfSize = rightSide ? halfSize.x : halfSize.y;
            var secondaryHalfSize = rightSide ? halfSize.y : halfSize.x;
            var fraction = (sign * (primaryHalfSize + radius) - primaryStart) / primaryMovement;
            var secondary = (rightSide ? start.y : start.x) + (rightSide ? movement.y : movement.x) * fraction;
            if (fraction < 0f || fraction > 1f || fraction >= nearestFraction || Mathf.Abs(secondary) > secondaryHalfSize)
            {
                return;
            }

            nearestFraction = fraction;
            nearestNormal = rightSide ? new Vector2(sign, 0f) : new Vector2(0f, sign);
        }

        private static void _considerCorner(
            Vector2 start,
            Vector2 movement,
            Vector2 halfSize,
            float radius,
            Vector2 cornerSigns,
            ref float nearestFraction,
            ref Vector2 nearestNormal)
        {
            var corner = Vector2.Scale(halfSize, cornerSigns);
            var offset = start - corner;
            var a = movement.sqrMagnitude;
            if (a <= Mathf.Epsilon)
            {
                return;
            }

            var b = 2f * Vector2.Dot(offset, movement);
            var c = offset.sqrMagnitude - radius * radius;
            var discriminant = b * b - 4f * a * c;
            if (discriminant < 0f)
            {
                return;
            }

            var fraction = (-b - Mathf.Sqrt(discriminant)) / (2f * a);
            if (fraction < 0f || fraction > 1f || fraction >= nearestFraction)
            {
                return;
            }

            var contactOffset = start + movement * fraction - corner;
            if (contactOffset.x * cornerSigns.x < -Mathf.Epsilon ||
                contactOffset.y * cornerSigns.y < -Mathf.Epsilon)
            {
                return;
            }

            var normal = contactOffset.normalized;
            if (Vector2.Dot(movement, normal) >= -Mathf.Epsilon)
            {
                return;
            }

            nearestFraction = fraction;
            nearestNormal = normal;
        }
    }
}
