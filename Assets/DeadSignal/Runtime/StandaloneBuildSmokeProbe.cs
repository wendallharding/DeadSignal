using System;
using System.Collections;
using UnityEngine;

namespace DeadSignal
{
    /// <summary>
    /// Provides a command-line-only health check for the built player without affecting ordinary runs.
    /// </summary>
    public sealed class StandaloneBuildSmokeProbe : MonoBehaviour
    {
        public const string COMMAND_LINE_ARGUMENT = "-deadSignalBuildSmoke";
        public const string PASS_MARKER = "[DEAD SIGNAL STANDALONE SMOKE] PASS";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void _startWhenRequested()
        {
            if (!IsRequested(Environment.GetCommandLineArgs()))
            {
                return;
            }

            var probeObject = new GameObject("Standalone Build Smoke Probe");
            DontDestroyOnLoad(probeObject);
            probeObject.AddComponent<StandaloneBuildSmokeProbe>();
        }

        public static bool IsRequested(string[] arguments)
        {
            return arguments != null && Array.Exists(
                arguments,
                argument => string.Equals(argument, COMMAND_LINE_ARGUMENT, StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;

            var game = FindFirstObjectByType<DeadSignalGame>();
            bool runtimeReady = game != null &&
                                game.transform.Find("Maintenance Drone") != null &&
                                game.transform.Find("Signal Shortcut Gate") != null &&
                                game.HasGeneratedAudio &&
                                game.HasMaintenanceDeckAssets &&
                                game.HasLowSignalWarningTexture &&
                                game.HasTowerActivationSweepTexture;
            if (!runtimeReady)
            {
                Debug.LogError("[DEAD SIGNAL STANDALONE SMOKE] FAIL | Runtime composition is incomplete.");
                Application.Quit(2);
                yield break;
            }

            Debug.Log($"{PASS_MARKER} | Runtime composition and core Resources loaded.");
            Application.Quit(0);
        }
    }
}
