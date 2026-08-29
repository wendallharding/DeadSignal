using NUnit.Framework;
using UnityEngine;
using DeadSignal.Combat;

namespace DeadSignal.Tests
{
    public sealed class CombatFeedbackTuningTests
    {
        [Test]
        public void Defaults_DefineBoundedPrewarmedPresentationPools()
        {
            var tuning = ScriptableObject.CreateInstance<CombatFeedbackTuning>();
            try
            {
                Assert.That(tuning.ImpactPrewarmCount, Is.EqualTo(12));
                Assert.That(tuning.ImpactMaximumCount, Is.EqualTo(16));
                Assert.That(tuning.SparkPrewarmCount, Is.EqualTo(12));
                Assert.That(tuning.SparkMaximumCount, Is.EqualTo(16));
                Assert.That(tuning.ChainPrewarmCount, Is.EqualTo(6));
                Assert.That(tuning.ChainMaximumCount, Is.EqualTo(8));
                Assert.That(tuning.ImpactDuration, Is.InRange(0.05f, 1.2f));
                Assert.That(tuning.SparkDuration, Is.InRange(0.05f, 1.2f));
                Assert.That(tuning.ChainDuration, Is.InRange(0.05f, 1.2f));
                Assert.That(tuning.ReducedFlashesMaximumAlpha, Is.LessThanOrEqualTo(0.3f));
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }
    }
}
