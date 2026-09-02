using System;
using System.Collections;
using System.IO;
using System.Linq;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class SecuritySwarmerPresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator SwarmerPresentation_CommunicatesGroupPressureContactAndPurgeWithoutMovingCombatRoot()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var reducedFlashesWasEnabled = game.IsReducedFlashesEnabled;
            var highContrastWasEnabled = game.IsHighContrastEnabled;
            if (reducedFlashesWasEnabled) game.DebugToggleReducedFlashes();
            if (highContrastWasEnabled) game.DebugToggleHighContrast();

            game.DebugApplyScenario(DebugScenario.EasternRoomCombat);
            yield return null;
            yield return null;

            var swarmer = game.transform.Find("Security Swarmer 1");
            var controller = swarmer.GetComponent<SecuritySwarmerPresentation>();
            var body = swarmer.Find("Swarmer Body");
            var needle = swarmer.Find("Swarmer Needle");
            var ownedEffects = swarmer.GetComponentsInChildren<LineRenderer>(true)
                .Where(renderer => renderer.gameObject.name.StartsWith("Security Swarmer", StringComparison.Ordinal))
                .ToArray();

            Assert.That(game.HasSwarmerAssets, Is.True);
            Assert.That(game.ActiveSwarmerCount, Is.EqualTo(3));
            Assert.That(controller.IsConfigured, Is.True);
            Assert.That(controller.IsWaking, Is.True);
            Assert.That(body.GetComponent<MeshFilter>().sharedMesh.name, Does.StartWith("SecuritySwarmer"),
                "The Swarmer must use its purpose-built mesh rather than a Unity primitive.");
            Assert.That(body.GetComponent<MeshRenderer>().sharedMaterial.mainTexture, Is.Not.Null,
                "The authored Swarmer material must retain its original texture atlas.");
            Assert.That(swarmer.GetComponentsInChildren<Collider>(true), Is.Empty,
                "Presentation geometry must remain independent from the deterministic collision radius.");
            Assert.That(ownedEffects, Has.Length.EqualTo(3),
                "Entry, pressure/contact, and purge feedback should be prewarmed once by each Swarmer owner.");
            Assert.That(ownedEffects.Select(renderer => renderer.sharedMaterial).Distinct().Count(), Is.EqualTo(1),
                "Every Swarmer event should reuse one owner-scoped tintable material.");
            Assert.That(ownedEffects.Max(renderer => renderer.widthMultiplier), Is.LessThanOrEqualTo(0.035f),
                "Swarmer population effects must remain visually cheaper than specialist shape language.");

            game.DebugSetThreatsFrozen(true);
            var player = game.transform.Find("Maintenance Drone");
            var playerPosition = game.DebugPlayerPosition;
            var activeSwarmers = Object.FindObjectsByType<SecuritySwarmerPresentation>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .OrderBy(presentation => presentation.name)
                .Select(presentation => presentation.transform)
                .ToArray();
            for (var index = 0; index < activeSwarmers.Length; index++)
            {
                activeSwarmers[index].position = playerPosition + player.forward * (2f + index * 0.55f) +
                                                   player.right * (index - 1) * 0.72f;
                activeSwarmers[index].rotation = Quaternion.LookRotation(playerPosition - activeSwarmers[index].position);
                activeSwarmers[index].GetComponent<SecuritySwarmerPresentation>().SetPressure(0.7f + index * 0.15f);
            }

            var rootPosition = swarmer.position;
            var rootScale = swarmer.localScale;
            var restNeedlePosition = needle.localPosition;
            yield return new WaitForSeconds(0.12f);
            Assert.That(controller.Pressure, Is.GreaterThan(0f));
            Assert.That(controller.IsStateEffectVisible, Is.True,
                "A moving or converging Swarmer should retain a narrow rear wake and forward pressure point.");
            Assert.That(needle.localPosition, Is.Not.EqualTo(restNeedlePosition),
                "Convergence pressure should extend the contact silhouette without moving gameplay collision.");
            Assert.That(swarmer.position, Is.EqualTo(rootPosition));
            Assert.That(swarmer.localScale, Is.EqualTo(rootScale));

            controller.PlayContact();
            yield return null;
            Assert.That(controller.IsContactReacting, Is.True);
            Assert.That(controller.IsContactEffectVisible, Is.True,
                "Contact warning should close an amber clamp around the attack direction.");
            Assert.That(swarmer.position, Is.EqualTo(rootPosition));
            _captureWhenRequested("P49-Swarmer-Group-Pressure-1600x900.png", 1600, 900);
            yield return null;

            var effectCountBeforePurge = swarmer.GetComponentsInChildren<LineRenderer>(true).Length;
            var effectMaterial = ownedEffects[0].sharedMaterial;
            var shotsBeforePurge = game.ShotsFired;
            game.DebugSetThreatsFrozen(false);
            game.DebugFireAt(swarmer.position);
            var purgeDeadline = Time.realtimeSinceStartup + 2f;
            while (game.SwarmersPurged == 0 && Time.realtimeSinceStartup < purgeDeadline)
            {
                yield return null;
            }
            game.DebugSetThreatsFrozen(true);
            Assert.That(game.SwarmersPurged, Is.EqualTo(1));
            Assert.That(game.ShotsFired - shotsBeforePurge, Is.EqualTo(1),
                "One fragile Swarmer should enter the rapid purge presentation after one basic bolt.");
            Assert.That(controller.IsPurgeVisible, Is.True);
            Assert.That(controller.IsContactEffectVisible, Is.True,
                "The one-bolt purge should retain source direction through its short collapse.");
            Assert.That(controller.IsPurgeEffectVisible, Is.True);
            Assert.That(swarmer.GetComponentsInChildren<LineRenderer>(true), Has.Length.EqualTo(effectCountBeforePurge),
                "Hit and purge events must reuse the prewarmed effect objects.");
            Assert.That(ownedEffects.All(renderer => renderer.sharedMaterial == effectMaterial), Is.True,
                "Hit and purge events must not create per-event materials.");
            yield return new WaitForSeconds(0.32f);
            Assert.That(controller.IsPurgeVisible, Is.False);
            Assert.That(swarmer.gameObject.activeSelf, Is.False);

            var reducedSwarmer = activeSwarmers[1];
            var reducedController = reducedSwarmer.GetComponent<SecuritySwarmerPresentation>();
            game.DebugToggleReducedFlashes();
            reducedSwarmer.position = playerPosition + Vector3.right * 1.8f;
            reducedSwarmer.rotation = Quaternion.LookRotation(playerPosition - reducedSwarmer.position);
            reducedController.SetPressure(1f);
            reducedController.PlayContact();
            yield return null;
            Assert.That(reducedController.IsStateEffectVisible, Is.True);
            Assert.That(reducedController.IsContactEffectVisible, Is.True);
            Assert.That(reducedController.MaximumEffectAlpha, Is.LessThanOrEqualTo(0.31f),
                "Reduced Flashes must preserve Swarmer pressure and contact shapes while capping opacity.");
            _captureWhenRequested("P49-Swarmer-Reduced-Flashes-1280x720.png", 1280, 720);

            if (!reducedFlashesWasEnabled) game.DebugToggleReducedFlashes();
            if (highContrastWasEnabled) game.DebugToggleHighContrast();
        }

        private static void _captureWhenRequested(string fileName, int width, int height)
        {
            const string ARGUMENT_PREFIX = "-deadSignalSwarmerCaptureDir=";
            var argument = Environment.GetCommandLineArgs()
                .FirstOrDefault(value => value.StartsWith(ARGUMENT_PREFIX, StringComparison.OrdinalIgnoreCase));
            if (argument == null) return;

            var directory = argument.Substring(ARGUMENT_PREFIX.Length).Trim('"');
            Directory.CreateDirectory(directory);
            var camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(candidate => candidate.enabled);
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var previousAspect = camera.aspect;
            try
            {
                camera.aspect = (float)width / height;
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                var capture = new Texture2D(width, height, TextureFormat.RGB24, false);
                capture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                capture.Apply();
                File.WriteAllBytes(Path.Combine(directory, fileName), capture.EncodeToPNG());
                Object.DestroyImmediate(capture);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.aspect = previousAspect;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
