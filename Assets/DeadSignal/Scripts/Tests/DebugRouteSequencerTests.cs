using NUnit.Framework;
using UnityEngine;
using DeadSignal.Diagnostics;

namespace DeadSignal.Tests
{
    public sealed class DebugRouteSequencerTests
    {
        [Test]
        public void OpeningRoute_AdvancesThroughArrivalActionAndVerification()
        {
            var sequencer = new DebugRouteSequencer();
            sequencer.Start(DebugRoutePreset.OpeningLoop, DebugAutomationMode.DeterministicValidation,
                DebugAutomationProfile.SafeNavigation, 72f);

            Assert.That(sequencer.State, Is.EqualTo(DebugRouteRunState.Navigating));
            Assert.That(sequencer.StepCount, Is.EqualTo(2));
            Assert.That(sequencer.TickNavigation(1.9f, 0.1f, false), Is.True);
            Assert.That(sequencer.ShouldIssueAction(), Is.True);
            Assert.That(sequencer.ShouldIssueAction(), Is.False);

            sequencer.TickVerification(0.1f, true, true, "tower online", 62f);

            Assert.That(sequencer.State, Is.EqualTo(DebugRouteRunState.Navigating));
            Assert.That(sequencer.StepNumber, Is.EqualTo(2));
            Assert.That(sequencer.Report, Does.Contain("ARRIVE"));
            Assert.That(sequencer.Report, Does.Contain("PASS"));
        }

        [Test]
        public void BlockedRoute_RecoversThenFailsWithDiagnostic()
        {
            var sequencer = new DebugRouteSequencer();
            sequencer.Start(DebugRoutePreset.OpeningLoop, DebugAutomationMode.AssistedPlaythrough,
                DebugAutomationProfile.LiveBalance, 72f);

            for (var recovery = 0; recovery < 5; recovery++)
            {
                sequencer.TickNavigation(10f, 2.1f, true);
            }

            Assert.That(sequencer.State, Is.EqualTo(DebugRouteRunState.Failed));
            Assert.That(sequencer.Report, Does.Contain("Route stalled"));
        }

        [Test]
        public void RecordedRoute_UsesExactRecordedNodes()
        {
            var sequencer = new DebugRouteSequencer();
            sequencer.Record(new Vector3(2f, 0f, 3f));
            sequencer.Record(new Vector3(5f, 0f, 7f));
            sequencer.Start(DebugRoutePreset.RecordedRoute, DebugAutomationMode.DeterministicValidation,
                DebugAutomationProfile.SafeNavigation, 72f);

            Assert.That(sequencer.StepCount, Is.EqualTo(2));
            Assert.That(sequencer.CurrentStep.UsesCustomPosition, Is.True);
            Assert.That(sequencer.CurrentStep.CustomPosition, Is.EqualTo(new Vector3(2f, 0f, 3f)));
        }
    }
}
