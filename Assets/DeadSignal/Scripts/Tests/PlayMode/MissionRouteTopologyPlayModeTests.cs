using System.Collections;
using DeadSignal.Application;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests
{
    public sealed class MissionRouteTopologyPlayModeTests
    {
        [UnityTest]
        public IEnumerator Scene_PreservesCriticalRouteAdjacencyAndInteractionAnchors()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();

            Assert.That(game, Is.Not.Null);
            Assert.That(scene, Is.Not.Null);
            Assert.That(scene.IsComplete, Is.True, scene.MissingReferences);
            Assert.That(scene.ExtractionPosition, Is.EqualTo(new Vector3(-9.2f, 0f, -5.6f)));
            Assert.That(scene.TowerPosition, Is.EqualTo(new Vector3(-0.6f, 0f, 0.4f)));
            Assert.That(scene.RelayTowerPosition.x, Is.GreaterThan(scene.TowerPosition.x));
            Assert.That(scene.SpineTowerPosition.x, Is.GreaterThan(scene.RelayTowerPosition.x));

            var departure = _require("Extraction Departure Channel");
            var cargo = _require("Northeast Salvage Annex");
            var coolant = _require("Southeast Coolant Gauntlet");
            var relayFork = _require("Northwest Relay Fork");
            var wardenBay = _require("Security Warden Staging Bay");
            var sapperCradle = _require("Signal Sapper Service Cradle");
            var transferVault = _require("Optional East Salvage Vault");
            var foundry = _require(game.transform, "Relay Foundry Region");
            var gantry = _require(foundry, "Relay Cooling Gantry Region");
            var spine = _require("Capacitor Spine Region");
            var trench = _require(spine, "Spine Discharge Trench Region");
            var induction = _require(game.transform, "Spine Induction Gallery Region");
            var flux = _require(induction, "Flux Bypass Region");
            var convergence = _require(induction, "Convergence Chamber Region");
            var breaker = _require(convergence, "Convergence Breaker Gallery Region");
            var furnace = _require(convergence, "Arc Furnace Region");
            var quench = _require(furnace, "Quench Loop Region");
            var wing = _require(furnace, "Security Trial Wing Region");

            Assert.That(departure.position, Is.EqualTo(new Vector3(-7.2f, 0f, -4.2f)));
            Assert.That(cargo.position, Is.EqualTo(new Vector3(9.7f, 0f, 6.3f)));
            Assert.That(coolant.position, Is.EqualTo(new Vector3(10.4f, 0f, -6.4f)));
            Assert.That(relayFork.position, Is.EqualTo(new Vector3(-5.8f, 0f, 7.2f)));
            Assert.That(wardenBay.position, Is.EqualTo(new Vector3(6.8f, 0f, 4.7f)));
            Assert.That(sapperCradle.position, Is.EqualTo(new Vector3(-10.8f, 0f, 5.7f)));
            Assert.That(transferVault.position, Is.EqualTo(new Vector3(16.7f, 0f, 0f)));
            Assert.That(foundry.position, Is.EqualTo(new Vector3(27.5f, 0f, 0f)));
            Assert.That(gantry.localPosition, Is.EqualTo(new Vector3(0f, 0f, -11.25f)));
            Assert.That(spine.position, Is.EqualTo(new Vector3(42.5f, 0f, 0f)));
            Assert.That(trench.localPosition, Is.EqualTo(new Vector3(0f, 0f, -8f)));
            Assert.That(induction.position, Is.EqualTo(new Vector3(42.5f, 0f, 8.5f)));
            Assert.That(flux.localPosition, Is.EqualTo(new Vector3(-10.5f, 0f, 4.25f)));
            Assert.That(convergence.localPosition, Is.EqualTo(new Vector3(0f, 0f, 8.5f)));
            Assert.That(breaker.localPosition, Is.EqualTo(new Vector3(10.5f, 0f, 0f)));
            Assert.That(furnace.localPosition, Is.EqualTo(new Vector3(0f, 0f, 8.5f)));
            Assert.That(quench.localPosition, Is.EqualTo(new Vector3(10.5f, 0f, 0f)));
            Assert.That(wing.localPosition, Is.EqualTo(new Vector3(0f, 0f, 7.5f)));

            Assert.That(_require(wing, "Commitment Room").position.z,
                Is.LessThan(_require(wing, "Lockdown Arena").position.z));
            Assert.That(_require(wing, "Lockdown Arena").position.z,
                Is.LessThan(_require(wing, "Reward Vault").position.z));
            Assert.That(_require(wing, "Lockdown Entry Door"), Is.Not.Null);
            Assert.That(_require(wing, "Reward Vault Door"), Is.Not.Null);
            Assert.That(_require(quench, "Quench Pressure Shutter"), Is.Not.Null);

            var spineApproach = _require(spine, "Capacitor Spine Activation Decal");
            Assert.That(game.SpineTowerInteractionPosition, Is.EqualTo(spineApproach.position));
            Assert.That(Vector3.Distance(game.SpineTowerInteractionPosition, game.SpineTowerPosition),
                Is.GreaterThan(2f), "Spine guidance must target the usable side of the tower blocker.");
        }

        private static Transform _require(string objectName)
        {
            var value = GameObject.Find(objectName)?.transform;
            Assert.That(value, Is.Not.Null, $"Missing scene-authored route root '{objectName}'.");
            return value;
        }

        private static Transform _require(Transform parent, string path)
        {
            var value = parent.Find(path);
            Assert.That(value, Is.Not.Null, $"Missing scene-authored route path '{parent.name}/{path}'.");
            return value;
        }
    }
}
