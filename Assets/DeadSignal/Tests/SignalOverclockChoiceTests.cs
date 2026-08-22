using NUnit.Framework;

namespace DeadSignal.Tests
{
    public sealed class SignalOverclockChoiceTests
    {
        [Test]
        public void FirstSalvage_OffersExactlyOnePermanentChoice()
        {
            var choice = new SignalOverclockChoice();

            choice.NotifySalvageCollected(1);

            Assert.That(choice.IsPending, Is.True);
            Assert.That(choice.TrySelect(SignalOverclock.ChainArc), Is.True);
            Assert.That(choice.Selected, Is.EqualTo(SignalOverclock.ChainArc));
            Assert.That(choice.IsPending, Is.False);
            choice.NotifySalvageCollected(2);
            Assert.That(choice.IsPending, Is.False);
            Assert.That(choice.TrySelect(SignalOverclock.OverdriveThrusters), Is.False);
            Assert.That(choice.Selected, Is.EqualTo(SignalOverclock.ChainArc));
        }

        [Test]
        public void InvalidOrPrematureSelection_DoesNotConsumeChoice()
        {
            var choice = new SignalOverclockChoice();

            Assert.That(choice.TrySelect(SignalOverclock.OverdriveThrusters), Is.False);
            choice.NotifySalvageCollected(1);
            Assert.That(choice.TrySelect(SignalOverclock.None), Is.False);
            Assert.That(choice.IsPending, Is.True);
            Assert.That(choice.Selected, Is.EqualTo(SignalOverclock.None));
        }
    }
}
