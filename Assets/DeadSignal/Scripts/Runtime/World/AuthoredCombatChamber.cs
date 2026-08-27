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

    public sealed class AuthoredCombatChamber : MonoBehaviour
    {
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

        public CombatChamberState State { get; private set; }
        public int Phase { get; private set; }
        public Transform CommitmentSwitch => m_commitmentSwitch;
        public AuthoredCombatScenario CombatScenario => m_combatScenario;
        public float RewardSignal => m_rewardSignal;
        public bool RewardAvailable => State == CombatChamberState.Cleared && m_reward != null && m_reward.activeSelf;
        public bool IsComplete => m_commitmentSwitch != null && m_lockdownThreshold != null && m_entryDoor != null &&
                                  m_rewardDoor != null && m_reward != null && m_clearedSignal != null &&
                                  m_combatScenario != null && m_combatScenario.IsComplete;

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
            m_entryDoor.SetActive(false);
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
            if (m_entryDoor != null) m_entryDoor.SetActive(true);
            if (m_rewardDoor != null) m_rewardDoor.SetActive(true);
            if (m_reward != null) m_reward.SetActive(true);
            if (m_clearedSignal != null) m_clearedSignal.SetActive(false);
        }

        private static float _flatDistance(Vector3 first, Vector3 second)
        {
            return Vector2.Distance(new Vector2(first.x, first.z), new Vector2(second.x, second.z));
        }
    }
}
