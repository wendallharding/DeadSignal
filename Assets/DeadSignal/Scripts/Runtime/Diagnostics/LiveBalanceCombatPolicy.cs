using DeadSignal.Combat;
using UnityEngine;

namespace DeadSignal.Diagnostics
{
    public readonly struct LiveBalanceThreatSnapshot
    {
        public LiveBalanceThreatSnapshot(SecurityReinforcement role, bool active, Vector3 position, bool urgent = false)
        {
            Role = role;
            Active = active;
            Position = position;
            Urgent = urgent;
        }

        public SecurityReinforcement Role { get; }
        public bool Active { get; }
        public Vector3 Position { get; }
        public bool Urgent { get; }
    }

    public readonly struct LiveBalanceCombatDecision
    {
        public LiveBalanceCombatDecision(
            SecurityReinforcement target, Vector3 aimDirection, Vector2 evasionDirection, bool shouldFire)
        {
            Target = target;
            AimDirection = aimDirection;
            EvasionDirection = evasionDirection;
            ShouldFire = shouldFire;
        }

        public SecurityReinforcement Target { get; }
        public Vector3 AimDirection { get; }
        public Vector2 EvasionDirection { get; }
        public bool ShouldFire { get; }
    }

    /// <summary>
    /// Drives a deliberately conservative combat response for assisted balance routes. It spends only when a threat is
    /// in a plausible bolt lane and reacts to the two attacks with explicit positional counterplay.
    /// </summary>
    public sealed class LiveBalanceCombatPolicy
    {
        private const float MAXIMUM_ENGAGEMENT_DISTANCE = 18f;
        private const float MAXIMUM_FIRE_DISTANCE = 11f;
        private const float MINIMUM_COUNTERATTACK_SIGNAL = 10f;
        private const float SHOT_CADENCE = 0.52f;
        private const float INTERCEPTOR_EVASION_DISTANCE = 5.5f;

        private float m_shotCooldown;
        private bool m_wasEvading;

        public int EvasionResponses { get; private set; }
        public int DirectedShots { get; private set; }

        public void Reset()
        {
            m_shotCooldown = 0f;
            m_wasEvading = false;
            EvasionResponses = 0;
            DirectedShots = 0;
        }

        public LiveBalanceCombatDecision Tick(
            float dt,
            Vector3 playerPosition,
            float signal,
            bool combatAwake,
            bool canFire,
            bool hasActiveBolt,
            bool interceptorCharging,
            bool suppressorWarning,
            bool playerSuppressed,
            Vector3 suppressorFieldCenter,
            LiveBalanceThreatSnapshot warden,
            LiveBalanceThreatSnapshot sapper,
            LiveBalanceThreatSnapshot interceptor,
            LiveBalanceThreatSnapshot suppressor)
        {
            m_shotCooldown = Mathf.Max(0f, m_shotCooldown - Mathf.Max(0f, dt));
            if (!combatAwake)
            {
                m_wasEvading = false;
                return default;
            }

            var target = _selectTarget(playerPosition, warden, sapper, interceptor, suppressor);
            var targetPosition = _positionFor(target, warden, sapper, interceptor, suppressor);
            var aimDirection = target == SecurityReinforcement.None
                ? Vector3.zero
                : _flatDirection(playerPosition, targetPosition);
            var evasionDirection = _evasionDirection(
                playerPosition,
                interceptorCharging,
                interceptor.Position,
                suppressorWarning || playerSuppressed,
                suppressorFieldCenter);
            var isEvading = evasionDirection.sqrMagnitude > 0.01f;
            if (isEvading && !m_wasEvading)
            {
                EvasionResponses++;
            }
            m_wasEvading = isEvading;

            var urgentTarget = target != SecurityReinforcement.None &&
                               _snapshotFor(target, warden, sapper, interceptor, suppressor).Urgent;
            var targetDistance = target == SecurityReinforcement.None
                ? float.PositiveInfinity
                : _flatDistance(playerPosition, targetPosition);
            var hasReserve = urgentTarget && signal >= MINIMUM_COUNTERATTACK_SIGNAL;
            var shouldFire = target != SecurityReinforcement.None && canFire && !hasActiveBolt &&
                             m_shotCooldown <= 0f && hasReserve && targetDistance <= MAXIMUM_FIRE_DISTANCE;
            if (shouldFire)
            {
                m_shotCooldown = SHOT_CADENCE;
                DirectedShots++;
            }

            return new LiveBalanceCombatDecision(target, aimDirection, evasionDirection, shouldFire);
        }

        public static Vector2 BlendMovement(Vector2 routeDirection, Vector2 evasionDirection)
        {
            if (evasionDirection.sqrMagnitude <= 0.01f)
            {
                return routeDirection;
            }

            var blended = routeDirection * 0.35f + evasionDirection.normalized;
            return blended.sqrMagnitude > 1f ? blended.normalized : blended;
        }

        private static SecurityReinforcement _selectTarget(
            Vector3 playerPosition,
            LiveBalanceThreatSnapshot warden,
            LiveBalanceThreatSnapshot sapper,
            LiveBalanceThreatSnapshot interceptor,
            LiveBalanceThreatSnapshot suppressor)
        {
            var target = SecurityReinforcement.None;
            var bestScore = float.PositiveInfinity;
            _consider(sapper, playerPosition, 0f, ref target, ref bestScore);
            _consider(suppressor, playerPosition, 2f, ref target, ref bestScore);
            _consider(interceptor, playerPosition, 4f, ref target, ref bestScore);
            _consider(warden, playerPosition, 6f, ref target, ref bestScore);
            return target;
        }

        private static void _consider(
            LiveBalanceThreatSnapshot threat,
            Vector3 playerPosition,
            float roleBias,
            ref SecurityReinforcement target,
            ref float bestScore)
        {
            if (!threat.Active)
            {
                return;
            }

            var distance = _flatDistance(playerPosition, threat.Position);
            var isGlobalSapperEmergency = threat.Role == SecurityReinforcement.Sapper && threat.Urgent;
            if (distance > MAXIMUM_ENGAGEMENT_DISTANCE && !isGlobalSapperEmergency)
            {
                return;
            }

            var score = distance + roleBias - (threat.Urgent ? 20f : 0f);
            if (score >= bestScore)
            {
                return;
            }

            target = threat.Role;
            bestScore = score;
        }

        private static Vector2 _evasionDirection(
            Vector3 playerPosition,
            bool interceptorCharging,
            Vector3 interceptorPosition,
            bool suppressorDanger,
            Vector3 suppressorFieldCenter)
        {
            if (suppressorDanger)
            {
                var away = _flatDirection(suppressorFieldCenter, playerPosition);
                if (away.sqrMagnitude > 0.01f)
                {
                    return new Vector2(away.x, away.z);
                }
            }

            var interceptorDelta = interceptorPosition - playerPosition;
            interceptorDelta.y = 0f;
            if (!interceptorCharging || interceptorDelta.sqrMagnitude >
                INTERCEPTOR_EVASION_DISTANCE * INTERCEPTOR_EVASION_DISTANCE)
            {
                return Vector2.zero;
            }

            interceptorDelta.Normalize();
            return new Vector2(-interceptorDelta.z, interceptorDelta.x);
        }

        private static LiveBalanceThreatSnapshot _snapshotFor(
            SecurityReinforcement target,
            LiveBalanceThreatSnapshot warden,
            LiveBalanceThreatSnapshot sapper,
            LiveBalanceThreatSnapshot interceptor,
            LiveBalanceThreatSnapshot suppressor)
        {
            return target switch
            {
                SecurityReinforcement.Warden => warden,
                SecurityReinforcement.Sapper => sapper,
                SecurityReinforcement.Interceptor => interceptor,
                SecurityReinforcement.Suppressor => suppressor,
                _ => default
            };
        }

        private static Vector3 _positionFor(
            SecurityReinforcement target,
            LiveBalanceThreatSnapshot warden,
            LiveBalanceThreatSnapshot sapper,
            LiveBalanceThreatSnapshot interceptor,
            LiveBalanceThreatSnapshot suppressor)
        {
            return _snapshotFor(target, warden, sapper, interceptor, suppressor).Position;
        }

        private static Vector3 _flatDirection(Vector3 origin, Vector3 target)
        {
            var direction = target - origin;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.zero;
        }

        private static float _flatDistance(Vector3 a, Vector3 b)
        {
            var delta = b - a;
            delta.y = 0f;
            return delta.magnitude;
        }
    }
}
