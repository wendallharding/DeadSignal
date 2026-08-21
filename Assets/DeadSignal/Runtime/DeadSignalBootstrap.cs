using Reflex.Core;
using Reflex.Injectors;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSignal
{
    public static class DeadSignalBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateFirstPlayable()
        {
            if (Object.FindFirstObjectByType<DeadSignalGame>() != null)
            {
                return;
            }

            var root = new GameObject("DEAD SIGNAL — Runtime First Playable");
            root.SetActive(false);

            var combatFeedback = root.AddComponent<CombatFeedbackController>();
            root.AddComponent<DeadSignalGame>();

            Container container = new ContainerBuilder()
                .SetName("DEAD SIGNAL Runtime")
                .RegisterValue(combatFeedback, new[] { typeof(ICombatFeedback) })
                .Build();
            GameObjectInjector.InjectObject(root, container);
            root.SetActive(true);
        }
    }
}
