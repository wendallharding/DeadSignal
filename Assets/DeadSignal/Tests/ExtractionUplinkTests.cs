using NUnit.Framework;

namespace DeadSignal.Tests
{
    public sealed class ExtractionUplinkTests
    {
        [Test]
        public void Tick_CompletesOnceAfterConfiguredDuration()
        {
            var uplink = new ExtractionUplink(8f);

            Assert.That(uplink.Begin(), Is.True);
            Assert.That(uplink.Begin(), Is.False);
            Assert.That(uplink.Tick(3f), Is.False);
            Assert.That(uplink.IsActive, Is.True);
            Assert.That(uplink.SecondsRemaining, Is.EqualTo(5f));
            Assert.That(uplink.Tick(5f), Is.True);
            Assert.That(uplink.IsActive, Is.False);
            Assert.That(uplink.IsComplete, Is.True);
            Assert.That(uplink.Tick(1f), Is.False);
            Assert.That(uplink.Begin(), Is.False);
        }

        [Test]
        public void Constructor_ClampsInvalidDurationToPlayableMinimum()
        {
            var uplink = new ExtractionUplink(0f);

            uplink.Begin();

            Assert.That(uplink.SecondsRemaining, Is.EqualTo(0.1f));
        }
    }
}
