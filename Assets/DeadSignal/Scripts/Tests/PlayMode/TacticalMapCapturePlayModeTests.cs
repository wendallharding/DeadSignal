using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using DeadSignal.Application;
using DeadSignal.Presentation;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class TacticalMapCapturePlayModeTests
    {
        [UnityTest]
        public IEnumerator TacticalMap_CapturesOpeningPoweredAndControllerZoomStates()
        {
            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_P37_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(captureDirectory) || UnityEngine.Application.isBatchMode)
            {
                yield break;
            }

            var originalWidth = Screen.width;
            var originalHeight = Screen.height;
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var map = Object.FindFirstObjectByType<MissionClarityHud>();
            var shell = Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include);
            Assert.That(game, Is.Not.Null);
            Assert.That(map, Is.Not.Null);
            Assert.That(shell, Is.Not.Null);

            if (shell.IsMenuVisible)
            {
                var start = System.Array.Find(shell.GetComponentsInChildren<Button>(true),
                    button => button.name == "Start Run");
                start.onClick.Invoke();
                while (shell.IsTransitioning)
                {
                    yield return null;
                }
            }

            Directory.CreateDirectory(captureDirectory);
            try
            {
                yield return _capture(game, captureDirectory, "P37-Opening-Fit.png", 1280, 720);

                game.DebugSetTimeScale(1f);
                game.DebugActivateSpineTower();
                game.DebugOpenShortcut();
                yield return null;
                yield return _capture(game, captureDirectory, "P37-Powered-Route.png", 1600, 900);

                Screen.SetResolution(3440, 1440, false);
                yield return null;
                yield return null;
                game.DebugSetTimeScale(0f);
                var gamepad = InputSystem.AddDevice<Gamepad>();
                try
                {
                    InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.RightShoulder));
                    yield return null;
                    InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = new Vector2(0.6f, 0.25f) });
                    yield return null;
                    Assert.That(map.TacticalMapZoom, Is.EqualTo(1.5f).Within(0.01f));
                    yield return new WaitForSecondsRealtime(0.15f);
                    yield return _writeScreenshot(captureDirectory, "P37-Controller-Zoom.png");
                }
                finally
                {
                    InputSystem.RemoveDevice(gamepad);
                }
            }
            finally
            {
                game.DebugSetTimeScale(1f);
                Screen.SetResolution(originalWidth, originalHeight, false);
            }
        }

        private static IEnumerator _capture(
            DeadSignalGame game,
            string directory,
            string fileName,
            int width,
            int height)
        {
            Screen.SetResolution(width, height, false);
            yield return null;
            yield return null;
            game.DebugSetTimeScale(0f);
            yield return null;
            yield return new WaitForSecondsRealtime(0.15f);
            yield return _writeScreenshot(directory, fileName);
        }

        private static IEnumerator _writeScreenshot(string directory, string fileName)
        {
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                File.WriteAllBytes(Path.Combine(directory, fileName), texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
