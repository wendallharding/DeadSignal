using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DeadSignal.Application;
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
            Assert.That(game, Is.Not.Null);
            Assert.That(map, Is.Not.Null);

            game.DebugSetTimeScale(0f);
            yield return null;
            if (!UnityEngine.Application.isBatchMode)
            {
                yield return new WaitForEndOfFrame();
            }

            Assert.That(map.LastTacticalMapCard.width, Is.GreaterThanOrEqualTo(Mathf.Min(524f, Screen.width - 36f)),
                "The tactical map should use enough screen area to separate its station labels.");
            Assert.That(map.LastTacticalMapWorldBounds.width, Is.LessThan(60f),
                "The tactical map should fit the playable station rather than the expanded presentation bounds.");
            Assert.That(map.LastTacticalMapWorldBounds.height, Is.LessThan(60f));
            Assert.That(map.LastTacticalMapObstacleCount,
                Is.EqualTo(Object.FindObjectsByType<AuthoredMapObstacle>(FindObjectsSortMode.None).Length));
            Assert.That(map.LastTacticalMapObstacleCount, Is.GreaterThan(100),
                "The map backdrop should include the authored walls and machinery that explain station space.");

            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_TACTICAL_MAP_CAPTURE_DIR");
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
    }
}
