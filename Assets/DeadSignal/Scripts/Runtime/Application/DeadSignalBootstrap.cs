using Reflex.Core;
using Reflex.Injectors;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadSignal.Combat;
using DeadSignal.Diagnostics;
using DeadSignal.Player;
using DeadSignal.Presentation;
using DeadSignal.World;

namespace DeadSignal.Application
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

            // The Test Runner and additive tooling can load support scenes before the playable scene.
            // Composition begins only when the authored world contract is present.
            if (Object.FindFirstObjectByType<DeadSignalSceneReferences>(FindObjectsInactive.Include) == null)
            {
                return;
            }

            var root = new GameObject("DEAD SIGNAL — Runtime First Playable");
            root.SetActive(false);

            var comfortSettings = new ComfortSettings();
            var input = new DeadSignalInput();
            var hudPrefab = Resources.Load<GameObject>("UI/DeadSignalHud");
            var shellPrefab = Resources.Load<GameObject>("UI/DeadSignalMainMenu");
            if (hudPrefab == null || shellPrefab == null)
            {
                Debug.LogError("The authored HUD or product-shell prefab was not found in Resources/UI.");
                Object.Destroy(root);
                return;
            }

            var hudInstance = Object.Instantiate(hudPrefab, root.transform);
            hudInstance.name = hudPrefab.name;
            var shellInstance = Object.Instantiate(shellPrefab, hudInstance.transform);
            shellInstance.name = shellPrefab.name;
            var audio = root.AddComponent<DeadSignalAudio>();
            var combatFeedback = root.AddComponent<CombatFeedbackController>();
            var hud = hudInstance.GetComponent<DeadSignalHud>();
            var shell = shellInstance.GetComponent<DeadSignalShellController>();
            var objectiveBeacon = hudInstance.GetComponent<ObjectiveBeaconHud>();
            var signalDust = root.AddComponent<SignalDustController>();
            var lowSignalWarning = hudInstance.GetComponent<LowSignalWarningController>();
            var directionalDamageFeedback = hudInstance.GetComponent<DirectionalDamageFeedbackController>();
            var towerActivationSweep = root.AddComponent<TowerActivationSweepController>();
            var stationStateFeedback = root.AddComponent<StationStateFeedbackController>();
            var game = root.AddComponent<DeadSignalGame>();
            DeadSignalDebugMenu debugMenu = null;
            if (DeadSignalDebugMenu.IsAvailable)
            {
                debugMenu = root.AddComponent<DeadSignalDebugMenu>();
            }

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
                .RegisterValue(directionalDamageFeedback, new[] { typeof(IDirectionalDamageFeedback) })
                .RegisterValue(towerActivationSweep, new[] { typeof(ITowerActivationSweep) })
                .RegisterValue(stationStateFeedback, new[] { typeof(IStationStateFeedback) })
                .Build();
            GameObjectInjector.InjectObject(root, container);
            GameObjectInjector.InjectObject(hudInstance, container);
            root.SetActive(true);
            if (shell == null)
            {
                Debug.LogError("The authored HUD prefab is missing its product-shell controller.");
                return;
            }
            shell.Configure(game, hud, comfortSettings, input);
            debugMenu?.Configure(game);
        }
    }
}
