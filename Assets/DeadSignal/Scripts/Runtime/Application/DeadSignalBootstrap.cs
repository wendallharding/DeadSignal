using Reflex.Core;
using Reflex.Injectors;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadSignal.Combat;
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
            if (hudPrefab == null)
            {
                Debug.LogError("The authored HUD prefab was not found at Resources/UI/DeadSignalHud.");
                Object.Destroy(root);
                return;
            }

            var hudInstance = Object.Instantiate(hudPrefab, root.transform);
            hudInstance.name = hudPrefab.name;
            var audio = root.AddComponent<DeadSignalAudio>();
            var combatFeedback = root.AddComponent<CombatFeedbackController>();
            var hud = hudInstance.GetComponent<DeadSignalHud>();
            var objectiveBeacon = hudInstance.GetComponent<ObjectiveBeaconHud>();
            var signalDust = root.AddComponent<SignalDustController>();
            var lowSignalWarning = hudInstance.GetComponent<LowSignalWarningController>();
            var towerActivationSweep = root.AddComponent<TowerActivationSweepController>();
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
                .RegisterValue(towerActivationSweep, new[] { typeof(ITowerActivationSweep) })
                .Build();
            GameObjectInjector.InjectObject(root, container);
            GameObjectInjector.InjectObject(hudInstance, container);
            root.SetActive(true);
        }
    }
}
