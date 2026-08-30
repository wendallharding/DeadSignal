using System.Collections;
using System.Linq;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.Missions;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class ExtractionDockReadabilityPlayModeTests
    {
        [UnityTest]
        public IEnumerator DockUplink_ShowsAuthoritativeLifecycleOutcomeAndReset()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var readability = Object.FindFirstObjectByType<AuthoredExtractionDockReadability>(FindObjectsInactive.Include);
            Assert.That(game, Is.Not.Null);
            Assert.That(readability, Is.Not.Null);
            Assert.That(readability.IsConfigured, Is.True);
            Assert.That(readability.HasStatusTexture, Is.True);
            Assert.That(readability.PresentationState, Is.EqualTo(ExtractionDockPresentationState.Dormant));

            var status = readability.transform.Find("Extraction Uplink Status");
            Assert.That(status, Is.Not.Null);
            Assert.That(status.GetComponent<MeshFilter>().sharedMesh.name, Is.EqualTo("ExtractionUplinkStatusReadability"));
            Assert.That(status.GetComponent<MeshFilter>().sharedMesh.normals.All(normal => normal.y > 0.9f), Is.True,
                "The Dock status glyph must face the top-down gameplay camera.");
            Assert.That(status.GetComponent<Renderer>().sharedMaterial.mainTexture.name,
                Is.EqualTo("ExtractionUplinkStatusGlyph"));
            Assert.That(status.GetComponentsInChildren<Collider>(true), Is.Empty);

            game.DebugActivateTower();
            yield return null;
            Assert.That(readability.PresentationState, Is.EqualTo(ExtractionDockPresentationState.Locked));

            game.DebugMakeExtractionReady();
            yield return null;
            Assert.That(readability.PresentationState, Is.EqualTo(ExtractionDockPresentationState.Available));

            var availableRotation = status.localRotation;
            game.DebugBeginExtraction(ExtractionUplinkMode.Stable);
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(readability.PresentationState, Is.EqualTo(ExtractionDockPresentationState.ActiveProgress));
            Assert.That(readability.ProgressNormalized, Is.GreaterThan(0f));
            Assert.That(Quaternion.Angle(status.localRotation, availableRotation), Is.GreaterThan(0.1f));

            game.DebugCompleteExtraction();
            Assert.That(readability.PresentationState, Is.EqualTo(ExtractionDockPresentationState.Complete));
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(readability.PresentationState, Is.EqualTo(ExtractionDockPresentationState.Victory));

            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            game = Object.FindFirstObjectByType<DeadSignalGame>();
            readability = Object.FindFirstObjectByType<AuthoredExtractionDockReadability>(FindObjectsInactive.Include);
            Assert.That(readability.PresentationState, Is.EqualTo(ExtractionDockPresentationState.Dormant));

            game.DebugApplyScenario(DebugScenario.Failure);
            yield return null;
            Assert.That(readability.PresentationState, Is.EqualTo(ExtractionDockPresentationState.Defeat));

            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            readability = Object.FindFirstObjectByType<AuthoredExtractionDockReadability>(FindObjectsInactive.Include);
            Assert.That(readability.PresentationState, Is.EqualTo(ExtractionDockPresentationState.Dormant));
        }
    }
}
