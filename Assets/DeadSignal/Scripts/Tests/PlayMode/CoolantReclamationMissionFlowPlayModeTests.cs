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
    public sealed class CoolantReclamationMissionFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator SealRequiresOrderedBaffleThreadingPickupAndExitBeforeCompletion()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var objective = Object.FindFirstObjectByType<AuthoredCoolantReclamationObjective>(FindObjectsInactive.Include);
            Assert.That(game, Is.Not.Null);
            Assert.That(scene, Is.Not.Null);
            Assert.That(objective, Is.Not.Null);
            Assert.That(objective.IsConfigured, Is.True);

            game.DebugActivateTower();
            scene.Player.position = game.CargoCommitmentPosition;
            yield return null;
            scene.Player.position = game.CargoCouplingPosition;
            yield return null;
            scene.Player.position = game.CargoWithdrawalPosition;
            yield return null;
            Assert.That(game.IsCargoCouplingSecured, Is.True);
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.CoolantSeal));
            Assert.That(game.CurrentSalvage, Is.EqualTo(1));

            scene.Player.position = objective.SecondBafflePosition;
            yield return null;
            scene.Player.position = objective.SealPosition;
            yield return null;

            Assert.That(game.IsCoolantSealSecured, Is.False,
                "Reaching the second baffle or seal first must not bypass the route.");
            Assert.That(game.CoolantSealPhase, Is.EqualTo(CoolantSealThreadingPhase.AwaitingFirstBaffle));

            scene.Player.position = objective.FirstBafflePosition;
            yield return null;
            Assert.That(game.CoolantSealPhase, Is.EqualTo(CoolantSealThreadingPhase.AwaitingSecondBaffle));

            scene.Player.position = objective.SecondBafflePosition;
            yield return null;
            Assert.That(game.CoolantSealPhase, Is.EqualTo(CoolantSealThreadingPhase.SealAvailable));

            scene.Player.position = objective.SealPosition;
            yield return null;
            Assert.That(game.IsCoolantSealSecured, Is.False,
                "Releasing the seal must not complete the objective before the outward crossing.");
            Assert.That(game.CoolantSealPhase, Is.EqualTo(CoolantSealThreadingPhase.Releasing));
            Assert.That(game.CurrentSalvage, Is.EqualTo(1));

            scene.Player.position = Vector3.Lerp(objective.SealPosition, objective.ReleasePosition, 0.5f);
            yield return null;
            Assert.That(game.IsCoolantSealSecured, Is.False);

            scene.Player.position = objective.ReleasePosition;
            yield return null;
            Assert.That(game.IsCoolantSealSecured, Is.True);
            Assert.That(game.CoolantSealPhase, Is.EqualTo(CoolantSealThreadingPhase.Complete));
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.RelayFork),
                "Both Central components must now advance to the authored Relay Fork routing step.");
            Assert.That(game.CurrentSalvage, Is.EqualTo(1),
                "The second Central job must retain the established single regional reward.");
            Assert.That(objective.transform.Find("First Baffle Route Marker").gameObject.activeSelf, Is.False);
            Assert.That(objective.transform.Find("Second Baffle Route Marker").gameObject.activeSelf, Is.False);
            Assert.That(objective.transform.Find("Coolant Release Marker").gameObject.activeSelf, Is.False);
            Assert.That(objective.transform.Find("Coolant Line Stable Marker").gameObject.activeSelf, Is.True);

            scene.Player.position = objective.FirstBafflePosition;
            yield return null;
            scene.Player.position = objective.SecondBafflePosition;
            yield return null;
            scene.Player.position = objective.SealPosition;
            yield return null;
            scene.Player.position = objective.ReleasePosition;
            yield return null;

            Assert.That(game.CurrentSalvage, Is.EqualTo(1), "The stabilized coolant line must remain idempotent.");
        }
    }
}
