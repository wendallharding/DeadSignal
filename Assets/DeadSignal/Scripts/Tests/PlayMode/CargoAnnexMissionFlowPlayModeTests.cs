using System.Collections;
using DeadSignal.Application;
using DeadSignal.Missions;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class CargoAnnexMissionFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator CouplingRequiresCommitPickupAndWithdrawalBeforeObjectiveCompletion()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var objective = Object.FindFirstObjectByType<AuthoredCargoAnnexObjective>(FindObjectsInactive.Include);
            Assert.That(game, Is.Not.Null);
            Assert.That(scene, Is.Not.Null);
            Assert.That(objective, Is.Not.Null);
            Assert.That(objective.IsConfigured, Is.True);

            game.DebugActivateTower();
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.CargoCoupling));
            Assert.That(game.CurrentSalvage, Is.Zero);

            var justOutsideCommitment = Vector3.MoveTowards(
                objective.CommitmentPosition,
                objective.WithdrawalPosition,
                0.05f);
            scene.Player.position = justOutsideCommitment;
            yield return null;

            Assert.That(game.IsCargoCouplingSecured, Is.False);
            Assert.That(game.CargoCouplingPhase, Is.EqualTo(CargoCouplingRetrievalPhase.AwaitingCommit));

            scene.Player.position = objective.CommitmentPosition;
            yield return null;

            Assert.That(game.IsCargoCouplingSecured, Is.False,
                "Taking the coupling inside the pocket must not complete the objective.");
            Assert.That(game.CargoCouplingPhase, Is.EqualTo(CargoCouplingRetrievalPhase.Withdrawing));
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.CargoCoupling));

            scene.Player.position = Vector3.Lerp(objective.CommitmentPosition, objective.WithdrawalPosition, 0.5f);
            yield return null;

            Assert.That(game.IsCargoCouplingSecured, Is.False);
            Assert.That(game.CurrentSalvage, Is.Zero);

            scene.Player.position = objective.WithdrawalPosition;
            yield return null;

            Assert.That(game.IsCargoCouplingSecured, Is.True);
            Assert.That(game.CargoCouplingPhase, Is.EqualTo(CargoCouplingRetrievalPhase.Complete));
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.CoolantSeal));
            Assert.That(game.CurrentSalvage, Is.EqualTo(1));
            Assert.That(objective.transform.Find("Cargo Commitment Marker").gameObject.activeSelf, Is.False);
            Assert.That(objective.transform.Find("Cargo Withdrawal Marker").gameObject.activeSelf, Is.False);
            Assert.That(objective.transform.Find("Power Coupling Secured Marker").gameObject.activeSelf, Is.True);

            scene.Player.position = objective.CouplingPosition;
            yield return null;
            scene.Player.position = objective.WithdrawalPosition;
            yield return null;

            Assert.That(game.CurrentSalvage, Is.EqualTo(1), "The completed coupling must remain idempotent.");
        }
    }
}
