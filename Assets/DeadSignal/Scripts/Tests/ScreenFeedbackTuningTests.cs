using NUnit.Framework;
using UnityEngine;
using DeadSignal.Presentation;

namespace DeadSignal.Tests
{
    public sealed class ScreenFeedbackTuningTests
    {
        [Test]
        public void Defaults_KeepScreenFeedbackRestrainedAndAccessibilitySafe()
        {
            var tuning = ScriptableObject.CreateInstance<ScreenFeedbackTuning>();
            try
            {
                Assert.That(tuning.WarningThreshold, Is.EqualTo(0.3f));
                Assert.That(tuning.CriticalThreshold, Is.EqualTo(0.25f));
                Assert.That(tuning.CriticalMaximumAlpha, Is.LessThanOrEqualTo(0.2f));
                Assert.That(tuning.DirectionalDuration, Is.LessThanOrEqualTo(0.6f));
                Assert.That(tuning.DirectionalMaximumAlpha, Is.LessThanOrEqualTo(0.5f));
                Assert.That(tuning.ReducedFlashesDirectionalAlpha,
                    Is.LessThan(tuning.DirectionalMaximumAlpha));
                Assert.That(tuning.HorizontalAnchorRadius, Is.LessThan(0.45f));
                Assert.That(tuning.VerticalAnchorRadius, Is.LessThan(0.45f));
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        [Test]
        public void CalculateScreenDirection_UsesSourceSideAndHandlesBehindCamera()
        {
            var right = DirectionalDamageFeedbackController.CalculateScreenDirection(
                new Vector3(0.8f, 0.5f, 1f), new Vector3(0.5f, 0.5f, 1f));
            var behind = DirectionalDamageFeedbackController.CalculateScreenDirection(
                new Vector3(0.8f, 0.5f, -1f), new Vector3(0.5f, 0.5f, 1f));

            Assert.That(right.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(right.y, Is.Zero.Within(0.001f));
            Assert.That(behind.x, Is.EqualTo(-1f).Within(0.001f));
        }
    }
}
