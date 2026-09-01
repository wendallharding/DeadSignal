using System.Collections;
using System.IO;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class InteractionPromptHudPlayModeTests
    {
        [UnityTest]
        public IEnumerator AuthoredPrompt_UsesActionStateGlyphsAndNonBlockingTransitions()
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

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var prompt = Object.FindFirstObjectByType<InteractionPromptHud>(FindObjectsInactive.Include);
            Assert.That(game, Is.Not.Null);
            Assert.That(prompt, Is.Not.Null);
            Assert.That(prompt.IsConfigured, Is.True);

            game.DebugTeleport(DebugLocation.CurrentObjective);
            yield return _waitFrames(12);

            Assert.That(prompt.gameObject.activeSelf, Is.True);
            Assert.That(prompt.CurrentState, Is.EqualTo(InteractionPromptState.Available));
            Assert.That(prompt.StateLabel, Is.EqualTo("ACTION READY"));
            Assert.That(prompt.PrimaryGlyph, Is.EqualTo("E"));
            Assert.That(prompt.PrimaryAction, Is.EqualTo("ACTIVATE SIGNAL TOWER"));
            Assert.That(prompt.Detail, Does.Contain("EMERGENCY LINK"));
            Assert.That(prompt.HasSecondaryAction, Is.False);
            Assert.That(prompt.Opacity, Is.GreaterThan(0.95f));

            prompt.Apply(
                new InteractionPromptPresentation(
                    true,
                    InteractionPromptState.Choice,
                    "RB",
                    "OVERDRIVE UPLINK",
                    "−12 SIGNAL  •  4.75s",
                    "X",
                    "STABLE UPLINK  •  FREE  •  6s"),
                0.2f);
            Assert.That(prompt.StateLabel, Is.EqualTo("SELECT ROUTE"));
            Assert.That(prompt.PrimaryGlyph, Is.EqualTo("RB"));
            Assert.That(prompt.SecondaryGlyph, Is.EqualTo("X"));
            Assert.That(prompt.HasSecondaryAction, Is.True);

            prompt.Apply(
                new InteractionPromptPresentation(
                    true,
                    InteractionPromptState.Progress,
                    "HOLD",
                    "MAINTAIN CHAMBER CONTROL",
                    "CALIBRATING  •  1.5s"),
                0.2f);
            Assert.That(prompt.Opacity, Is.EqualTo(1f).Within(0.001f));

            prompt.Apply(
                new InteractionPromptPresentation(
                    true,
                    InteractionPromptState.Progress,
                    "HOLD",
                    "MAINTAIN CHAMBER CONTROL",
                    "CALIBRATING  •  1.4s"),
                1f / 60f);
            Assert.That(prompt.Detail, Is.EqualTo("CALIBRATING  •  1.4s"));
            Assert.That(prompt.Opacity, Is.EqualTo(1f).Within(0.001f),
                "Updating a live countdown must not restart the prompt entrance transition.");

            prompt.Apply(
                new InteractionPromptPresentation(
                    true,
                    InteractionPromptState.Blocked,
                    "",
                    "INSUFFICIENT SIGNAL",
                    "SHORTCUT COST 16  •  RESERVE 9"),
                0.2f);
            Assert.That(prompt.StateLabel, Is.EqualTo("SYSTEM LOCK"));
            Assert.That(prompt.PrimaryGlyph, Is.Empty);
            Assert.That(prompt.HasSecondaryAction, Is.False);

            prompt.Apply(InteractionPromptPresentation.Hidden, 0.2f);
            Assert.That(prompt.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator AuthoredPrompt_CapturesAvailableChoiceAndBlockedStatesWhenRequested()
        {
            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_P35_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(captureDirectory))
            {
                Assert.Pass("P35 capture directory was not requested.");
            }

            Directory.CreateDirectory(captureDirectory);
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var shell = Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include);
            if (shell.IsMenuVisible)
            {
                var start = System.Array.Find(shell.GetComponentsInChildren<Button>(true), button => button.name == "Start Run");
                start.onClick.Invoke();
                while (shell.IsTransitioning)
                {
                    yield return null;
                }
            }

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var prompt = Object.FindFirstObjectByType<InteractionPromptHud>(FindObjectsInactive.Include);
            game.DebugTeleport(DebugLocation.CurrentObjective);
            yield return _waitFrames(12);
            _captureHud(new Vector2Int(1600, 900),
                Path.Combine(captureDirectory, "P35-Prompt-Available-1600x900.png"));

            prompt.Apply(
                new InteractionPromptPresentation(
                    true,
                    InteractionPromptState.Choice,
                    "RB",
                    "OVERDRIVE UPLINK",
                    "−12 SIGNAL  •  4.75s",
                    "X",
                    "STABLE UPLINK  •  FREE  •  6s"),
                0.2f);
            _captureHud(new Vector2Int(1280, 720),
                Path.Combine(captureDirectory, "P35-Prompt-Choice-1280x720.png"));

            prompt.Apply(
                new InteractionPromptPresentation(
                    true,
                    InteractionPromptState.Blocked,
                    "",
                    "INSUFFICIENT SIGNAL",
                    "SHORTCUT COST 16  •  RESERVE 9"),
                0.2f);
            _captureHud(new Vector2Int(3440, 1440),
                Path.Combine(captureDirectory, "P35-Prompt-Blocked-3440x1440.png"));
        }

        private static IEnumerator _waitFrames(int count)
        {
            for (var index = 0; index < count; index++)
            {
                yield return null;
            }
        }

        private static void _captureHud(Vector2Int resolution, string path)
        {
            var canvas = Object.FindFirstObjectByType<DeadSignalHud>(FindObjectsInactive.Include).GetComponent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>();
            var camera = Object.FindFirstObjectByType<Camera>();
            var previousRenderMode = canvas.renderMode;
            var previousWorldCamera = canvas.worldCamera;
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
                scaler.uiScaleMode = previousScaleMode;
                scaler.scaleFactor = previousScaleFactor;
                Object.DestroyImmediate(capture);
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }
        }
    }
}
