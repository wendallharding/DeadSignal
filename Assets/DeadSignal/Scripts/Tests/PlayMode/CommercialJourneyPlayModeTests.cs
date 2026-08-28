using System.Collections;
using System.IO;
using DeadSignal.Application;
using DeadSignal.Combat;
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
        [SetUp]
        public void SetUp()
        {
            m_hadRouteVariant = PlayerPrefs.HasKey(ROUTE_VARIANT_KEY);
            m_initialRouteVariant = PlayerPrefs.GetInt(ROUTE_VARIANT_KEY, 0);
            PlayerPrefs.SetInt(ROUTE_VARIANT_KEY, 0);
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_hadRouteVariant)
            {
                PlayerPrefs.SetInt(ROUTE_VARIANT_KEY, m_initialRouteVariant);
            }
            else
            {
                PlayerPrefs.DeleteKey(ROUTE_VARIANT_KEY);
            }
            PlayerPrefs.Save();
        }

        [UnityTest]
        [Category("OptionalRouteRegression")]
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

            var timeout = Time.realtimeSinceStartup + 75f;
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
            Assert.That(game.CurrentExtractionSuppressionProfile, Is.EqualTo(ExtractionSuppressionProfile.PiercingCrossLane),
                "Optional Quench greed should make the bounded extraction response counter the evolved weapon.");

            var victoryTimeout = Time.realtimeSinceStartup + 8f;
            while (game.CurrentRunOutcome == RunOutcome.Running && Time.realtimeSinceStartup < victoryTimeout)
            {
                yield return null;
            }

            Assert.That(game.CurrentRunOutcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.Extraction));
            Assert.That(game.CurrentMissionGuidanceTitle, Is.EqualTo("EXTRACT OR GREED"));
            Assert.That(game.CurrentMissionGuidanceAction, Is.EqualTo("RETURN TO THE CYAN DOCK"));
            Assert.That(game.CurrentObjectiveBeaconLabel, Is.EqualTo(game.CurrentMissionGuidanceAction));
            Assert.That(game.CurrentObjectiveBeaconHint, Does.Contain("THREE TOWERS"));
            Assert.That(game.DebugRouteSequenceReport, Does.Contain("Outcome Victory"));
            Assert.That(game.DebugRouteSequenceReport, Does.Contain("Objective Extraction  Phase 7  EXTRACT OR GREED"));
            Assert.That(game.DebugRouteSequenceReport, Does.Contain("Journey OPTIONAL GREED"));
            Assert.That(File.Exists(game.DebugLastCapturePath), Is.True, game.DebugLastCapturePath);
            var report = File.ReadAllText(game.DebugLastCapturePath);
            Assert.That(report, Does.Contain("Outcome Victory"));
            Assert.That(report, Does.Contain("Room Extraction Dock  Anchor Dock Uplink"));
            Assert.That(report, Does.Contain("Journey OPTIONAL GREED"));
            game.DebugSetTimeScale(1f);
        }

        [UnityTest]
        [Category("RouteRegression")]
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

            var routeTimeout = Time.realtimeSinceStartup + 75f;
            while (game.DebugRouteSequenceState is DebugRouteRunState.Navigating or DebugRouteRunState.Verifying &&
                   Time.realtimeSinceStartup < routeTimeout)
            {
                yield return null;
            }

            Assert.That(game.DebugRouteSequenceState, Is.EqualTo(DebugRouteRunState.Completed),
                $"{game.DebugRouteSequenceReport}\n{game.DebugRouteSequenceStatus}\n{game.DebugTelemetry}");
            Assert.That(game.IsOptionalSalvageSecured, Is.False,
                $"{game.DebugRouteSequenceReport}\n{game.DebugRouteSequenceStatus}\n{game.DebugTelemetry}");
            Assert.That(game.IsExtractionUplinkActive, Is.True);
            Assert.That(game.CurrentExtractionSuppressionProfile, Is.EqualTo(ExtractionSuppressionProfile.Standard),
                "Required withdrawal should preserve the established extraction response.");

            var victoryTimeout = Time.realtimeSinceStartup + 8f;
            while (game.CurrentRunOutcome == RunOutcome.Running && Time.realtimeSinceStartup < victoryTimeout)
            {
                yield return null;
            }

            Assert.That(game.CurrentRunOutcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.Extraction));
            Assert.That(game.CurrentMissionGuidanceTitle, Is.EqualTo("EXTRACT OR GREED"));
            Assert.That(game.CurrentObjectiveBeaconLabel, Is.EqualTo(game.CurrentMissionGuidanceAction));
            Assert.That(game.DebugRouteSequenceReport, Does.Contain("Outcome Victory"));
            Assert.That(game.DebugRouteSequenceReport, Does.Contain("Objective Extraction  Phase 7  EXTRACT OR GREED"));
            Assert.That(game.DebugRouteSequenceReport, Does.Contain("Journey REQUIRED WITHDRAWAL"));
            Assert.That(game.DebugRouteSequenceReport, Does.Contain("Final Signal"));
            Assert.That(File.Exists(game.DebugLastCapturePath), Is.True, game.DebugLastCapturePath);
            var report = File.ReadAllText(game.DebugLastCapturePath);
            Assert.That(report, Does.Contain("Outcome Victory"));
            Assert.That(report, Does.Contain("Room Extraction Dock  Anchor Dock Uplink"));
            Assert.That(report, Does.Contain("Journey REQUIRED WITHDRAWAL"));
            Assert.That(report, Does.Contain("Combat "));
            Assert.That(report, Does.Contain("Guidance response proxy"));
            Assert.That(report, Does.Contain("Wrong-turn proxies 0  Backtrack legs 10"));
            Assert.That(report, Does.Contain("PASS Cargo coupling"));
            Assert.That(report, Does.Contain("PASS Coolant seal"));
            Assert.That(report, Does.Contain("PASS Relay Fork routing"));
            Assert.That(report, Does.Contain("PASS Transfer-vault assembly"));
            Assert.That(report, Does.Contain("PASS Central payload installation"));
            Assert.That(report, Does.Contain("PASS Cooling Gantry stabilization"));
            Assert.That(report, Does.Contain("PASS Foundry payload installation"));
            Assert.That(report, Does.Contain("PASS Flux shunt routing"));
            Assert.That(report, Does.Contain("PASS Breaker distribution reset"));
            Assert.That(report, Does.Contain("PASS Arc Furnace forging"));
            Assert.That(report, Does.Contain("PASS Quench stabilization"));
            Assert.That(report, Does.Contain("PASS Room A commitment"));
            Assert.That(report, Does.Contain("PASS Room B lockdown"));
            Assert.That(report, Does.Contain("PASS Room C station capacitor"));
            Assert.That(report, Does.Contain("PASS Relay powered shortcut"));
            Assert.That(report, Does.Contain("PASS Transfer-vault return feed"));
            Assert.That(report, Does.Contain("PASS Central powered foothold"));
            Assert.That(report, Does.Contain("Objective-room coverage 19/19"));
            Assert.That(report, Does.Contain("Rooms without a compatibility-route objective 0"));
            game.DebugSetTimeScale(1f);
        }

        [UnityTest]
        public IEnumerator OptionalCache_ForecastsRicochetCoverFlushBeforeCommitment()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game, Is.Not.Null);
            game.DebugActivateTower();
            game.DebugCollectNextCache();
            game.DebugSelectOverclock(SignalOverclock.ChainArc);
            game.DebugActivateRelayTower();
            game.DebugSelectWeapon(SignalWeaponOverclock.ControlledRicochet);
            game.DebugCollectNextCache();
            game.DebugSelectAuxiliary(SignalAuxiliaryOverclock.FeedbackShield);
            game.DebugActivateSpineTower();
            game.DebugChargeInductionLattice();
            game.DebugRouteFluxShunt();
            game.DebugCompleteConvergenceCalibration();
            game.DebugResetBreakerDistribution();
            game.DebugForgeLattice();
            game.DebugStabilizeCore();
            game.DebugCommitSecurityTrial();
            game.DebugCompleteSecurityTrial();
            game.DebugRecoverStationCapacitor();
            game.DebugInstallSpineCore();
            game.DebugCompletePoweredWithdrawal();
            yield return null;

            Assert.That(game.IsExtractionReady, Is.True);
            Assert.That(game.IsOptionalSalvageSecured, Is.False);
            Assert.That(game.CurrentMissionObjective, Does.Contain("COUNTERTRACE: COVER FLUSH AT EXTRACTION"),
                "Ricochet greed should disclose the cover-flush consequence while the cache can still be abandoned.");

            game.DebugCollectNextCache();
            game.DebugBeginExtraction(ExtractionUplinkMode.Stable);
            yield return null;

            Assert.That(game.IsOptionalSalvageSecured, Is.True);
            Assert.That(game.CurrentExtractionSuppressionProfile, Is.EqualTo(ExtractionSuppressionProfile.RicochetCoverFlush));
            Assert.That(game.CurrentMissionObjective, Does.Contain("COVER FLUSH — LEAVE YOUR CURRENT ANCHOR"),
                "The active pursuit should repeat the disclosed exit response when the field becomes actionable.");
        }

        [UnityTest]
        [Category("LiveBalance")]
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

            var timeout = Time.realtimeSinceStartup + 85f;
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
        [Category("LiveBalance")]
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

            var timeout = Time.realtimeSinceStartup + 115f;
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
            TestContext.WriteLine(game.DebugRouteSequenceReport);
            TestContext.WriteLine(game.DebugTelemetry);
            game.DebugSetTimeScale(1f);
        }

        private const string ROUTE_VARIANT_KEY = "DeadSignal.RouteVariant";

        private bool m_hadRouteVariant;
        private int m_initialRouteVariant;
    }
}
