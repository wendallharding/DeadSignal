using System;
using System.Collections.Generic;

namespace DeadSignal.Missions
{
    public enum MissionObjectiveId
    {
        CentralTower,
        CentralPayload,
        RelayTower,
        RelayPayload,
        SpineTower,
        SpinePayload,
        Extraction,
        CargoCoupling,
        CoolantSeal,
        RelayFork,
        CentralAssembly,
        CentralInstallation
    }

    public enum MissionCompletionRule
    {
        CentralTowerOnline,
        CentralPayloadSecured,
        RelayTowerOnline,
        RelayPayloadSecured,
        SpineTowerOnline,
        SpinePayloadSecured,
        ExtractionComplete,
        CargoCouplingSecured,
        CoolantSealSecured,
        RelayFeedsRouted,
        CentralPayloadAssembled,
        CentralPayloadInstalled
    }

    [Flags]
    public enum MissionWorldMutation
    {
        None = 0,
        CentralTerritoryPowered = 1 << 0,
        CentralPayloadSecured = 1 << 1,
        RelayTerritoryPowered = 1 << 2,
        RelayPayloadSecured = 1 << 3,
        SpineTerritoryPowered = 1 << 4,
        SpinePayloadSecured = 1 << 5,
        RunCompleted = 1 << 6,
        CargoCouplingSecured = 1 << 7,
        CoolantSealSecured = 1 << 8,
        RelayFeedsRouted = 1 << 9,
        CentralPayloadAssembled = 1 << 10,
        CentralPayloadInstalled = 1 << 11
    }

    public enum MissionRewardKind
    {
        None,
        SignalRefill,
        WeaponCalibration,
        WeaponEvolution,
        Victory
    }

    public readonly struct MissionReward
    {
        public MissionReward(MissionRewardKind kind, float amount = 0f)
        {
            Kind = kind;
            Amount = amount;
        }

        public MissionRewardKind Kind { get; }
        public float Amount { get; }
    }

    public sealed class MissionObjectiveDefinition
    {
        public MissionObjectiveDefinition(
            MissionObjectiveId id,
            MissionStage legacyStage,
            string owningRoom,
            string anchorId,
            MissionCompletionRule completionRule,
            MissionWorldMutation worldMutations,
            MissionReward[] rewards,
            MissionGuidanceState guidance,
            MissionObjectiveId[] prerequisites,
            MissionObjectiveId[] successors)
        {
            if (string.IsNullOrWhiteSpace(owningRoom))
            {
                throw new ArgumentException("An objective must name its owning room.", nameof(owningRoom));
            }
            if (string.IsNullOrWhiteSpace(anchorId))
            {
                throw new ArgumentException("An objective must name its authored interaction anchor.", nameof(anchorId));
            }

            Id = id;
            LegacyStage = legacyStage;
            OwningRoom = owningRoom;
            AnchorId = anchorId;
            CompletionRule = completionRule;
            WorldMutations = worldMutations;
            Rewards = rewards ?? Array.Empty<MissionReward>();
            Guidance = guidance;
            Prerequisites = prerequisites ?? Array.Empty<MissionObjectiveId>();
            Successors = successors ?? Array.Empty<MissionObjectiveId>();
        }

        public MissionObjectiveId Id { get; }
        public MissionStage LegacyStage { get; }
        public string OwningRoom { get; }
        public string AnchorId { get; }
        public MissionCompletionRule CompletionRule { get; }
        public MissionWorldMutation WorldMutations { get; }
        public IReadOnlyList<MissionReward> Rewards { get; }
        public MissionGuidanceState Guidance { get; }
        public IReadOnlyList<MissionObjectiveId> Prerequisites { get; }
        public IReadOnlyList<MissionObjectiveId> Successors { get; }
    }

    /// <summary>
    /// Engine-independent objective contract. Definition order is the deterministic tie-breaker for future parallel jobs.
    /// </summary>
    public sealed class MissionObjectiveGraph
    {
        public MissionObjectiveGraph(params MissionObjectiveDefinition[] definitions)
        {
            if (definitions == null || definitions.Length == 0)
            {
                throw new ArgumentException("An objective graph requires at least one definition.", nameof(definitions));
            }

            m_definitions = (MissionObjectiveDefinition[])definitions.Clone();
            m_byId = new Dictionary<MissionObjectiveId, MissionObjectiveDefinition>(m_definitions.Length);
            foreach (var definition in m_definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException("Objective definitions cannot contain null entries.", nameof(definitions));
                }
                if (!m_byId.TryAdd(definition.Id, definition))
                {
                    throw new ArgumentException($"Duplicate objective id {definition.Id}.", nameof(definitions));
                }
            }

            _validateLinks();
            _validateAcyclic();
        }

        public IReadOnlyList<MissionObjectiveDefinition> Definitions => m_definitions;

        public MissionObjectiveDefinition Get(MissionObjectiveId id)
        {
            return m_byId[id];
        }

        public MissionObjectiveDefinition Evaluate(Func<MissionCompletionRule, bool> isComplete)
        {
            if (isComplete == null)
            {
                throw new ArgumentNullException(nameof(isComplete));
            }

            foreach (var definition in m_definitions)
            {
                if (!isComplete(definition.CompletionRule) && _arePrerequisitesComplete(definition, isComplete))
                {
                    return definition;
                }
            }

            return m_definitions[m_definitions.Length - 1];
        }

        public bool IsAvailable(MissionObjectiveId id, Func<MissionCompletionRule, bool> isComplete)
        {
            if (isComplete == null)
            {
                throw new ArgumentNullException(nameof(isComplete));
            }

            return m_byId.TryGetValue(id, out var definition) &&
                   !isComplete(definition.CompletionRule) &&
                   _arePrerequisitesComplete(definition, isComplete);
        }

        private bool _arePrerequisitesComplete(MissionObjectiveDefinition definition,
            Func<MissionCompletionRule, bool> isComplete)
        {
            foreach (var prerequisiteId in definition.Prerequisites)
            {
                if (!isComplete(m_byId[prerequisiteId].CompletionRule))
                {
                    return false;
                }
            }

            return true;
        }

        private void _validateLinks()
        {
            foreach (var definition in m_definitions)
            {
                foreach (var prerequisiteId in definition.Prerequisites)
                {
                    _requireLinkedDefinition(definition.Id, prerequisiteId, "prerequisite");
                    if (!_contains(m_byId[prerequisiteId].Successors, definition.Id))
                    {
                        throw new ArgumentException($"Objective link {prerequisiteId} -> {definition.Id} is not reciprocal.");
                    }
                }
                foreach (var successorId in definition.Successors)
                {
                    _requireLinkedDefinition(definition.Id, successorId, "successor");
                    if (!_contains(m_byId[successorId].Prerequisites, definition.Id))
                    {
                        throw new ArgumentException($"Objective link {definition.Id} -> {successorId} is not reciprocal.");
                    }
                }
            }
        }

        private void _validateAcyclic()
        {
            var visiting = new HashSet<MissionObjectiveId>();
            var visited = new HashSet<MissionObjectiveId>();
            foreach (var definition in m_definitions)
            {
                _visit(definition.Id, visiting, visited);
            }
        }

        private void _visit(MissionObjectiveId id, HashSet<MissionObjectiveId> visiting, HashSet<MissionObjectiveId> visited)
        {
            if (visited.Contains(id))
            {
                return;
            }
            if (!visiting.Add(id))
            {
                throw new ArgumentException($"Objective graph contains a cycle at {id}.");
            }

            foreach (var successorId in m_byId[id].Successors)
            {
                _visit(successorId, visiting, visited);
            }

            visiting.Remove(id);
            visited.Add(id);
        }

        private void _requireLinkedDefinition(MissionObjectiveId ownerId, MissionObjectiveId linkedId, string linkType)
        {
            if (!m_byId.ContainsKey(linkedId))
            {
                throw new ArgumentException($"Objective {ownerId} references missing {linkType} {linkedId}.");
            }
        }

        private static bool _contains(IReadOnlyList<MissionObjectiveId> ids, MissionObjectiveId expected)
        {
            foreach (var id in ids)
            {
                if (id == expected)
                {
                    return true;
                }
            }

            return false;
        }

        private readonly MissionObjectiveDefinition[] m_definitions;
        private readonly Dictionary<MissionObjectiveId, MissionObjectiveDefinition> m_byId;
    }

    public static class CompatibilityMissionObjectiveGraph
    {
        public static MissionObjectiveGraph Instance { get; } = new MissionObjectiveGraph(
            _definition(MissionObjectiveId.CentralTower, MissionStage.CentralTower, "Central Maintenance Concourse", "Central Tower",
                MissionCompletionRule.CentralTowerOnline, MissionWorldMutation.CentralTerritoryPowered,
                new[] { new MissionReward(MissionRewardKind.SignalRefill, RunModel.TowerRefill) },
                new MissionGuidanceState(1, "RESTORE CENTRAL", "ACTIVATE THE CENTRAL SIGNAL TOWER",
                    $"SIGNAL -{RunModel.TowerCost:0}  //  REFILL +{RunModel.TowerRefill:0}"),
                Array.Empty<MissionObjectiveId>(), new[] { MissionObjectiveId.CargoCoupling, MissionObjectiveId.CoolantSeal }),
            _definition(MissionObjectiveId.CargoCoupling, MissionStage.CentralPayload, "Cargo Annex", "Power Coupling Socket",
                MissionCompletionRule.CargoCouplingSecured, MissionWorldMutation.CargoCouplingSecured,
                Array.Empty<MissionReward>(),
                new MissionGuidanceState(2, "RESTART CENTRAL", "RECOVER THE CARGO POWER COUPLING",
                    "COUPLING + COOLANT SEAL REQUIRED  //  EITHER ORDER"),
                new[] { MissionObjectiveId.CentralTower }, new[] { MissionObjectiveId.RelayFork }),
            _definition(MissionObjectiveId.CoolantSeal, MissionStage.CentralPayload, "Coolant Reclamation", "Coolant Seal Socket",
                MissionCompletionRule.CoolantSealSecured, MissionWorldMutation.CoolantSealSecured,
                Array.Empty<MissionReward>(),
                new MissionGuidanceState(2, "RESTART CENTRAL: 1/2", "THREAD THE BAFFLES FOR THE COOLANT SEAL",
                    "ONE CENTRAL COMPONENT REMAINS"),
                new[] { MissionObjectiveId.CentralTower }, new[] { MissionObjectiveId.RelayFork }),
            _definition(MissionObjectiveId.RelayFork, MissionStage.CentralPayload,
                "Relay Fork", "Relay Bank Anchor",
                MissionCompletionRule.RelayFeedsRouted, MissionWorldMutation.RelayFeedsRouted,
                Array.Empty<MissionReward>(),
                new MissionGuidanceState(2, "CENTRAL COMPONENTS READY", "ROUTE BOTH FEEDS AT THE RELAY FORK",
                    "COUPLING + COOLANT SEAL SECURED  //  REROUTE"),
                new[] { MissionObjectiveId.CargoCoupling, MissionObjectiveId.CoolantSeal },
                new[] { MissionObjectiveId.CentralAssembly }),
            _definition(MissionObjectiveId.CentralAssembly, MissionStage.CentralPayload,
                "East Transfer Vault", "Transfer Assembly Socket",
                MissionCompletionRule.CentralPayloadAssembled, MissionWorldMutation.CentralPayloadAssembled,
                Array.Empty<MissionReward>(),
                new MissionGuidanceState(2, "FEEDS ROUTED", "ASSEMBLE THE CENTRAL PAYLOAD IN THE TRANSFER VAULT",
                    "TRANSFER CYCLE READY  //  ASSEMBLE"),
                new[] { MissionObjectiveId.RelayFork }, new[] { MissionObjectiveId.CentralInstallation }),
            _definition(MissionObjectiveId.CentralInstallation, MissionStage.CentralPayload,
                "Central Maintenance Concourse", "Central Payload Installation Anchor",
                MissionCompletionRule.CentralPayloadInstalled, MissionWorldMutation.CentralPayloadInstalled,
                Array.Empty<MissionReward>(),
                new MissionGuidanceState(2, "CENTRAL PAYLOAD ASSEMBLED", "RETURN TO CENTRAL AND INSTALL THE PAYLOAD",
                    "ONE INSTALLATION RETURN  //  OPENS RELAY ROUTE"),
                new[] { MissionObjectiveId.CentralAssembly }, new[] { MissionObjectiveId.RelayTower }),
            _definition(MissionObjectiveId.RelayTower, MissionStage.RelayTower, "Relay Foundry", "Relay Tower",
                MissionCompletionRule.RelayTowerOnline, MissionWorldMutation.RelayTerritoryPowered,
                new[] { new MissionReward(MissionRewardKind.SignalRefill, RunModel.RelayTowerRefill) },
                new MissionGuidanceState(3, "EXTEND THE NETWORK", "RESTORE THE RELAY FOUNDRY TOWER",
                    $"SIGNAL -{RunModel.RelayTowerCost:0}  //  POWERS FOUNDRY + UNLOCKS PROCESSING"),
                new[] { MissionObjectiveId.CentralInstallation }, new[] { MissionObjectiveId.RelayPayload }),
            _definition(MissionObjectiveId.RelayPayload, MissionStage.RelayPayload,
                "Relay Foundry / Cooling Gantry", "Relay Payload Socket",
                MissionCompletionRule.RelayPayloadSecured, MissionWorldMutation.RelayPayloadSecured,
                Array.Empty<MissionReward>(),
                new MissionGuidanceState(4, "RELAY PAYLOAD", "CHOOSE FOUNDRY OR COOLING GANTRY",
                    "INNER COVER OR EXCHANGER LOOP  //  ONE REQUIRED"),
                new[] { MissionObjectiveId.RelayTower }, new[] { MissionObjectiveId.SpineTower }),
            _definition(MissionObjectiveId.SpineTower, MissionStage.SpineTower, "Capacitor Spine", "Capacitor Spine Activation Decal",
                MissionCompletionRule.SpineTowerOnline, MissionWorldMutation.SpineTerritoryPowered,
                new[]
                {
                    new MissionReward(MissionRewardKind.SignalRefill, RunModel.SpineTowerRefill),
                    new MissionReward(MissionRewardKind.WeaponEvolution)
                },
                new MissionGuidanceState(5, "POWER THE SPINE", "RESTORE THE CAPACITOR SPINE TOWER",
                    $"SIGNAL -{RunModel.SpineTowerCost:0}  //  EVOLVE WEAPON"),
                new[] { MissionObjectiveId.RelayPayload }, new[] { MissionObjectiveId.SpinePayload }),
            _definition(MissionObjectiveId.SpinePayload, MissionStage.SpinePayload, "Capacitor Spine", "Spine Payload Socket",
                MissionCompletionRule.SpinePayloadSecured, MissionWorldMutation.SpinePayloadSecured,
                Array.Empty<MissionReward>(),
                new MissionGuidanceState(6, "FINAL PAYLOAD", "SECURE ONE SPINE PAYLOAD", "GALLERY OR FURNACE-SIDE ROUTE  //  ONE REQUIRED"),
                new[] { MissionObjectiveId.SpineTower }, new[] { MissionObjectiveId.Extraction }),
            _definition(MissionObjectiveId.Extraction, MissionStage.Extraction, "Extraction Dock", "Dock Uplink",
                MissionCompletionRule.ExtractionComplete, MissionWorldMutation.RunCompleted,
                new[] { new MissionReward(MissionRewardKind.Victory) },
                new MissionGuidanceState(7, "EXTRACT OR GREED", "RETURN TO THE CYAN DOCK",
                    "THREE TOWERS + THREE PAYLOADS SECURED  //  QUENCH CACHE OPTIONAL"),
                new[] { MissionObjectiveId.SpinePayload }, Array.Empty<MissionObjectiveId>()));

        private static MissionObjectiveDefinition _definition(
            MissionObjectiveId id,
            MissionStage legacyStage,
            string owningRoom,
            string anchorId,
            MissionCompletionRule completionRule,
            MissionWorldMutation worldMutations,
            MissionReward[] rewards,
            MissionGuidanceState guidance,
            MissionObjectiveId[] prerequisites,
            MissionObjectiveId[] successors)
        {
            return new MissionObjectiveDefinition(id, legacyStage, owningRoom, anchorId, completionRule, worldMutations,
                rewards, guidance, prerequisites, successors);
        }
    }
}
