using System;
using System.Collections;
using System.IO;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.Presentation;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests
{
    public sealed class StationBackdropPlayModeTests
    {
        [UnityTest]
        public IEnumerator StationBackdrop_CoversArenaWithoutChangingTraversal()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var backdrop = Object.FindFirstObjectByType<AuthoredStationBackdrop>();
            var references = Object.FindFirstObjectByType<DeadSignalSceneReferences>();

            Assert.That(backdrop, Is.Not.Null, "The authored scene should contain its station underdeck backdrop.");
            Assert.That(references, Is.Not.Null);
            const float cameraSafetyMargin = 15f;
            Assert.That(backdrop.Coverage.x,
                Is.GreaterThanOrEqualTo((references.ArenaHalfExtents.x + cameraSafetyMargin) * 2f));
            Assert.That(backdrop.Coverage.y,
                Is.GreaterThanOrEqualTo((references.ArenaHalfExtents.y + cameraSafetyMargin) * 2f));
            Assert.That(backdrop.GetComponentInChildren<Collider>(), Is.Null,
                "The visual underdeck must not create movement, projectile, or NavMesh collision.");
            Assert.That(backdrop.StructureRenderers, Has.Length.EqualTo(4));
            var structureRenderer = backdrop.StructureRenderers[0];
            Assert.That(structureRenderer, Is.Not.Null);
            Assert.That(structureRenderer.name, Is.EqualTo("Modular Underdeck Ribs"));
            Assert.That(structureRenderer.GetComponent<MeshFilter>().sharedMesh.vertexCount,
                Is.GreaterThanOrEqualTo(500));
            Assert.That(structureRenderer.GetComponent<Collider>(), Is.Null,
                "The modular ribs are presentation only and must not become traversable or projectile-authoritative.");
            Assert.That(structureRenderer.bounds.min.x, Is.LessThanOrEqualTo(-backdrop.Coverage.x * 0.45f));
            Assert.That(structureRenderer.bounds.max.x, Is.GreaterThanOrEqualTo(backdrop.Coverage.x * 0.45f));
            Assert.That(structureRenderer.bounds.min.z, Is.LessThanOrEqualTo(-backdrop.Coverage.y * 0.45f));
            Assert.That(structureRenderer.bounds.max.z, Is.GreaterThanOrEqualTo(backdrop.Coverage.y * 0.45f));

            var expectedDepthLayers = new[]
            {
                "Distant Superstructure",
                "Service Shafts",
                "Distant Machinery Silhouettes"
            };
            var highestPoint = float.NegativeInfinity;
            var lowestPoint = float.PositiveInfinity;
            for (var i = 1; i < backdrop.StructureRenderers.Length; i++)
            {
                var depthRenderer = backdrop.StructureRenderers[i];
                Assert.That(depthRenderer, Is.Not.Null);
                Assert.That(depthRenderer.name, Is.EqualTo(expectedDepthLayers[i - 1]));
                Assert.That(depthRenderer.GetComponent<MeshFilter>().sharedMesh.vertexCount,
                    Is.GreaterThanOrEqualTo(160));
                Assert.That(depthRenderer.GetComponent<Collider>(), Is.Null,
                    "Depth and parallax layers must remain presentation-only.");
                Assert.That(depthRenderer.bounds.max.y, Is.LessThan(0f),
                    "Distant station silhouettes must remain below the playable deck.");
                highestPoint = Mathf.Max(highestPoint, depthRenderer.bounds.max.y);
                lowestPoint = Mathf.Min(lowestPoint, depthRenderer.bounds.min.y);
            }
            Assert.That(highestPoint - lowestPoint, Is.GreaterThanOrEqualTo(0.65f),
                "Separated underdeck elevations provide bounded perspective parallax without scripted motion.");

            var renderer = backdrop.GetComponentInChildren<Renderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sharedMaterial.mainTexture.name, Is.EqualTo("StationUnderdeckAlbedo"));
            Assert.That(renderer.bounds.min.x,
                Is.LessThanOrEqualTo(-references.ArenaHalfExtents.x - cameraSafetyMargin));
            Assert.That(renderer.bounds.max.x,
                Is.GreaterThanOrEqualTo(references.ArenaHalfExtents.x + cameraSafetyMargin));
            Assert.That(renderer.bounds.min.z,
                Is.LessThanOrEqualTo(-references.ArenaHalfExtents.y - cameraSafetyMargin));
            Assert.That(renderer.bounds.max.z,
                Is.GreaterThanOrEqualTo(references.ArenaHalfExtents.y + cameraSafetyMargin));

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game, Is.Not.Null);
            game.DebugTeleport(DebugLocation.CentralTower);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P31-Central-Depth-1600x900.png", 1600, 900);
            game.DebugTeleport(DebugLocation.SpineTower);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P31-Spine-Depth-1280x720.png", 1280, 720);
            game.DebugTeleport(DebugLocation.FarEast);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P31-Deep-Core-Depth-1600x900.png", 1600, 900);
            game.DebugTeleport(DebugLocation.Extraction);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P31-Dock-Depth-1600x900.png", 1600, 900);
        }

        private static void _captureIfRequested(Camera camera, string fileName, int width, int height)
        {
            var captureDirectory = Environment.GetEnvironmentVariable("DEAD_SIGNAL_P31_CAPTURE_DIR");
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
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(renderTexture);
            }
        }

        [UnityTest]
        public IEnumerator ForegroundOcclusion_ReconfigureRestoresPreviouslyHiddenRenderers()
        {
            var cameraObject = new GameObject("Occlusion Test Camera");
            var player = new GameObject("Occlusion Test Player");
            var obstacleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var controllerObject = new GameObject("Occlusion Test Controller");
            var footprintMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                cameraObject.transform.position = new Vector3(0f, 3f, -6f);
                cameraObject.transform.LookAt(new Vector3(0f, 0.5f, 0f));
                player.transform.position = Vector3.zero;
                obstacleObject.transform.position = new Vector3(0f, 0.75f, -2f);
                obstacleObject.transform.localScale = new Vector3(2f, 1.5f, 0.5f);
                var obstacle = obstacleObject.AddComponent<AuthoredMapObstacle>();
                var renderer = obstacleObject.GetComponent<Renderer>();
                var controller = controllerObject.AddComponent<ForegroundOcclusionController>();

                controller.Configure(
                    camera,
                    player.transform,
                    new[] { obstacle },
                    new AuthoredForegroundCutaway[0],
                    footprintMaterial);
                yield return null;
                Assert.That(renderer.forceRenderingOff, Is.True);
                Assert.That(controller.VisibleFootprintCount, Is.EqualTo(1));
                var footprint = controllerObject.transform.Find("Foreground Cutaway Footprint");
                Assert.That(footprint, Is.Not.Null,
                    "A hidden collision-authoritative wall needs a visible footprint cue.");
                Assert.That(footprint.GetComponent<Collider>(), Is.Null,
                    "The cutaway cue must not add traversal or projectile collision.");

                controller.Configure(
                    camera,
                    player.transform,
                    new AuthoredMapObstacle[0],
                    new AuthoredForegroundCutaway[0],
                    footprintMaterial);
                Assert.That(renderer.forceRenderingOff, Is.False,
                    "Refreshing authored obstacles must not strand an old cutaway renderer in the hidden state.");
                Assert.That(controller.VisibleFootprintCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(obstacleObject);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(footprintMaterial);
            }
        }

        [UnityTest]
        public IEnumerator ForegroundOcclusion_UsesBoundedTacticalWindowNearPlayer()
        {
            var cameraObject = new GameObject("Tactical Window Camera");
            var player = new GameObject("Tactical Window Player");
            var obstacleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var controllerObject = new GameObject("Tactical Window Controller");
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                cameraObject.transform.position = new Vector3(0f, 6f, -9f);
                cameraObject.transform.LookAt(new Vector3(0f, 0.5f, 0f));
                camera.targetTexture = new RenderTexture(800, 450, 16);
                player.transform.position = Vector3.zero;
                obstacleObject.transform.position = new Vector3(0f, 1.25f, -2.4f);
                obstacleObject.transform.localScale = new Vector3(4f, 1.4f, 0.5f);
                var obstacle = obstacleObject.AddComponent<AuthoredMapObstacle>();
                var renderer = obstacleObject.GetComponent<Renderer>();
                var controller = controllerObject.AddComponent<ForegroundOcclusionController>();

                controller.Configure(camera, player.transform, new[] { obstacle }, new AuthoredForegroundCutaway[0]);
                yield return null;

                var playerPoint = camera.WorldToScreenPoint(player.transform.position + Vector3.up * 0.35f);
                var closestWallPoint = camera.WorldToScreenPoint(new Vector3(0f, renderer.bounds.min.y, renderer.bounds.max.z));
                Assert.That(Mathf.Abs(playerPoint.y - closestWallPoint.y), Is.GreaterThan(24f),
                    "This regression wall should narrowly miss the former fixed cutaway margin.");
                Assert.That(renderer.forceRenderingOff, Is.True,
                    "A tall foreground wall inside the bounded tactical window should cut away before hiding the route.");
            }
            finally
            {
                if (cameraObject.TryGetComponent<Camera>(out var camera) && camera.targetTexture != null)
                {
                    var targetTexture = camera.targetTexture;
                    camera.targetTexture = null;
                    Object.DestroyImmediate(targetTexture);
                }

                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(obstacleObject);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [UnityTest]
        public IEnumerator ForegroundOcclusion_CutsWideForegroundFaceBeforeItConsumesTheFrame()
        {
            var cameraObject = new GameObject("Wide Foreground Camera");
            var player = new GameObject("Wide Foreground Player");
            var obstacleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var controllerObject = new GameObject("Wide Foreground Controller");
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                cameraObject.transform.position = new Vector3(0f, 6f, -9f);
                cameraObject.transform.LookAt(new Vector3(0f, 0.5f, 0f));
                camera.targetTexture = new RenderTexture(800, 450, 16);
                player.transform.position = Vector3.zero;
                obstacleObject.transform.position = new Vector3(4.5f, 1.5f, -3f);
                obstacleObject.transform.localScale = new Vector3(5f, 2.5f, 1.5f);
                var obstacle = obstacleObject.AddComponent<AuthoredMapObstacle>();
                var renderer = obstacleObject.GetComponent<Renderer>();
                var controller = controllerObject.AddComponent<ForegroundOcclusionController>();

                controller.Configure(camera, player.transform, new[] { obstacle }, new AuthoredForegroundCutaway[0]);
                yield return null;

                Assert.That(renderer.forceRenderingOff, Is.True,
                    "A foreground shell consuming at least a tenth of the frame should not remain opaque at the edge.");
                Assert.That(controller.WideCutawayCount, Is.EqualTo(1));
                var footprint = controllerObject.transform.Find("Foreground Cutaway Footprint");
                Assert.That(footprint, Is.Not.Null);
                Assert.That(footprint.GetComponent<Renderer>().sharedMaterial.name,
                    Is.EqualTo("ForegroundCutawayFootprintWide"));
            }
            finally
            {
                if (cameraObject.TryGetComponent<Camera>(out var camera) && camera.targetTexture != null)
                {
                    var targetTexture = camera.targetTexture;
                    camera.targetTexture = null;
                    Object.DestroyImmediate(targetTexture);
                }

                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(obstacleObject);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [UnityTest]
        public IEnumerator ForegroundOcclusion_UsesSceneAuthoredWallShellBindings()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var bindings = Object.FindObjectsByType<AuthoredForegroundCutaway>(FindObjectsSortMode.None);
            Assert.That(bindings, Has.Length.GreaterThanOrEqualTo(4));

            var hasCentralBoundary = false;
            foreach (var binding in bindings)
            {
                Assert.That(binding.Renderers, Is.Not.Null.And.Not.Empty);
                foreach (var renderer in binding.Renderers)
                {
                    Assert.That(renderer, Is.Not.Null);
                    hasCentralBoundary |= renderer.name == "North Bulkhead" ||
                                          renderer.name == "South Bulkhead" ||
                                          renderer.name == "West Bulkhead";
                }

                if (binding.CollisionOwner != null)
                {
                    Assert.That(binding.CollisionOwner.enabled, Is.True,
                        "Cutaway authoring must reference collision authority without disabling it.");
                }
            }

            Assert.That(hasCentralBoundary, Is.True,
                "The central shell boundaries must participate in the explicit foreground cutaway pass.");
        }

        [UnityTest]
        public IEnumerator ForegroundOcclusion_HidesExplicitSiblingWithoutChangingCollision()
        {
            var cameraObject = new GameObject("Authored Cutaway Camera");
            var player = new GameObject("Authored Cutaway Player");
            var collisionOwnerObject = new GameObject("Authored Collision Owner");
            var wallShell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var bindingObject = new GameObject("Authored Cutaway Binding");
            var controllerObject = new GameObject("Authored Cutaway Controller");
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                cameraObject.transform.position = new Vector3(0f, 3f, -6f);
                cameraObject.transform.LookAt(new Vector3(0f, 0.5f, 0f));
                player.transform.position = Vector3.zero;
                collisionOwnerObject.transform.position = new Vector3(0f, 0f, -2f);
                var collisionOwner = collisionOwnerObject.AddComponent<AuthoredMapObstacle>();
                collisionOwner.Configure(new Vector2(1f, 0.25f));
                wallShell.transform.position = new Vector3(0f, 0.75f, -2f);
                wallShell.transform.localScale = new Vector3(2f, 1.5f, 0.5f);
                var renderer = wallShell.GetComponent<Renderer>();
                var collider = wallShell.GetComponent<Collider>();
                var binding = bindingObject.AddComponent<AuthoredForegroundCutaway>();
                binding.Configure(collisionOwner, renderer);
                var controller = controllerObject.AddComponent<ForegroundOcclusionController>();

                controller.Configure(camera, player.transform, new[] { collisionOwner }, new[] { binding });
                yield return null;

                Assert.That(renderer.forceRenderingOff, Is.True);
                Assert.That(collider.enabled, Is.True,
                    "Presentation cutaways must not disable the shell's existing physical authority.");
                Assert.That(controller.VisibleFootprintCount, Is.EqualTo(1));
                var footprint = controllerObject.transform.Find("Foreground Cutaway Footprint");
                Assert.That(footprint, Is.Not.Null);
                Assert.That(footprint.GetComponent<Renderer>().sharedMaterial.name,
                    Is.EqualTo("ForegroundCutawayFootprintAuthored"));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(bindingObject);
                Object.DestroyImmediate(wallShell);
                Object.DestroyImmediate(collisionOwnerObject);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void ForegroundCutaway_ResourcesArePackagedForRuntime()
        {
            var texture = Resources.Load<Texture2D>("VFX/ForegroundCutawayFootprint");
            var material = Resources.Load<Material>("Materials/ForegroundCutawayFootprint");
            var authoredTexture = Resources.Load<Texture2D>("VFX/ForegroundCutawayFootprintAuthored");
            var authoredMaterial = Resources.Load<Material>("Materials/ForegroundCutawayFootprintAuthored");
            var wideTexture = Resources.Load<Texture2D>("VFX/ForegroundCutawayFootprintWide");
            var wideMaterial = Resources.Load<Material>("Materials/ForegroundCutawayFootprintWide");

            Assert.That(texture, Is.Not.Null);
            Assert.That(material, Is.Not.Null);
            Assert.That(material.mainTexture, Is.SameAs(texture));
            Assert.That(authoredTexture, Is.Not.Null);
            Assert.That(authoredMaterial, Is.Not.Null);
            Assert.That(authoredMaterial.mainTexture, Is.SameAs(authoredTexture));
            Assert.That(wideTexture, Is.Not.Null);
            Assert.That(wideMaterial, Is.Not.Null);
            Assert.That(wideMaterial.mainTexture, Is.SameAs(wideTexture));
        }
    }
}
