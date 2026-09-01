using System.Collections;
using System.IO;
using DeadSignal.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DeadSignal.Tests
{
    public sealed class MainMenuPresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator AuthoredMainMenu_PreservesHierarchyFocusAndAspectCoverage()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var shell = Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include);
            var presentation = Object.FindFirstObjectByType<MainMenuPresentation>(FindObjectsInactive.Include);
            Assert.That(shell, Is.Not.Null);
            Assert.That(presentation, Is.Not.Null);
            shell.DebugShowMenu();
            yield return null;

            var mainPanel = presentation.MainPanel;
            Assert.That(mainPanel, Is.Not.Null);
            Assert.That(mainPanel.anchorMin, Is.EqualTo(new Vector2(0.07f, 0.5f)));
            Assert.That(mainPanel.anchorMax, Is.EqualTo(mainPanel.anchorMin));
            Assert.That(mainPanel.pivot, Is.EqualTo(new Vector2(0f, 0.5f)));
            Assert.That(mainPanel.sizeDelta, Is.EqualTo(new Vector2(560f, 670f)));
            Assert.That(mainPanel.Find("Title").GetComponent<Text>().text, Is.EqualTo("DEAD SIGNAL"));
            Assert.That(mainPanel.Find("Route").GetComponent<Text>().text,
                Is.EqualTo("RESTART  /  EXTEND  /  REBUILD  /  WITHDRAW"));
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Start Run"));
            Assert.That(presentation.SelectionDetail.text, Is.EqualTo("BEGIN STATION RESTORATION"));

            foreach (var viewport in new[]
                     {
                         new Vector2(1280f, 720f),
                         new Vector2(1600f, 900f),
                         new Vector2(3440f, 1440f)
                     })
            {
                var uv = MainMenuPresentation.CalculateBackdropUvRect(new Vector2(1672f, 941f), viewport, Vector2.zero);
                Assert.That(uv.xMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(uv.yMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(uv.xMax, Is.LessThanOrEqualTo(1f));
                Assert.That(uv.yMax, Is.LessThanOrEqualTo(1f));
                Assert.That(Mathf.Min(uv.width, uv.height), Is.GreaterThan(0.4f));

                var logicalWidth = viewport.x * 1080f / viewport.y;
                var panelLeft = logicalWidth * mainPanel.anchorMin.x;
                Assert.That(panelLeft, Is.GreaterThanOrEqualTo(64f));
                Assert.That(panelLeft + mainPanel.sizeDelta.x, Is.LessThan(logicalWidth - 64f));
                Assert.That(mainPanel.sizeDelta.y, Is.LessThan(1080f - 64f));
            }

            var settings = _button(shell, "Settings");
            EventSystem.current.SetSelectedGameObject(settings.gameObject);
            presentation.ApplyPresentationForViewport(new Vector2(1600f, 900f), true, 0f);
            Assert.That(presentation.SelectionRail.anchoredPosition.y,
                Is.EqualTo((settings.transform as RectTransform).anchoredPosition.y).Within(0.01f));
            Assert.That(presentation.SelectionDetail.text, Does.Contain("ACCESSIBILITY"));
            Assert.That(presentation.SignalSweep.anchorMin.x, Is.EqualTo(0.62f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator AuthoredMainMenu_CapturesTargetAspectStatesWhenRequested()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_P38_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(captureDirectory))
            {
                Assert.Pass("P38 capture directory was not requested.");
            }

            Directory.CreateDirectory(captureDirectory);
            var shell = Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include);
            var presentation = Object.FindFirstObjectByType<MainMenuPresentation>(FindObjectsInactive.Include);
            shell.DebugShowMenu();
            yield return null;

            var canvas = presentation.GetComponentInParent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>();
            var camera = Object.FindFirstObjectByType<Camera>();
            Assert.That(canvas, Is.Not.Null);
            Assert.That(scaler, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);

            EventSystem.current.SetSelectedGameObject(_button(shell, "Start Run").gameObject);
            presentation.ApplyPresentationForViewport(new Vector2(1600f, 900f), true, 0f);
            presentation.ApplyPresentationForViewport(new Vector2(1600f, 900f), false, 7.5f);
            _capture(canvas, scaler, camera, new Vector2Int(1600, 900), presentation,
                Path.Combine(captureDirectory, "P38-Main-Start-1600x900.png"), false, 7.5f);

            EventSystem.current.SetSelectedGameObject(_button(shell, "Settings").gameObject);
            presentation.ApplyPresentationForViewport(new Vector2(1280f, 720f), true, 0f);
            _capture(canvas, scaler, camera, new Vector2Int(1280, 720), presentation,
                Path.Combine(captureDirectory, "P38-Main-Controller-1280x720.png"), false, 13f);

            EventSystem.current.SetSelectedGameObject(_button(shell, "Start Run").gameObject);
            presentation.ApplyPresentationForViewport(new Vector2(3440f, 1440f), true, 0f);
            _capture(canvas, scaler, camera, new Vector2Int(3440, 1440), presentation,
                Path.Combine(captureDirectory, "P38-Main-Reduced-Flashes-3440x1440.png"), true, 0f);
        }

        private static void _capture(Canvas canvas, CanvasScaler scaler, Camera camera, Vector2Int resolution,
            MainMenuPresentation presentation, string path, bool reducedMotion, float phase)
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
                presentation.ApplyPresentationForViewport(resolution, reducedMotion, phase);
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

        private static Button _button(DeadSignalShellController shell, string name)
        {
            foreach (var button in shell.GetComponentsInChildren<Button>(true))
            {
                if (button.name == name && button.gameObject.activeInHierarchy)
                {
                    return button;
                }
            }

            Assert.Fail($"Active authored menu button '{name}' was not found.");
            return null;
        }
    }
}
