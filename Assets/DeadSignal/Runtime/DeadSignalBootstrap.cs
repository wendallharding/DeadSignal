using Reflex.Core;
using Reflex.Injectors;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSignal
{
    public static class DeadSignalBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void _registerSceneBootstrap()
        {
            SceneManager.sceneLoaded -= _handleSceneLoaded;
            SceneManager.sceneLoaded += _handleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void _createInitialPlayable()
        {
            _createFirstPlayable();
        }

        private static void _handleSceneLoaded(Scene loadedScene, LoadSceneMode loadSceneMode)
        {
            _createFirstPlayable();
        }

        private static void _createFirstPlayable()
        {
            if (Object.FindFirstObjectByType<DeadSignalGame>() != null)
            {
                return;
            }

            var root = new GameObject("DEAD SIGNAL — Runtime First Playable");
            root.SetActive(false);

            var comfortSettings = new ComfortSettings();
            var input = new DeadSignalInput();
            var audio = root.AddComponent<DeadSignalAudio>();
            var combatFeedback = root.AddComponent<CombatFeedbackController>();
            var hud = root.AddComponent<DeadSignalHud>();
            var objectiveBeacon = root.AddComponent<ObjectiveBeaconHud>();
            var signalDust = root.AddComponent<SignalDustController>();
            var lowSignalWarning = root.AddComponent<LowSignalWarningController>();
            root.AddComponent<DeadSignalGame>();

            var container = new ContainerBuilder()
                .SetName("DEAD SIGNAL Runtime")
                .RegisterValue(comfortSettings, new[] { typeof(IComfortSettings) })
                .RegisterValue(input, new[] { typeof(IDeadSignalInput) })
                .RegisterValue(audio, new[] { typeof(IDeadSignalAudio) })
                .RegisterValue(combatFeedback, new[] { typeof(ICombatFeedback) })
                .RegisterValue(hud, new[] { typeof(IDeadSignalHud) })
                .RegisterValue(objectiveBeacon, new[] { typeof(IObjectiveBeacon) })
                .RegisterValue(signalDust, new[] { typeof(ISignalDust) })
                .RegisterValue(lowSignalWarning, new[] { typeof(ILowSignalWarning) })
                .Build();
            GameObjectInjector.InjectObject(root, container);
            root.SetActive(true);
        }
    }
}
