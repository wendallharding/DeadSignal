using System.Linq;
using NUnit.Framework;
using UnityEngine;
using DeadSignal.Diagnostics;
using DeadSignal.Missions;

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

            Assert.That(sequencer.StepCount, Is.EqualTo(24));
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.ActivateCentralTower));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.CollectCache));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.CollectCache));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.RouteCentralComponents));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.AssembleCentralPayload));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.InstallCentralPayload));
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.CentralTower));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.ActivateRelayTower));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.CollectCache));
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.CurrentObjective));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.SelectWeaponOverclock));
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.RelayTower));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.CaptureScreenshot));
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.SpineTower));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.VentSpineBerth));
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.CurrentObjective));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.ActivateSpineTower));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.ChargeInductionLattice));
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.CurrentObjective));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.RouteFluxShunt));
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.CurrentObjective));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.CompleteConvergenceCalibration));
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.CurrentObjective));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.ResetBreakerDistribution));
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.CurrentObjective));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.ForgeLattice));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.StabilizeCore));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.CurrentObjective));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.CacheFour));
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.CollectCache));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.SpineTower));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.RelayTower));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.CentralTower));
        }

        [Test]
        public void RequiredExtractionRoute_MatchesFullJourneyWithoutOptionalCacheCommitment()
        {
            var sequencer = new DebugRouteSequencer();
            sequencer.Start(DebugRoutePreset.RequiredExtraction, DebugAutomationMode.DeterministicValidation,
                DebugAutomationProfile.SafeNavigation, 72f);

            Assert.That(sequencer.StepCount, Is.EqualTo(23));
            for (var step = 0; step < 19; step++)
            {
                Assert.That(sequencer.CurrentStep.Location, Is.Not.EqualTo(DebugLocation.CacheFour));
                _completeCurrentStep(sequencer);
            }

            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.SpineTower));
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.CaptureScreenshot));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.RelayTower));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.CentralTower));
            _completeCurrentStep(sequencer);
            Assert.That(sequencer.CurrentStep.Location, Is.EqualTo(DebugLocation.Extraction));
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.BeginStableExtraction));
        }

        [Test]
        public void RequiredExtractionRoute_RelayNetworkActUsesOneInstallationReturnAndDistinctConsequences()
        {
            var sequencer = new DebugRouteSequencer();
            sequencer.Start(DebugRoutePreset.RequiredExtraction, DebugAutomationMode.DeterministicValidation,
                DebugAutomationProfile.SafeNavigation, 72f);

            while (sequencer.CurrentStep.Action != DebugRouteAction.ActivateRelayTower)
            {
                _completeCurrentStep(sequencer);
            }

            var relayActRooms = new string[6];
            var relayActActions = new DebugRouteAction[6];
            var installationReturns = 0;
            for (var step = 0; step < relayActRooms.Length; step++)
            {
                relayActRooms[step] = sequencer.CurrentStep.RoomName;
                relayActActions[step] = sequencer.CurrentStep.Action;
                if (sequencer.CurrentStep.IsBacktrack)
                {
                    installationReturns++;
                }

                _completeCurrentStep(sequencer);
            }

            Assert.That(relayActRooms, Is.EqualTo(new[]
            {
                "Relay Foundry",
                "Cooling Gantry",
                "Relay Foundry",
                "Capacitor Spine",
                "Spine Discharge Trench",
                "Capacitor Spine"
            }));
            Assert.That(relayActActions, Is.EqualTo(new[]
            {
                DebugRouteAction.ActivateRelayTower,
                DebugRouteAction.CollectCache,
                DebugRouteAction.SelectWeaponOverclock,
                DebugRouteAction.CaptureScreenshot,
                DebugRouteAction.VentSpineBerth,
                DebugRouteAction.ActivateSpineTower
            }));
            Assert.That(installationReturns, Is.EqualTo(1),
                "Act II should require only the Gantry-to-Foundry installation return.");
            Assert.That(sequencer.CurrentStep.Action, Is.EqualTo(DebugRouteAction.ChargeInductionLattice),
                "The accepted Relay network act should hand off to Induction charging.");
        }

        [Test]
        public void FinishReport_RecordsComparableJourneyCombatAndSignalEvidence()
        {
            var sequencer = new DebugRouteSequencer();
            var metrics = new DeadSignal.Missions.RunMetrics();
            sequencer.Start(DebugRoutePreset.RequiredExtraction, DebugAutomationMode.AssistedPlaythrough,
                DebugAutomationProfile.LiveBalance, 72f);
            metrics.Advance(12f, false, true);
            metrics.RecordShot();
            metrics.RecordSecurityHit();
            metrics.RecordThreatPurge(14f);

            var report = sequencer.FinishReport(
                41f, metrics, false, true, new Vector3(2f, 0f, 3f), RunOutcome.Victory, 1, 2,
                CompatibilityMissionObjectiveGraph.Instance.Definitions.Last());

            Assert.That(report, Does.Contain("Outcome Victory"));
            Assert.That(report, Does.Contain("Objective Extraction  Phase 7  EXTRACT OR GREED"));
            Assert.That(report, Does.Contain("Room Extraction Dock  Anchor Dock Uplink"));
            Assert.That(report, Does.Contain("Journey REQUIRED WITHDRAWAL"));
            Assert.That(report, Does.Contain("Time 12.00s"));
            Assert.That(report, Does.Contain("Combat 12.00s"));
            Assert.That(report, Does.Contain("Hits 1"));
            Assert.That(report, Does.Contain("Shots 1"));
            Assert.That(report, Does.Contain("Live policy shots 1  Evasion responses 2"));
            Assert.That(report, Does.Contain("Recovered 14.0"));
            Assert.That(report, Does.Contain("Guidance response proxy"));
            Assert.That(report, Does.Contain("Wrong-turn proxies 0"));
            Assert.That(report, Does.Contain("Objective-room coverage 1/19: Extraction Dock"));
            Assert.That(report, Does.Contain("Rooms without a compatibility-route objective 18"));
        }

        [Test]
        public void RequiredExtractionReport_RecordsCoverageGuidanceWrongTurnsAndBacktracking()
        {
            var sequencer = new DebugRouteSequencer();
            var metrics = new RunMetrics();
            sequencer.Start(DebugRoutePreset.RequiredExtraction, DebugAutomationMode.DeterministicValidation,
                DebugAutomationProfile.SafeNavigation, 72f);

            while (sequencer.CurrentStep != null)
            {
                sequencer.TickNavigation(10f, 0.1f, false, true);
                sequencer.TickNavigation(9f, 0.2f, false, true);
                sequencer.TickNavigation(0f, 0.1f, false, true);
                sequencer.RecordMissionRoomVisit(sequencer.CurrentStep.RoomName);
                Assert.That(sequencer.ShouldIssueAction(), Is.True);
                sequencer.TickVerification(0.1f, true, true, "verified", 72f);
            }

            var report = sequencer.FinishReport(72f, metrics, false, false, Vector3.zero, RunOutcome.Victory);

            Assert.That(report, Does.Contain("Guidance response proxy avg 0.30s"));
            Assert.That(report, Does.Contain("Wrong-turn proxies 0  Backtrack legs 6"));
            Assert.That(report, Does.Contain("Extraction Dock"));
            Assert.That(report, Does.Contain("Central Chamber"));
            Assert.That(report, Does.Contain("Relay Foundry"));
            Assert.That(report, Does.Contain("Cooling Gantry"));
            Assert.That(report, Does.Contain("Spine Discharge Trench"));
            Assert.That(report, Does.Contain("Capacitor Spine"));
        }

        [Test]
        public void FinishReport_ReplacesSummaryWithFreshTerminalMetrics()
        {
            var sequencer = new DebugRouteSequencer();
            var metrics = new RunMetrics();
            sequencer.Start(DebugRoutePreset.RequiredExtraction, DebugAutomationMode.AssistedPlaythrough,
                DebugAutomationProfile.LiveBalance, 72f);
            metrics.Advance(12f, false);

            var runningReport = sequencer.FinishReport(
                41f, metrics, false, true, new Vector3(2f, 0f, 3f), RunOutcome.Running);
            metrics.Advance(8f, true);
            metrics.RecordSecurityHit();
            var victoryReport = sequencer.FinishReport(
                33f, metrics, false, true, new Vector3(-8f, 0f, -5f), RunOutcome.Victory);

            Assert.That(runningReport, Does.Contain("Outcome Running"));
            Assert.That(victoryReport, Does.Contain("Outcome Victory"));
            Assert.That(victoryReport, Does.Contain("Time 20.00s"));
            Assert.That(victoryReport, Does.Contain("Hits 1"));
            Assert.That(victoryReport, Does.Contain("Position -8.00,-5.00"));
            Assert.That(victoryReport.Split(new[] { "Final Signal" }, System.StringSplitOptions.None).Length - 1,
                Is.EqualTo(1));
        }

        [TestCase(DebugRoutePreset.RequiredExtraction, true)]
        [TestCase(DebugRoutePreset.FullExtraction, true)]
        [TestCase(DebugRoutePreset.OpeningLoop, false)]
        public void CompletedRoute_OnlyExtractionJourneysAwaitTerminalOutcome(
            DebugRoutePreset preset, bool expected)
        {
            var sequencer = new DebugRouteSequencer();
            sequencer.Start(preset, DebugAutomationMode.DeterministicValidation,
                DebugAutomationProfile.SafeNavigation, 72f);
            while (sequencer.CurrentStep != null)
            {
                _completeCurrentStep(sequencer);
            }

            Assert.That(sequencer.State, Is.EqualTo(DebugRouteRunState.Completed));
            Assert.That(sequencer.AwaitsRunOutcomeReport, Is.EqualTo(expected));
        }

        private static void _completeCurrentStep(DebugRouteSequencer sequencer)
        {
            Assert.That(sequencer.TickNavigation(0f, 0.1f, false), Is.True);
            Assert.That(sequencer.ShouldIssueAction(), Is.True);
            sequencer.TickVerification(0.1f, true, true, "verified", 72f);
        }
    }
}
