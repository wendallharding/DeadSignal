using NUnit.Framework;

namespace DeadSignal.Tests
{
    public sealed class ExtractionUplinkTests
    {
        [Test]
        public void Tick_CompletesOnceAfterConfiguredDuration()
        {
            var uplink = new ExtractionUplink(8f, 5f, 12f);

            Assert.That(uplink.Begin(ExtractionUplinkMode.Stable), Is.True);
            Assert.That(uplink.Mode, Is.EqualTo(ExtractionUplinkMode.Stable));
            Assert.That(uplink.Begin(ExtractionUplinkMode.Overdrive), Is.False);
            Assert.That(uplink.Tick(3f), Is.False);
            Assert.That(uplink.IsActive, Is.True);
            Assert.That(uplink.SecondsRemaining, Is.EqualTo(5f));
            Assert.That(uplink.Tick(5f), Is.True);
            Assert.That(uplink.IsActive, Is.False);
            Assert.That(uplink.IsComplete, Is.True);
            Assert.That(uplink.Tick(1f), Is.False);
            Assert.That(uplink.Begin(ExtractionUplinkMode.Stable), Is.False);
        }

        [Test]
        public void Constructor_ClampsInvalidDurationToPlayableMinimum()
        {
            var uplink = new ExtractionUplink(0f, -1f, -4f);

            uplink.Begin(ExtractionUplinkMode.Overdrive);

            Assert.That(uplink.SecondsRemaining, Is.EqualTo(0.1f));
            Assert.That(uplink.OverdriveSignalCost, Is.Zero);
        }

        [Test]
        public void Begin_SelectsOneConfiguredModeAndRejectsNone()
        {
            var stable = new ExtractionUplink(6f, 4.75f, 12f);

            Assert.That(stable.Begin(ExtractionUplinkMode.None), Is.False);
            Assert.That(stable.Begin(ExtractionUplinkMode.Stable), Is.True);
            Assert.That(stable.SecondsRemaining, Is.EqualTo(6f));

            var overdrive = new ExtractionUplink(6f, 4.75f, 12f);
            Assert.That(overdrive.Begin(ExtractionUplinkMode.Overdrive), Is.True);
            Assert.That(overdrive.Mode, Is.EqualTo(ExtractionUplinkMode.Overdrive));
            Assert.That(overdrive.SecondsRemaining, Is.EqualTo(4.75f));
            Assert.That(overdrive.OverdriveSignalCost, Is.EqualTo(12f));
        }

        [Test]
        public void CanAffordOverdrive_PreservesOnePositiveSignalReserve()
        {
            var uplink = new ExtractionUplink(6f, 4.75f, 12f);

            Assert.That(uplink.CanAffordOverdrive(11.99f), Is.False);
            Assert.That(uplink.CanAffordOverdrive(12f), Is.False);
            Assert.That(uplink.CanAffordOverdrive(12.01f), Is.True);
        }

        [Test]
        public void Accelerate_OnlyCreditsActiveUplinkAndCapsAtRemainingTime()
        {
            var uplink = new ExtractionUplink(6f, 4.75f, 12f);

            Assert.That(uplink.Accelerate(0.75f), Is.Zero);
            uplink.Begin(ExtractionUplinkMode.Stable);

            Assert.That(uplink.Accelerate(0.75f), Is.EqualTo(0.75f));
            Assert.That(uplink.SecondsRemaining, Is.EqualTo(5.25f));
            Assert.That(uplink.Accelerate(8f), Is.EqualTo(5.25f));
            Assert.That(uplink.SecondsRemaining, Is.Zero);
            Assert.That(uplink.IsComplete, Is.True);
            Assert.That(uplink.Accelerate(0.75f), Is.Zero);
        }
    }
}
