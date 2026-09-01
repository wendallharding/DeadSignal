using System.Collections;
using System.IO;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.Missions;
using DeadSignal.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DeadSignal.Tests
{
    public sealed class OutcomePresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator DefeatPresentation_StagesEvidenceAndTracksRecoveryFocus()
        {
            yield return _enterDefeat();

            var presentation = Object.FindFirstObjectByType<OutcomePresentation>(FindObjectsInactive.Include);
            Assert.That(presentation, Is.Not.Null);
            Assert.That(presentation.IsConfigured, Is.True);
            Assert.That(presentation.IsDefeatPresentation, Is.True);
            Assert.That(presentation.EvidenceOpacity, Is.LessThan(1f));
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Restart Run"));
            Assert.That(presentation.SelectionDetail, Does.Contain("STATION ENTRY"));

            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(presentation.EvidenceOpacity, Is.EqualTo(1f).Within(0.001f));

            var mainMenu = _activeButton("Main Menu");
            EventSystem.current.SetSelectedGameObject(mainMenu.gameObject);
            yield return null;
            Assert.That(presentation.SelectionDetail, Does.Contain("MISSION CONTROL"));

            presentation.Present(RunOutcome.Destroyed, true);
            Assert.That(presentation.EvidenceOpacity, Is.EqualTo(1f),
                "Reduced Flashes should expose stable evidence immediately instead of staging multiple fades.");
        }

        [UnityTest]
        public IEnumerator DefeatPresentation_CapturesTargetAspectStatesWhenRequested()
        {
            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_P40_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(captureDirectory))
            {
                Assert.Pass("P40 capture directory was not requested.");
            }

            Directory.CreateDirectory(captureDirectory);
            yield return _enterDefeat();
            yield return new WaitForSecondsRealtime(0.8f);

            var presentation = Object.FindFirstObjectByType<OutcomePresentation>(FindObjectsInactive.Include);
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var canvas = presentation.GetComponentInParent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>();
            var camera = Object.FindFirstObjectByType<Camera>();

            EventSystem.current.SetSelectedGameObject(_activeButton("Restart Run").gameObject);
            yield return null;
            _capture(canvas, scaler, camera, new Vector2Int(1600, 900),
                Path.Combine(captureDirectory, "P40-Defeat-Restart-1600x900.png"));

            EventSystem.current.SetSelectedGameObject(_activeButton("Main Menu").gameObject);
            yield return null;
            _capture(canvas, scaler, camera, new Vector2Int(1280, 720),
                Path.Combine(captureDirectory, "P40-Defeat-Main-Menu-1280x720.png"));

            game.DebugToggleReducedFlashes();
            yield return null;
            _capture(canvas, scaler, camera, new Vector2Int(3440, 1440),
                Path.Combine(captureDirectory, "P40-Defeat-Reduced-Flashes-3440x1440.png"));
            game.DebugToggleReducedFlashes();
        }

        [UnityTest]
        public IEnumerator VictoryPresentation_StagesDebriefAndTracksDeploymentFocus()
        {
            yield return _enterVictory();

            var presentation = Object.FindFirstObjectByType<OutcomePresentation>(FindObjectsInactive.Include);
            Assert.That(presentation, Is.Not.Null);
            Assert.That(presentation.IsConfigured, Is.True);
            Assert.That(presentation.IsVictoryPresentation, Is.True);
            Assert.That(presentation.Protocol, Does.Contain("EXTRACTION VERIFIED"));
            Assert.That(presentation.EvidenceLabel, Does.Contain("EXTRACTED TELEMETRY"));
            Assert.That(presentation.OptionsLabel, Is.EqualTo("NEXT DEPLOYMENT"));
            Assert.That(presentation.EvidenceOpacity, Is.LessThan(1f));
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Restart Run"));
            Assert.That(presentation.SelectionDetail, Does.Contain("NEW RECOVERY"));

            var report = presentation.transform.Find("Run Report").GetComponent<Text>();
            Assert.That(report.text, Does.Contain("MISSION "));
            Assert.That(report.text, Does.Contain("CENTRAL > RELAY > SPINE > DOCK"));
            Assert.That(report.text, Does.Contain("COMBAT"));
            Assert.That(report.text, Does.Contain("SIGNAL"));
            Assert.That(report.text, Does.Contain("BUILD"));
            Assert.That(report.text, Does.Contain("CHAIN ARC"));
            Assert.That(report.text, Does.Contain("FEEDBACK SHIELD"));
            Assert.That(report.text, Does.Contain("PIERCING PULSE EVOLVED"));
            Assert.That(report.preferredHeight, Is.LessThanOrEqualTo(report.rectTransform.rect.height));

            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(presentation.EvidenceOpacity, Is.EqualTo(1f).Within(0.001f));

            EventSystem.current.SetSelectedGameObject(_activeButton("Main Menu").gameObject);
            yield return null;
            Assert.That(presentation.SelectionDetail, Does.Contain("MISSION CONTROL"));

            presentation.Present(RunOutcome.Victory, true);
            Assert.That(presentation.EvidenceOpacity, Is.EqualTo(1f),
                "Reduced Flashes should expose the complete debrief immediately instead of staging multiple fades.");
        }

        [UnityTest]
        public IEnumerator VictoryPresentation_CapturesTargetAspectStatesWhenRequested()
        {
            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_P41_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(captureDirectory))
            {
                Assert.Pass("P41 capture directory was not requested.");
            }

            Directory.CreateDirectory(captureDirectory);
            yield return _enterVictory();
            yield return new WaitForSecondsRealtime(1.5f);

            var presentation = Object.FindFirstObjectByType<OutcomePresentation>(FindObjectsInactive.Include);
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var canvas = presentation.GetComponentInParent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>();
            var camera = Object.FindFirstObjectByType<Camera>();

            EventSystem.current.SetSelectedGameObject(_activeButton("Restart Run").gameObject);
            yield return null;
            _capture(canvas, scaler, camera, new Vector2Int(1600, 900),
                Path.Combine(captureDirectory, "P41-Victory-Restart-1600x900.png"));

            EventSystem.current.SetSelectedGameObject(_activeButton("Main Menu").gameObject);
            yield return null;
            _capture(canvas, scaler, camera, new Vector2Int(1280, 720),
                Path.Combine(captureDirectory, "P41-Victory-Main-Menu-1280x720.png"));

            game.DebugToggleReducedFlashes();
            yield return null;
            _capture(canvas, scaler, camera, new Vector2Int(3440, 1440),
                Path.Combine(captureDirectory, "P41-Victory-Reduced-Flashes-3440x1440.png"));
            game.DebugToggleReducedFlashes();
        }

        private static IEnumerator _enterDefeat()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var shell = Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include);
            shell.DebugShowMenu();
            yield return null;
            _menuButton(shell, "Start Run").onClick.Invoke();
            var deadline = Time.realtimeSinceStartup + 1f;
            while (shell.IsTransitioning && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(shell.IsTransitioning, Is.False);
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            if (game.IsReducedFlashesEnabled)
            {
                game.DebugToggleReducedFlashes();
            }
            game.DebugApplyScenario(DebugScenario.Failure);
            yield return null;
        }

        private static IEnumerator _enterVictory()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var shell = Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include);
            shell.DebugShowMenu();
            yield return null;
            _menuButton(shell, "Start Run").onClick.Invoke();
            var deadline = Time.realtimeSinceStartup + 1f;
            while (shell.IsTransitioning && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(shell.IsTransitioning, Is.False);
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            if (game.IsReducedFlashesEnabled)
            {
                game.DebugToggleReducedFlashes();
            }
            game.DebugApplyScenario(DebugScenario.Victory);
            yield return null;
        }

        private static Button _menuButton(DeadSignalShellController shell, string name)
        {
            foreach (var button in shell.GetComponentsInChildren<Button>(true))
            {
                if (button.name == name && button.gameObject.activeInHierarchy)
                {
                    return button;
                }
            }

            Assert.Fail($"Active menu button '{name}' was not found.");
            return null;
        }

        private static Button _activeButton(string name)
        {
            var hud = Object.FindFirstObjectByType<DeadSignalHud>(FindObjectsInactive.Include);
            foreach (var button in hud.GetComponentsInChildren<Button>(true))
            {
                if (button.name == name && button.gameObject.activeInHierarchy)
                {
                    return button;
                }
            }

            Assert.Fail($"Active outcome button '{name}' was not found.");
            return null;
        }

        private static void _capture(Canvas canvas, CanvasScaler scaler, Camera camera, Vector2Int resolution, string path)
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
