using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DeadSignal.Application;
using DeadSignal.Combat;
using DeadSignal.Missions;
using DeadSignal.World;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSignal.Diagnostics
{
    /// <summary>Captures opt-in rendering evidence from a development player without affecting ordinary runs.</summary>
    public sealed class StandaloneRenderingBudgetProbe : MonoBehaviour
    {
        public const string COMMAND_LINE_ARGUMENT = "-deadSignalRenderBudget";
        public const string REPORT_ARGUMENT = "-deadSignalRenderBudgetReport";
        public const string CAPTURE_DIRECTORY_ARGUMENT = "-deadSignalRenderBudgetCaptureDir";
        public const string STATE_ARGUMENT = "-deadSignalRenderBudgetState";
        public const string PASS_MARKER = "[DEAD SIGNAL RENDER BUDGET] PASS";

        private const int WARMUP_FRAMES = 60;
        private const int SAMPLE_FRAMES = 240;
        private const int TOP_OWNER_COUNT = 8;

        private static bool s_started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void _startWhenRequested()
        {
            if (s_started || !IsRequested(Environment.GetCommandLineArgs()))
            {
                return;
            }

            s_started = true;
            var probeObject = new GameObject("Standalone Rendering Budget Probe");
            DontDestroyOnLoad(probeObject);
            probeObject.AddComponent<StandaloneRenderingBudgetProbe>();
        }

        public static bool IsRequested(string[] arguments)
        {
            return arguments != null && Array.Exists(
                arguments,
                argument => string.Equals(argument, COMMAND_LINE_ARGUMENT, StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerator Start()
        {
            if (!Debug.isDebugBuild)
            {
                Debug.LogError("[DEAD SIGNAL RENDER BUDGET] FAIL | Probe requires a development player.");
                UnityEngine.Application.Quit(2);
                yield break;
            }

            QualitySettings.vSyncCount = 0;
            UnityEngine.Application.targetFrameRate = -1;
            Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
            yield return _waitFrames(10);

            var arguments = Environment.GetCommandLineArgs();
            var requestedState = _argumentValue(arguments, STATE_ARGUMENT);
            var samples = new List<RenderBudgetSample>();
            switch (requestedState?.ToLowerInvariant())
            {
                case "quiet":
                    yield return _stageQuiet();
                    yield return _sampleCurrentState("Quiet Central", samples);
                    break;
                case "mixed":
                    yield return _stageMixedCombat();
                    yield return _sampleCurrentState("Mixed Combat", samples);
                    break;
                case "trial":
                    yield return _stageSecurityTrial();
                    yield return _sampleCurrentState("Security Trial Active", samples);
                    break;
                case "extraction":
                    yield return _stageExtraction();
                    yield return _sampleCurrentState("Extraction Pursuit", samples);
                    break;
                default:
                    Debug.LogError($"[DEAD SIGNAL RENDER BUDGET] FAIL | Unknown state '{requestedState}'.");
                    UnityEngine.Application.Quit(2);
                    yield break;
            }

            var reportPath = _argumentValue(arguments, REPORT_ARGUMENT);
            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                var directory = Path.GetDirectoryName(reportPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(reportPath, _buildReport(samples));
            }

            Debug.Log($"{PASS_MARKER} | {samples.Count} representative states captured.");
            UnityEngine.Application.Quit(0);
        }

        private static IEnumerator _stageQuiet()
        {
            var game = FindFirstObjectByType<DeadSignalGame>();
            game.SetMainMenuOpen(false);
            game.DebugTeleport(DebugLocation.CentralTower);
            yield return _waitFrames(45);
        }

        private static IEnumerator _stageMixedCombat()
        {
            var game = FindFirstObjectByType<DeadSignalGame>();
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

        private static IEnumerator _stageSecurityTrial()
        {
            var game = FindFirstObjectByType<DeadSignalGame>();
            var chamber = FindFirstObjectByType<AuthoredCombatChamber>();
            var scene = FindFirstObjectByType<DeadSignalSceneReferences>();
            game.SetMainMenuOpen(false);
            game.DebugCommitSecurityTrial();
            scene.Player.position = chamber.LockdownThreshold.TransformPoint(new Vector3(0f, 0f, 1f));
            if (!chamber.TryBeginLockdown(scene.Player.position))
            {
                throw new InvalidOperationException("Security Trial lockdown could not begin.");
            }

            yield return _waitFrames(3);
            if (!chamber.AdvancePhase() || !chamber.AdvancePhase())
            {
                throw new InvalidOperationException("Security Trial could not reach phase three.");
            }

            game.DebugSpawnThreat(SecurityReinforcement.Sapper);
            game.DebugSetThreatsFrozen(true);
            scene.Player.position = chamber.ArenaPosition + Vector3.back * 1.8f;
            yield return _waitFrames(45);
        }

        private static IEnumerator _stageExtraction()
        {
            var game = FindFirstObjectByType<DeadSignalGame>();
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
            var camera = FindFirstObjectByType<DeadSignalSceneReferences>().PlayerCamera;
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

            for (var index = 0; index < WARMUP_FRAMES; index++)
            {
                camera.Render();
                yield return null;
            }

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
            Debug.Log(sample.ToReportLine());

            var captureDirectory = _argumentValue(Environment.GetCommandLineArgs(), CAPTURE_DIRECTORY_ARGUMENT);
            if (!string.IsNullOrWhiteSpace(captureDirectory))
            {
                Directory.CreateDirectory(captureDirectory);
                _capture(renderTexture, Path.Combine(captureDirectory, $"P57B-{_fileToken(label)}-1600x900.png"));
            }

            camera.targetTexture = previousTarget;
            UnityEngine.Object.Destroy(renderTexture);
        }

        private static RenderInventory _inventory(Camera camera)
        {
            var game = FindFirstObjectByType<DeadSignalGame>();
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var visibleRenderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(renderer => renderer.enabled && GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                .ToArray();
            var transparentRenderers = visibleRenderers.Count(renderer => renderer.sharedMaterials.Any(material =>
                material != null && material.renderQueue >= (int)RenderQueue.Transparent));
            var visibleMaterials = visibleRenderers.SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null).Distinct().ToArray();
            var enabledLights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(light => light.enabled).ToArray();
            var ownerGroups = visibleRenderers
                .GroupBy(renderer => _ownerName(renderer.transform, game != null ? game.transform : null))
                .Select(group => new RenderOwner(
                    group.Key,
                    group.Count(),
                    group.Sum(renderer => Math.Max(1, renderer.sharedMaterials.Length)),
                    group.Count(renderer => renderer.shadowCastingMode != ShadowCastingMode.Off)))
                .OrderByDescending(owner => owner.EstimatedSubmissions)
                .ThenBy(owner => owner.Name)
                .Take(TOP_OWNER_COUNT)
                .ToArray();
            return new RenderInventory(
                visibleRenderers.Length,
                transparentRenderers,
                visibleMaterials.Length,
                visibleMaterials.Count(material => material.name.EndsWith(" (Instance)")),
                enabledLights.Length,
                enabledLights.Count(light => light.shadows != LightShadows.None),
                ownerGroups);
        }

        private static string _ownerName(Transform renderer, Transform gameRoot)
        {
            if (gameRoot != null && renderer.IsChildOf(gameRoot))
            {
                var owner = renderer;
                while (owner.parent != null && owner.parent != gameRoot)
                {
                    owner = owner.parent;
                }

                return owner.name;
            }

            return renderer.root.name;
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

        private static string _argumentValue(string[] arguments, string argumentName)
        {
            if (arguments == null)
            {
                return null;
            }

            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], argumentName, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }

            return null;
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
            builder.AppendLine("# P57B development-player rendering evidence");
            builder.AppendLine();
            builder.AppendLine("Unity 6000.3.11f1 Windows development player, D3D11, 1600x900 offscreen camera pass, " +
                               $"{WARMUP_FRAMES}-frame warmup and {SAMPLE_FRAMES}-frame sample per state. " +
                               "Owner estimates count visible material slots plus one contribution per shadow-casting renderer; " +
                               "they attribute likely submission pressure but do not replace a GPU frame-debugger capture.");
            builder.AppendLine();
            builder.AppendLine("| State | Frame p95 (ms) | Draw p95 | Batches p95 | SetPass p95 | GC p95 (B) | " +
                               "Visible renderers | Transparent | Materials | Runtime instances | Lights | Shadows |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
            foreach (var sample in samples)
            {
                builder.AppendLine(sample.ToMarkdownRow());
            }

            builder.AppendLine();
            builder.AppendLine("Soft presentation thresholds: <=300 draw calls and <=250 batches. Owner attribution:");
            foreach (var sample in samples)
            {
                builder.AppendLine();
                builder.AppendLine($"## {sample.Label}");
                builder.AppendLine();
                builder.AppendLine("| Owner | Visible renderers | Material slots | Shadow casters | Estimated submissions |");
                builder.AppendLine("| --- | ---: | ---: | ---: | ---: |");
                foreach (var owner in sample.OwnerGroups)
                {
                    builder.AppendLine(owner.ToMarkdownRow());
                }
            }

            return builder.ToString();
        }

        private static string _fileToken(string label) => label.Replace(" ", "-");

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
                UnityEngine.Object.Destroy(texture);
            }
        }

        private static IEnumerator _waitFrames(int count)
        {
            for (var index = 0; index < count; index++)
            {
                yield return null;
            }
        }

        private readonly struct RenderOwner
        {
            public RenderOwner(string name, int visibleRenderers, int materialSlots, int shadowCasters)
            {
                Name = name;
                VisibleRenderers = visibleRenderers;
                MaterialSlots = materialSlots;
                ShadowCasters = shadowCasters;
            }

            public string Name { get; }
            public int VisibleRenderers { get; }
            public int MaterialSlots { get; }
            public int ShadowCasters { get; }
            public int EstimatedSubmissions => MaterialSlots + ShadowCasters;

            public string ToMarkdownRow() =>
                $"| {Name} | {VisibleRenderers} | {MaterialSlots} | {ShadowCasters} | {EstimatedSubmissions} |";
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
                RenderOwner[] ownerGroups)
            {
                VisibleRenderers = visibleRenderers;
                TransparentRenderers = transparentRenderers;
                VisibleMaterials = visibleMaterials;
                RuntimeMaterialInstances = runtimeMaterialInstances;
                EnabledLights = enabledLights;
                ShadowedLights = shadowedLights;
                OwnerGroups = ownerGroups;
            }

            public int VisibleRenderers { get; }
            public int TransparentRenderers { get; }
            public int VisibleMaterials { get; }
            public int RuntimeMaterialInstances { get; }
            public int EnabledLights { get; }
            public int ShadowedLights { get; }
            public RenderOwner[] OwnerGroups { get; }
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
            public RenderOwner[] OwnerGroups => Inventory.OwnerGroups;

            public string ToReportLine() =>
                $"[DEAD SIGNAL RENDER BUDGET] {Label}: {FrameTimeP95:0.###} ms p95, {DrawCallsP95} draws, " +
                $"{BatchesP95} batches, {SetPassCallsP95} SetPass, {GcAllocatedP95} B GC, " +
                $"{Inventory.VisibleRenderers} renderers, {Inventory.TransparentRenderers} transparent, " +
                $"{Inventory.VisibleMaterials} materials, {Inventory.RuntimeMaterialInstances} runtime instances, " +
                $"{Inventory.EnabledLights} lights/{Inventory.ShadowedLights} shadows.";

            public string ToMarkdownRow() =>
                $"| {Label} | {FrameTimeP95:0.###} | {DrawCallsP95} | {BatchesP95} | {SetPassCallsP95} | " +
                $"{GcAllocatedP95} | {Inventory.VisibleRenderers} | {Inventory.TransparentRenderers} | " +
                $"{Inventory.VisibleMaterials} | {Inventory.RuntimeMaterialInstances} | {Inventory.EnabledLights} | " +
                $"{Inventory.ShadowedLights} |";

            private RenderInventory Inventory { get; }
        }
    }
}
