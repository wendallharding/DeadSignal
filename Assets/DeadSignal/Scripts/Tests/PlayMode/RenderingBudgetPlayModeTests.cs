using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DeadSignal.Application;
using DeadSignal.Combat;
using DeadSignal.Diagnostics;
using DeadSignal.Missions;
using DeadSignal.Presentation;
using DeadSignal.World;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class RenderingBudgetPlayModeTests
    {
        private const int WARMUP_FRAMES = 30;
        private const int SAMPLE_FRAMES = 120;

        [UnityTest]
        public IEnumerator RepresentativeFrames_StayWithinRenderingAndOverdrawBudgets()
        {
            var samples = new List<RenderBudgetSample>();

            yield return _loadAndStageQuiet();
            yield return _sampleCurrentState("Quiet Central", samples);

            yield return _loadAndStageMixedCombat();
            yield return _sampleCurrentState("Mixed Combat", samples);

            yield return _loadAndStageSecurityTrial();
            yield return _sampleCurrentState("Security Trial Active", samples);

            yield return _loadAndStageExtraction();
            yield return _sampleCurrentState("Extraction Pursuit", samples);

            var reportPath = Environment.GetEnvironmentVariable("DEAD_SIGNAL_P57_REPORT_PATH");
            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                var directory = Path.GetDirectoryName(reportPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(reportPath, _buildReport(samples));
            }

            var tuning = Resources.Load<EnvironmentLightingTuning>("Tuning/EnvironmentLightingTuning");
            Assert.That(tuning, Is.Not.Null);
            Assert.That(samples.All(sample => sample.EnabledLights <= tuning.MaximumVisibleRealtimeLights), Is.True,
                "Every representative frame must retain the authored realtime-light ceiling.");
            Assert.That(samples.All(sample => sample.ShadowedLights <= tuning.MaximumShadowedRealtimeLights), Is.True,
                "Every representative frame must retain the authored shadow-light ceiling.");
            Assert.That(samples.All(sample => sample.RuntimeMaterialInstances == 0), Is.True,
                "Representative frames must not create renderer-owned material instances.");
        }

        private static IEnumerator _loadAndStageQuiet()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            game.SetMainMenuOpen(false);
            game.DebugTeleport(DebugLocation.CentralTower);
            yield return _waitFrames(45);
        }

        private static IEnumerator _loadAndStageMixedCombat()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            game.SetMainMenuOpen(false);
            game.DebugActivateTower();
            game.DebugTeleport(DebugLocation.CentralTower);
            game.DebugSpawnThreat(SecurityReinforcement.Warden);
            game.DebugSpawnThreat(SecurityReinforcement.Sapper);
            game.DebugSpawnThreat(SecurityReinforcement.Interceptor);
            game.DebugSpawnThreat(SecurityReinforcement.Suppressor);
            game.DebugSetThreatsFrozen(true);
            var playerPosition = game.DebugPlayerPosition;
            _moveThreat(game, SecurityReinforcement.Warden, playerPosition + new Vector3(-4f, 0f, 2f));
            _moveThreat(game, SecurityReinforcement.Sapper, playerPosition + new Vector3(4f, 0f, 2f));
            _moveThreat(game, SecurityReinforcement.Interceptor, playerPosition + new Vector3(-3f, 0f, -3f));
            _moveThreat(game, SecurityReinforcement.Suppressor, playerPosition + new Vector3(3f, 0f, -3f));
            game.DebugExerciseCombatFeedback();
            yield return _waitFrames(45);
        }

        private static IEnumerator _loadAndStageSecurityTrial()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var chamber = Object.FindFirstObjectByType<AuthoredCombatChamber>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            game.SetMainMenuOpen(false);
            game.DebugCommitSecurityTrial();
            scene.Player.position = chamber.LockdownThreshold.TransformPoint(new Vector3(0f, 0f, 1f));
            Assert.That(chamber.TryBeginLockdown(scene.Player.position), Is.True);
            yield return _waitFrames(3);
            Assert.That(chamber.AdvancePhase(), Is.True);
            Assert.That(chamber.AdvancePhase(), Is.True);
            game.DebugSpawnThreat(SecurityReinforcement.Sapper);
            Assert.That(chamber.Phase, Is.EqualTo(3));
            game.DebugSetThreatsFrozen(true);
            scene.Player.position = chamber.ArenaPosition + Vector3.back * 1.8f;
            yield return _waitFrames(45);
        }

        private static IEnumerator _loadAndStageExtraction()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            game.SetMainMenuOpen(false);
            game.DebugMakeExtractionReady();
            game.DebugBeginExtraction(ExtractionUplinkMode.Stable);
            game.DebugTeleport(DebugLocation.Extraction);
            game.DebugSpawnThreat(SecurityReinforcement.Suppressor);
            game.DebugSetThreatsFrozen(true);
            game.DebugExerciseCombatFeedback();
            yield return _waitFrames(45);
        }

        private static IEnumerator _sampleCurrentState(string label, ICollection<RenderBudgetSample> samples)
        {
            var camera = Object.FindFirstObjectByType<DeadSignalSceneReferences>().PlayerCamera;
            var frameTimes = new long[SAMPLE_FRAMES];
            var drawCalls = new long[SAMPLE_FRAMES];
            var batches = new long[SAMPLE_FRAMES];
            var setPassCalls = new long[SAMPLE_FRAMES];
            var gcAllocated = new long[SAMPLE_FRAMES];
            using var drawCallRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            using var batchRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            using var setPassRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            using var gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            var renderTexture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            camera.targetTexture = renderTexture;

            yield return _waitFrames(WARMUP_FRAMES);
            for (var index = 0; index < SAMPLE_FRAMES; index++)
            {
                camera.Render();
                yield return null;
                frameTimes[index] = (long)(Time.unscaledDeltaTime * 1000000f);
                drawCalls[index] = drawCallRecorder.Valid ? drawCallRecorder.LastValue : -1;
                batches[index] = batchRecorder.Valid ? batchRecorder.LastValue : -1;
                setPassCalls[index] = setPassRecorder.Valid ? setPassRecorder.LastValue : -1;
                gcAllocated[index] = gcRecorder.Valid ? gcRecorder.LastValue : -1;
            }

            var inventory = _inventory(camera);
            var sample = new RenderBudgetSample(
                label,
                _percentile(frameTimes, 0.95f) / 1000f,
                _percentile(drawCalls, 0.95f),
                _percentile(batches, 0.95f),
                _percentile(setPassCalls, 0.95f),
                _percentile(gcAllocated, 0.95f),
                inventory);
            samples.Add(sample);
            TestContext.Out.WriteLine(sample.ToReportLine());

            var captureDirectory = Environment.GetEnvironmentVariable("DEAD_SIGNAL_P57_CAPTURE_DIR");
            if (!string.IsNullOrWhiteSpace(captureDirectory))
            {
                Directory.CreateDirectory(captureDirectory);
                _capture(renderTexture, Path.Combine(captureDirectory, $"P57-{_fileToken(label)}-1600x900.png"));
            }

            camera.targetTexture = previousTarget;
            Object.DestroyImmediate(renderTexture);
        }

        private static RenderInventory _inventory(Camera camera)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var visibleRenderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(renderer => renderer.enabled && GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                .ToArray();
            var transparentRenderers = visibleRenderers.Where(renderer => renderer.sharedMaterials.Any(material =>
                material != null && material.renderQueue >= (int)RenderQueue.Transparent)).ToArray();
            var visibleMaterials = visibleRenderers.SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null).Distinct().ToArray();
            var runtimeMaterialInstances = visibleMaterials.Count(material => material.name.EndsWith(" (Instance)"));
            var enabledLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(light => light.enabled).ToArray();
            var particles = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(system => system.isPlaying).ToArray();
            return new RenderInventory(
                visibleRenderers.Length,
                transparentRenderers.Length,
                visibleMaterials.Length,
                runtimeMaterialInstances,
                enabledLights.Length,
                enabledLights.Count(light => light.shadows != LightShadows.None),
                particles.Length,
                particles.Sum(system => system.particleCount),
                particles.Sum(system => system.main.maxParticles));
        }

        private static void _moveThreat(DeadSignalGame game, SecurityReinforcement reinforcement, Vector3 position)
        {
            var name = reinforcement switch
            {
                SecurityReinforcement.Warden => "Security Warden",
                SecurityReinforcement.Sapper => "Signal Sapper",
                SecurityReinforcement.Interceptor => "Security Interceptor",
                SecurityReinforcement.Suppressor => "Security Suppressor",
                _ => null
            };
            var threat = name == null ? null : game.transform.Find(name);
            if (threat != null)
            {
                threat.position = position;
            }
        }

        private static long _percentile(long[] values, float percentile)
        {
            var validValues = values.Where(value => value >= 0).OrderBy(value => value).ToArray();
            if (validValues.Length == 0)
            {
                return -1;
            }

            var index = Mathf.Clamp(Mathf.CeilToInt(validValues.Length * percentile) - 1, 0, validValues.Length - 1);
            return validValues[index];
        }

        private static string _buildReport(IEnumerable<RenderBudgetSample> samples)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# P57 rendering-budget evidence");
            builder.AppendLine();
            builder.AppendLine("Unity 6000.3.11f1 PlayMode development-Editor proxy, 1600×900, 30-frame warmup and " +
                               "120-frame sample per state. Frame time is wall-frame delta; CPU/GPU player percentiles " +
                               "remain a P60 packaged-player gate. The GC counter includes the Unity Test Framework and " +
                               "explicit camera-render harness, so it is diagnostic noise rather than a gameplay " +
                               "steady-state allocation verdict.");
            builder.AppendLine();
            builder.AppendLine("| State | Frame p95 (ms) | Draw p95 | Batches p95 | SetPass p95 | GC p95 (B) | " +
                               "Visible renderers | Transparent | Materials | Runtime instances | Lights | Shadows | " +
                               "Particle systems | Live particles | Particle capacity |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
            foreach (var sample in samples)
            {
                builder.AppendLine(sample.ToMarkdownRow());
            }

            builder.AppendLine();
            builder.AppendLine("Soft presentation thresholds: ≤300 draw calls and ≤250 batches. Authored lighting " +
                               "ceilings come from EnvironmentLightingTuning. Transparent count is a conservative " +
                               "frustum inventory, not a pixel-perfect overdraw heatmap.");
            return builder.ToString();
        }

        private static void _capture(RenderTexture renderTexture, string path)
        {
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            try
            {
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(texture);
            }
        }

        private static string _fileToken(string label) => label.Replace(" ", "-");

        private static IEnumerator _waitFrames(int count)
        {
            for (var index = 0; index < count; index++)
            {
                yield return null;
            }
        }

        private readonly struct RenderInventory
        {
            public RenderInventory(
                int visibleRenderers,
                int transparentRenderers,
                int visibleMaterials,
                int runtimeMaterialInstances,
                int enabledLights,
                int shadowedLights,
                int activeParticleSystems,
                int liveParticles,
                int particleCapacity)
            {
                VisibleRenderers = visibleRenderers;
                TransparentRenderers = transparentRenderers;
                VisibleMaterials = visibleMaterials;
                RuntimeMaterialInstances = runtimeMaterialInstances;
                EnabledLights = enabledLights;
                ShadowedLights = shadowedLights;
                ActiveParticleSystems = activeParticleSystems;
                LiveParticles = liveParticles;
                ParticleCapacity = particleCapacity;
            }

            public int VisibleRenderers { get; }
            public int TransparentRenderers { get; }
            public int VisibleMaterials { get; }
            public int RuntimeMaterialInstances { get; }
            public int EnabledLights { get; }
            public int ShadowedLights { get; }
            public int ActiveParticleSystems { get; }
            public int LiveParticles { get; }
            public int ParticleCapacity { get; }
        }

        private readonly struct RenderBudgetSample
        {
            public RenderBudgetSample(
                string label,
                float frameTimeP95,
                long drawCallsP95,
                long batchesP95,
                long setPassCallsP95,
                long gcAllocatedP95,
                RenderInventory inventory)
            {
                Label = label;
                FrameTimeP95 = frameTimeP95;
                DrawCallsP95 = drawCallsP95;
                BatchesP95 = batchesP95;
                SetPassCallsP95 = setPassCallsP95;
                GcAllocatedP95 = gcAllocatedP95;
                Inventory = inventory;
            }

            public string Label { get; }
            public float FrameTimeP95 { get; }
            public long DrawCallsP95 { get; }
            public long BatchesP95 { get; }
            public long SetPassCallsP95 { get; }
            public long GcAllocatedP95 { get; }
            public int EnabledLights => Inventory.EnabledLights;
            public int ShadowedLights => Inventory.ShadowedLights;
            public int RuntimeMaterialInstances => Inventory.RuntimeMaterialInstances;

            public string ToReportLine() =>
                $"P57 {Label}: {FrameTimeP95:0.###} ms p95, {DrawCallsP95} draws, {BatchesP95} batches, " +
                $"{SetPassCallsP95} SetPass, {GcAllocatedP95} B GC, {Inventory.VisibleRenderers} renderers, " +
                $"{Inventory.TransparentRenderers} transparent, {Inventory.VisibleMaterials} materials, " +
                $"{Inventory.RuntimeMaterialInstances} runtime instances, {EnabledLights} lights/{ShadowedLights} shadows, " +
                $"{Inventory.ActiveParticleSystems} particle systems, {Inventory.LiveParticles}/{Inventory.ParticleCapacity} particles.";

            public string ToMarkdownRow() =>
                $"| {Label} | {FrameTimeP95:0.###} | {DrawCallsP95} | {BatchesP95} | {SetPassCallsP95} | " +
                $"{GcAllocatedP95} | {Inventory.VisibleRenderers} | {Inventory.TransparentRenderers} | " +
                $"{Inventory.VisibleMaterials} | {Inventory.RuntimeMaterialInstances} | {EnabledLights} | " +
                $"{ShadowedLights} | {Inventory.ActiveParticleSystems} | {Inventory.LiveParticles} | " +
                $"{Inventory.ParticleCapacity} |";

            private RenderInventory Inventory { get; }
        }
    }
}
