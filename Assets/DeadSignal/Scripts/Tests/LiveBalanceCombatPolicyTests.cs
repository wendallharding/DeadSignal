using DeadSignal.Combat;
using DeadSignal.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace DeadSignal.Tests
{
    public sealed class LiveBalanceCombatPolicyTests
    {
        [Test]
        public void UrgentSapper_IsTargetedAheadOfCloserRoutineThreat()
        {
            var policy = new LiveBalanceCombatPolicy();

            var decision = policy.Tick(
                0.6f, Vector3.zero, 30f, true, true, false, false, false, false, Vector3.zero,
                _threat(SecurityReinforcement.Warden, new Vector3(2f, 0f, 0f)),
                _threat(SecurityReinforcement.Sapper, new Vector3(9f, 0f, 0f), true),
                default,
                default);

            Assert.That(decision.Target, Is.EqualTo(SecurityReinforcement.Sapper));
            Assert.That(decision.ShouldFire, Is.True);
            Assert.That(decision.AimDirection, Is.EqualTo(Vector3.right));
        }

        [Test]
        public void RoutineFire_PreservesCriticalSignalReserve()
        {
            var policy = new LiveBalanceCombatPolicy();

            var decision = policy.Tick(
                0.6f, Vector3.zero, 23f, true, true, false, false, false, false, Vector3.zero,
                _threat(SecurityReinforcement.Warden, new Vector3(4f, 0f, 0f)),
                default,
                default,
                default);

            Assert.That(decision.Target, Is.EqualTo(SecurityReinforcement.Warden));
            Assert.That(decision.ShouldFire, Is.False);
        }

        [Test]
        public void RoutineThreat_DoesNotDistractRouteAtHighSignal()
        {
            var policy = new LiveBalanceCombatPolicy();

            var decision = policy.Tick(
                0.6f, Vector3.zero, 100f, true, true, false, false, false, false, Vector3.zero,
                _threat(SecurityReinforcement.Warden, new Vector3(4f, 0f, 0f)),
                default,
                default,
                default);

            Assert.That(decision.Target, Is.EqualTo(SecurityReinforcement.Warden));
            Assert.That(decision.ShouldFire, Is.False);
        }

        [Test]
        public void InterceptorCharge_ProducesPerpendicularEvasionOncePerWarning()
        {
            var policy = new LiveBalanceCombatPolicy();
            var interceptor = _threat(SecurityReinforcement.Interceptor, new Vector3(3f, 0f, 0f), true);

            var first = policy.Tick(
                0.1f, Vector3.zero, 30f, true, false, false, true, false, false, Vector3.zero,
                default, default, interceptor, default);
            policy.Tick(
                0.1f, Vector3.zero, 30f, true, false, false, true, false, false, Vector3.zero,
                default, default, interceptor, default);

            Assert.That(first.EvasionDirection, Is.EqualTo(Vector2.up));
            Assert.That(policy.EvasionResponses, Is.EqualTo(1));
        }

        [Test]
        public void SuppressionDanger_PullsMovementOutOfField()
        {
            var policy = new LiveBalanceCombatPolicy();

            var decision = policy.Tick(
                0.1f, new Vector3(2f, 0f, 0f), 30f, true, false, false, false, true, false, Vector3.zero,
                default, default, default, _threat(SecurityReinforcement.Suppressor, Vector3.zero, true));
            var movement = LiveBalanceCombatPolicy.BlendMovement(Vector2.up, decision.EvasionDirection);

            Assert.That(decision.EvasionDirection, Is.EqualTo(Vector2.right));
            Assert.That(movement.x, Is.GreaterThan(movement.y));
        }

        [Test]
        public void UrgentThreatOutsideReliableBoltLane_IsTrackedWithoutFiring()
        {
            var policy = new LiveBalanceCombatPolicy();

            var decision = policy.Tick(
                0.6f, Vector3.zero, 100f, true, true, false, false, false, false, Vector3.zero,
                default,
                _threat(SecurityReinforcement.Sapper, new Vector3(12f, 0f, 0f), true),
                default,
                default);

            Assert.That(decision.Target, Is.EqualTo(SecurityReinforcement.Sapper));
            Assert.That(decision.ShouldFire, Is.False);
        }

        [Test]
        public void DistantUrgentSapper_ForcesRouteAbandonmentTarget()
        {
            var policy = new LiveBalanceCombatPolicy();

            var decision = policy.Tick(
                0.6f, Vector3.zero, 100f, true, true, false, false, false, false, Vector3.zero,
                default,
                _threat(SecurityReinforcement.Sapper, new Vector3(30f, 0f, 0f), true),
                default,
                default);

            Assert.That(decision.Target, Is.EqualTo(SecurityReinforcement.Sapper));
            Assert.That(decision.ShouldFire, Is.False);
        }

        private static LiveBalanceThreatSnapshot _threat(
            SecurityReinforcement role, Vector3 position, bool urgent = false)
        {
            return new LiveBalanceThreatSnapshot(role, true, position, urgent);
        }
    }
}
