using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DeadSignal.Editor
{
    /// <summary>Builds an isolated development player for the opt-in rendering-budget probe.</summary>
    public static class DeadSignalRenderingBudgetBuild
    {
        public const string EXECUTABLE_PATH = "Build/P57B/DeadSignalP57B.exe";

        public static void BuildDevelopmentPlayer()
        {
            var enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path) && File.Exists(scene.path))
                .Select(scene => scene.path)
                .ToArray();
            if (enabledScenes.Length == 0)
            {
                throw new BuildFailedException("P57B requires at least one valid enabled scene.");
            }

            var outputPath = Path.GetFullPath(EXECUTABLE_PATH);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException(
                "Could not resolve the P57B build directory."));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = enabledScenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"P57B development-player build failed with {report.summary.totalErrors} errors. " +
                    $"Result: {report.summary.result}.");
            }

            Debug.Log($"[DEAD SIGNAL P57B BUILD] PASS | {outputPath} | " +
                      $"{report.summary.totalSize} bytes | {report.summary.totalTime.TotalSeconds:0.00}s");
        }
    }
}
