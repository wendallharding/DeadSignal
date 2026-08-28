using System.Collections;
using System.Linq;
using DeadSignal.Application;
using DeadSignal.Combat;
using DeadSignal.Diagnostics;
using DeadSignal.Presentation;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DeadSignal.Tests
{
    public sealed class EdgeIndicatorHudPlayModeTests
    {
        [UnityTest]
        public IEnumerator ObjectiveAndSpecialists_UseClampedEdgeIndicatorsWithoutWorldRouteLine()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(Resources.Load<EdgeIndicatorTuning>("Tuning/EdgeIndicatorTuning"), Is.Not.Null);
            Assert.That(game.transform.Find("Objective Route Pulse"), Is.Null);
            Assert.That(Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None)
                .Any(line => line.name == "Objective Route Pulse"), Is.False);
            Assert.That(game.transform.Find("Critical Signal Route"), Is.Null,
                "The legacy low-Signal route line must not exist for progression events to enable later.");

            game.DebugTeleport(DebugLocation.FarEast);
            yield return _waitFrames(45);
            game.DebugTeleport(DebugLocation.CurrentObjective);
            yield return null;
            Assert.That(game.IsObjectiveEdgeIndicatorVisible, Is.True,
                "The objective icon should remain visible over an on-screen target.");
            Assert.That(game.IsObjectiveIndicatorCompact, Is.True,
                "An on-screen objective should hide the edge card details and retain only its icon.");
            var objectivePanel = GameObject.Find("Objective Beacon");
            Assert.That(objectivePanel.GetComponent<Image>().enabled, Is.False);
            Assert.That(objectivePanel.transform.Find("Objective").gameObject.activeSelf, Is.False);
            Assert.That(objectivePanel.transform.Find("Hint").gameObject.activeSelf, Is.False);
            Assert.That(objectivePanel.transform.Find("Distance").gameObject.activeSelf, Is.False);
            Assert.That(objectivePanel.transform.Find("Direction").gameObject.activeSelf, Is.True);
            var objectiveIcon = objectivePanel.transform.Find("Direction") as RectTransform;
            var camera = Object.FindFirstObjectByType<Camera>();
            var expectedScreenPosition = camera.WorldToScreenPoint(game.CurrentObjectiveBeaconTarget + Vector3.up * 1.6f);
            var transitioningScreenPosition = RectTransformUtility.WorldToScreenPoint(null, objectiveIcon.position);
            Assert.That(Vector2.Distance(transitioningScreenPosition, expectedScreenPosition), Is.GreaterThan(2f),
                "The compact icon should interpolate from the edge instead of popping directly onto the objective.");

            yield return _waitFrames(45);
            expectedScreenPosition = camera.WorldToScreenPoint(game.CurrentObjectiveBeaconTarget + Vector3.up * 1.6f);
            var iconScreenPosition = RectTransformUtility.WorldToScreenPoint(null, objectiveIcon.position);
            Assert.That(Vector2.Distance(iconScreenPosition, expectedScreenPosition), Is.LessThan(2f),
                "The interpolated compact icon should settle over the visible world objective.");

            game.DebugTeleport(DebugLocation.FarEast);
            yield return _waitFrames(20);
            Assert.That(game.IsObjectiveEdgeIndicatorVisible, Is.True);
            Assert.That(game.IsObjectiveIndicatorCompact, Is.False);
            Assert.That(objectivePanel.GetComponent<Image>().enabled, Is.True);
            Assert.That(objectivePanel.transform.Find("Objective").gameObject.activeSelf, Is.True);
            Assert.That(objectiveIcon.pivot, Is.EqualTo(Vector2.one * 0.5f),
                "The card icon must rotate around its center instead of orbiting below the card.");
            var objectivePanelRect = objectivePanel.GetComponent<RectTransform>();
            Assert.That(objectivePanelRect.rect.Contains(
                    objectivePanelRect.InverseTransformPoint(objectiveIcon.position)), Is.True,
                "The restored edge icon center should sit inside the objective card.");

            game.DebugSpawnThreat(SecurityReinforcement.Warden);
            game.DebugSpawnThreat(SecurityReinforcement.Sapper);
            game.DebugSpawnThreat(SecurityReinforcement.Interceptor);
            game.DebugSpawnThreat(SecurityReinforcement.Suppressor);
            var offscreenPosition = game.CurrentObjectiveBeaconTarget;
            game.transform.Find("Security Warden").position = offscreenPosition + Vector3.left * 2f;
            game.transform.Find("Signal Sapper").position = offscreenPosition + Vector3.right * 2f;
            game.transform.Find("Security Interceptor").position = offscreenPosition + Vector3.forward * 2f;
            game.transform.Find("Security Suppressor").position = offscreenPosition + Vector3.back * 2f;
            yield return _waitFrames(4);

            Assert.That(game.ActiveEnemyEdgeIndicatorCount, Is.EqualTo(3),
                "Four off-screen specialists should be prioritized into the three-indicator cap.");
            var activeIndicators = Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Where(text => text.transform.parent != null &&
                               text.transform.parent.name.StartsWith("Enemy Edge Indicator") &&
                               text.transform.parent.gameObject.activeInHierarchy)
                .Select(text => text.text)
                .ToArray();
            Assert.That(activeIndicators, Does.Contain("SAPPER"),
                "The latched Sapper should outrank lower urgency off-screen roles.");
        }

        [UnityTest]
        public IEnumerator Swarmers_CollapseIntoOneCountedOffscreenIndicator()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var chamber = Object.FindFirstObjectByType<AuthoredCombatChamber>();
            var player = game.transform.Find("Maintenance Drone");
            player.position = chamber.CommitmentSwitch.position;
            Assert.That(chamber.TryArm(player.position), Is.True);
            var threshold = chamber.transform.Find("Lockdown Threshold");
            player.position = threshold.TransformPoint(new Vector3(0f, 0f, 1f));
            yield return _waitFrames(2);
            game.DebugSetThreatsFrozen(true);
            foreach (var swarmer in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                         .Where(transform => transform.name.StartsWith("Security Swarmer") &&
                                             transform.gameObject.activeInHierarchy))
            {
                swarmer.position = player.position + Vector3.right * 100f;
            }
            yield return _waitFrames(3);

            Assert.That(game.ActiveEnemyEdgeIndicatorCount, Is.LessThanOrEqualTo(3));
            var labels = Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Where(text => text.gameObject.activeInHierarchy && text.transform.parent != null &&
                               text.transform.parent.name.StartsWith("Enemy Edge Indicator"))
                .Select(text => text.text)
                .ToArray();
            Assert.That(labels, Does.Contain("SWARM ×3"));
            Assert.That(labels.Count(label => label.StartsWith("SWARM ×")), Is.EqualTo(1),
                "All off-screen Swarmers should collapse into one counted indicator.");
        }

        private static IEnumerator _waitFrames(int count)
        {
            for (var index = 0; index < count; index++)
            {
                yield return null;
            }
        }
    }
}
