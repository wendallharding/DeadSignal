using UnityEngine;

namespace DeadSignal.World
{
    public enum CombatChamberState
    {
        Dormant,
        Armed,
        Lockdown,
        Cleared
    }

    public enum TrialCommitmentPresentationState
    {
        Locked,
        Available,
        CommittedActive,
        Complete
    }

    public enum LockdownChamberPresentationState
    {
        Dormant,
        Available,
        Armed,
        LockedActive,
        Cleared
    }

    public enum TrialCapacitorPresentationState
    {
        Locked,
        Available,
        CollectedActive,
        EmptyVaultComplete
    }

    public sealed class AuthoredCombatChamber : MonoBehaviour
    {
        private const float COMMITMENT_TRANSITION_SECONDS = 0.75f;
        private const float CAPACITOR_COLLECTION_TRANSITION_SECONDS = 0.75f;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform m_commitmentSwitch;
        [SerializeField] private Transform m_lockdownThreshold;
        [SerializeField] private GameObject m_entryDoor;
        [SerializeField] private GameObject m_rewardDoor;
        [SerializeField] private GameObject m_reward;
        [SerializeField] private GameObject m_clearedSignal;
        [SerializeField] private AuthoredCombatScenario m_combatScenario;
        [SerializeField] private float m_interactionRadius = 1.8f;
        [SerializeField] private float m_lockdownTriggerDepth = 0.75f;
        [SerializeField] private float m_rewardSignal = 20f;
        [SerializeField] private Renderer[] m_commitmentReadabilityRenderers;
        [SerializeField] private Transform m_commitmentSelector;
        [SerializeField] private Renderer m_lockdownReadabilityRenderer;
        [SerializeField] private Transform m_lockdownPhaseSelector;
        [SerializeField] private AuthoredRouteDoorReadability m_entryDoorReadability;
        [SerializeField] private AuthoredRouteDoorReadability m_rewardDoorReadability;
        [SerializeField] private Renderer m_capacitorReadabilityRenderer;
        [SerializeField] private Transform m_capacitorSelector;

        public CombatChamberState State { get; private set; }
        public int Phase { get; private set; }
        public Transform CommitmentSwitch => m_commitmentSwitch;
        public Transform LockdownThreshold => m_lockdownThreshold;
        public Vector3 ArenaPosition => m_combatScenario != null && m_combatScenario.CameraFocus != null
            ? m_combatScenario.CameraFocus.position
            : transform.position;
        public Vector3 RewardPosition => m_reward != null ? m_reward.transform.position : transform.position;
        public AuthoredCombatScenario CombatScenario => m_combatScenario;
        public float RewardSignal => m_rewardSignal;
        public bool RewardAvailable => State == CombatChamberState.Cleared && m_reward != null && m_reward.activeSelf;
        public bool HasCommitmentReadabilityAssets =>
            m_commitmentReadabilityRenderers is { Length: > 0 } && m_commitmentSelector != null;
        public bool HasLockdownReadabilityAssets => m_lockdownReadabilityRenderer != null &&
                                                    m_lockdownPhaseSelector != null &&
                                                    m_entryDoorReadability?.IsConfigured == true &&
                                                    m_rewardDoorReadability?.IsConfigured == true &&
                                                    m_capacitorReadabilityRenderer != null &&
                                                    m_capacitorSelector != null;
        public TrialCommitmentPresentationState CommitmentPresentationState { get; private set; } =
            TrialCommitmentPresentationState.Locked;
        public LockdownChamberPresentationState LockdownPresentationState { get; private set; } =
            LockdownChamberPresentationState.Dormant;
        public TrialCapacitorPresentationState CapacitorPresentationState { get; private set; } =
            TrialCapacitorPresentationState.Locked;
        public bool IsComplete => m_commitmentSwitch != null && m_lockdownThreshold != null && m_entryDoor != null &&
                                  m_rewardDoor != null && m_reward != null && m_clearedSignal != null &&
                                  m_combatScenario != null && m_combatScenario.IsComplete;

        private void Update()
        {
            if (m_commitmentTransitionRemaining > 0f)
            {
                m_commitmentTransitionRemaining = Mathf.Max(
                    0f,
                    m_commitmentTransitionRemaining - Time.unscaledDeltaTime);
                _refreshCommitmentPresentation();
            }

            if (m_capacitorCollectionTransitionRemaining > 0f)
            {
                m_capacitorCollectionTransitionRemaining = Mathf.Max(
                    0f,
                    m_capacitorCollectionTransitionRemaining - Time.unscaledDeltaTime);
                _refreshLockdownPresentation();
            }
        }

        public void Configure(
            Transform commitmentSwitch,
            Transform lockdownThreshold,
            GameObject entryDoor,
            GameObject rewardDoor,
            GameObject reward,
            GameObject clearedSignal,
            AuthoredCombatScenario combatScenario,
            float interactionRadius,
            float lockdownTriggerDepth,
            float rewardSignal)
        {
            m_commitmentSwitch = commitmentSwitch;
            m_lockdownThreshold = lockdownThreshold;
            m_entryDoor = entryDoor;
            m_rewardDoor = rewardDoor;
            m_reward = reward;
            m_clearedSignal = clearedSignal;
            m_combatScenario = combatScenario;
            m_interactionRadius = Mathf.Max(0.5f, interactionRadius);
            m_lockdownTriggerDepth = Mathf.Max(0.1f, lockdownTriggerDepth);
            m_rewardSignal = Mathf.Max(0f, rewardSignal);
            ResetState();
        }

        public void ConfigureCommitmentReadability(Renderer[] readabilityRenderers, Transform selector)
        {
            m_commitmentReadabilityRenderers = readabilityRenderers;
            m_commitmentSelector = selector;
            m_hasAppliedCommitmentPresentation = false;
            _refreshCommitmentPresentation();
        }

        public void ConfigureLockdownReadability(
            Renderer lockdownRenderer,
            Transform phaseSelector,
            AuthoredRouteDoorReadability entryDoorReadability,
            AuthoredRouteDoorReadability rewardDoorReadability,
            Renderer capacitorRenderer,
            Transform capacitorSelector)
        {
            m_lockdownReadabilityRenderer = lockdownRenderer;
            m_lockdownPhaseSelector = phaseSelector;
            m_entryDoorReadability = entryDoorReadability;
            m_rewardDoorReadability = rewardDoorReadability;
            m_capacitorReadabilityRenderer = capacitorRenderer;
            m_capacitorSelector = capacitorSelector;
            m_hasAppliedLockdownPresentation = false;
            m_hasAppliedCapacitorPresentation = false;
            _refreshLockdownPresentation();
        }

        public void SetCommitmentAvailable(bool available)
        {
            m_commitmentAvailable = available;
            _refreshCommitmentPresentation();
            _refreshLockdownPresentation();
        }

        public bool CanInteract(Vector3 playerPosition)
        {
            return State == CombatChamberState.Dormant &&
                   _flatDistance(playerPosition, m_commitmentSwitch.position) <= m_interactionRadius;
        }

        public bool TryArm(Vector3 playerPosition)
        {
            if (!CanInteract(playerPosition))
            {
                return false;
            }

            State = CombatChamberState.Armed;
            m_commitmentTransitionRemaining = COMMITMENT_TRANSITION_SECONDS;
            _setDoorOpen(m_entryDoor, m_entryDoorReadability, true);
            _refreshCommitmentPresentation();
            _refreshLockdownPresentation();
            return true;
        }

        public bool TryBeginLockdown(Vector3 playerPosition)
        {
            if (State != CombatChamberState.Armed)
            {
                return false;
            }

            var localPosition = m_lockdownThreshold.InverseTransformPoint(playerPosition);
            if (localPosition.z < m_lockdownTriggerDepth)
            {
                return false;
            }

            State = CombatChamberState.Lockdown;
            Phase = 1;
            _setDoorOpen(m_entryDoor, m_entryDoorReadability, false);
            _refreshCommitmentPresentation();
            _refreshLockdownPresentation();
            return true;
        }

        public bool AdvancePhase()
        {
            if (State != CombatChamberState.Lockdown || Phase >= 3)
            {
                return false;
            }

            Phase++;
            _refreshLockdownPresentation();
            return true;
        }

        public void Complete()
        {
            State = CombatChamberState.Cleared;
            Phase = 0;
            _setDoorOpen(m_entryDoor, m_entryDoorReadability, true);
            _setDoorOpen(m_rewardDoor, m_rewardDoorReadability, true);
            m_clearedSignal.SetActive(true);
            _refreshCommitmentPresentation();
            _refreshLockdownPresentation();
        }

        public bool TryCollectReward(Vector3 playerPosition)
        {
            if (!RewardAvailable || _flatDistance(playerPosition, m_reward.transform.position) > m_interactionRadius)
            {
                return false;
            }

            m_reward.SetActive(false);
            m_capacitorCollectionTransitionRemaining = CAPACITOR_COLLECTION_TRANSITION_SECONDS;
            _refreshLockdownPresentation();
            return true;
        }

        public void ResetState()
        {
            State = CombatChamberState.Dormant;
            Phase = 0;
            m_commitmentAvailable = false;
            m_commitmentTransitionRemaining = 0f;
            m_capacitorCollectionTransitionRemaining = 0f;
            m_hasAppliedCommitmentPresentation = false;
            m_hasAppliedLockdownPresentation = false;
            m_hasAppliedCapacitorPresentation = false;
            _setDoorOpen(m_entryDoor, m_entryDoorReadability, false);
            _setDoorOpen(m_rewardDoor, m_rewardDoorReadability, false);
            if (m_reward != null) m_reward.SetActive(true);
            if (m_clearedSignal != null) m_clearedSignal.SetActive(false);
            _refreshCommitmentPresentation();
            _refreshLockdownPresentation();
        }

        private void _refreshLockdownPresentation()
        {
            var lockdownState = State switch
            {
                CombatChamberState.Armed => LockdownChamberPresentationState.Armed,
                CombatChamberState.Lockdown => LockdownChamberPresentationState.LockedActive,
                CombatChamberState.Cleared => LockdownChamberPresentationState.Cleared,
                _ => m_commitmentAvailable
                    ? LockdownChamberPresentationState.Available
                    : LockdownChamberPresentationState.Dormant
            };
            if (!m_hasAppliedLockdownPresentation || lockdownState != LockdownPresentationState)
            {
                m_hasAppliedLockdownPresentation = true;
                LockdownPresentationState = lockdownState;
                var color = lockdownState switch
                {
                    LockdownChamberPresentationState.Dormant => new Color(0.34f, 0.045f, 0.04f),
                    LockdownChamberPresentationState.Available => new Color(1f, 0.46f, 0.055f),
                    LockdownChamberPresentationState.Armed => new Color(0.95f, 0.05f, 0.72f),
                    LockdownChamberPresentationState.LockedActive => new Color(0.92f, 0.035f, 0.08f),
                    _ => new Color(0.04f, 0.94f, 1f)
                };
                _setRendererColor(
                    m_lockdownReadabilityRenderer,
                    color,
                    lockdownState == LockdownChamberPresentationState.Dormant ? 0.16f : 1.15f);
            }

            if (m_lockdownPhaseSelector != null)
            {
                var phaseAngle = LockdownPresentationState == LockdownChamberPresentationState.LockedActive
                    ? 78f * Mathf.Clamp(Phase, 1, 3)
                    : LockdownPresentationState == LockdownChamberPresentationState.Cleared ? 312f : 0f;
                m_lockdownPhaseSelector.localRotation = Quaternion.Euler(0f, phaseAngle, 0f);
            }

            var capacitorState = State != CombatChamberState.Cleared
                ? TrialCapacitorPresentationState.Locked
                : m_reward != null && m_reward.activeSelf
                    ? TrialCapacitorPresentationState.Available
                    : m_capacitorCollectionTransitionRemaining > 0f
                        ? TrialCapacitorPresentationState.CollectedActive
                        : TrialCapacitorPresentationState.EmptyVaultComplete;
            if (!m_hasAppliedCapacitorPresentation || capacitorState != CapacitorPresentationState)
            {
                m_hasAppliedCapacitorPresentation = true;
                CapacitorPresentationState = capacitorState;
                var color = capacitorState switch
                {
                    TrialCapacitorPresentationState.Locked => new Color(0.34f, 0.045f, 0.04f),
                    TrialCapacitorPresentationState.Available => new Color(1f, 0.46f, 0.055f),
                    TrialCapacitorPresentationState.CollectedActive => new Color(0.95f, 0.05f, 0.72f),
                    _ => new Color(0.04f, 0.94f, 1f)
                };
                _setRendererColor(
                    m_capacitorReadabilityRenderer,
                    color,
                    capacitorState == TrialCapacitorPresentationState.Locked ? 0.16f : 1.15f);
            }

            if (m_capacitorSelector != null)
            {
                var collectionProgress = 1f -
                    m_capacitorCollectionTransitionRemaining / CAPACITOR_COLLECTION_TRANSITION_SECONDS;
                var targetAngle = CapacitorPresentationState switch
                {
                    TrialCapacitorPresentationState.Available => 0f,
                    TrialCapacitorPresentationState.CollectedActive => Mathf.Lerp(0f, 120f, collectionProgress),
                    TrialCapacitorPresentationState.EmptyVaultComplete => 120f,
                    _ => -24f
                };
                m_capacitorSelector.localRotation = Quaternion.Euler(0f, targetAngle, 0f);
            }
        }

        private void _refreshCommitmentPresentation()
        {
            var presentationState = State switch
            {
                CombatChamberState.Armed => TrialCommitmentPresentationState.CommittedActive,
                CombatChamberState.Lockdown => TrialCommitmentPresentationState.CommittedActive,
                CombatChamberState.Cleared => TrialCommitmentPresentationState.Complete,
                _ => m_commitmentAvailable
                    ? TrialCommitmentPresentationState.Available
                    : TrialCommitmentPresentationState.Locked
            };
            if (!m_hasAppliedCommitmentPresentation || presentationState != CommitmentPresentationState)
            {
                m_hasAppliedCommitmentPresentation = true;
                CommitmentPresentationState = presentationState;
                var color = presentationState switch
                {
                    TrialCommitmentPresentationState.Locked => new Color(0.34f, 0.045f, 0.04f),
                    TrialCommitmentPresentationState.Available => new Color(1f, 0.46f, 0.055f),
                    TrialCommitmentPresentationState.CommittedActive => new Color(0.92f, 0.035f, 0.08f),
                    _ => new Color(0.04f, 0.94f, 1f)
                };
                var emissionMultiplier = presentationState == TrialCommitmentPresentationState.Locked ? 0.16f : 1.15f;
                _setCommitmentColors(color, emissionMultiplier);
            }

            if (m_commitmentSelector != null)
            {
                var targetAngle = presentationState switch
                {
                    TrialCommitmentPresentationState.Locked => -28f,
                    TrialCommitmentPresentationState.Available => 0f,
                    TrialCommitmentPresentationState.CommittedActive => Mathf.Lerp(
                        0f,
                        118f,
                        1f - m_commitmentTransitionRemaining / COMMITMENT_TRANSITION_SECONDS),
                    _ => 118f
                };
                m_commitmentSelector.localRotation = Quaternion.Euler(0f, targetAngle, 0f);
            }
        }

        private void _setCommitmentColors(Color color, float emissionMultiplier)
        {
            if (m_commitmentReadabilityRenderers == null)
            {
                return;
            }

            var properties = new MaterialPropertyBlock();
            foreach (var renderer in m_commitmentReadabilityRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(properties);
                properties.SetColor(s_baseColor, color);
                properties.SetColor(s_emissionColor, color * emissionMultiplier);
                renderer.SetPropertyBlock(properties);
                properties.Clear();
            }
        }

        private static void _setRendererColor(Renderer renderer, Color color, float emissionMultiplier)
        {
            if (renderer == null)
            {
                return;
            }

            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetColor(s_baseColor, color);
            properties.SetColor(s_emissionColor, color * emissionMultiplier);
            renderer.SetPropertyBlock(properties);
        }

        private static void _setDoorOpen(
            GameObject door,
            AuthoredRouteDoorReadability readability,
            bool open)
        {
            if (readability != null)
            {
                door.SetActive(true);
                readability.SetOpen(open);
                return;
            }

            if (door != null)
            {
                door.SetActive(!open);
            }
        }

        private static float _flatDistance(Vector3 first, Vector3 second)
        {
            return Vector2.Distance(new Vector2(first.x, first.z), new Vector2(second.x, second.z));
        }

        private bool m_commitmentAvailable;
        private bool m_hasAppliedCommitmentPresentation;
        private bool m_hasAppliedLockdownPresentation;
        private bool m_hasAppliedCapacitorPresentation;
        private float m_commitmentTransitionRemaining;
        private float m_capacitorCollectionTransitionRemaining;
    }
}
