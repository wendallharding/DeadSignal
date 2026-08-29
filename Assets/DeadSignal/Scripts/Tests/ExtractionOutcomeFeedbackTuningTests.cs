using NUnit.Framework;
using UnityEngine;
using DeadSignal.Presentation;

namespace DeadSignal.Tests
{
    public sealed class ExtractionOutcomeFeedbackTuningTests
    {
        [Test]
        public void AuthoredTuning_BoundsProgressAndTerminalEffects()
        {
            var tuning = Resources.Load<ExtractionOutcomeFeedbackTuning>("Tuning/ExtractionOutcomeFeedbackTuning");

            Assert.That(tuning, Is.Not.Null);
            Assert.That(tuning.EventPoolSize, Is.EqualTo(2));
            Assert.That(tuning.EventDuration, Is.InRange(0.1f, 1.2f));
            Assert.That(tuning.ReducedFlashesEventMaximumAlpha, Is.LessThanOrEqualTo(0.3f));
            Assert.That(tuning.ReducedFlashesProgressMaximumAlpha,
                Is.LessThanOrEqualTo(tuning.ProgressMaximumAlpha));
            Assert.That(tuning.VictoryDiameter, Is.GreaterThan(tuning.CompletionDiameter));
            Assert.That(tuning.CompletionDiameter, Is.GreaterThan(tuning.ProgressDiameter));
        }
    }
}
