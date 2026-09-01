using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using DeadSignal.Application;
using DeadSignal.Combat;
using DeadSignal.Presentation;
using DeadSignal.World;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class TacticalMapPresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator PausedMap_FitsThePlayableStationAndDrawsAuthoredStructure()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var map = Object.FindFirstObjectByType<MissionClarityHud>();
            var combatFeedback = Object.FindFirstObjectByType<CombatFeedbackController>();
            var shell = Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include);
            Assert.That(game, Is.Not.Null);
            Assert.That(map, Is.Not.Null);
            Assert.That(combatFeedback, Is.Not.Null);
            Assert.That(shell, Is.Not.Null);

            if (shell.IsMenuVisible)
            {
                var start = System.Array.Find(shell.GetComponentsInChildren<Button>(true),
                    button => button.name == "Start Run");
                Assert.That(start, Is.Not.Null);
                start.onClick.Invoke();
                while (shell.IsTransitioning)
                {
                    yield return null;
                }
            }

            combatFeedback.PlaySignalImpact(game.transform.position + Vector3.up * 0.5f, false);
            Assert.That(combatFeedback.IsHitStopped, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(map.IsTacticalMapVisible, Is.False,
                "Combat hit-stop must not be presented as an intentional tactical-map pause.");

            game.DebugSetTimeScale(0f);
            Assert.That(combatFeedback.IsPaused, Is.True);
            Assert.That(map.IsTacticalMapVisible, Is.True);
            yield return null;
            if (UnityEngine.Application.isBatchMode)
            {
                Assert.That(Object.FindObjectsByType<AuthoredMapObstacle>(FindObjectsSortMode.None).Length,
                    Is.GreaterThan(100));
                game.DebugSetTimeScale(1f);
                yield break;
            }
            yield return new WaitForEndOfFrame();

            Assert.That(map.LastTacticalMapCard.width, Is.GreaterThanOrEqualTo(Mathf.Min(524f, Screen.width - 36f)),
                "The tactical map should use enough screen area to separate its station labels.");
            Assert.That(map.LastTacticalMapWorldBounds.width, Is.LessThan(110f),
                "The tactical map should fit the playable station rather than the expanded presentation bounds.");
            Assert.That(map.LastTacticalMapWorldBounds.height, Is.LessThan(80f));
            Assert.That(map.LastTacticalMapObstacleCount,
                Is.EqualTo(Object.FindObjectsByType<AuthoredMapObstacle>(FindObjectsSortMode.None).Length));
            Assert.That(map.LastTacticalMapObstacleCount, Is.GreaterThan(100),
                "The map backdrop should include the authored walls and machinery that explain station space.");

            var captureDirectory = _captureDirectory();
            if (!string.IsNullOrWhiteSpace(captureDirectory))
            {
                Directory.CreateDirectory(captureDirectory);
                var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(captureDirectory,
                    $"TacticalMap-{Screen.width}x{Screen.height}.png"), texture.EncodeToPNG());
                Object.Destroy(texture);
            }

            game.DebugSetTimeScale(1f);
        }

        private static string _captureDirectory()
        {
            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_TACTICAL_MAP_CAPTURE_DIR");
            if (!string.IsNullOrWhiteSpace(captureDirectory))
            {
                return captureDirectory;
            }

            const string argumentPrefix = "-deadSignalTacticalMapCaptureDir=";
            foreach (var argument in System.Environment.GetCommandLineArgs())
            {
                if (argument.StartsWith(argumentPrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    return argument.Substring(argumentPrefix.Length);
                }

                if (string.Equals(argument, "-deadSignalTacticalMapCapture", System.StringComparison.OrdinalIgnoreCase))
                {
                    captureDirectory = Path.Combine(UnityEngine.Application.temporaryCachePath, "DeadSignalTacticalMap");
                }
            }

            return captureDirectory ?? string.Empty;
        }
    }
}
