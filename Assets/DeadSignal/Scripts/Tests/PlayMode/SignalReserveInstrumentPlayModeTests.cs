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
    public sealed class SignalReserveInstrumentPlayModeTests
    {
        [UnityTest]
        public IEnumerator AuthoredInstrument_RendersStableCriticalAndTransactionStates()
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

            var instrument = Object.FindFirstObjectByType<SignalReserveInstrument>(FindObjectsInactive.Include);
            var tuning = Resources.Load<SignalHudTuning>("Tuning/SignalHudTuning");
            Assert.That(instrument, Is.Not.Null);
            Assert.That(instrument.IsConfigured, Is.True);
            Assert.That(tuning, Is.Not.Null);

            instrument.Apply(83f, 100f, SignalHudPresentation.Evaluate(83f, 100f, false, 0f, tuning),
                0f, true, 0f, string.Empty, 0f, tuning);
            instrument.Apply(83f, 100f, SignalHudPresentation.Evaluate(83f, 100f, false, 0f, tuning),
                0f, true, 0f, string.Empty, tuning.RecoveryLabelSeconds + 0.1f, tuning);
            Assert.That(instrument.ReserveLabel, Does.StartWith("◆  SIGNAL  083"));
            Assert.That(instrument.FlowLabel, Does.Contain("POWERED"));

            instrument.Apply(19f, 100f, SignalHudPresentation.Evaluate(19f, 100f, true, 1f, tuning),
                1.4f, false, 0f, string.Empty, 0.1f, tuning);
            Assert.That(instrument.ReserveLabel, Does.StartWith("!!  SIGNAL  019"));
            Assert.That(instrument.FlowLabel, Does.Contain("−1.4/s"));

            instrument.Apply(54f, 100f, SignalHudPresentation.Evaluate(54f, 100f, false, 0f, tuning),
                0f, true, 16f, "SHORTCUT", 0.1f, tuning);
            Assert.That(instrument.TransactionLabel, Is.EqualTo("PREVIEW  SHORTCUT  −16  →  038"));
            Assert.That(instrument.TransactionMarkerRatio, Is.EqualTo(0.38f).Within(0.001f));

            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_P33_CAPTURE_DIR");
            if (!string.IsNullOrWhiteSpace(captureDirectory))
            {
                Directory.CreateDirectory(captureDirectory);
                var canvas = instrument.GetComponentInParent<Canvas>();
                var scaler = canvas.GetComponent<CanvasScaler>();
                var camera = Object.FindFirstObjectByType<Camera>();
                _applyAndCapture(instrument, tuning, canvas, scaler, camera, 83f, 0f, true, 0f, string.Empty,
                    new Vector2Int(1600, 900), Path.Combine(captureDirectory, "P33-Signal-Stable-1600x900.png"));
                _applyAndCapture(instrument, tuning, canvas, scaler, camera, 19f, 1.4f, false, 0f, string.Empty,
                    new Vector2Int(1280, 720), Path.Combine(captureDirectory, "P33-Signal-Critical-1280x720.png"));
                _applyAndCapture(instrument, tuning, canvas, scaler, camera, 54f, 0f, true, 16f, "SHORTCUT",
                    new Vector2Int(3440, 1440), Path.Combine(captureDirectory, "P33-Signal-Preview-3440x1440.png"));
            }
        }

        private static void _applyAndCapture(
            SignalReserveInstrument instrument,
            SignalHudTuning tuning,
            Canvas canvas,
            CanvasScaler scaler,
            Camera camera,
            float signal,
            float drain,
            bool powered,
            float cost,
            string transaction,
            Vector2Int resolution,
            string path)
        {
            instrument.Apply(signal, 100f, SignalHudPresentation.Evaluate(signal, 100f, true, 0f, tuning),
                drain, powered, cost, transaction, 0.1f, tuning);
            instrument.Apply(signal, 100f, SignalHudPresentation.Evaluate(signal, 100f, true, 0f, tuning),
                drain, powered, cost, transaction, tuning.RecoveryLabelSeconds + 0.1f, tuning);
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
