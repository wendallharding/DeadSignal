using Reflex.Core;
using Reflex.Injectors;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSignal
{
    public static class DeadSignalBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void _createFirstPlayable()
        {
            if (Object.FindFirstObjectByType<DeadSignalGame>() != null)
            {
                return;
            }

            var root = new GameObject("DEAD SIGNAL — Runtime First Playable");
            root.SetActive(false);

            var comfortSettings = new ComfortSettings();
            var combatFeedback = root.AddComponent<CombatFeedbackController>();
            root.AddComponent<DeadSignalGame>();

            Container container = new ContainerBuilder()
                .SetName("DEAD SIGNAL Runtime")
                .RegisterValue(comfortSettings, new[] { typeof(IComfortSettings) })
                .RegisterValue(combatFeedback, new[] { typeof(ICombatFeedback) })
                .Build();
            GameObjectInjector.InjectObject(root, container);
            root.SetActive(true);
        }
    }
}
