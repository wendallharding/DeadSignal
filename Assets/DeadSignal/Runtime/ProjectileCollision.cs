using UnityEngine;

namespace DeadSignal
{
    /// <summary>
    /// Provides deterministic swept collision queries for fast top-down projectiles.
    /// </summary>
    public static class ProjectileCollision
    {
        public static bool TryGetCircleHitFraction(
            Vector3 start,
            Vector3 end,
            Vector3 center,
            float radius,
            out float hitFraction)
        {
            var start2D = new Vector2(start.x, start.z);
            var delta = new Vector2(end.x - start.x, end.z - start.z);
            var offset = start2D - new Vector2(center.x, center.z);
            var radiusSquared = radius * radius;
            if (offset.sqrMagnitude <= radiusSquared)
            {
                hitFraction = 0f;
                return true;
            }

            var a = Vector2.Dot(delta, delta);
            if (a <= Mathf.Epsilon)
            {
                hitFraction = 0f;
                return false;
            }

            var centerFraction = -Vector2.Dot(offset, delta) / a;
            var closestFraction = Mathf.Clamp01(centerFraction);
            var closestOffset = offset + delta * closestFraction;
            if (closestOffset.sqrMagnitude > radiusSquared)
            {
                hitFraction = 0f;
                return false;
            }

            var perpendicularSquared = Mathf.Max(0f, offset.sqrMagnitude -
                Mathf.Pow(Vector2.Dot(offset, delta), 2f) / a);
            var contactOffset = Mathf.Sqrt(Mathf.Max(0f, radiusSquared - perpendicularSquared) / a);
            hitFraction = Mathf.Clamp01(centerFraction - contactOffset);
            return true;
        }

        public static bool TryGetOrientedBoxHitFraction(
            Vector3 start,
            Vector3 end,
            Vector2 center,
            Vector2 halfSize,
            Vector2 rightAxis,
            Vector2 forwardAxis,
            float radius,
            out float hitFraction)
        {
            var startOffset = new Vector2(start.x - center.x, start.z - center.y);
            var endOffset = new Vector2(end.x - center.x, end.z - center.y);
            var localStart = new Vector2(Vector2.Dot(startOffset, rightAxis), Vector2.Dot(startOffset, forwardAxis));
            var localEnd = new Vector2(Vector2.Dot(endOffset, rightAxis), Vector2.Dot(endOffset, forwardAxis));
            var expandedHalfSize = halfSize + Vector2.one * Mathf.Max(0f, radius);
            var entry = 0f;
            var exit = 1f;

            if (!_clipAxis(localStart.x, localEnd.x - localStart.x, expandedHalfSize.x, ref entry, ref exit) ||
                !_clipAxis(localStart.y, localEnd.y - localStart.y, expandedHalfSize.y, ref entry, ref exit))
            {
                hitFraction = 0f;
                return false;
            }

            hitFraction = entry;
            return true;
        }

        private static bool _clipAxis(float start, float delta, float extent, ref float entry, ref float exit)
        {
            if (Mathf.Abs(delta) <= Mathf.Epsilon)
            {
                return Mathf.Abs(start) <= extent;
            }

            var first = (-extent - start) / delta;
            var second = (extent - start) / delta;
            if (first > second)
            {
                (first, second) = (second, first);
            }

            entry = Mathf.Max(entry, first);
            exit = Mathf.Min(exit, second);
            return entry <= exit && exit >= 0f && entry <= 1f;
        }
    }
}
