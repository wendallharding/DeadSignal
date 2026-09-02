using System;
using System.Collections;
using System.IO;
using System.Linq;
using DeadSignal.Application;
using DeadSignal.Combat;
using DeadSignal.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests
{
    public sealed class BasicFirePresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator BasicFire_ReusesAuthoredLaunchRigAndPreservesProjectileRules()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var player = game.transform.Find("Maintenance Drone");
            var presentation = player.GetComponent<PlayerCombatPresentation>();
            var tuning = Resources.Load<SignalBoltPresentationTuning>("Tuning/SignalBoltPresentationTuning");
            var authoredDrone = Resources.Load<GameObject>("Actors/MaintenanceDroneAssembly");
            var toolMesh = authoredDrone.transform.Find("Drone Tool").GetComponent<MeshFilter>().sharedMesh;
            var initialReducedFlashes = game.IsReducedFlashesEnabled;

            try
            {
                if (game.IsReducedFlashesEnabled)
                {
                    game.DebugToggleReducedFlashes();
                }

                Assert.That(presentation, Is.Not.Null);
                Assert.That(tuning, Is.Not.Null);
                Assert.That(toolMesh.vertexCount, Is.GreaterThanOrEqualTo(300),
                    "The authored emitter should retain its crown and paired muzzle prongs.");
                Assert.That(tuning.Speed, Is.EqualTo(13.5f));
                Assert.That(tuning.Lifetime, Is.EqualTo(1.5f));
                Assert.That(tuning.FireCooldown, Is.EqualTo(0.16f));
                Assert.That(tuning.CollisionRadius, Is.EqualTo(0.08f));
                Assert.That(tuning.StartingWidth, Is.EqualTo(0.24f));
                Assert.That(presentation.MuzzleEffectObjectCount, Is.EqualTo(1));
                Assert.That(player.Cast<Transform>().Count(child => child.name == "Basic Fire Presentation"), Is.EqualTo(1));

                game.DebugFireAt(player.position + Vector3.forward * 20f);
                yield return null;

                Assert.That(presentation.RecoilRemaining, Is.GreaterThan(0f));
                Assert.That(presentation.IsLaunchStreakActive, Is.True);
                Assert.That(presentation.IsMuzzleLightActive, Is.True);
                _captureWhenRequested("P42-Basic-Fire-1600x900.png", 1600, 900);

                for (var shot = 0; shot < 3; shot++)
                {
                    yield return new WaitForSeconds(0.17f);
                    game.DebugFireAt(player.position + Vector3.forward * 20f);
                    yield return null;
                }

                Assert.That(presentation.MuzzleEffectObjectCount, Is.EqualTo(1));
                Assert.That(player.Cast<Transform>().Count(child => child.name == "Basic Fire Presentation"), Is.EqualTo(1),
                    "Repeated basic fire must reuse one launch rig instead of allocating a hierarchy per shot.");

                if (!game.IsReducedFlashesEnabled)
                {
                    game.DebugToggleReducedFlashes();
                }
                yield return new WaitForSeconds(0.17f);
                game.DebugFireAt(player.position + Vector3.forward * 20f);
                yield return null;

                Assert.That(presentation.IsLaunchStreakActive, Is.True,
                    "Reduced Flashes should retain a subdued directional launch read.");
                Assert.That(presentation.IsMuzzleLightActive, Is.False,
                    "Reduced Flashes must suppress the transient muzzle light.");
                _captureWhenRequested("P42-Basic-Fire-Reduced-Flashes-1280x720.png", 1280, 720);
            }
            finally
            {
                if (game.IsReducedFlashesEnabled != initialReducedFlashes)
                {
                    game.DebugToggleReducedFlashes();
                }
            }
        }

        private static void _captureWhenRequested(string fileName, int width, int height)
        {
            const string ARGUMENT_PREFIX = "-deadSignalBasicFireCaptureDir=";
            var argument = Environment.GetCommandLineArgs()
                .FirstOrDefault(value => value.StartsWith(ARGUMENT_PREFIX, StringComparison.OrdinalIgnoreCase));
            if (argument == null)
            {
                return;
            }

            var directory = argument.Substring(ARGUMENT_PREFIX.Length).Trim('"');
            Directory.CreateDirectory(directory);
            var camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .First(candidate => candidate.enabled);
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
