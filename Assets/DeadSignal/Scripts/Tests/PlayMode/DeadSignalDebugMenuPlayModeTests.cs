using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.Missions;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class DeadSignalDebugMenuPlayModeTests
    {
        [UnityTest]
        public IEnumerator Bootstrap_CreatesClosedAutoUiDebugMenuInEditor()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var menu = Object.FindFirstObjectByType<DeadSignalDebugMenu>(FindObjectsInactive.Include);
            Canvas debugCanvas = null;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas.name == "DEAD SIGNAL — Debug Menu")
                {
                    debugCanvas = canvas;
                    break;
                }
            }

            Assert.That(game, Is.Not.Null);
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.IsOpen, Is.False);
            Assert.That(debugCanvas, Is.Not.Null);
            Assert.That(debugCanvas.gameObject.activeSelf, Is.False);
            Assert.That(Resources.Load<GameObject>("UI/Debug/AutoUI"), Is.Not.Null);
            Assert.That(Resources.Load<GameObject>("UI/Debug/Canvas_DebugMenu"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Commands_CreatePlayableFeatureScenariosWithoutBypassingInvariants()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game, Is.Not.Null);

            game.DebugSetSignal(1f);
            Assert.That(game.CurrentSignal, Is.EqualTo(1f));

            game.DebugActivateTower();
            Assert.That(game.IsTowerOnline, Is.True);
            Assert.That(game.CurrentSignal, Is.GreaterThan(1f));

            game.DebugCollectNextCache();
            Assert.That(game.CurrentSalvage, Is.EqualTo(1));
            Assert.That(game.IsOverclockChoicePending, Is.True);

            game.DebugSelectOverclock(SignalOverclock.ChainArc);
            Assert.That(game.SelectedOverclock, Is.EqualTo(SignalOverclock.ChainArc));

            game.DebugMakeExtractionReady();
            Assert.That(game.CurrentSalvage, Is.EqualTo(RunModel.SalvageRequired));

            game.DebugBeginExtraction(ExtractionUplinkMode.Stable);
            Assert.That(game.IsExtractionUplinkActive, Is.True);

            game.DebugCompleteExtraction();
            Assert.That(game.CurrentRunOutcome, Is.EqualTo(RunOutcome.Victory));
        }
    }
}
