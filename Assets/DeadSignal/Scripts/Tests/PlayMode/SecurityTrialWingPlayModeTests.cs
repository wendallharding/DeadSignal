using System.Collections;
using DeadSignal.Application;
using DeadSignal.Combat;
using DeadSignal.Presentation;
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
            Assert.That(chamber.HasCommitmentReadabilityAssets, Is.True);
            Assert.That(chamber.CommitmentPresentationState, Is.EqualTo(TrialCommitmentPresentationState.Locked));
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
            Assert.That(wing.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(15));
            Assert.That(wing.GetComponentsInChildren<Collider>().Length, Is.Zero);
            Assert.That(Resources.Load<GameObject>("Environment/SecurityTrialWingRegion"), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>("Environment/SecurityTrialCommitmentStatusPanel"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/SecurityTrialCommitmentStatusReadability"), Is.Not.Null);
            Assert.That(Resources.Load<Material>(
                "Materials/SecurityTrialReadability/SecurityTrialCommitmentStatus"), Is.Not.Null);
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(138));
        }

        [UnityTest]
        public IEnumerator CommitmentBreaker_PresentsLifecycleAndResetsWithoutChangingWarningAuthority()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var chamber = Object.FindFirstObjectByType<AuthoredCombatChamber>();
            var selector = chamber.CommitmentSwitch.Find("Commitment Status");
            var warningLeft = chamber.transform.Find("Commitment Room/Trial Warning Left").gameObject;
            var warningRight = chamber.transform.Find("Commitment Room/Trial Warning Right").gameObject;

            Assert.That(selector, Is.Not.Null);
            Assert.That(selector.GetComponents<Collider>(), Is.Empty);
            Assert.That(chamber.CommitmentPresentationState, Is.EqualTo(TrialCommitmentPresentationState.Locked));
            Assert.That(selector.localEulerAngles.y, Is.EqualTo(332f).Within(0.2f));
            Assert.That(warningLeft.activeSelf && warningRight.activeSelf, Is.True);

            game.DebugStabilizeCore();
            Assert.That(chamber.CommitmentPresentationState, Is.EqualTo(TrialCommitmentPresentationState.Available));
            Assert.That(selector.localEulerAngles.y, Is.EqualTo(0f).Within(0.2f));

            game.DebugCommitSecurityTrial();
            Assert.That(chamber.State, Is.EqualTo(CombatChamberState.Armed));
            Assert.That(chamber.CommitmentPresentationState,
                Is.EqualTo(TrialCommitmentPresentationState.CommittedActive));
            var transitionDeadline = Time.realtimeSinceStartup + 1f;
            while (Time.realtimeSinceStartup < transitionDeadline && selector.localEulerAngles.y < 117f)
            {
                yield return null;
            }
            Assert.That(selector.localEulerAngles.y, Is.EqualTo(118f).Within(1f));
            Assert.That(warningLeft.activeSelf && warningRight.activeSelf, Is.True);

            game.DebugCompleteSecurityTrial();
            Assert.That(chamber.CommitmentPresentationState, Is.EqualTo(TrialCommitmentPresentationState.Complete));
            Assert.That(selector.localEulerAngles.y, Is.EqualTo(118f).Within(0.2f));
            Assert.That(warningLeft.activeSelf && warningRight.activeSelf, Is.True);

            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            chamber = Object.FindFirstObjectByType<AuthoredCombatChamber>();
            Assert.That(chamber.CommitmentPresentationState, Is.EqualTo(TrialCommitmentPresentationState.Locked));
            Assert.That(chamber.CommitmentSwitch.Find("Commitment Status").localEulerAngles.y,
                Is.EqualTo(332f).Within(0.2f));
        }

        [UnityTest]
        public IEnumerator Threshold_SealsEntryAndDeploysFirstSwarmerPhase()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var player = game.transform.Find("Maintenance Drone");
            var chamber = Object.FindFirstObjectByType<AuthoredCombatChamber>();
            var feedback = Object.FindFirstObjectByType<StationStateFeedbackController>();
            var entryDoor = chamber.transform.Find("Lockdown Entry Door").gameObject;
            var threshold = chamber.transform.Find("Lockdown Threshold");

            game.DebugCommitSecurityTrial();
            player.position = chamber.CommitmentSwitch.position;
            Assert.That(game.IsSecurityTrialCommitted, Is.True);
            Assert.That(entryDoor.activeSelf, Is.False);
            player.position = threshold.TransformPoint(new Vector3(0f, 0f, 1f));
            yield return null;

            Assert.That(game.CurrentCombatChamberState, Is.EqualTo(CombatChamberState.Lockdown));
            Assert.That(game.CombatChamberPhase, Is.EqualTo(1));
            Assert.That(entryDoor.activeSelf, Is.True);
            Assert.That(game.ActiveSwarmerCount, Is.EqualTo(3));
            Assert.That(game.PeakSwarmerCount, Is.EqualTo(3));
            Assert.That(feedback.LastKind, Is.EqualTo(StationStateFeedbackKind.LockdownEntry));
            Assert.That(feedback.LastPosition, Is.EqualTo(chamber.ArenaPosition));
        }

        [UnityTest]
        public IEnumerator DoorBoundaries_SealEveryFlankAndOpenOnlyTheCenterPassages()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var chamber = Object.FindFirstObjectByType<AuthoredCombatChamber>();
            var wing = chamber.transform;
            const float entryBoundaryZ = 3f;
            const float rewardBoundaryZ = 39f;

            _assertBoundarySealed(wing, entryBoundaryZ, "The dormant entry boundary must be fully sealed.");
            _assertBoundarySealed(wing, rewardBoundaryZ, "Room C must be inaccessible before clearance.");

            Assert.That(chamber.TryArm(chamber.CommitmentSwitch.position), Is.True);
            Assert.That(_isBlockedAt(wing, new Vector3(0f, 0f, entryBoundaryZ)), Is.False,
                "Arming should open only the centered A-to-B passage.");
            _assertBoundaryFlanksSealed(wing, entryBoundaryZ);
            _assertBoundarySealed(wing, rewardBoundaryZ, "Arming must not expose Room C.");

            chamber.Complete();
            Assert.That(_isBlockedAt(wing, new Vector3(0f, 0f, rewardBoundaryZ)), Is.False,
                "Clearing should open only the centered B-to-C passage.");
            _assertBoundaryFlanksSealed(wing, rewardBoundaryZ);
        }

        [UnityTest]
        public IEnumerator ThreePhases_ClearDoorsExposeRewardAndPreserveBoundedConcurrency()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var player = game.transform.Find("Maintenance Drone");
            var chamber = Object.FindFirstObjectByType<AuthoredCombatChamber>();
            var feedback = Object.FindFirstObjectByType<StationStateFeedbackController>();
            var entryDoor = chamber.transform.Find("Lockdown Entry Door").gameObject;
            var rewardDoor = chamber.transform.Find("Reward Vault Door").gameObject;
            var reward = chamber.transform.Find("Reward Vault/Trial Capacitor Reward").gameObject;
            var clearedSignal = chamber.transform.Find("Cleared Return Signal").gameObject;
            var threshold = chamber.transform.Find("Lockdown Threshold");

            game.DebugCommitSecurityTrial();
            player.position = chamber.CommitmentSwitch.position;
            player.position = threshold.TransformPoint(new Vector3(0f, 0f, 1f));
            yield return null;

            game.DebugPurgeSwarmers();
            yield return null;

            Assert.That(game.CombatChamberPhase, Is.EqualTo(2));
            Assert.That(feedback.LastKind, Is.EqualTo(StationStateFeedbackKind.PhaseTransition));
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
            Assert.That(feedback.LastKind, Is.EqualTo(StationStateFeedbackKind.PhaseTransition));
            Assert.That(game.ActiveSwarmerCount, Is.EqualTo(4));
            Assert.That(game.SapperHealth, Is.GreaterThan(0f));

            var playCountBeforeClear = feedback.PlayCount;
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
            Assert.That(game.IsSecurityTrialCleared, Is.True);
            Assert.That(game.CurrentMissionObjectiveId,
                Is.EqualTo(DeadSignal.Missions.MissionObjectiveId.StationCapacitor));
            Assert.That(game.CombatChamberPhase, Is.Zero);
            Assert.That(game.ActiveSwarmerCount, Is.Zero);
            Assert.That(game.PeakThreatConcurrency, Is.EqualTo(5));
            Assert.That(game.SwarmersSpawned, Is.EqualTo(11));
            Assert.That(game.SwarmersPurged, Is.EqualTo(11));
            Assert.That(entryDoor.activeSelf, Is.False);
            Assert.That(rewardDoor.activeSelf, Is.False);
            Assert.That(reward.activeSelf, Is.True);
            Assert.That(clearedSignal.activeSelf, Is.True);
            Assert.That(feedback.LastKind, Is.EqualTo(StationStateFeedbackKind.RewardRelease));
            Assert.That(feedback.LastPosition, Is.EqualTo(chamber.RewardPosition));
            Assert.That(feedback.PlayCount, Is.EqualTo(playCountBeforeClear + 2),
                "Clear should emit one arena resolution and one distinct vault reward release.");
            game.DebugRecoverStationCapacitor();
            Assert.That(game.IsStationCapacitorRecovered, Is.True);
            Assert.That(chamber.RewardAvailable, Is.False);
            Assert.That(feedback.LastKind, Is.EqualTo(StationStateFeedbackKind.Recovery));
            Assert.That(feedback.PlayCount, Is.EqualTo(playCountBeforeClear + 3));
            TestContext.WriteLine(
                $"Security trial peak={game.PeakThreatConcurrency} spawned={game.SwarmersSpawned} " +
                $"purged={game.SwarmersPurged} contacts={game.SwarmerContacts}");
        }

        private static void _assertBoundarySealed(Transform wing, float localZ, string message)
        {
            for (var localX = -17f; localX <= 17f; localX += 0.5f)
            {
                Assert.That(_isBlockedAt(wing, new Vector3(localX, 0f, localZ)), Is.True,
                    $"{message} Gap detected at local x={localX:0.0}, z={localZ:0.0}.");
            }
        }

        private static void _assertBoundaryFlanksSealed(Transform wing, float localZ)
        {
            for (var localX = 2f; localX <= 17f; localX += 0.5f)
            {
                Assert.That(_isBlockedAt(wing, new Vector3(localX, 0f, localZ)), Is.True,
                    $"Positive flank gap detected at local x={localX:0.0}, z={localZ:0.0}.");
                Assert.That(_isBlockedAt(wing, new Vector3(-localX, 0f, localZ)), Is.True,
                    $"Negative flank gap detected at local x={-localX:0.0}, z={localZ:0.0}.");
            }
        }

        private static bool _isBlockedAt(Transform wing, Vector3 localPosition)
        {
            var worldPosition = wing.TransformPoint(localPosition);
            foreach (var obstacle in wing.GetComponentsInChildren<AuthoredMapObstacle>(true))
            {
                if (obstacle.gameObject.activeInHierarchy && obstacle.OverlapsCircle(worldPosition, 0.3f))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
