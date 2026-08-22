using NUnit.Framework;
using UnityEngine;

namespace DeadSignal.Tests
{
    public sealed class SignalOverclockChoiceTests
    {
        [Test]
        public void FirstAndSecondSalvage_OfferOneChoiceFromEachBuildLayer()
        {
            var choice = new SignalOverclockChoice();

            choice.NotifySalvageCollected(1);

            Assert.That(choice.IsPrimaryPending, Is.True);
            Assert.That(choice.TrySelect(SignalOverclock.ChainArc), Is.True);
            Assert.That(choice.Selected, Is.EqualTo(SignalOverclock.ChainArc));
            Assert.That(choice.IsPending, Is.False);

            choice.NotifySalvageCollected(2);

            Assert.That(choice.IsAuxiliaryPending, Is.True);
            Assert.That(choice.TrySelect(SignalAuxiliaryOverclock.FeedbackShield), Is.True);
            Assert.That(choice.SelectedAuxiliary, Is.EqualTo(SignalAuxiliaryOverclock.FeedbackShield));
            Assert.That(choice.IsFeedbackShieldCharged, Is.True);
            Assert.That(choice.IsPending, Is.False);
            Assert.That(choice.TrySelect(SignalAuxiliaryOverclock.EmergencyCapacitor), Is.False);
        }

        [Test]
        public void SecondSalvageBeforePrimarySelection_QueuesAuxiliaryChoice()
        {
            var choice = new SignalOverclockChoice();

            choice.NotifySalvageCollected(1);
            choice.NotifySalvageCollected(2);

            Assert.That(choice.IsPrimaryPending, Is.True);
            Assert.That(choice.TrySelect(SignalOverclock.OverdriveThrusters), Is.True);
            Assert.That(choice.IsAuxiliaryPending, Is.True);
        }

        [Test]
        public void EmergencyCapacitor_TriggersOnceAtTunedLowSignalThreshold()
        {
            var choice = new SignalOverclockChoice();
            var model = new RunModel();
            var tuning = ScriptableObject.CreateInstance<SignalOverclockTuning>();
            try
            {
                choice.NotifySalvageCollected(1);
                choice.TrySelect(SignalOverclock.ChainArc);
                choice.NotifySalvageCollected(2);
                choice.TrySelect(SignalAuxiliaryOverclock.EmergencyCapacitor);

                Assert.That(choice.TryTriggerEmergencyCapacitor(model, tuning), Is.Zero);
                Assert.That(model.TrySpend(46f), Is.True);
                Assert.That(choice.TryTriggerEmergencyCapacitor(model, tuning), Is.Zero);
                Assert.That(model.TrySpend(RunModel.SecurityHitCost), Is.True);
                Assert.That(model.Signal, Is.EqualTo(8f));

                Assert.That(choice.TryTriggerEmergencyCapacitor(model, tuning), Is.EqualTo(22f));
                Assert.That(model.Signal, Is.EqualTo(30f));
                Assert.That(choice.IsEmergencyCapacitorAvailable, Is.False);
                Assert.That(choice.TryTriggerEmergencyCapacitor(model, tuning), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        [Test]
        public void FeedbackShield_AbsorbsOnceAndOnlyPurgeRechargesIt()
        {
            var choice = new SignalOverclockChoice();
            choice.NotifySalvageCollected(1);
            choice.TrySelect(SignalOverclock.OverdriveThrusters);
            choice.NotifySalvageCollected(2);
            choice.TrySelect(SignalAuxiliaryOverclock.FeedbackShield);

            Assert.That(choice.TryAbsorbThreatDamage(), Is.True);
            Assert.That(choice.IsFeedbackShieldCharged, Is.False);
            Assert.That(choice.TryAbsorbThreatDamage(), Is.False);
            Assert.That(choice.NotifyThreatPurged(), Is.True);
            Assert.That(choice.IsFeedbackShieldCharged, Is.True);
            Assert.That(choice.NotifyThreatPurged(), Is.False);
            Assert.That(choice.TryAbsorbThreatDamage(), Is.True);
        }

        [Test]
        public void InvalidOrPrematureSelection_DoesNotConsumeChoice()
        {
            var choice = new SignalOverclockChoice();

            Assert.That(choice.TrySelect(SignalOverclock.OverdriveThrusters), Is.False);
            Assert.That(choice.TrySelect(SignalAuxiliaryOverclock.FeedbackShield), Is.False);
            choice.NotifySalvageCollected(1);
            Assert.That(choice.TrySelect(SignalOverclock.None), Is.False);
            Assert.That(choice.IsPrimaryPending, Is.True);
            Assert.That(choice.Selected, Is.EqualTo(SignalOverclock.None));
        }
    }
}
