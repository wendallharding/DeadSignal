using System;
using System.Collections;
using System.IO;
using DeadSignal.Application;
using DeadSignal.Missions;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

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
            Assert.That(objective.HasReadabilityAssets, Is.True);
            Assert.That(objective.PresentationState, Is.EqualTo(CoolantReclamationPresentationState.Locked));
            var obstacles = Object.FindObjectsByType<AuthoredMapObstacle>(FindObjectsSortMode.None);
            var objectivePositions = new[]
            {
                objective.FirstBafflePosition,
                objective.SecondBafflePosition,
                objective.SealPosition,
                objective.ReleasePosition
            };
            foreach (var objectivePosition in objectivePositions)
            {
                var blockingObstacle = Array.Find(obstacles, obstacle => obstacle.OverlapsCircle(objectivePosition, 0.48f));
                Assert.That(blockingObstacle, Is.Null,
                    $"Coolant objective at {objectivePosition} must remain accessible; blocked by {blockingObstacle?.name}.");
            }

            var hero = objective.GetComponentInChildren<AuthoredCoolantReclamationHeroFinish>(true);
            Assert.That(hero, Is.Not.Null);
            var heroMesh = hero.FinishRenderer.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(heroMesh.name, Is.EqualTo("CoolantReclamationHeroFinish"));
            Assert.That(heroMesh.vertexCount, Is.GreaterThanOrEqualTo(80));
            Assert.That(hero.FinishRenderer.sharedMaterials, Has.Length.EqualTo(4));
            Assert.That(hero.FinishRenderer.sharedMaterials[0].mainTexture.name,
                Is.EqualTo("CoolantReclamationHeroAtlas"));
            Assert.That(hero.FinishRenderer.GetComponentsInChildren<Collider>(true), Is.Empty,
                "The Coolant room finish must remain presentation-only.");
            Assert.That(hero.BaffleRendererCount, Is.EqualTo(8));
            Assert.That(hero.AppliedState, Is.EqualTo(CoolantReclamationPresentationState.Locked));

            var statusBase = objective.transform.Find("Coolant Status Base");
            var statusDial = objective.transform.Find("Coolant Status Dial");
            Assert.That(statusBase, Is.Not.Null);
            Assert.That(statusDial, Is.Not.Null);
            Assert.That(statusBase.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("CoolantStatusBaseReadability"));
            Assert.That(statusDial.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("CoolantStatusDialReadability"));
            Assert.That(statusBase.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThanOrEqualTo(24));
            Assert.That(statusDial.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThanOrEqualTo(24));
            Assert.That(statusDial.GetComponent<MeshFilter>().sharedMesh.HasVertexAttribute(
                UnityEngine.Rendering.VertexAttribute.TexCoord0), Is.True);
            Assert.That(statusDial.GetComponent<Renderer>().sharedMaterial.mainTexture.name,
                Is.EqualTo("CoolantReclamationStatusPanel"));
            Assert.That(statusBase.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(statusDial.GetComponentsInChildren<Collider>(true), Is.Empty,
                "Coolant readability meshes must remain presentation-only.");
            Assert.That(objective.transform.Find("First Baffle Route Marker").gameObject.activeSelf, Is.False,
                "Locked Coolant machinery must not imply that its threading route is active.");

            game.DebugActivateTower();
            yield return null;
            Assert.That(objective.PresentationState, Is.EqualTo(CoolantReclamationPresentationState.FirstBaffle));
            Assert.That(hero.AppliedState, Is.EqualTo(CoolantReclamationPresentationState.FirstBaffle));
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
            Assert.That(objective.PresentationState, Is.EqualTo(CoolantReclamationPresentationState.SecondBaffle));

            scene.Player.position = objective.SecondBafflePosition;
            yield return null;
            Assert.That(game.CoolantSealPhase, Is.EqualTo(CoolantSealThreadingPhase.SealAvailable));
            Assert.That(objective.PresentationState, Is.EqualTo(CoolantReclamationPresentationState.Release));

            scene.Player.position = objective.SealPosition;
            yield return null;
            Assert.That(game.IsCoolantSealSecured, Is.False,
                "Releasing the seal must not complete the objective before the outward crossing.");
            Assert.That(game.CoolantSealPhase, Is.EqualTo(CoolantSealThreadingPhase.Releasing));
            Assert.That(game.CurrentSalvage, Is.EqualTo(1));
            Assert.That(objective.PresentationState, Is.EqualTo(CoolantReclamationPresentationState.Release));

            scene.Player.position = Vector3.Lerp(objective.SealPosition, objective.ReleasePosition, 0.5f);
            yield return null;
            Assert.That(game.IsCoolantSealSecured, Is.False);

            scene.Player.position = objective.ReleasePosition;
            yield return null;
            Assert.That(game.IsCoolantSealSecured, Is.True);
            Assert.That(game.CoolantSealPhase, Is.EqualTo(CoolantSealThreadingPhase.Complete));
            Assert.That(objective.PresentationState, Is.EqualTo(CoolantReclamationPresentationState.Stable));
            Assert.That(hero.AppliedState, Is.EqualTo(CoolantReclamationPresentationState.Stable));
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

            var capturePath = Environment.GetEnvironmentVariable("DEAD_SIGNAL_COOLANT_HERO_CAPTURE");
            if (!string.IsNullOrWhiteSpace(capturePath))
            {
                scene.Player.position = Vector3.Lerp(objective.FirstBafflePosition, objective.SecondBafflePosition, 0.5f);
                yield return null;
                yield return new WaitForSecondsRealtime(0.6f);
                _captureCamera(scene.PlayerCamera, capturePath);
            }

            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            objective = Object.FindFirstObjectByType<AuthoredCoolantReclamationObjective>(FindObjectsInactive.Include);
            Assert.That(objective.PresentationState, Is.EqualTo(CoolantReclamationPresentationState.Locked),
                "A fresh run must restore the persistent dormant Coolant read.");
            hero = objective.GetComponentInChildren<AuthoredCoolantReclamationHeroFinish>(true);
            Assert.That(hero.AppliedState, Is.EqualTo(CoolantReclamationPresentationState.Locked));
        }

        private static void _captureCamera(Camera camera, string path)
        {
            Assert.That(camera, Is.Not.Null);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var renderTexture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(renderTexture);
            }
        }
    }
}
