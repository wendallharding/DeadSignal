using NUnit.Framework;

namespace DeadSignal.Tests
{
    public sealed class StandaloneBuildSmokeProbeTests
    {
        [Test]
        public void IsRequested_RecognizesDedicatedArgumentWithoutCaseSensitivity()
        {
            Assert.That(StandaloneBuildSmokeProbe.IsRequested(new[] { "DeadSignal.exe", "-DEADSIGNALBUILDSMOKE" }), Is.True);
        }

        [Test]
        public void IsRequested_IgnoresOrdinaryLaunchAndNullArguments()
        {
            Assert.That(StandaloneBuildSmokeProbe.IsRequested(new[] { "DeadSignal.exe", "-batchmode" }), Is.False);
            Assert.That(StandaloneBuildSmokeProbe.IsRequested(null), Is.False);
        }
    }
}
