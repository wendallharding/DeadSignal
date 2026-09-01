using System.Collections;
using System.IO;
using DeadSignal.Application;
using DeadSignal.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class HudCompositionPlayModeTests
    {
        [UnityTest]
        public IEnumerator RunHud_UsesSafeResponsiveCompositionAtTargetAspectRatios()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var shell = Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include);
            Assert.That(shell, Is.Not.Null);
            if (shell.IsMenuVisible)
            {
                var start = System.Array.Find(shell.GetComponentsInChildren<Button>(true), button => button.name == "Start Run");
                Assert.That(start, Is.Not.Null);
                start.onClick.Invoke();
                while (shell.IsTransitioning)
                {
                    yield return null;
                }
            }

            var layout = Object.FindFirstObjectByType<HudCompositionLayout>(FindObjectsInactive.Include);
            Assert.That(layout, Is.Not.Null);
            Assert.That(layout.IsConfigured, Is.True);
            Assert.That(layout.SafeArea.name, Is.EqualTo("Run HUD"));
            Assert.That(layout.CompositionFrame.name, Is.EqualTo("Composition Frame"));
            Assert.That(layout.CompositionFrame.parent, Is.EqualTo(layout.SafeArea));

            var scaler = layout.GetComponent<CanvasScaler>();
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(1f).Within(0.001f));

            foreach (var resolution in new[]
                     {
                         new Vector2(1280f, 720f),
                         new Vector2(1600f, 900f),
                         new Vector2(3440f, 1440f)
                     })
            {
                var inset = resolution.y / 36f;
                var safeArea = Rect.MinMaxRect(inset, inset, resolution.x - inset, resolution.y - inset);
                layout.ApplyLayout(safeArea, resolution);

                Assert.That(layout.SafeArea.anchorMin.x, Is.EqualTo(safeArea.xMin / resolution.x).Within(0.001f));
                Assert.That(layout.SafeArea.anchorMin.y, Is.EqualTo(safeArea.yMin / resolution.y).Within(0.001f));
                Assert.That(layout.SafeArea.anchorMax.x, Is.EqualTo(safeArea.xMax / resolution.x).Within(0.001f));
                Assert.That(layout.SafeArea.anchorMax.y, Is.EqualTo(safeArea.yMax / resolution.y).Within(0.001f));

                var expectedLogicalWidth = Mathf.Min(layout.MaximumContentWidth, safeArea.width * 1080f / resolution.y);
                Assert.That(layout.CompositionFrame.sizeDelta.x, Is.EqualTo(expectedLogicalWidth).Within(0.01f));
                Assert.That(layout.CompositionFrame.sizeDelta.x, Is.LessThanOrEqualTo(layout.MaximumContentWidth));
                Assert.That(layout.CompositionFrame.sizeDelta.y, Is.Zero.Within(0.001f));
            }

            var signalStatus = layout.CompositionFrame.Find("Signal Status") as RectTransform;
            var objectiveStatus = layout.CompositionFrame.Find("Objective Status") as RectTransform;
            var feedback = layout.CompositionFrame.Find("Feedback") as RectTransform;
            var contextPrompt = layout.CompositionFrame.Find("Context Prompt") as RectTransform;
            Assert.That(signalStatus, Is.Not.Null);
            Assert.That(objectiveStatus, Is.Not.Null);
            Assert.That(feedback, Is.Not.Null);
            Assert.That(contextPrompt, Is.Not.Null);
            Assert.That(signalStatus.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(objectiveStatus.anchorMax, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(feedback.anchorMin.x, Is.EqualTo(0.5f));
            Assert.That(contextPrompt.anchorMin.x, Is.EqualTo(0.5f));
            Assert.That(layout.SafeArea.Find("Objective Beacon"), Is.Not.Null,
                "The objective and enemy edge layer must retain the full safe viewport instead of the capped static frame.");

            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_P32_CAPTURE_DIR");
            if (!string.IsNullOrWhiteSpace(captureDirectory))
            {
                Directory.CreateDirectory(captureDirectory);
                var canvas = layout.GetComponent<Canvas>();
                var camera = Object.FindFirstObjectByType<Camera>();
                Assert.That(canvas, Is.Not.Null);
                Assert.That(camera, Is.Not.Null);
                foreach (var resolution in new[]
                         {
                             new Vector2Int(1280, 720),
                             new Vector2Int(1600, 900),
                             new Vector2Int(3440, 1440)
                         })
                {
                    layout.ApplyLayout(new Rect(Vector2.zero, resolution), resolution);
                    _captureAtResolution(canvas, scaler, camera, resolution, Path.Combine(
                        captureDirectory,
                        $"P32-HUD-{resolution.x}x{resolution.y}.png"));
                    yield return null;
                }
            }
        }

        private static void _captureAtResolution(
            Canvas canvas,
            CanvasScaler scaler,
            Camera camera,
            Vector2Int resolution,
            string path)
        {
            var previousRenderMode = canvas.renderMode;
            var previousWorldCamera = canvas.worldCamera;
            var previousPlaneDistance = canvas.planeDistance;
            var previousScaleMode = scaler.uiScaleMode;
            var previousScaleFactor = scaler.scaleFactor;
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var renderTexture = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false);
            try
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = resolution.y / 1080f;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = Mathf.Max(camera.nearClipPlane + 0.1f, 1f);
                camera.targetTexture = renderTexture;
                Canvas.ForceUpdateCanvases();
                camera.Render();

                RenderTexture.active = renderTexture;
                capture.ReadPixels(new Rect(0f, 0f, resolution.x, resolution.y), 0, 0);
                capture.Apply(false);
                File.WriteAllBytes(path, capture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                canvas.renderMode = previousRenderMode;
                canvas.worldCamera = previousWorldCamera;
                canvas.planeDistance = previousPlaneDistance;
                scaler.uiScaleMode = previousScaleMode;
                scaler.scaleFactor = previousScaleFactor;
                Object.DestroyImmediate(capture);
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }
        }
    }
}
