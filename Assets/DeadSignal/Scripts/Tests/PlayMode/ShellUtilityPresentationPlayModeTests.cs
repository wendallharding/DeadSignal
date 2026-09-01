using System.Collections;
using System.IO;
using DeadSignal.Application;
using DeadSignal.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DeadSignal.Tests
{
    public sealed class ShellUtilityPresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator SettingsAndControls_ExplainActionsConfirmChangesAndRespectTargetAspects()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var shell = Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include);
            var presentation = Object.FindFirstObjectByType<MainMenuPresentation>(FindObjectsInactive.Include);
            shell.DebugShowMenu();
            yield return null;

            _button(shell, "Settings").onClick.Invoke();
            yield return null;
            Assert.That(shell.CurrentPage, Is.EqualTo(ProductShellPage.Settings));
            Assert.That(presentation.SettingsPanel.sizeDelta, Is.EqualTo(new Vector2(900f, 650f)));
            Assert.That(presentation.SettingsPanel.Find("Section").GetComponent<Text>().text,
                Is.EqualTo("SYSTEM  /  ACCESSIBILITY"));
            Assert.That(presentation.UtilityDetail.text, Does.Contain("camera impulses"));
            Assert.That(presentation.UtilityInputHint.text, Does.Contain("ENTER  APPLY"));
            _button(shell, "Steady Camera").onClick.Invoke();
            yield return null;
            Assert.That(presentation.UtilityConfirmation.text, Does.StartWith("PREFERENCE SAVED"));
            _button(shell, "Steady Camera").onClick.Invoke();
            yield return null;

            _button(shell, "Back").onClick.Invoke();
            _button(shell, "Controls").onClick.Invoke();
            yield return null;
            Assert.That(shell.CurrentPage, Is.EqualTo(ProductShellPage.Controls));
            Assert.That(presentation.ControlsPanel.sizeDelta, Is.EqualTo(new Vector2(1000f, 720f)));
            Assert.That(presentation.ControlsPanel.Find("Movement Diagram").GetComponent<RawImage>().texture, Is.Not.Null);
            Assert.That(presentation.ControlsPanel.Find("Aim Diagram").GetComponent<RawImage>().texture, Is.Not.Null);
            Assert.That(presentation.ControlsPanel.Find("Control Diagram Labels").GetComponent<Text>().text,
                Does.Contain("RIGHT STICK  AIM"));

            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadDown));
                yield return null;
                Assert.That(presentation.ControlsPanel.Find("Utility Input Hint").GetComponent<Text>().text,
                    Does.Contain("A  APPLY"));
            }
            finally
            {
                InputSystem.RemoveDevice(gamepad);
            }

            foreach (var viewport in new[]
                     {
                         new Vector2(1280f, 720f),
                         new Vector2(1600f, 900f),
                         new Vector2(3440f, 1440f)
                     })
            {
                _assertPanelInsideViewport(presentation.SettingsPanel, viewport);
                _assertPanelInsideViewport(presentation.ControlsPanel, viewport);
            }
        }

        [UnityTest]
        public IEnumerator PauseSurface_PreservesHeldStateFocusAndDeviceGuidance()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                yield return _pressAndRelease(gamepad, GamepadButton.Start);
                var game = Object.FindFirstObjectByType<DeadSignalGame>();
                var presentation = Object.FindFirstObjectByType<PauseMenuPresentation>(FindObjectsInactive.Include);
                Assert.That(game.IsPaused, Is.True);
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(presentation.PausePanel.gameObject.activeSelf, Is.True);
                Assert.That(presentation.PausePanel.Find("Pause Header").GetComponent<Text>().text, Is.EqualTo("RUN PAUSED"));
                Assert.That(presentation.PausePanel.Find("Pause Subhead").GetComponent<Text>().text,
                    Does.Contain("SIGNAL DRAIN SUSPENDED"));
                Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Resume Run"));
                Assert.That(presentation.SelectionDetail.text, Does.Contain("HELD STATION STATE"));
                Assert.That(presentation.InputHint.text, Does.Contain("MENU  RESUME"));

                var mainMenu = presentation.PausePanel.Find("Main Menu").GetComponent<Button>();
                EventSystem.current.SetSelectedGameObject(mainMenu.gameObject);
                presentation.Apply(true);
                Assert.That(presentation.SelectionRail.anchoredPosition.x,
                    Is.EqualTo((mainMenu.transform as RectTransform).anchoredPosition.x).Within(0.01f));
                Assert.That(presentation.SelectionDetail.text, Does.Contain("END THIS RUN"));
            }
            finally
            {
                Time.timeScale = 1f;
                InputSystem.RemoveDevice(gamepad);
            }
        }

        [UnityTest]
        public IEnumerator ShellUtilitySurfaces_CaptureTargetAspectStatesWhenRequested()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_P39_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(captureDirectory))
            {
                Assert.Pass("P39 capture directory was not requested.");
            }

            Directory.CreateDirectory(captureDirectory);
            var shell = Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include);
            var menuPresentation = Object.FindFirstObjectByType<MainMenuPresentation>(FindObjectsInactive.Include);
            var canvas = menuPresentation.GetComponentInParent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>();
            var camera = Object.FindFirstObjectByType<Camera>();
            shell.DebugShowMenu();
            yield return null;

            _button(shell, "Settings").onClick.Invoke();
            yield return null;
            _capture(canvas, scaler, camera, new Vector2Int(1280, 720), menuPresentation,
                Path.Combine(captureDirectory, "P39-Settings-1280x720.png"));

            _button(shell, "Back").onClick.Invoke();
            _button(shell, "Controls").onClick.Invoke();
            yield return null;
            _capture(canvas, scaler, camera, new Vector2Int(1600, 900), menuPresentation,
                Path.Combine(captureDirectory, "P39-Controls-1600x900.png"));

            _button(shell, "Back").onClick.Invoke();
            _button(shell, "Start Run").onClick.Invoke();
            var transitionDeadline = Time.realtimeSinceStartup + 1f;
            while (shell.IsMenuVisible && Time.realtimeSinceStartup < transitionDeadline)
            {
                yield return null;
            }
            Assert.That(shell.IsMenuVisible, Is.False);
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var gamepad = InputSystem.AddDevice<Gamepad>();
            yield return _pressAndRelease(gamepad, GamepadButton.Start);
            var hud = Object.FindFirstObjectByType<DeadSignalHud>(FindObjectsInactive.Include);
            var pause = Object.FindFirstObjectByType<PauseMenuPresentation>(FindObjectsInactive.Include);
            Assert.That(game.IsPaused, Is.True);
            Assert.That(pause.PausePanel.gameObject.activeSelf, Is.True);
            pause.Apply(true);
            _capture(hud.GetComponentInParent<Canvas>(), hud.GetComponentInParent<Canvas>().GetComponent<CanvasScaler>(), camera,
                new Vector2Int(3440, 1440), null, Path.Combine(captureDirectory, "P39-Pause-3440x1440.png"));
            Time.timeScale = 1f;
            InputSystem.RemoveDevice(gamepad);
        }

        private static void _assertPanelInsideViewport(RectTransform panel, Vector2 viewport)
        {
            var logicalWidth = viewport.x * 1080f / viewport.y;
            var left = logicalWidth * panel.anchorMin.x;
            Assert.That(left, Is.GreaterThanOrEqualTo(48f));
            Assert.That(left + panel.sizeDelta.x, Is.LessThan(logicalWidth - 48f));
            Assert.That(panel.sizeDelta.y, Is.LessThan(1080f - 64f));
        }

        private static IEnumerator _pressAndRelease(Gamepad gamepad, GamepadButton button)
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(button));
            yield return null;
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return null;
        }

        private static void _capture(Canvas canvas, CanvasScaler scaler, Camera camera, Vector2Int resolution,
            MainMenuPresentation presentation, string path)
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
                presentation?.ApplyPresentationForViewport(resolution, true, 0f);
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

            Assert.Fail($"Active authored shell button '{name}' was not found.");
            return null;
        }
    }
}
