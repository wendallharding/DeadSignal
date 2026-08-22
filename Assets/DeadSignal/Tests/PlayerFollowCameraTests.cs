using NUnit.Framework;
using UnityEngine;

namespace DeadSignal.Tests
{
    public sealed class PlayerFollowCameraTests
    {
        [Test]
        public void CalculateClampedFocus_AsymmetricPerspectiveFootprintPreservesEveryArenaEdge()
        {
            var focus = PlayerFollowCamera.CalculateClampedFocus(
                new Vector3(100f, 3f, -100f),
                new Vector2(13.2f, 8.8f),
                new Vector2(-10f, -4f),
                new Vector2(10f, 8f),
                0.35f);

            Assert.That(focus.x, Is.EqualTo(2.85f).Within(0.0001f));
            Assert.That(focus.y, Is.Zero);
            Assert.That(focus.z, Is.EqualTo(-4.45f).Within(0.0001f));
        }

        [Test]
        public void CalculateClampedFocus_OversizedViewLocksAxisToArenaCenter()
        {
            var focus = PlayerFollowCamera.CalculateClampedFocus(
                new Vector3(8f, 0f, 6f),
                new Vector2(7f, 4f),
                new Vector2(-8f, -5f),
                new Vector2(8f, 5f),
                0.25f);

            Assert.That(focus, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void PlayerCameraTuning_HasSafeAuthoredDefaults()
        {
            var tuning = Resources.Load<PlayerCameraTuning>("Tuning/PlayerCameraTuning");

            Assert.That(tuning, Is.Not.Null);
            Assert.That(tuning.FieldOfView, Is.InRange(35f, 45f));
            Assert.That(tuning.Pitch, Is.InRange(50f, 65f));
            Assert.That(tuning.Height, Is.InRange(8f, 16f));
            Assert.That(tuning.FollowDistance, Is.InRange(4f, 12f));
            Assert.That(tuning.FollowSharpness, Is.GreaterThan(0f));
            Assert.That(tuning.LookAheadDistance, Is.InRange(0f, 2f));
            Assert.That(tuning.LookAheadSharpness, Is.GreaterThan(0f));
            Assert.That(tuning.ArenaEdgePadding, Is.GreaterThanOrEqualTo(0f));
        }
    }
}
