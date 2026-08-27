using System.Collections;
using DeadSignal.Application;
using DeadSignal.Combat;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests
{
    public sealed class SecurityTrialWingPlayModeTests
    {
        [UnityTest]
        public IEnumerator Wing_AuthorsCommitmentArenaRewardAndRuntimeContract()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var gallery = game.transform.Find("Spine Induction Gallery Region");
            var wing = gallery.Find(
                "Convergence Chamber Region/Arc Furnace Region/Security Trial Wing Region");
            var chamber = wing.GetComponent<AuthoredCombatChamber>();

            Assert.That(game.HasAuthoredCombatChamber, Is.True);
            Assert.That(game.CurrentCombatChamberState, Is.EqualTo(CombatChamberState.Dormant));
            Assert.That(wing.Find("Commitment Room"), Is.Not.Null);
            Assert.That(wing.Find("Lockdown Arena"), Is.Not.Null);
            Assert.That(wing.Find("Reward Vault"), Is.Not.Null);
            var arenaDeck = wing.Find("Lockdown Arena/Arena Deck");
            Assert.That(arenaDeck.localScale.x * arenaDeck.localScale.z, Is.EqualTo(1260f).Within(0.1f),
                "Room B should provide nine times the original 140-square-unit combat floor.");
            Assert.That(wing.Find("Lockdown Entry Door").gameObject.activeSelf, Is.True);
            Assert.That(wing.Find("Reward Vault Door").gameObject.activeSelf, Is.True);
            Assert.That(wing.Find("Cleared Return Signal").gameObject.activeSelf, Is.False);
            Assert.That(chamber.IsComplete, Is.True);
            Assert.That(chamber.RewardSignal, Is.EqualTo(20f));
            Assert.That(wing.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(11));
            Assert.That(wing.GetComponentsInChildren<Collider>().Length, Is.Zero);
            Assert.That(Resources.Load<GameObject>("Environment/SecurityTrialWingRegion"), Is.Not.Null);
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(135));
        }

        [UnityTest]
        public IEnumerator Threshold_SealsEntryAndDeploysFirstSwarmerPhase()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var player = game.transform.Find("Maintenance Drone");
            var chamber = Object.FindFirstObjectByType<AuthoredCombatChamber>();
            var entryDoor = chamber.transform.Find("Lockdown Entry Door").gameObject;
            var threshold = chamber.transform.Find("Lockdown Threshold");

            player.position = chamber.CommitmentSwitch.position;
            Assert.That(chamber.TryArm(player.position), Is.True);
            Assert.That(entryDoor.activeSelf, Is.False);
            player.position = threshold.TransformPoint(new Vector3(0f, 0f, 1f));
            yield return null;

            Assert.That(game.CurrentCombatChamberState, Is.EqualTo(CombatChamberState.Lockdown));
            Assert.That(game.CombatChamberPhase, Is.EqualTo(1));
            Assert.That(entryDoor.activeSelf, Is.True);
            Assert.That(game.ActiveSwarmerCount, Is.EqualTo(3));
            Assert.That(game.PeakSwarmerCount, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator ThreePhases_ClearDoorsExposeRewardAndPreserveBoundedConcurrency()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var player = game.transform.Find("Maintenance Drone");
            var chamber = Object.FindFirstObjectByType<AuthoredCombatChamber>();
            var entryDoor = chamber.transform.Find("Lockdown Entry Door").gameObject;
            var rewardDoor = chamber.transform.Find("Reward Vault Door").gameObject;
            var reward = chamber.transform.Find("Reward Vault/Trial Capacitor Reward").gameObject;
            var clearedSignal = chamber.transform.Find("Cleared Return Signal").gameObject;
            var threshold = chamber.transform.Find("Lockdown Threshold");

            player.position = chamber.CommitmentSwitch.position;
            Assert.That(chamber.TryArm(player.position), Is.True);
            player.position = threshold.TransformPoint(new Vector3(0f, 0f, 1f));
            yield return null;

            game.DebugPurgeSwarmers();
            yield return null;

            Assert.That(game.CombatChamberPhase, Is.EqualTo(2));
            Assert.That(game.ActiveSwarmerCount, Is.EqualTo(4));
            Assert.That(game.WardenHealth, Is.GreaterThan(0f));

            game.DebugPurgeSwarmers();
            game.DebugPurgeThreat(SecurityReinforcement.Warden);
            Assert.That(game.ActiveSwarmerCount, Is.Zero);
            Assert.That(game.WardenHealth, Is.Zero);
            var phaseDeadline = Time.realtimeSinceStartup + 1f;
            while (game.CombatChamberPhase == 2 && Time.realtimeSinceStartup < phaseDeadline)
            {
                yield return null;
            }

            Assert.That(game.CombatChamberPhase, Is.EqualTo(3));
            Assert.That(game.ActiveSwarmerCount, Is.EqualTo(4));
            Assert.That(game.SapperHealth, Is.GreaterThan(0f));

            game.DebugPurgeSwarmers();
            game.DebugPurgeThreat(SecurityReinforcement.Sapper);
            Assert.That(game.ActiveSwarmerCount, Is.Zero);
            Assert.That(game.SapperHealth, Is.Zero);
            var clearDeadline = Time.realtimeSinceStartup + 1f;
            while (game.CurrentCombatChamberState == CombatChamberState.Lockdown &&
                   Time.realtimeSinceStartup < clearDeadline)
            {
                yield return null;
            }

            Assert.That(game.CurrentCombatChamberState, Is.EqualTo(CombatChamberState.Cleared));
            Assert.That(game.CombatChamberPhase, Is.Zero);
            Assert.That(game.ActiveSwarmerCount, Is.Zero);
            Assert.That(game.PeakThreatConcurrency, Is.EqualTo(5));
            Assert.That(game.SwarmersSpawned, Is.EqualTo(11));
            Assert.That(game.SwarmersPurged, Is.EqualTo(11));
            Assert.That(entryDoor.activeSelf, Is.False);
            Assert.That(rewardDoor.activeSelf, Is.False);
            Assert.That(reward.activeSelf, Is.True);
            Assert.That(clearedSignal.activeSelf, Is.True);
            Assert.That(chamber.TryCollectReward(reward.transform.position), Is.True);
            Assert.That(chamber.RewardAvailable, Is.False);
            TestContext.WriteLine(
                $"Security trial peak={game.PeakThreatConcurrency} spawned={game.SwarmersSpawned} " +
                $"purged={game.SwarmersPurged} contacts={game.SwarmerContacts}");
        }
    }
}
