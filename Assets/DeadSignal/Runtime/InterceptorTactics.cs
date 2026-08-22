using UnityEngine;

namespace DeadSignal
{
    /// <summary>
    /// Deterministic positioning rules for the retreat-cutting Interceptor.
    /// </summary>
    public static class InterceptorTactics
    {
        public static int SelectSafestEntrance(Vector3 playerPosition, Vector3 firstEntrance, Vector3 secondEntrance)
        {
            var firstDistance = _flatSqrDistance(playerPosition, firstEntrance);
            var secondDistance = _flatSqrDistance(playerPosition, secondEntrance);
            return secondDistance > firstDistance ? 1 : 0;
        }

        public static Vector3 CalculateCutoffPoint(Vector3 playerPosition, Vector3 extractionPosition, float routeFraction)
        {
            routeFraction = Mathf.Clamp01(routeFraction);
            var cutoff = Vector3.Lerp(playerPosition, extractionPosition, routeFraction);
            cutoff.y = 0f;
            return cutoff;
        }

        public static Vector3 CalculateSuppressionExitPoint(
            Vector3 fieldCenter,
            Vector3 playerPosition,
            Vector3 interceptorPosition,
            float fieldRadius,
            float exitMargin)
        {
            var escapeDirection = playerPosition - fieldCenter;
            escapeDirection.y = 0f;
            if (escapeDirection.sqrMagnitude < 0.01f)
            {
                escapeDirection = fieldCenter - interceptorPosition;
                escapeDirection.y = 0f;
            }

            if (escapeDirection.sqrMagnitude < 0.01f)
            {
                escapeDirection = Vector3.forward;
            }

            var cutoff = fieldCenter +
                         escapeDirection.normalized * (Mathf.Max(0f, fieldRadius) + Mathf.Max(0f, exitMargin));
            cutoff.y = 0f;
            return cutoff;
        }

        private static float _flatSqrDistance(Vector3 first, Vector3 second)
        {
            var delta = first - second;
            delta.y = 0f;
            return delta.sqrMagnitude;
        }
    }
}
