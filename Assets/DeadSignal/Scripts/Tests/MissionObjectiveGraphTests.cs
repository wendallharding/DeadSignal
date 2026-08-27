using System;
using System.Linq;
using DeadSignal.Missions;
using NUnit.Framework;
using UnityEngine;

namespace DeadSignal.Tests
{
    public sealed class MissionObjectiveGraphTests
    {
        [Test]
        public void CompatibilityGraph_ExpressesCompleteObjectiveContracts()
        {
            var definitions = CompatibilityMissionObjectiveGraph.Instance.Definitions;

            Assert.That(definitions.Count, Is.EqualTo(7));
            Assert.That(definitions.Select(definition => definition.Id), Is.Unique);
            Assert.That(definitions.All(definition => !string.IsNullOrWhiteSpace(definition.OwningRoom)), Is.True);
            Assert.That(definitions.All(definition => !string.IsNullOrWhiteSpace(definition.AnchorId)), Is.True);
            Assert.That(definitions.All(definition => !string.IsNullOrWhiteSpace(definition.Guidance.Title)), Is.True);
            Assert.That(definitions.All(definition => !string.IsNullOrWhiteSpace(definition.Guidance.Action)), Is.True);
            Assert.That(definitions.Select(definition => definition.CompletionRule), Is.Unique);
            Assert.That(definitions[0].WorldMutations.HasFlag(MissionWorldMutation.CentralTerritoryPowered), Is.True);
            Assert.That(definitions[0].Rewards.Single().Kind, Is.EqualTo(MissionRewardKind.SignalRefill));
            Assert.That(definitions[2].Rewards.Select(reward => reward.Kind),
                Is.EquivalentTo(new[] { MissionRewardKind.SignalRefill, MissionRewardKind.WeaponCalibration }));
            Assert.That(definitions[4].Rewards.Select(reward => reward.Kind),
                Is.EquivalentTo(new[] { MissionRewardKind.SignalRefill, MissionRewardKind.WeaponEvolution }));
            Assert.That(definitions[6].Rewards.Single().Kind, Is.EqualTo(MissionRewardKind.Victory));
        }

        [Test]
        public void AuthoredCompatibilityGraph_MatchesCompiledFallback()
        {
            var configuration = Resources.Load<MissionObjectiveGraphConfiguration>("Tuning/CompatibilityMissionObjectives");

            Assert.That(configuration, Is.Not.Null);
            Assert.That(configuration.ObjectiveCount, Is.EqualTo(7));
            var authored = configuration.BuildGraph().Definitions;
            var fallback = CompatibilityMissionObjectiveGraph.Instance.Definitions;
            Assert.That(authored.Count, Is.EqualTo(fallback.Count));
            for (var index = 0; index < authored.Count; index++)
            {
                Assert.That(authored[index].Id, Is.EqualTo(fallback[index].Id));
                Assert.That(authored[index].LegacyStage, Is.EqualTo(fallback[index].LegacyStage));
                Assert.That(authored[index].OwningRoom, Is.EqualTo(fallback[index].OwningRoom));
                Assert.That(authored[index].AnchorId, Is.EqualTo(fallback[index].AnchorId));
                Assert.That(authored[index].CompletionRule, Is.EqualTo(fallback[index].CompletionRule));
                Assert.That(authored[index].WorldMutations, Is.EqualTo(fallback[index].WorldMutations));
                Assert.That(authored[index].Guidance.Phase, Is.EqualTo(fallback[index].Guidance.Phase));
                Assert.That(authored[index].Guidance.Title, Is.EqualTo(fallback[index].Guidance.Title));
                Assert.That(authored[index].Guidance.Action, Is.EqualTo(fallback[index].Guidance.Action));
                Assert.That(authored[index].Guidance.Advisory, Is.EqualTo(fallback[index].Guidance.Advisory));
                Assert.That(authored[index].Prerequisites, Is.EqualTo(fallback[index].Prerequisites));
                Assert.That(authored[index].Successors, Is.EqualTo(fallback[index].Successors));
                Assert.That(authored[index].Rewards.Select(reward => (reward.Kind, reward.Amount)),
                    Is.EqualTo(fallback[index].Rewards.Select(reward => (reward.Kind, reward.Amount))));
            }
        }

        [Test]
        public void CompatibilityGraph_ResolvesEveryLegacyStageInOrder()
        {
            var run = new RunModel();

            Assert.That(run.CurrentMissionStage, Is.EqualTo(MissionStage.CentralTower));
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.CentralTower));
            Assert.That(run.TryActivateTower(), Is.True);
            Assert.That(run.CurrentMissionStage, Is.EqualTo(MissionStage.CentralPayload));
            Assert.That(run.CollectPayload(SignalRegion.Central), Is.True);
            Assert.That(run.CurrentMissionStage, Is.EqualTo(MissionStage.RelayTower));
            Assert.That(run.TryActivateRelayTower(), Is.True);
            Assert.That(run.CurrentMissionStage, Is.EqualTo(MissionStage.RelayPayload));
            Assert.That(run.CollectPayload(SignalRegion.Relay), Is.True);
            Assert.That(run.CurrentMissionStage, Is.EqualTo(MissionStage.SpineTower));
            Assert.That(run.TryActivateSpineTower(), Is.True);
            Assert.That(run.CurrentMissionStage, Is.EqualTo(MissionStage.SpinePayload));
            Assert.That(run.CollectPayload(SignalRegion.Spine), Is.True);
            Assert.That(run.CurrentMissionStage, Is.EqualTo(MissionStage.Extraction));
            Assert.That(run.TryExtract(), Is.True);
            Assert.That(run.CurrentMissionStage, Is.EqualTo(MissionStage.Extraction));
        }

        [Test]
        public void Graph_RejectsDuplicateIdsAndCycles()
        {
            var first = _definition(MissionObjectiveId.CentralTower, MissionObjectiveId.CentralPayload,
                Array.Empty<MissionObjectiveId>());
            var duplicate = _definition(MissionObjectiveId.CentralTower, MissionObjectiveId.CentralPayload,
                Array.Empty<MissionObjectiveId>());
            Assert.That(() => new MissionObjectiveGraph(first, duplicate), Throws.ArgumentException);

            var cycleStart = _definition(MissionObjectiveId.CentralTower, MissionObjectiveId.CentralPayload,
                new[] { MissionObjectiveId.CentralPayload });
            var cycleEnd = _definition(MissionObjectiveId.CentralPayload, MissionObjectiveId.CentralTower,
                new[] { MissionObjectiveId.CentralTower });
            Assert.That(() => new MissionObjectiveGraph(cycleStart, cycleEnd), Throws.ArgumentException);
        }

        private static MissionObjectiveDefinition _definition(MissionObjectiveId id, MissionObjectiveId successor,
            MissionObjectiveId[] prerequisites)
        {
            return new MissionObjectiveDefinition(id, MissionStage.CentralTower, "Test Room", "Test Anchor",
                MissionCompletionRule.CentralTowerOnline, MissionWorldMutation.None, Array.Empty<MissionReward>(),
                new MissionGuidanceState(1, "TEST", "TEST", "TEST"), prerequisites, new[] { successor });
        }
    }
}
