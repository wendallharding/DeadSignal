using NUnit.Framework;
using DeadSignal.Presentation;

namespace DeadSignal.Tests
{
    public sealed class LowSignalWarningControllerTests
    {
        [Test]
        public void CalculateIntensity_StaysHiddenAboveThresholdAndStrengthensNearFailure()
        {
            float safe = LowSignalWarningController.CalculateIntensity(
                LowSignalWarningController.WarningThreshold,
                false,
                1f);
            float warning = LowSignalWarningController.CalculateIntensity(15f, false, 1f);
            float critical = LowSignalWarningController.CalculateIntensity(0f, false, 1f);

            Assert.That(safe, Is.Zero);
            Assert.That(warning, Is.GreaterThan(0f));
            Assert.That(critical, Is.GreaterThan(warning));
        }

        [Test]
        public void CalculateIntensity_ReducedFlashesRemovesPulseAndCapsOpacity()
        {
            float lowPhase = LowSignalWarningController.CalculateIntensity(0f, true, 0f);
            float highPhase = LowSignalWarningController.CalculateIntensity(0f, true, 1f);

            Assert.That(lowPhase, Is.EqualTo(highPhase));
            Assert.That(highPhase, Is.LessThanOrEqualTo(0.16f));
        }
    }
}
