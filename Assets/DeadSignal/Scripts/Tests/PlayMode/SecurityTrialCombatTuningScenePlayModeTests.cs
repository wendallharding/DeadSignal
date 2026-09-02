using System.Collections;
using System.Linq;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests
{
    public sealed class SecurityTrialCombatTuningScenePlayModeTests
    {
        [UnityTest]
        public IEnumerator Scene_IsolatesWingAndBeginsRoomBLockdown()
        {
            yield return SceneManager.LoadSceneAsync("SecurityTrialCombatTuning");
            yield return null;
            yield return null;

            var tuningScene = Object.FindFirstObjectByType<SecurityTrialCombatTuningScene>();
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var chamber = Object.FindFirstObjectByType<AuthoredCombatChamber>();
            var sceneReferences = Object.FindFirstObjectByType<DeadSignalSceneReferences>();

            Assert.That(tuningScene, Is.Not.Null);
            Assert.That(tuningScene.IsPrepared, Is.True);
            Assert.That(tuningScene.BeginsInRoomB, Is.True);
            Assert.That(game, Is.Not.Null);
            Assert.That(chamber, Is.Not.Null);
            Assert.That(chamber.State, Is.EqualTo(CombatChamberState.Lockdown));
            Assert.That(chamber.Phase, Is.EqualTo(1));
            Assert.That(sceneReferences.Player.position, Is.EqualTo(chamber.CombatScenario.PlayerAnchor.position));

            foreach (var obstacle in Object.FindObjectsByType<AuthoredMapObstacle>(FindObjectsSortMode.None))
            {
                Assert.That(obstacle.transform.IsChildOf(chamber.transform), Is.True,
                    $"Only Room A-B-C collision should remain active, but found {obstacle.name}.");
            }

            var activeObstacleNames = Object.FindObjectsByType<AuthoredMapObstacle>(FindObjectsSortMode.None)
                .Select(obstacle => obstacle.name)
                .OrderBy(name => name)
                .ToArray();
            Assert.That(activeObstacleNames.Length, Is.EqualTo(15), string.Join(", ", activeObstacleNames));
        }
    }
}
