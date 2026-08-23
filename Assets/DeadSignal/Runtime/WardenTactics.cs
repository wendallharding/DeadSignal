using UnityEngine;

namespace DeadSignal
{
    public static class WardenTactics
    {
        public static Vector3 CalculateSapperScreenPoint(
            Vector3 playerPosition,
            Vector3 sapperPosition,
            float screenDistance,
            float screenBreakDistance)
        {
            playerPosition.y = 0f;
            sapperPosition.y = 0f;
            var approach = playerPosition - sapperPosition;
            if (approach.sqrMagnitude <= screenBreakDistance * screenBreakDistance)
            {
                return playerPosition;
            }

            if (approach.sqrMagnitude <= 0.0001f)
            {
                return sapperPosition;
            }

            return sapperPosition + approach.normalized * Mathf.Min(screenDistance, approach.magnitude);
        }
    }
}
