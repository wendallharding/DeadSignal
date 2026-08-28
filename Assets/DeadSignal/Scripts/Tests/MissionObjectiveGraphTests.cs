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

            Assert.That(definitions.Count, Is.EqualTo(16));
            Assert.That(definitions.Select(definition => definition.Id), Is.Unique);
            Assert.That(definitions.All(definition => !string.IsNullOrWhiteSpace(definition.OwningRoom)), Is.True);
            Assert.That(definitions.All(definition => !string.IsNullOrWhiteSpace(definition.AnchorId)), Is.True);
            Assert.That(definitions.All(definition => !string.IsNullOrWhiteSpace(definition.Guidance.Title)), Is.True);
            Assert.That(definitions.All(definition => !string.IsNullOrWhiteSpace(definition.Guidance.Action)), Is.True);
            Assert.That(definitions.Select(definition => definition.CompletionRule), Is.Unique);
            Assert.That(definitions[0].WorldMutations.HasFlag(MissionWorldMutation.CentralTerritoryPowered), Is.True);
            Assert.That(definitions[0].Rewards.Single().Kind, Is.EqualTo(MissionRewardKind.SignalRefill));
            Assert.That(definitions[6].Rewards.Select(reward => reward.Kind),
                Is.EquivalentTo(new[] { MissionRewardKind.SignalRefill }));
            Assert.That(definitions[8].Rewards.Select(reward => reward.Kind),
                Is.EquivalentTo(new[] { MissionRewardKind.WeaponCalibration }));
            Assert.That(definitions[10].Rewards.Select(reward => reward.Kind),
                Is.EquivalentTo(new[] { MissionRewardKind.SignalRefill, MissionRewardKind.WeaponEvolution }));
            Assert.That(definitions[15].Rewards.Single().Kind, Is.EqualTo(MissionRewardKind.Victory));
        }

        [Test]
        public void AuthoredCompatibilityGraph_MatchesCompiledFallback()
        {
            var configuration = Resources.Load<MissionObjectiveGraphConfiguration>("Tuning/CompatibilityMissionObjectives");

            Assert.That(configuration, Is.Not.Null);
            Assert.That(configuration.ObjectiveCount, Is.EqualTo(16));
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
            var configuration = Resources.Load<MissionObjectiveGraphConfiguration>("Tuning/CompatibilityMissionObjectives");
            Assert.That(configuration, Is.Not.Null);
            var run = new RunModel(configuration.BuildGraph());

            _assertObjective(run, MissionObjectiveId.CentralTower, MissionStage.CentralTower,
                MissionCompletionRule.CentralTowerOnline, MissionWorldMutation.CentralTerritoryPowered,
                "RESTORE CENTRAL");
            Assert.That(run.TryActivateRelayTower(), Is.False);
            Assert.That(run.CollectPayload(SignalRegion.Central), Is.False);
            var signalBeforeCentral = run.Signal;
            Assert.That(run.TryActivateTower(), Is.True);
            Assert.That(run.Signal, Is.EqualTo(Math.Min(RunModel.MaximumSignal,
                signalBeforeCentral - RunModel.TowerCost + RunModel.TowerRefill)));
            _assertObjective(run, MissionObjectiveId.CargoCoupling, MissionStage.CentralPayload,
                MissionCompletionRule.CargoCouplingSecured, MissionWorldMutation.CargoCouplingSecured,
                "RESTART CENTRAL");
            Assert.That(run.TryActivateTower(), Is.False);
            Assert.That(run.CollectPayload(SignalRegion.Relay), Is.False);
            Assert.That(run.CollectPayload(SignalRegion.Central), Is.True);
            Assert.That(run.Salvage, Is.EqualTo(1));
            _assertObjective(run, MissionObjectiveId.RelayFork, MissionStage.CentralPayload,
                MissionCompletionRule.RelayFeedsRouted, MissionWorldMutation.RelayFeedsRouted,
                "CENTRAL COMPONENTS READY");
            Assert.That(run.TryAssembleCentralPayload(), Is.False);
            Assert.That(run.TryRouteCentralComponents(), Is.True);
            _assertObjective(run, MissionObjectiveId.CentralAssembly, MissionStage.CentralPayload,
                MissionCompletionRule.CentralPayloadAssembled, MissionWorldMutation.CentralPayloadAssembled,
                "FEEDS ROUTED");
            Assert.That(run.TryAssembleCentralPayload(), Is.True);
            _assertObjective(run, MissionObjectiveId.CentralInstallation, MissionStage.CentralPayload,
                MissionCompletionRule.CentralPayloadInstalled, MissionWorldMutation.CentralPayloadInstalled,
                "CENTRAL PAYLOAD ASSEMBLED");
            Assert.That(run.TryActivateRelayTower(), Is.False);
            Assert.That(run.TryInstallCentralPayload(), Is.True);
            _assertObjective(run, MissionObjectiveId.RelayTower, MissionStage.RelayTower,
                MissionCompletionRule.RelayTowerOnline, MissionWorldMutation.RelayTerritoryPowered,
                "EXTEND THE NETWORK");
            Assert.That(run.CollectPayload(SignalRegion.Central), Is.False);
            var signalBeforeRelay = run.Signal;
            Assert.That(run.TryActivateRelayTower(), Is.True);
            Assert.That(run.Signal, Is.EqualTo(Math.Min(RunModel.MaximumSignal,
                signalBeforeRelay + RunModel.RelayTowerRefill)));
            _assertObjective(run, MissionObjectiveId.RelayPayload, MissionStage.RelayPayload,
                MissionCompletionRule.RelayPayloadStabilized, MissionWorldMutation.RelayPayloadStabilized,
                "STABILIZE RELAY PAYLOAD");
            Assert.That(run.TryActivateSpineTower(), Is.False);
            Assert.That(run.CollectPayload(SignalRegion.Spine), Is.False);
            Assert.That(run.CollectPayload(SignalRegion.Relay), Is.True);
            Assert.That(run.Salvage, Is.EqualTo(2));
            Assert.That(run.RelayPayloadStabilized, Is.True);
            Assert.That(run.RelayPayloadSecured, Is.False);
            _assertObjective(run, MissionObjectiveId.RelayInstallation, MissionStage.RelayPayload,
                MissionCompletionRule.RelayPayloadSecured, MissionWorldMutation.RelayPayloadSecured,
                "PAYLOAD STABILIZED");
            Assert.That(run.TryActivateSpineTower(), Is.False);
            Assert.That(run.TryInstallRelayPayload(), Is.True);
            Assert.That(run.TryInstallRelayPayload(), Is.False);
            _assertObjective(run, MissionObjectiveId.SpineVenting, MissionStage.SpineTower,
                MissionCompletionRule.SpineBerthVented, MissionWorldMutation.SpineBerthVented,
                "SPINE BERTH PRESSURIZED");
            Assert.That(run.TryActivateSpineTower(), Is.False);
            Assert.That(run.TryVentSpineBerth(), Is.True);
            Assert.That(run.TryVentSpineBerth(), Is.False);
            _assertObjective(run, MissionObjectiveId.SpineTower, MissionStage.SpineTower,
                MissionCompletionRule.SpineRelayResultInstalled,
                MissionWorldMutation.SpineTerritoryPowered | MissionWorldMutation.SpineRelayResultInstalled |
                MissionWorldMutation.DeepReturnNetworkPowered | MissionWorldMutation.CoreRebuildUnlocked,
                "INSTALL THE RELAY RESULT");
            var signalBeforeSpine = run.Signal;
            Assert.That(run.TryActivateSpineTower(), Is.True);
            Assert.That(run.DeepReturnNetworkPowered, Is.True);
            Assert.That(run.CoreRebuildUnlocked, Is.True);
            Assert.That(run.Signal, Is.EqualTo(Math.Min(RunModel.MaximumSignal,
                signalBeforeSpine + RunModel.SpineTowerRefill)));
            _assertObjective(run, MissionObjectiveId.InductionLattice, MissionStage.SpinePayload,
                MissionCompletionRule.InductionLatticeCharged, MissionWorldMutation.InductionLatticeCharged,
                "REBUILD THE SIGNAL CORE");
            Assert.That(run.TryChargeInductionLattice(), Is.True);
            Assert.That(run.TryChargeInductionLattice(), Is.False);
            Assert.That(run.InductionLatticeCharged, Is.True);
            _assertObjective(run, MissionObjectiveId.FluxShunt, MissionStage.SpinePayload,
                MissionCompletionRule.FluxShuntRouted, MissionWorldMutation.FluxShuntRouted,
                "LATTICE CHARGED");
            Assert.That(run.TryRouteFluxShunt(), Is.True);
            Assert.That(run.TryRouteFluxShunt(), Is.False);
            Assert.That(run.FluxShuntRouted, Is.True);
            _assertObjective(run, MissionObjectiveId.ConvergenceCalibration, MissionStage.SpinePayload,
                MissionCompletionRule.ConvergenceCalibrated, MissionWorldMutation.ConvergenceCalibrated,
                "CONVERGENCE FEED ONLINE");
            Assert.That(run.TryBeginConvergenceCalibration(), Is.True);
            Assert.That(run.AdvanceConvergenceCalibration(run.ConvergenceCalibrationDuration, true), Is.True);
            Assert.That(run.ConvergenceCalibrated, Is.True);
            _assertObjective(run, MissionObjectiveId.SpinePayload, MissionStage.SpinePayload,
                MissionCompletionRule.SpinePayloadSecured, MissionWorldMutation.SpinePayloadSecured,
                "FINAL PAYLOAD");
            Assert.That(run.TryExtract(), Is.False);
            Assert.That(run.CollectPayload(SignalRegion.Spine), Is.True);
            Assert.That(run.Salvage, Is.EqualTo(3));
            _assertObjective(run, MissionObjectiveId.Extraction, MissionStage.Extraction,
                MissionCompletionRule.ExtractionComplete, MissionWorldMutation.RunCompleted,
                "EXTRACT OR GREED");
            Assert.That(run.TryExtract(), Is.True);
            Assert.That(run.Outcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(run.CurrentMissionStage, Is.EqualTo(MissionStage.Extraction));
            Assert.That(run.TryExtract(), Is.False);
        }


        [Test]
        public void CentralJobs_AreBothAvailableAndCompleteInEitherOrder()
        {
            var run = new RunModel();
            Assert.That(run.TryActivateTower(), Is.True);
            Assert.That(run.CanCollectPayload(SignalRegion.Central, CentralComponentKind.PowerCoupling), Is.True);
            Assert.That(run.CanCollectPayload(SignalRegion.Central, CentralComponentKind.CoolantSeal), Is.True);

            Assert.That(run.CollectPayload(SignalRegion.Central, CentralComponentKind.CoolantSeal), Is.True);
            Assert.That(run.CoolantSealSecured, Is.True);
            Assert.That(run.CargoCouplingSecured, Is.False);
            Assert.That(run.CentralPayloadSecured, Is.False);
            Assert.That(run.Salvage, Is.EqualTo(1));
            Assert.That(run.TryActivateRelayTower(), Is.False);
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.CargoCoupling));

            Assert.That(run.CollectPayload(SignalRegion.Central, CentralComponentKind.PowerCoupling), Is.True);
            Assert.That(run.CentralPayloadSecured, Is.False);
            Assert.That(run.Salvage, Is.EqualTo(1));
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.RelayFork));
            Assert.That(run.CanCollectPayload(SignalRegion.Central, CentralComponentKind.CoolantSeal), Is.False);
            Assert.That(run.TryAssembleCentralPayload(), Is.False);
            Assert.That(run.TryRouteCentralComponents(), Is.True);
            Assert.That(run.TryRouteCentralComponents(), Is.False);
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.CentralAssembly));
            Assert.That(run.TryAssembleCentralPayload(), Is.True);
            Assert.That(run.TryAssembleCentralPayload(), Is.False);
            Assert.That(run.CentralPayloadAssembled, Is.True);
            Assert.That(run.CentralPayloadSecured, Is.False);
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.CentralInstallation));
            Assert.That(run.TryActivateRelayTower(), Is.False);
            Assert.That(run.TryInstallCentralPayload(), Is.True);
            Assert.That(run.TryInstallCentralPayload(), Is.False);
            Assert.That(run.CentralPayloadSecured, Is.True);
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.RelayTower));
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

        private static void _assertObjective(
            RunModel run,
            MissionObjectiveId id,
            MissionStage stage,
            MissionCompletionRule completionRule,
            MissionWorldMutation mutation,
            string guidanceTitle)
        {
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(id));
            Assert.That(run.CurrentMissionStage, Is.EqualTo(stage));
            Assert.That(run.CurrentObjective.CompletionRule, Is.EqualTo(completionRule));
            Assert.That(run.CurrentObjective.WorldMutations, Is.EqualTo(mutation));
            Assert.That(run.CurrentObjective.Guidance.Title, Is.EqualTo(guidanceTitle));
        }
    }
}
