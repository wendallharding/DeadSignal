using System.Collections;
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
            Assert.That(game.SelectedWeaponOverclock, Is.EqualTo(SignalWeaponOverclock.PiercingPulse));
            Assert.That(game.CurrentSalvage, Is.EqualTo(RunModel.SalvageRequired));
            Assert.That(game.IsOptionalSalvageSecured, Is.True);
            Assert.That(game.IsExtractionUplinkActive, Is.True);
            game.DebugSetTimeScale(1f);
        }
    }
}
