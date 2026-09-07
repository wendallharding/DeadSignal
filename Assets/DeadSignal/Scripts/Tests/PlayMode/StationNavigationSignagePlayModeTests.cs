using System;
using System.Collections;
using System.IO;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests
{
    public sealed class StationNavigationSignagePlayModeTests
    {
        [UnityTest]
        public IEnumerator StationRoute_HasTextFreeColliderFreeSignageLayers()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var signage = Object.FindFirstObjectByType<AuthoredStationNavigationSignage>();
            Assert.That(signage, Is.Not.Null);
            Assert.That(signage.IsConfigured, Is.True);
            Assert.That(signage.LayerCount, Is.EqualTo(5));
            Assert.That(signage.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(signage.GetComponentsInChildren<AuthoredMapObstacle>(true), Is.Empty);
            Assert.That(signage.GetComponentsInChildren<TextMesh>(true), Is.Empty,
                "World signage must use learned symbols instead of duplicating objective copy.");

            var expectedLayers = new[]
            {
                "Sector Symbols", "Hazard Bands", "Directional Chevrons", "Room Identifiers", "Powered Return Decals"
            };
            foreach (var layerName in expectedLayers)
            {
                var layer = signage.transform.Find(layerName);
                Assert.That(layer, Is.Not.Null, $"Missing signage layer {layerName}.");
                Assert.That(layer.GetComponent<MeshRenderer>(), Is.Not.Null);
                Assert.That(layer.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
            }

            Assert.That(Resources.Load<GameObject>("Environment/StationNavigationSignageKit"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/StationSectorSymbols"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/StationHazardBands"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/StationDirectionalChevrons"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/StationRoomIdentifiers"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/StationPoweredReturnDecals"), Is.Not.Null);

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var references = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            Assert.That(game, Is.Not.Null);
            Assert.That(references, Is.Not.Null);
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(141),
                "Room B's widened scene authority adds three blockers; collider-free signage must not alter that total.");

            game.DebugTeleport(DebugLocation.CentralTower);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P27-Central-Signage-1600x900.png", 1600, 900);
            references.Player.position = new Vector3(42.5f, 0f, 18.2f);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P27-Spine-Signage-1280x720.png", 1280, 720);
            references.Player.position = new Vector3(42.5f, 0f, 50.2f);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P27-Trial-Signage-1600x900.png", 1600, 900);
        }

        [UnityTest]
        public IEnumerator StationRoute_HighContrastSeparatesHazardsFromPoweredReturnCuesByValueAndShape()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var signage = Object.FindFirstObjectByType<AuthoredStationNavigationSignage>();
            var references = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var initialHighContrast = game.IsHighContrastEnabled;
            if (initialHighContrast)
            {
                game.DebugToggleHighContrast();
            }

            try
            {
                var hazardRenderer = signage.Layers[1];
                var poweredRouteRenderer = signage.Layers[4];
                Assert.That(hazardRenderer.GetComponent<MeshFilter>().sharedMesh,
                    Is.Not.EqualTo(poweredRouteRenderer.GetComponent<MeshFilter>().sharedMesh),
                    "Hazards and powered return cues must retain different authored silhouettes.");
                Assert.That(_luminance(_presentedColor(hazardRenderer)) - _luminance(_presentedColor(poweredRouteRenderer)),
                    Is.LessThan(0.05f),
                    "The normal palette documents the demonstrated grayscale collision corrected by High Contrast.");

                references.Player.position = new Vector3(42.5f, 0f, 50.2f);
                yield return new WaitForSecondsRealtime(0.4f);
                _captureIfRequested(references.PlayerCamera, "P55B-World-Cues-Normal-1600x900.png", 1600, 900,
                    "DEAD_SIGNAL_P55B_CAPTURE_DIR");

                game.DebugToggleHighContrast();
                yield return null;

                Assert.That(signage.IsHighContrastEnabled, Is.True);
                var hazardColor = _presentedColor(hazardRenderer);
                var poweredRouteColor = _presentedColor(poweredRouteRenderer);
                Assert.That(hazardColor.r, Is.EqualTo(hazardColor.g).Within(0.001f));
                Assert.That(hazardColor.g, Is.EqualTo(hazardColor.b).Within(0.001f));
                Assert.That(poweredRouteColor.r, Is.EqualTo(poweredRouteColor.g).Within(0.001f));
                Assert.That(poweredRouteColor.g, Is.EqualTo(poweredRouteColor.b).Within(0.001f));
                Assert.That(_luminance(hazardColor) - _luminance(poweredRouteColor), Is.GreaterThan(0.2f),
                    "High Contrast must separate hazard bands from powered-return cues without relying on hue.");
                Assert.That(signage.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(game.HasPlayerDroneAssets, Is.True);
                Assert.That(game.HasSignalBoltAssets, Is.True);
                Assert.That(game.HasSecurityInterceptorAssets, Is.True);
                Assert.That(game.HasSecuritySuppressorAssets, Is.True);

                _captureIfRequested(references.PlayerCamera, "P55B-World-Cues-Accessible-1280x720.png", 1280, 720,
                    "DEAD_SIGNAL_P55B_CAPTURE_DIR");
            }
            finally
            {
                if (game.IsHighContrastEnabled != initialHighContrast)
                {
                    game.DebugToggleHighContrast();
                }
            }
        }

        private static Color _presentedColor(Renderer renderer)
        {
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            return properties.GetColor("_BaseColor");
        }

        private static float _luminance(Color color) => color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;

        private static void _captureIfRequested(
            Camera camera,
            string fileName,
            int width,
            int height,
            string environmentVariable = "DEAD_SIGNAL_P27_CAPTURE_DIR")
        {
            var captureDirectory = Environment.GetEnvironmentVariable(environmentVariable);
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
                Object.Destroy(renderTexture);
                Object.Destroy(texture);
            }
        }
    }
}
