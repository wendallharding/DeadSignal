using NUnit.Framework;
using UnityEngine;

namespace DeadSignal.Tests
{
    public sealed class ProjectileCollisionTests
    {
        [Test]
        public void TryGetOrientedBoxHitFraction_SweepsThroughThinRotatedCover()
        {
            var rotation = Quaternion.Euler(0f, 45f, 0f);
            var right = rotation * Vector3.right;
            var forward = rotation * Vector3.forward;

            var didHit = ProjectileCollision.TryGetOrientedBoxHitFraction(
                new Vector3(-3f, 0f, 0f),
                new Vector3(3f, 0f, 0f),
                Vector2.zero,
                new Vector2(1.2f, 0.12f),
                new Vector2(right.x, right.z),
                new Vector2(forward.x, forward.z),
                0.08f,
                out var hitFraction);

            Assert.That(didHit, Is.True);
            Assert.That(hitFraction, Is.InRange(0f, 1f));
        }

        [Test]
        public void TryGetOrientedBoxHitFraction_LeavesParallelMissClear()
        {
            var didHit = ProjectileCollision.TryGetOrientedBoxHitFraction(
                new Vector3(-3f, 0f, 1f),
                new Vector3(3f, 0f, 1f),
                Vector2.zero,
                new Vector2(1f, 0.2f),
                Vector2.right,
                Vector2.up,
                0.08f,
                out _);

            Assert.That(didHit, Is.False);
        }

        [Test]
        public void TryGetCircleHitFraction_ReturnsNearestEntryAlongSegment()
        {
            var didHit = ProjectileCollision.TryGetCircleHitFraction(
                Vector3.zero,
                Vector3.right * 10f,
                Vector3.right * 6f,
                1f,
                out var hitFraction);

            Assert.That(didHit, Is.True);
            Assert.That(hitFraction, Is.EqualTo(0.5f).Within(0.0001f));
        }
    }
}
