using System;
using UnityEngine;

namespace DeadSignal.Missions
{
    [Serializable]
    public sealed class MissionRewardConfiguration
    {
        [SerializeField] private MissionRewardKind m_kind;
        [SerializeField] private float m_amount;

        public MissionRewardConfiguration(MissionReward reward)
        {
            m_kind = reward.Kind;
            m_amount = reward.Amount;
        }

        public MissionReward ToReward()
        {
            return new MissionReward(m_kind, m_amount);
        }
    }

    [Serializable]
    public sealed class MissionObjectiveConfiguration
    {
        [SerializeField] private MissionObjectiveId m_id;
        [SerializeField] private MissionStage m_legacyStage;
        [SerializeField] private string m_owningRoom;
        [SerializeField] private string m_anchorId;
        [SerializeField] private MissionCompletionRule m_completionRule;
        [SerializeField] private MissionWorldMutation m_worldMutations;
        [SerializeField] private MissionRewardConfiguration[] m_rewards;
        [SerializeField] private int m_guidancePhase;
        [SerializeField] private string m_guidanceTitle;
        [SerializeField] private string m_guidanceAction;
        [SerializeField] private string m_guidanceAdvisory;
        [SerializeField] private MissionObjectiveId[] m_prerequisites;
        [SerializeField] private MissionObjectiveId[] m_successors;

        public MissionObjectiveConfiguration(MissionObjectiveDefinition definition)
        {
            m_id = definition.Id;
            m_legacyStage = definition.LegacyStage;
            m_owningRoom = definition.OwningRoom;
            m_anchorId = definition.AnchorId;
            m_completionRule = definition.CompletionRule;
            m_worldMutations = definition.WorldMutations;
            m_rewards = new MissionRewardConfiguration[definition.Rewards.Count];
            for (var index = 0; index < definition.Rewards.Count; index++)
            {
                m_rewards[index] = new MissionRewardConfiguration(definition.Rewards[index]);
            }
            m_guidancePhase = definition.Guidance.Phase;
            m_guidanceTitle = definition.Guidance.Title;
            m_guidanceAction = definition.Guidance.Action;
            m_guidanceAdvisory = definition.Guidance.Advisory;
            m_prerequisites = _copy(definition.Prerequisites);
            m_successors = _copy(definition.Successors);
        }

        public MissionObjectiveDefinition ToDefinition()
        {
            var rewardConfigurations = m_rewards ?? Array.Empty<MissionRewardConfiguration>();
            var rewards = new MissionReward[rewardConfigurations.Length];
            for (var index = 0; index < rewardConfigurations.Length; index++)
            {
                if (rewardConfigurations[index] == null)
                {
                    throw new InvalidOperationException($"Objective {m_id} contains a null reward configuration.");
                }

                rewards[index] = rewardConfigurations[index].ToReward();
            }

            return new MissionObjectiveDefinition(
                m_id,
                m_legacyStage,
                m_owningRoom,
                m_anchorId,
                m_completionRule,
                m_worldMutations,
                rewards,
                new MissionGuidanceState(m_guidancePhase, m_guidanceTitle, m_guidanceAction, m_guidanceAdvisory),
                m_prerequisites ?? Array.Empty<MissionObjectiveId>(),
                m_successors ?? Array.Empty<MissionObjectiveId>());
        }

        private static MissionObjectiveId[] _copy(System.Collections.Generic.IReadOnlyList<MissionObjectiveId> source)
        {
            var copy = new MissionObjectiveId[source.Count];
            for (var index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return copy;
        }
    }

    [CreateAssetMenu(fileName = "CompatibilityMissionObjectives", menuName = "DEAD SIGNAL/Mission Objective Graph")]
    public sealed class MissionObjectiveGraphConfiguration : ScriptableObject
    {
        [SerializeField] private MissionObjectiveConfiguration[] m_objectives;

        public int ObjectiveCount => m_objectives?.Length ?? 0;

        public MissionObjectiveGraph BuildGraph()
        {
            var objectiveConfigurations = m_objectives ?? Array.Empty<MissionObjectiveConfiguration>();
            var definitions = new MissionObjectiveDefinition[objectiveConfigurations.Length];
            for (var index = 0; index < objectiveConfigurations.Length; index++)
            {
                if (objectiveConfigurations[index] == null)
                {
                    throw new InvalidOperationException($"Mission objective configuration contains a null entry at index {index}.");
                }

                definitions[index] = objectiveConfigurations[index].ToDefinition();
            }

            return new MissionObjectiveGraph(definitions);
        }

        public void ReplaceDefinitions(System.Collections.Generic.IReadOnlyList<MissionObjectiveDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            m_objectives = new MissionObjectiveConfiguration[definitions.Count];
            for (var index = 0; index < definitions.Count; index++)
            {
                m_objectives[index] = new MissionObjectiveConfiguration(definitions[index]);
            }
        }
    }
}
