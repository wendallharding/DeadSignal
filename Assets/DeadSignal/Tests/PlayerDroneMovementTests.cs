using NUnit.Framework;
using UnityEngine;

namespace DeadSignal.Tests
{
    public sealed class PlayerDroneMovementTests
    {
        [Test]
        public void MovementTuning_HasResponsiveAuthoredDefaults()
        {
            var tuning = Resources.Load<PlayerDroneMovementTuning>("Tuning/PlayerDroneMovementTuning");

            Assert.That(tuning, Is.Not.Null);
            Assert.That(tuning.MaximumSpeed, Is.EqualTo(6.4f).Within(0.001f));
            Assert.That(tuning.MaximumSpeed / tuning.Acceleration, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(tuning.MaximumSpeed / tuning.Braking, Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(tuning.ReversalAccelerationMultiplier, Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(tuning.MaximumBankDegrees, Is.InRange(8f, 12f));
        }

        [Test]
        public void Tick_AcceleratesAndBrakesOverMultipleUpdates()
        {
            var tuning = Resources.Load<PlayerDroneMovementTuning>("Tuning/PlayerDroneMovementTuning");
            var movement = new PlayerDroneMovement();

            var firstVelocity = movement.Tick(Vector2.up, 0.05f, tuning);
            Assert.That(firstVelocity.z, Is.GreaterThan(0f).And.LessThan(tuning.MaximumSpeed));

            for (var index = 0; index < 3; index++)
            {
                movement.Tick(Vector2.up, 0.05f, tuning);
            }

            Assert.That(movement.Velocity.z, Is.EqualTo(tuning.MaximumSpeed).Within(0.001f));

            var firstBrake = movement.Tick(Vector2.zero, 0.05f, tuning);
            Assert.That(firstBrake.z, Is.GreaterThan(0f).And.LessThan(tuning.MaximumSpeed));

            for (var index = 0; index < 5; index++)
            {
                movement.Tick(Vector2.zero, 0.05f, tuning);
            }

            Assert.That(movement.Velocity.sqrMagnitude, Is.LessThan(0.000001f));
        }

        [Test]
        public void ApplyResolvedVelocity_PreservesCollisionSlideAndRemovesBlockedMotion()
        {
            var movement = new PlayerDroneMovement();

            movement.ApplyResolvedVelocity(new Vector3(3f, 2f, 0f));

            Assert.That(movement.Velocity, Is.EqualTo(new Vector3(3f, 0f, 0f)));
        }

        [Test]
        public void Tick_OverdriveMultipliersIncreaseSpeedAndResponse()
        {
            var tuning = Resources.Load<PlayerDroneMovementTuning>("Tuning/PlayerDroneMovementTuning");
            var baseline = new PlayerDroneMovement();
            var overdrive = new PlayerDroneMovement();

            var baselineVelocity = baseline.Tick(Vector2.up, 0.05f, tuning);
            var overdriveVelocity = overdrive.Tick(Vector2.up, 0.05f, tuning, 1.25f, 1.2f);

            Assert.That(overdriveVelocity.z, Is.GreaterThan(baselineVelocity.z));
            for (var index = 0; index < 10; index++)
            {
                baseline.Tick(Vector2.up, 0.05f, tuning);
                overdrive.Tick(Vector2.up, 0.05f, tuning, 1.25f, 1.2f);
            }

            Assert.That(baseline.Velocity.z, Is.EqualTo(tuning.MaximumSpeed).Within(0.001f));
            Assert.That(overdrive.Velocity.z, Is.EqualTo(tuning.MaximumSpeed * 1.25f).Within(0.001f));
        }
    }
}
