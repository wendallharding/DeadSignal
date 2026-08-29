using DeadSignal.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace DeadSignal.Tests
{
    public sealed class WeaponTransformationFeedbackTuningTests
    {
        [Test]
        public void Defaults_DefineBoundedAccessibleTransformationFeedback()
        {
            var tuning = ScriptableObject.CreateInstance<WeaponTransformationFeedbackTuning>();
            try
            {
                Assert.That(tuning.PoolSize, Is.EqualTo(2));
                Assert.That(tuning.Duration, Is.InRange(0.1f, 1.2f));
                Assert.That(tuning.ReducedFlashesMaximumAlpha, Is.LessThanOrEqualTo(0.3f));
                Assert.That(tuning.EvolutionDiameter, Is.GreaterThan(tuning.TransformationDiameter));
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }
    }
}
