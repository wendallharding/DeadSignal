using NUnit.Framework;
using UnityEngine;
using DeadSignal.Presentation;

namespace DeadSignal.Tests
{
    public sealed class ProductShellTransitionTuningTests
    {
        [Test]
        public void Duration_ReducedFlashesUsesSlowerLuminanceChange()
        {
            var tuning = ScriptableObject.CreateInstance<ProductShellTransitionTuning>();
            try
            {
                Assert.That(tuning.StandardDuration, Is.InRange(0.05f, 0.3f));
                Assert.That(tuning.ReducedFlashesDuration, Is.GreaterThanOrEqualTo(tuning.StandardDuration));
                Assert.That(tuning.Duration(false), Is.EqualTo(tuning.StandardDuration));
                Assert.That(tuning.Duration(true), Is.EqualTo(tuning.ReducedFlashesDuration));
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }
    }
}
