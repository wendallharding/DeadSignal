using DeadSignal.Application;
using DeadSignal.Presentation;
using DeadSignal.World;
using UnityEngine;

namespace DeadSignal.Diagnostics
{
    /// <summary>
    /// Isolates the production Security Trial wing and starts its Room B encounter for focused combat tuning.
    /// The complete authored-world contract remains loaded as hidden scaffolding so the production gameplay stack is reused.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class SecurityTrialCombatTuningScene : MonoBehaviour
    {
        [Header("Combat Lab Start")]
        [SerializeField] private bool m_beginInRoomB = true;
        [SerializeField] private bool m_infiniteSignal;
        [SerializeField] private bool m_invulnerable;

        public bool IsPrepared { get; private set; }
        public bool BeginsInRoomB => m_beginInRoomB;

        private void Awake()
        {
            m_sceneReferences = Object.FindFirstObjectByType<DeadSignalSceneReferences>(FindObjectsInactive.Include);
            m_combatChamber = Object.FindFirstObjectByType<AuthoredCombatChamber>(FindObjectsInactive.Include);
            if (m_sceneReferences == null || m_combatChamber == null || !m_combatChamber.IsComplete)
            {
                Debug.LogError("The Security Trial combat-tuning scene requires the authored world and complete A-B-C wing.", this);
                enabled = false;
                return;
            }

            _isolateSecurityTrialWing();
        }

        private void Start()
        {
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            if (game == null)
            {
                Debug.LogError("The Security Trial combat-tuning scene could not find the DEAD SIGNAL runtime.", this);
                enabled = false;
                return;
            }

            var shell = Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include);
            if (shell != null)
            {
                shell.gameObject.SetActive(false);
            }

            game.SetMainMenuOpen(false);
            game.DebugSetInfiniteSignal(m_infiniteSignal);
            game.DebugSetInvulnerable(m_invulnerable);

            if (m_beginInRoomB)
            {
                game.DebugCommitSecurityTrial();
                m_sceneReferences.Player.SetPositionAndRotation(
                    m_combatChamber.CombatScenario.PlayerAnchor.position,
                    m_combatChamber.CombatScenario.PlayerAnchor.rotation);
            }
            else
            {
                game.DebugStabilizeCore();
                m_sceneReferences.Player.SetPositionAndRotation(
                    m_combatChamber.CommitmentSwitch.position + Vector3.back * 1.2f,
                    m_combatChamber.CommitmentSwitch.rotation);
            }

            IsPrepared = true;
        }

        private void _isolateSecurityTrialWing()
        {
            var wing = m_combatChamber.transform;
            foreach (var obstacle in Object.FindObjectsByType<AuthoredMapObstacle>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (!obstacle.transform.IsChildOf(wing))
                {
                    obstacle.gameObject.SetActive(false);
                }
            }

            foreach (var sceneRenderer in Object.FindObjectsByType<Renderer>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (!sceneRenderer.transform.IsChildOf(wing) && !_isActorPresentation(sceneRenderer.transform))
                {
                    sceneRenderer.enabled = false;
                }
            }

            foreach (var sceneLight in Object.FindObjectsByType<Light>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (sceneLight != m_sceneReferences.KeyLight && !sceneLight.transform.IsChildOf(wing))
                {
                    sceneLight.enabled = false;
                }
            }
        }

        private bool _isActorPresentation(Transform candidate)
        {
            return _isChildOf(candidate, m_sceneReferences.Player) ||
                   _isChildOf(candidate, m_sceneReferences.Warden) ||
                   _isChildOf(candidate, m_sceneReferences.Sapper) ||
                   _isChildOf(candidate, m_sceneReferences.Interceptor) ||
                   _isChildOf(candidate, m_sceneReferences.Suppressor);
        }

        private static bool _isChildOf(Transform candidate, Transform parent)
        {
            return parent != null && (candidate == parent || candidate.IsChildOf(parent));
        }

        private DeadSignalSceneReferences m_sceneReferences;
        private AuthoredCombatChamber m_combatChamber;
    }
}
