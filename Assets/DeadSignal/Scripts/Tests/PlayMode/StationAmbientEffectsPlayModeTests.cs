using System.Collections;
using System.IO;
using System.Linq;
using DeadSignal.Application;
using DeadSignal.Player;
using DeadSignal.Presentation;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class StationAmbientEffectsPlayModeTests
    {
        [UnityTest]
        public IEnumerator AuthoredMachinery_OwnsFixedCulledAmbientEmitters()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var controller = Object.FindFirstObjectByType<StationAmbientEffectsController>();
            var emitters = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(item => item.name.StartsWith("Ambient ")).ToArray();
            Assert.That(game, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Assert.That(game.HasStationAmbientEffectsTuning, Is.True);
            Assert.That(game.StationAmbientEmitterCount, Is.GreaterThanOrEqualTo(9));
            Assert.That(game.StationAmbientParticleSystemCount, Is.EqualTo(game.StationAmbientEmitterCount));
            Assert.That(emitters.Length, Is.EqualTo(game.StationAmbientEmitterCount));
            Assert.That(emitters.All(item => item.main.maxParticles <= 12), Is.True);
            Assert.That(emitters.All(item => item.GetComponent<Collider>() == null), Is.True,
                "Ambient emitters must never become false hazards or movement blockers.");
            Assert.That(emitters.All(item => item.GetComponent<Light>() == null), Is.True,
                "Ambient particles should not add unbudgeted practical lights.");
            Assert.That(emitters.Select(item => item.GetComponent<ParticleSystemRenderer>().sharedMaterial)
                .Distinct().Count(), Is.EqualTo(1), "Every fixed emitter should share one owner-scoped material.");
            Assert.That(game.ActiveStationAmbientEmitterCount, Is.GreaterThan(0));
            Assert.That(game.ActiveStationAmbientEmitterCount, Is.LessThan(game.StationAmbientEmitterCount),
                "The opening camera neighborhood should not simulate the complete station ambience set.");
        }

        [UnityTest]
        public IEnumerator DistancePauseAndReducedFlashes_PreserveTheAmbientBudget()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var references = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var furnace = Object.FindFirstObjectByType<AuthoredFurnaceForgeObjective>();
            var controller = Object.FindFirstObjectByType<StationAmbientEffectsController>();
            var initialReducedFlashes = game.IsReducedFlashesEnabled;
            try
            {
                references.Player.position = furnace.Position;
                yield return null;
                Assert.That(game.ActiveStationAmbientEmitterCount, Is.GreaterThan(0));
                var localParticles = Object.FindObjectsByType<ParticleSystem>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(item => item.name.StartsWith("Ambient ") && item.isPlaying).ToArray();
                Assert.That(localParticles, Is.Not.Empty);

                if (!game.IsReducedFlashesEnabled)
                {
                    game.DebugToggleReducedFlashes();
                }
                yield return null;
                Assert.That(game.StationAmbientMaximumVisibleAlpha, Is.LessThanOrEqualTo(0.4f));

                controller.SetPaused(true);
                yield return null;
                Assert.That(localParticles.All(item => item.isPaused), Is.True);
                controller.SetPaused(false);
                yield return null;
                Assert.That(localParticles.Any(item => item.isPlaying), Is.True);
            }
            finally
            {
                controller.SetPaused(false);
                if (game.IsReducedFlashesEnabled != initialReducedFlashes)
                {
                    game.DebugToggleReducedFlashes();
                }
            }
        }

        [UnityTest]
        public IEnumerator AmbientMachinery_RendersHeatAndReducedCoolantWithoutFalseHazardShapes()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var references = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var furnace = Object.FindFirstObjectByType<AuthoredFurnaceForgeObjective>();
            var coolant = Object.FindFirstObjectByType<AuthoredCoolantReclamationObjective>();
            var cameraController = Object.FindFirstObjectByType<PlayerFollowCamera>();
            var initialReducedFlashes = game.IsReducedFlashesEnabled;
            try
            {
                game.SetMainMenuOpen(false);
                if (game.IsReducedFlashesEnabled)
                {
                    game.DebugToggleReducedFlashes();
                }
                references.Player.position = furnace.Position + Vector3.back * 1.8f;
                cameraController.SnapToFocus(furnace.Position);
                yield return new WaitForSecondsRealtime(1.1f);
                Assert.That(_activeAmbientParticleCount(), Is.GreaterThan(0));
                _captureIfRequested(references.PlayerCamera, "P52-Furnace-Heat-Ambience-1600x900.png", 1600, 900);

                if (!game.IsReducedFlashesEnabled)
                {
                    game.DebugToggleReducedFlashes();
                }
                references.Player.position = coolant.SealPosition + Vector3.back * 1.4f;
                cameraController.SnapToFocus(coolant.SealPosition);
                yield return new WaitForSecondsRealtime(1.1f);
                Assert.That(_activeAmbientParticleCount(), Is.GreaterThan(0));
                _captureIfRequested(references.PlayerCamera,
                    "P52-Coolant-Mist-Reduced-Flashes-1280x720.png", 1280, 720);
            }
            finally
            {
                if (game.IsReducedFlashesEnabled != initialReducedFlashes)
                {
                    game.DebugToggleReducedFlashes();
                }
            }
        }

        private static int _activeAmbientParticleCount()
        {
            return Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(item => item.name.StartsWith("Ambient ")).Sum(item => item.particleCount);
        }

        private static void _captureIfRequested(Camera camera, string fileName, int width, int height)
        {
            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_P52_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(captureDirectory))
            {
                return;
            }

            Directory.CreateDirectory(captureDirectory);
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(captureDirectory, fileName), texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.Destroy(texture);
                Object.Destroy(renderTexture);
            }
        }
    }
}
