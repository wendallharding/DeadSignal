using System.Collections;
using System.IO;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.Missions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class CommercialJourneyPlayModeTests
    {
        [UnityTest]
        public IEnumerator FullExtraction_TraversesThreeTowersOptionalCacheAndWeaponEvolution()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game, Is.Not.Null);
            Assert.That(game.HasRuntimeNavMesh, Is.True, game.DebugNavMeshStatus);
            game.DebugSetTimeScale(2f);
            game.DebugStartRouteSequence(DebugRoutePreset.FullExtraction, DebugAutomationMode.DeterministicValidation,
                DebugAutomationProfile.SafeNavigation);

            var timeout = Time.realtimeSinceStartup + 45f;
            while ((game.DebugRouteSequenceState == DebugRouteRunState.Navigating ||
                    game.DebugRouteSequenceState == DebugRouteRunState.Verifying) && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(game.DebugRouteSequenceState, Is.EqualTo(DebugRouteRunState.Completed),
                $"{game.DebugRouteSequenceReport}\n{game.DebugRouteSequenceStatus}\n{game.DebugTelemetry}");
            Assert.That(game.IsRelayTowerOnline, Is.True);
            Assert.That(game.IsSpineTowerOnline, Is.True);
            Assert.That(game.IsCentralPayloadSecured, Is.True);
            Assert.That(game.IsRelayPayloadSecured, Is.True);
            Assert.That(game.IsSpinePayloadSecured, Is.True);
            Assert.That(game.IsExtractionReady, Is.True);
            Assert.That(game.CurrentMissionStage, Is.EqualTo(MissionStage.Extraction));
            Assert.That(game.SelectedWeaponOverclock, Is.EqualTo(SignalWeaponOverclock.PiercingPulse));
            Assert.That(game.CurrentSalvage, Is.EqualTo(RunModel.SalvageRequired));
            Assert.That(game.IsOptionalSalvageSecured, Is.True);
            Assert.That(game.IsExtractionUplinkActive, Is.True);

            var victoryTimeout = Time.realtimeSinceStartup + 8f;
            while (game.CurrentRunOutcome == RunOutcome.Running && Time.realtimeSinceStartup < victoryTimeout)
            {
                yield return null;
            }

            Assert.That(game.CurrentRunOutcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(game.DebugRouteSequenceReport, Does.Contain("Outcome Victory"));
            Assert.That(game.DebugRouteSequenceReport, Does.Contain("Journey OPTIONAL GREED"));
            Assert.That(File.Exists(game.DebugLastCapturePath), Is.True, game.DebugLastCapturePath);
            var report = File.ReadAllText(game.DebugLastCapturePath);
            Assert.That(report, Does.Contain("Outcome Victory"));
            Assert.That(report, Does.Contain("Journey OPTIONAL GREED"));
            game.DebugSetTimeScale(1f);
        }

        [UnityTest]
        public IEnumerator RequiredExtraction_ReachesVictoryAndPreservesMatchedReport()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game, Is.Not.Null);
            game.DebugSetTimeScale(2f);
            game.DebugStartRouteSequence(DebugRoutePreset.RequiredExtraction,
                DebugAutomationMode.DeterministicValidation, DebugAutomationProfile.SafeNavigation);

            var routeTimeout = Time.realtimeSinceStartup + 45f;
            while (game.DebugRouteSequenceState is DebugRouteRunState.Navigating or DebugRouteRunState.Verifying &&
                   Time.realtimeSinceStartup < routeTimeout)
            {
                yield return null;
            }

            Assert.That(game.DebugRouteSequenceState, Is.EqualTo(DebugRouteRunState.Completed),
                $"{game.DebugRouteSequenceReport}\n{game.DebugRouteSequenceStatus}\n{game.DebugTelemetry}");
            Assert.That(game.IsOptionalSalvageSecured, Is.False);
            Assert.That(game.IsExtractionUplinkActive, Is.True);

            var victoryTimeout = Time.realtimeSinceStartup + 8f;
            while (game.CurrentRunOutcome == RunOutcome.Running && Time.realtimeSinceStartup < victoryTimeout)
            {
                yield return null;
            }

            Assert.That(game.CurrentRunOutcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(game.DebugRouteSequenceReport, Does.Contain("Outcome Victory"));
            Assert.That(game.DebugRouteSequenceReport, Does.Contain("Journey REQUIRED WITHDRAWAL"));
            Assert.That(game.DebugRouteSequenceReport, Does.Contain("Final Signal"));
            Assert.That(File.Exists(game.DebugLastCapturePath), Is.True, game.DebugLastCapturePath);
            var report = File.ReadAllText(game.DebugLastCapturePath);
            Assert.That(report, Does.Contain("Outcome Victory"));
            Assert.That(report, Does.Contain("Journey REQUIRED WITHDRAWAL"));
            game.DebugSetTimeScale(1f);
        }

        [UnityTest]
        public IEnumerator LiveBalanceRequiredExtraction_FightsEvadesAndReachesTerminalOutcome()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game, Is.Not.Null);
            game.DebugSetTimeScale(4f);
            game.DebugStartRouteSequence(DebugRoutePreset.RequiredExtraction,
                DebugAutomationMode.AssistedPlaythrough, DebugAutomationProfile.LiveBalance);

            var timeout = Time.realtimeSinceStartup + 55f;
            while (game.CurrentRunOutcome == RunOutcome.Running && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }
            yield return null;

            Assert.That(game.CurrentRunOutcome, Is.EqualTo(RunOutcome.Victory),
                $"{game.DebugRouteSequenceReport}\n{game.DebugRouteSequenceStatus}\n{game.DebugTelemetry}");
            Assert.That(game.ShotsFired, Is.GreaterThan(0));
            Assert.That(game.SelectedOverclock, Is.EqualTo(SignalOverclock.OverdriveThrusters));
            Assert.That(game.DebugLiveBalanceDirectedShots, Is.EqualTo(game.ShotsFired));
            Assert.That(game.DebugLiveBalanceEvasionResponses, Is.GreaterThan(0));
            Assert.That(game.DebugRouteSequenceReport, Does.Contain("Live policy shots"));
            Assert.That(game.DebugRouteSequenceReport, Does.Contain("Evasion responses"));
            game.DebugSetTimeScale(1f);
        }

        [UnityTest]
        public IEnumerator LiveBalanceFullExtraction_CommitsToGreedAndReachesTerminalOutcome()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game, Is.Not.Null);
            game.DebugSetTimeScale(4f);
            game.DebugStartRouteSequence(DebugRoutePreset.FullExtraction,
                DebugAutomationMode.AssistedPlaythrough, DebugAutomationProfile.LiveBalance);

            var timeout = Time.realtimeSinceStartup + 70f;
            while (game.CurrentRunOutcome == RunOutcome.Running && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }
            yield return null;

            Assert.That(game.CurrentRunOutcome, Is.EqualTo(RunOutcome.Victory),
                $"{game.DebugRouteSequenceReport}\n{game.DebugRouteSequenceStatus}\n{game.DebugTelemetry}");
            Assert.That(game.IsOptionalSalvageSecured, Is.True);
            Assert.That(game.ShotsFired, Is.GreaterThan(0));
            Assert.That(game.SelectedOverclock, Is.EqualTo(SignalOverclock.OverdriveThrusters));
            Assert.That(game.ThreatsPurged, Is.GreaterThan(0));
            Assert.That(game.DebugLiveBalanceDirectedShots, Is.EqualTo(game.ShotsFired));
            Assert.That(game.DebugRouteSequenceReport, Does.Contain("Journey OPTIONAL GREED"));
            game.DebugSetTimeScale(1f);
        }
    }
}
