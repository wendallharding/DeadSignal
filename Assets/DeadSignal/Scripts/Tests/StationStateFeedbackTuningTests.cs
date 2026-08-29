using NUnit.Framework;
using UnityEngine;
using DeadSignal.Presentation;

namespace DeadSignal.Tests
{
    public sealed class StationStateFeedbackTuningTests
    {
        [Test]
        public void Defaults_AreBoundedAndAccessibilitySafe()
        {
            var tuning = ScriptableObject.CreateInstance<StationStateFeedbackTuning>();
            try
            {
                Assert.That(tuning.PoolSize, Is.InRange(2, 8));
                Assert.That(tuning.Duration, Is.LessThanOrEqualTo(1.2f));
                Assert.That(tuning.MaximumAlpha, Is.LessThanOrEqualTo(0.7f));
                Assert.That(tuning.ReducedFlashesMaximumAlpha, Is.LessThanOrEqualTo(0.3f));
                Assert.That(tuning.EndingDiameterMultiplier, Is.GreaterThan(tuning.StartingDiameterMultiplier));
                Assert.That(tuning.AvailableColor.r, Is.GreaterThan(tuning.AvailableColor.b));
                Assert.That(tuning.CompleteColor.b, Is.GreaterThan(tuning.CompleteColor.r));
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }
    }
}
