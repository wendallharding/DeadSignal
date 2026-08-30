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

    public sealed class AuthoredCombatChamber : MonoBehaviour
    {
        private const float COMMITMENT_TRANSITION_SECONDS = 0.75f;

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
        public TrialCommitmentPresentationState CommitmentPresentationState { get; private set; } =
            TrialCommitmentPresentationState.Locked;
        public bool IsComplete => m_commitmentSwitch != null && m_lockdownThreshold != null && m_entryDoor != null &&
                                  m_rewardDoor != null && m_reward != null && m_clearedSignal != null &&
                                  m_combatScenario != null && m_combatScenario.IsComplete;

        private void Update()
        {
            if (m_commitmentTransitionRemaining <= 0f)
            {
                return;
            }

            m_commitmentTransitionRemaining = Mathf.Max(
                0f,
                m_commitmentTransitionRemaining - Time.unscaledDeltaTime);
            _refreshCommitmentPresentation();
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

        public void SetCommitmentAvailable(bool available)
        {
            m_commitmentAvailable = available;
            _refreshCommitmentPresentation();
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
            m_entryDoor.SetActive(false);
            _refreshCommitmentPresentation();
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
            m_entryDoor.SetActive(true);
            _refreshCommitmentPresentation();
            return true;
        }

        public bool AdvancePhase()
        {
            if (State != CombatChamberState.Lockdown || Phase >= 3)
            {
                return false;
            }

            Phase++;
            return true;
        }

        public void Complete()
        {
            State = CombatChamberState.Cleared;
            Phase = 0;
            m_entryDoor.SetActive(false);
            m_rewardDoor.SetActive(false);
            m_clearedSignal.SetActive(true);
            _refreshCommitmentPresentation();
        }

        public bool TryCollectReward(Vector3 playerPosition)
        {
            if (!RewardAvailable || _flatDistance(playerPosition, m_reward.transform.position) > m_interactionRadius)
            {
                return false;
            }

            m_reward.SetActive(false);
            return true;
        }

        public void ResetState()
        {
            State = CombatChamberState.Dormant;
            Phase = 0;
            m_commitmentAvailable = false;
            m_commitmentTransitionRemaining = 0f;
            m_hasAppliedCommitmentPresentation = false;
            if (m_entryDoor != null) m_entryDoor.SetActive(true);
            if (m_rewardDoor != null) m_rewardDoor.SetActive(true);
            if (m_reward != null) m_reward.SetActive(true);
            if (m_clearedSignal != null) m_clearedSignal.SetActive(false);
            _refreshCommitmentPresentation();
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

        private static float _flatDistance(Vector3 first, Vector3 second)
        {
            return Vector2.Distance(new Vector2(first.x, first.z), new Vector2(second.x, second.z));
        }

        private bool m_commitmentAvailable;
        private bool m_hasAppliedCommitmentPresentation;
        private float m_commitmentTransitionRemaining;
    }
}
