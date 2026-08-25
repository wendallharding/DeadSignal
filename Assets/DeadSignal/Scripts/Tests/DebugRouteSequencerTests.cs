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
        public void CompleteNavMeshDetour_DoesNotFalseStallWhileStraightLineDistanceIncreases()
        {
            var sequencer = new DebugRouteSequencer();
            sequencer.Start(DebugRoutePreset.OpeningLoop, DebugAutomationMode.DeterministicValidation,
                DebugAutomationProfile.SafeNavigation, 72f);

            for (var segment = 0; segment < 5; segment++)
            {
                sequencer.TickNavigation(10f + segment, 2.1f, false, true);
            }

            Assert.That(sequencer.State, Is.EqualTo(DebugRouteRunState.Navigating));
            Assert.That(sequencer.RecoveryCount, Is.Zero);
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

        [Test]
        public void FullExtractionRoute_CoversThreeTowersWeaponEvolutionAndOptionalCache()
        {
            var sequencer = new DebugRouteSequencer();
            sequencer.Start(DebugRoutePreset.FullExtraction, DebugAutomationMode.DeterministicValidation,
                DebugAutomationProfile.SafeNavigation, 72f);

            Assert.That(sequencer.StepCount, Is.EqualTo(9));
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.ActivateCentralTower));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.CollectCache));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.ActivateRelayTower));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.SelectWeaponOverclock));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.CollectCache));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.ActivateSpineTower));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.CurrentObjective));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.CacheFour));
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.CollectCache));
        }

        [Test]
        public void RequiredExtractionRoute_MatchesFullJourneyWithoutOptionalCacheCommitment()
        {
            var sequencer = new DebugRouteSequencer();
            sequencer.Start(DebugRoutePreset.RequiredExtraction, DebugAutomationMode.DeterministicValidation,
                DebugAutomationProfile.SafeNavigation, 72f);

            Assert.That(sequencer.StepCount, Is.EqualTo(9));
            for (var step = 0; step < 7; step++)
            {
                Assert.That(sequencer.CurrentStep.Location, Is.Not.EqualTo(DebugLocation.CacheFour));
                _completeCurrentStep(sequencer);
            }

            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.SpineTower));
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.CaptureScreenshot));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.Extraction));
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.BeginStableExtraction));
        }

        [Test]
        public void FinishReport_RecordsComparableJourneyCombatAndSignalEvidence()
        {
            var sequencer = new DebugRouteSequencer();
            var metrics = new DeadSignal.Missions.RunMetrics();
            sequencer.Start(DebugRoutePreset.RequiredExtraction, DebugAutomationMode.AssistedPlaythrough,
                DebugAutomationProfile.LiveBalance, 72f);
            metrics.Advance(12f, false);
            metrics.RecordShot();
            metrics.RecordSecurityHit();
            metrics.RecordThreatPurge(14f);

            var report = sequencer.FinishReport(41f, metrics, false, true, new Vector3(2f, 0f, 3f));

            Assert.That(report, Does.Contain("Journey REQUIRED WITHDRAWAL"));
            Assert.That(report, Does.Contain("Time 12.00s"));
            Assert.That(report, Does.Contain("Hits 1"));
            Assert.That(report, Does.Contain("Shots 1"));
            Assert.That(report, Does.Contain("Recovered 14.0"));
        }

        private static void _completeCurrentStep(DebugRouteSequencer sequencer)
        {
            Assert.That(sequencer.TickNavigation(0f, 0.1f, false), Is.True);
            Assert.That(sequencer.ShouldIssueAction(), Is.True);
            sequencer.TickVerification(0.1f, true, true, "verified", 72f);
        }
    }
}
