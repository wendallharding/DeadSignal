using System.Collections;
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
    public sealed class SpineNetworkReadabilityPlayModeTests
    {
        [UnityTest]
        public IEnumerator TrenchAndTower_ShowAuthoritativeLifecycleAndReset()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var venting = Object.FindFirstObjectByType<AuthoredSpineVentingObjective>(FindObjectsInactive.Include);
            var tower = Object.FindFirstObjectByType<AuthoredSpineTowerReadability>(FindObjectsInactive.Include);
            var returnGate = GameObject.Find("Capacitor Spine Region").GetComponent<AuthoredRouteDoorReadability>();
            Assert.That(game, Is.Not.Null);
            Assert.That(venting, Is.Not.Null);
            Assert.That(tower, Is.Not.Null);
            Assert.That(returnGate, Is.Not.Null);
            Assert.That(venting.HasReadabilityAssets, Is.True);
            Assert.That(tower.IsConfigured, Is.True);
            Assert.That(returnGate.IsConfigured, Is.True);
            Assert.That(venting.PresentationState, Is.EqualTo(SpineBerthPresentationState.DormantPressurized));
            Assert.That(tower.PresentationState, Is.EqualTo(SpineTowerPresentationState.PressurizedLocked));
            Assert.That(returnGate.PresentationState, Is.EqualTo(RouteDoorPresentationState.Locked));

            var pressureConsole = venting.transform.Find("Spine Berth Discharge Control/Pressure Status Console");
            var pressureSelector = venting.transform.Find("Spine Berth Discharge Control/Pressure Selector");
            var towerConsole = tower.transform.Find("Spine Tower Status Console");
            var towerSelector = tower.transform.Find("Spine Tower Network Selector");
            var returnThreshold = returnGate.transform.Find("Spine Return Threshold");
            Assert.That(pressureConsole.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("SpinePressureConsoleReadability"));
            Assert.That(pressureSelector.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("SpinePressureSelectorReadability"));
            Assert.That(pressureConsole.GetComponent<MeshFilter>().sharedMesh.normals.All(normal => normal.y > 0.9f),
                Is.True, "The floor-mounted pressure console must face the gameplay camera.");
            Assert.That(pressureSelector.GetComponent<MeshFilter>().sharedMesh.normals.All(normal => normal.y > 0.9f),
                Is.True, "The mechanical selector must face the gameplay camera.");
            Assert.That(pressureConsole.GetComponent<Renderer>().sharedMaterial.mainTexture.name,
                Is.EqualTo("SpineDischargeTrenchRouteDecal"));
            Assert.That(towerConsole.GetComponent<Renderer>().sharedMaterial.mainTexture.name,
                Is.EqualTo("CapacitorSpineActivationDecal"));
            Assert.That(pressureConsole.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(pressureSelector.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(towerConsole.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(towerSelector.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(returnThreshold.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("RouteDoorThresholdReadability"));
            Assert.That(returnThreshold.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(returnThreshold.gameObject.activeSelf, Is.True);

            game.DebugActivateRelayTower();
            game.DebugCollectNextCache();
            game.DebugInstallRelayPayload();
            game.DebugSelectWeapon(SignalWeaponOverclock.PiercingPulse);
            yield return null;
            Assert.That(venting.PresentationState, Is.EqualTo(SpineBerthPresentationState.VentAvailable));
            Assert.That(tower.PresentationState, Is.EqualTo(SpineTowerPresentationState.PressurizedLocked));

            game.DebugVentSpineBerth();
            yield return null;
            Assert.That(venting.PresentationState, Is.EqualTo(SpineBerthPresentationState.VentingActive));
            Assert.That(tower.PresentationState, Is.EqualTo(SpineTowerPresentationState.ActivationAvailable));
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(venting.PresentationState, Is.EqualTo(SpineBerthPresentationState.Vented));

            game.DebugActivateSpineTower();
            yield return null;
            Assert.That(tower.PresentationState, Is.EqualTo(SpineTowerPresentationState.Activating));
            Assert.That(returnGate.PresentationState, Is.EqualTo(RouteDoorPresentationState.Open));
            Assert.That(returnGate.transform.Find("Capacitor Transfer Bank").gameObject.activeSelf, Is.False);
            Assert.That(returnThreshold.gameObject.activeSelf, Is.True,
                "The powered return must retain a readable threshold after its blocking bank retracts.");
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(tower.PresentationState, Is.EqualTo(SpineTowerPresentationState.Powered));

            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            venting = Object.FindFirstObjectByType<AuthoredSpineVentingObjective>(FindObjectsInactive.Include);
            tower = Object.FindFirstObjectByType<AuthoredSpineTowerReadability>(FindObjectsInactive.Include);
            returnGate = GameObject.Find("Capacitor Spine Region").GetComponent<AuthoredRouteDoorReadability>();
            Assert.That(venting.PresentationState, Is.EqualTo(SpineBerthPresentationState.DormantPressurized));
            Assert.That(tower.PresentationState, Is.EqualTo(SpineTowerPresentationState.PressurizedLocked));
            Assert.That(returnGate.PresentationState, Is.EqualTo(RouteDoorPresentationState.Locked));
        }
    }
}
