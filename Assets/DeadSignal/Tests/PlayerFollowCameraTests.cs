using NUnit.Framework;
using UnityEngine;

namespace DeadSignal.Tests
{
    public sealed class PlayerFollowCameraTests
    {
        [Test]
        public void CalculateClampedFocus_WideViewPreservesEveryArenaEdge()
        {
            var focus = PlayerFollowCamera.CalculateClampedFocus(
                new Vector3(100f, 3f, -100f),
                new Vector2(13.2f, 8.8f),
                5.8f,
                16f / 9f,
                0.35f);

            Assert.That(focus.x, Is.EqualTo(2.538889f).Within(0.0001f));
            Assert.That(focus.y, Is.Zero);
            Assert.That(focus.z, Is.EqualTo(-2.65f).Within(0.0001f));
        }

        [Test]
        public void CalculateClampedFocus_OversizedViewLocksAxisToArenaCenter()
        {
            var focus = PlayerFollowCamera.CalculateClampedFocus(
                new Vector3(8f, 0f, 6f),
                new Vector2(7f, 4f),
                5f,
                2f,
                0.25f);

            Assert.That(focus, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void PlayerCameraTuning_HasSafeAuthoredDefaults()
        {
            var tuning = Resources.Load<PlayerCameraTuning>("Tuning/PlayerCameraTuning");

            Assert.That(tuning, Is.Not.Null);
            Assert.That(tuning.OrthographicSize, Is.InRange(4f, 7f));
            Assert.That(tuning.FollowSharpness, Is.GreaterThan(0f));
            Assert.That(tuning.LookAheadDistance, Is.InRange(0f, 2f));
            Assert.That(tuning.LookAheadSharpness, Is.GreaterThan(0f));
            Assert.That(tuning.ArenaEdgePadding, Is.GreaterThanOrEqualTo(0f));
        }
    }
}
