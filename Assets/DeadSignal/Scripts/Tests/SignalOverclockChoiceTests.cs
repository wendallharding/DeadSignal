using NUnit.Framework;
using UnityEngine;
using DeadSignal.Missions;

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
        public void RelayActivation_QueuesOneWeaponChoiceAlongsideUnresolvedCacheChoice()
        {
            var choice = new SignalOverclockChoice();
            choice.NotifySalvageCollected(1);

            choice.NotifyRelayActivated();

            Assert.That(choice.IsPrimaryPending, Is.True);
            Assert.That(choice.IsWeaponPending, Is.True);
            Assert.That(choice.TrySelect(SignalWeaponOverclock.PiercingPulse), Is.True);
            Assert.That(choice.SelectedWeapon, Is.EqualTo(SignalWeaponOverclock.PiercingPulse));
            Assert.That(choice.IsPrimaryPending, Is.True,
                "Resolving the Relay weapon layer must not discard an unresolved cache choice.");
            choice.NotifyRelayActivated();
            Assert.That(choice.IsWeaponPending, Is.False,
                "The one-time Relay reward must not be offered again after selection.");
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
            var tuning = ScriptableObject.CreateInstance<SignalOverclockTuning>();
            try
            {
                choice.NotifySalvageCollected(1);
                choice.TrySelect(SignalOverclock.OverdriveThrusters);
                choice.NotifySalvageCollected(2);
                choice.TrySelect(SignalAuxiliaryOverclock.FeedbackShield);

                Assert.That(choice.TryAbsorbThreatDamage(tuning), Is.True);
                Assert.That(choice.IsFeedbackShieldCharged, Is.False);
                Assert.That(choice.TryAbsorbThreatDamage(tuning), Is.False);
                Assert.That(choice.NotifyThreatPurged(), Is.True);
                Assert.That(choice.IsFeedbackShieldCharged, Is.True);
                Assert.That(choice.NotifyThreatPurged(), Is.False);
                Assert.That(choice.TryAbsorbThreatDamage(tuning), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        [TestCase(SignalAuxiliaryOverclock.EmergencyCapacitor, SignalOverclockSynergy.ArcOverload)]
        [TestCase(SignalAuxiliaryOverclock.FeedbackShield, SignalOverclockSynergy.ReactiveArc)]
        public void ChainArcPair_PrimesOneDoubleJumpFromItsAuxiliaryTrigger(
            SignalAuxiliaryOverclock auxiliary,
            SignalOverclockSynergy expectedSynergy)
        {
            var choice = new SignalOverclockChoice();
            var model = new RunModel();
            var tuning = ScriptableObject.CreateInstance<SignalOverclockTuning>();
            try
            {
                choice.NotifySalvageCollected(1);
                choice.TrySelect(SignalOverclock.ChainArc);
                choice.NotifySalvageCollected(2);
                choice.TrySelect(auxiliary);

                Assert.That(choice.Synergy, Is.EqualTo(expectedSynergy));
                if (auxiliary == SignalAuxiliaryOverclock.EmergencyCapacitor)
                {
                    model.TrySpend(50f);
                    model.TrySpend(25f);
                    Assert.That(choice.TryTriggerEmergencyCapacitor(model, tuning), Is.GreaterThan(0f));
                }
                else
                {
                    Assert.That(choice.TryAbsorbThreatDamage(tuning), Is.True);
                }

                Assert.That(choice.IsChainArcOverloadReady, Is.True);
                Assert.That(choice.TryConsumeChainArcOverload(), Is.True);
                Assert.That(choice.TryConsumeChainArcOverload(), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        [TestCase(SignalAuxiliaryOverclock.EmergencyCapacitor, SignalOverclockSynergy.CapacitorSurge)]
        [TestCase(SignalAuxiliaryOverclock.FeedbackShield, SignalOverclockSynergy.ShieldSurge)]
        public void OverdrivePair_TriggersOneTunedEscapeSurge(
            SignalAuxiliaryOverclock auxiliary,
            SignalOverclockSynergy expectedSynergy)
        {
            var choice = new SignalOverclockChoice();
            var model = new RunModel();
            var tuning = ScriptableObject.CreateInstance<SignalOverclockTuning>();
            try
            {
                choice.NotifySalvageCollected(1);
                choice.TrySelect(SignalOverclock.OverdriveThrusters);
                choice.NotifySalvageCollected(2);
                choice.TrySelect(auxiliary);

                if (auxiliary == SignalAuxiliaryOverclock.EmergencyCapacitor)
                {
                    model.TrySpend(50f);
                    model.TrySpend(25f);
                    choice.TryTriggerEmergencyCapacitor(model, tuning);
                }
                else
                {
                    choice.TryAbsorbThreatDamage(tuning);
                }

                Assert.That(choice.Synergy, Is.EqualTo(expectedSynergy));
                Assert.That(choice.IsOverdriveSurgeActive, Is.True);
                Assert.That(choice.OverdriveSurgeSecondsRemaining,
                    Is.EqualTo(tuning.OverdriveSynergySurgeDuration).Within(0.001f));
                choice.Tick(tuning.OverdriveSynergySurgeDuration);
                Assert.That(choice.IsOverdriveSurgeActive, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        [Test]
        public void InvalidOrPrematureSelection_DoesNotConsumeChoice()
        {
            var choice = new SignalOverclockChoice();

            Assert.That(choice.TrySelect(SignalOverclock.OverdriveThrusters), Is.False);
            Assert.That(choice.TrySelect(SignalAuxiliaryOverclock.FeedbackShield), Is.False);
            Assert.That(choice.TrySelect(SignalWeaponOverclock.ControlledRicochet), Is.False);
            choice.NotifySalvageCollected(1);
            Assert.That(choice.TrySelect(SignalOverclock.None), Is.False);
            Assert.That(choice.IsPrimaryPending, Is.True);
            Assert.That(choice.Selected, Is.EqualTo(SignalOverclock.None));
        }
    }
}
