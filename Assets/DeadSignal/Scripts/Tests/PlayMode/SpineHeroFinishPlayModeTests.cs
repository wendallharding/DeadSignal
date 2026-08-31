using System.Collections;
using System.IO;
using System.Linq;
using DeadSignal.Application;
using DeadSignal.Missions;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class SpineHeroFinishPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroFinish_PreservesLifecycleApproachesAndProjectileAuthority()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var spine = game.transform.Find("Capacitor Spine Region");
            var trench = spine.Find("Spine Discharge Trench Region");
            var spineFinish = spine.GetComponent<AuthoredSpineHeroFinish>();
            var trenchFinish = trench.GetComponent<AuthoredSpineHeroFinish>();
            var venting = trench.GetComponent<AuthoredSpineVentingObjective>();
            var tower = spine.GetComponent<AuthoredSpineTowerReadability>();

            Assert.That(spineFinish, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(trenchFinish, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(spineFinish.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(trenchFinish.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(spineFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("CapacitorSpineHeroFinish"));
            Assert.That(trenchFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("DischargeTrenchHeroFinish"));
            Assert.That(spineFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.EqualTo(128));
            Assert.That(trenchFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.EqualTo(128));
            Assert.That(spineFinish.FinishRenderer.sharedMaterials, Has.Length.EqualTo(4));
            Assert.That(trenchFinish.FinishRenderer.sharedMaterials, Has.Length.EqualTo(4));
            Assert.That(spineFinish.FinishRenderer.sharedMaterials[0].name, Is.EqualTo("SpineHeroAlloy"));
            Assert.That(spineFinish.FinishRenderer.sharedMaterials[1].name, Is.EqualTo("SpineHeroCeramic"));
            Assert.That(spineFinish.FinishRenderer.sharedMaterials[2].name, Is.EqualTo("SpineHeroConductor"));
            Assert.That(spineFinish.FinishRenderer.sharedMaterials[3].name, Is.EqualTo("SpineHeroInsulator"));
            Assert.That(spine.GetComponentsInChildren<AuthoredMapObstacle>(), Has.Length.EqualTo(18));
            Assert.That(trench.GetComponentsInChildren<AuthoredMapObstacle>(), Has.Length.EqualTo(6));
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(138));
            Assert.That(game.AuthoredInterceptorEntranceCount, Is.EqualTo(9));
            Assert.That(venting.PresentationState, Is.EqualTo(SpineBerthPresentationState.DormantPressurized));
            Assert.That(tower.PresentationState, Is.EqualTo(SpineTowerPresentationState.PressurizedLocked));

            game.DebugActivateRelayTower();
            game.DebugCollectNextCache();
            game.DebugInstallRelayPayload();
            game.DebugSelectWeapon(SignalWeaponOverclock.PiercingPulse);
            yield return null;
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.SpineVenting));
            Assert.That(venting.PresentationState, Is.EqualTo(SpineBerthPresentationState.VentAvailable));
            Assert.That(tower.PresentationState, Is.EqualTo(SpineTowerPresentationState.PressurizedLocked));

            game.DebugVentSpineBerth();
            yield return null;
            Assert.That(venting.PresentationState, Is.EqualTo(SpineBerthPresentationState.VentingActive));
            Assert.That(tower.PresentationState, Is.EqualTo(SpineTowerPresentationState.ActivationAvailable));
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(venting.PresentationState, Is.EqualTo(SpineBerthPresentationState.Vented));

            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_SPINE_HERO_CAPTURE_DIR");
            if (!string.IsNullOrWhiteSpace(captureDirectory))
            {
                Directory.CreateDirectory(captureDirectory);
                game.transform.Find("Maintenance Drone").position = spine.position + new Vector3(0.6f, 0f, -1.7f);
                yield return new WaitForSecondsRealtime(0.25f);
                _captureCamera(scene.PlayerCamera,
                    Path.Combine(captureDirectory, "P09-Capacitor-Spine-Hero-Finish-1600x900.png"));
            }

            game.DebugActivateSpineTower();
            yield return null;
            Assert.That(tower.PresentationState, Is.EqualTo(SpineTowerPresentationState.Activating));
            Assert.That(spine.Find("Capacitor Transfer Bank").gameObject.activeSelf, Is.False);
            Assert.That(spine.Find("Spine Return Threshold").gameObject.activeSelf, Is.True);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(tower.PresentationState, Is.EqualTo(SpineTowerPresentationState.Powered));
            Assert.That(trench.Find("Discharge Trench Signal Lines").gameObject.activeSelf, Is.True);

            var dischargeCoil = trench.Find("Central Discharge Coil");
            var dischargeObstacles = dischargeCoil.GetComponents<AuthoredMapObstacle>();
            Assert.That(dischargeObstacles, Is.Not.Empty);
            Assert.That(dischargeObstacles.All(obstacle => obstacle.OverlapsCircle(dischargeCoil.position, 0.35f)),
                Is.True, "The central discharge coil must retain movement and projectile authority beneath its finish.");

            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            venting = Object.FindFirstObjectByType<AuthoredSpineVentingObjective>(FindObjectsInactive.Include);
            tower = Object.FindFirstObjectByType<AuthoredSpineTowerReadability>(FindObjectsInactive.Include);
            Assert.That(venting.PresentationState, Is.EqualTo(SpineBerthPresentationState.DormantPressurized));
            Assert.That(tower.PresentationState, Is.EqualTo(SpineTowerPresentationState.PressurizedLocked));
        }

        private static void _captureCamera(Camera camera, string path)
        {
            Assert.That(camera, Is.Not.Null);
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
