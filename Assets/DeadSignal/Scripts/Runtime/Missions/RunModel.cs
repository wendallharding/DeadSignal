using System;

namespace DeadSignal.Missions
{
    public enum RunOutcome
    {
        Running,
        Victory,
        Destroyed
    }

    public enum SignalRegion
    {
        Central,
        Relay,
        Spine
    }

    public enum CentralComponentKind
    {
        None,
        PowerCoupling,
        CoolantSeal
    }

    public enum MissionStage
    {
        CentralTower,
        CentralPayload,
        RelayTower,
        RelayPayload,
        SpineTower,
        SpinePayload,
        Extraction
    }

    /// <summary>
    /// Small deterministic run report used by the result screen and future balance sessions.
    /// It deliberately stores no personal or persistent data.
    /// </summary>
    public sealed class RunMetrics
    {
        public float ElapsedSeconds { get; private set; }
        public float DeadZoneSeconds { get; private set; }
        public float CombatSeconds { get; private set; }
        public int ShotsFired { get; private set; }
        public int SecurityHits { get; private set; }
        public int SapperPulses { get; private set; }
        public int ThreatsPurged { get; private set; }
        public int SwarmerContacts { get; private set; }
        public int SwarmersPurged { get; private set; }
        public float SignalRecovered { get; private set; }
        public int BestSalvageChain { get; private set; }
        public float SalvageSignalRecovered { get; private set; }
        public float PassiveSignalSpent { get; private set; }
        public float MovementSignalSpent { get; private set; }
        public float WeaponSignalSpent { get; private set; }
        public float MinimumSignal { get; private set; } = RunModel.StartingSignal;
        public int PeakThreatConcurrency { get; private set; }

        public void Advance(float seconds, bool isPowered)
        {
            Advance(seconds, isPowered, false);
        }

        public void Advance(float seconds, bool isPowered, bool isCombatActive)
        {
            if (seconds <= 0f)
            {
                return;
            }

            ElapsedSeconds += seconds;
            if (!isPowered)
            {
                DeadZoneSeconds += seconds;
            }
            if (isCombatActive)
            {
                CombatSeconds += seconds;
            }
        }

        public void RecordShot()
        {
            ShotsFired++;
            WeaponSignalSpent += RunModel.ShotCost;
        }

        public void RecordTraversalDrain(float passive, float movement)
        {
            PassiveSignalSpent += Math.Max(0f, passive);
            MovementSignalSpent += Math.Max(0f, movement);
        }

        public void RecordSignal(float signal)
        {
            MinimumSignal = Math.Min(MinimumSignal, Math.Max(0f, signal));
        }

        public void RecordThreatConcurrency(int activeThreats)
        {
            PeakThreatConcurrency = Math.Max(PeakThreatConcurrency, Math.Max(0, activeThreats));
        }

        public void RecordSecurityHit()
        {
            SecurityHits++;
        }

        public void RecordSapperPulse()
        {
            SapperPulses++;
        }

        public void RecordThreatPurge(float signalRecovered)
        {
            ThreatsPurged++;
            SignalRecovered += Math.Max(0f, signalRecovered);
        }

        public void RecordSwarmerContact()
        {
            SwarmerContacts++;
        }

        public void RecordSwarmerPurge(float signalRecovered)
        {
            SwarmersPurged++;
            RecordThreatPurge(signalRecovered);
        }

        public void RecordSalvageChain(int chainCount, float signalRecovered)
        {
            BestSalvageChain = Math.Max(BestSalvageChain, chainCount);
            RecordSalvageSignalRecovered(signalRecovered);
        }

        public void RecordSalvageSignalRecovered(float signalRecovered)
        {
            SalvageSignalRecovered += Math.Max(0f, signalRecovered);
        }

        public void RecordDepartureSurge(float signalRecovered)
        {
            SignalRecovered += Math.Max(0f, signalRecovered);
        }
    }

    /// <summary>
    /// Deterministic, engine-independent rules for the vertical slice.
    /// Runtime presentation and input live in focused orchestration services; tests can exercise this model directly.
    /// </summary>
    public sealed class RunModel
    {
        public const float MaximumSignal = 100f;
        public const float StartingSignal = 72f;
        public const float ShotCost = 0f;
        public const float TowerCost = 10f;
        public const float TowerRefill = 62f;
        public const float RelayTowerRefill = 44f;
        public const float SpineTowerRefill = 34f;
        public const float ShortcutCost = 16f;
        public const float SecurityHitCost = 18f;
        public const float SapperPulseCost = 8f;
        public const int SalvageRequired = 3;
        public const float CriticalRecoveryDuration = 5f;

        public RunModel(MissionObjectiveGraph objectiveGraph = null)
        {
            m_objectiveGraph = objectiveGraph ?? CompatibilityMissionObjectiveGraph.Instance;
        }

        public float Signal { get; private set; } = StartingSignal;
        public int Salvage { get; private set; }
        public bool TowerOnline { get; private set; }
        public bool RelayTowerOnline { get; private set; }
        public bool SpineRelayResultInstalled { get; private set; }
        public bool SpineTowerOnline => SpineRelayResultInstalled;
        public bool DeepReturnNetworkPowered => SpineRelayResultInstalled;
        public bool CoreRebuildUnlocked => SpineRelayResultInstalled;
        public bool ShortcutOpen { get; private set; }
        public bool OptionalSalvageSecured { get; private set; }
        public bool CentralPayloadSecured { get; private set; }
        public bool CentralPayloadAssembled { get; private set; }
        public bool CargoCouplingSecured { get; private set; }
        public bool CoolantSealSecured { get; private set; }
        public bool RelayFeedsRouted { get; private set; }
        public bool RelayPayloadStabilized { get; private set; }
        public bool RelayPayloadSecured { get; private set; }
        public bool SpineBerthVented { get; private set; }
        public bool SpinePayloadSecured { get; private set; }
        public RunOutcome Outcome { get; private set; } = RunOutcome.Running;
        public float CriticalRecoveryRemaining { get; private set; }
        public bool IsCriticalRecovery => CriticalRecoveryRemaining > 0f && Outcome == RunOutcome.Running;

        public bool HasAllRegionalPayloads => CentralPayloadSecured && RelayPayloadSecured && SpinePayloadSecured;
        public bool CanExtract => Outcome == RunOutcome.Running && TowerOnline && RelayTowerOnline && SpineTowerOnline &&
                                  HasAllRegionalPayloads;
        public MissionObjectiveDefinition CurrentObjective => m_objectiveGraph.Evaluate(_isObjectiveComplete);
        public MissionStage CurrentMissionStage => CurrentObjective.LegacyStage;

        public static float PassiveDrainRate(bool isPowered) => isPowered ? 0f : 2.8f;
        public static float MovementDrainRate(bool isMoving, bool isPowered) =>
            isMoving ? (isPowered ? 0.38f : 3.2f) : 0f;

        public bool TrySpend(float amount)
        {
            if (Outcome != RunOutcome.Running || amount < 0f || Signal < amount)
            {
                return false;
            }

            Signal -= amount;
            _evaluateSignal();
            return true;
        }

        public void Advance(float seconds, bool isMoving, bool isPowered)
        {
            if (Outcome != RunOutcome.Running || seconds <= 0f)
            {
                return;
            }

            var passiveDrain = PassiveDrainRate(isPowered);
            var movementDrain = MovementDrainRate(isMoving, isPowered);
            Signal -= (passiveDrain + movementDrain) * seconds;
            Signal = Math.Max(0f, Signal);
            _evaluateSignal(seconds);
        }

        public bool TryActivateTower()
        {
            if (!_isCurrentObjective(MissionObjectiveId.CentralTower) || TowerOnline || Outcome != RunOutcome.Running ||
                (Signal < TowerCost && !IsCriticalRecovery))
            {
                return false;
            }

            TowerOnline = true;
            // Activation is one atomic transaction: the tower refill lands before a zero-Signal
            // death evaluation, so spending the drone's last 10 Signal on rescue is valid.
            Signal = Math.Min(MaximumSignal, Math.Max(0f, Signal - TowerCost) + TowerRefill);
            CriticalRecoveryRemaining = 0f;
            return true;
        }

        public bool TryOpenShortcut()
        {
            // The gate is network machinery and must not consume the drone's final Signal.
            // Requiring strictly more than the cost keeps a successful interaction playable.
            if (!TowerOnline || ShortcutOpen || Outcome != RunOutcome.Running || Signal <= ShortcutCost)
            {
                return false;
            }

            Signal -= ShortcutCost;
            ShortcutOpen = true;
            return true;
        }

        public bool TryActivateRelayTower()
        {
            if (!_isCurrentObjective(MissionObjectiveId.RelayTower) || !TowerOnline || !CentralPayloadSecured ||
                RelayTowerOnline || Outcome != RunOutcome.Running)
            {
                return false;
            }

            Signal = Math.Min(MaximumSignal, Signal + RelayTowerRefill);
            RelayTowerOnline = true;
            CriticalRecoveryRemaining = 0f;
            return true;
        }

        public bool TryRouteCentralComponents()
        {
            if (!_isCurrentObjective(MissionObjectiveId.RelayFork) || !CargoCouplingSecured || !CoolantSealSecured ||
                RelayFeedsRouted || Outcome != RunOutcome.Running)
            {
                return false;
            }

            RelayFeedsRouted = true;
            return true;
        }

        public bool TryAssembleCentralPayload()
        {
            if (!_isCurrentObjective(MissionObjectiveId.CentralAssembly) || !RelayFeedsRouted ||
                CentralPayloadAssembled || Outcome != RunOutcome.Running)
            {
                return false;
            }

            CentralPayloadAssembled = true;
            return true;
        }

        public bool TryInstallCentralPayload()
        {
            if (!_isCurrentObjective(MissionObjectiveId.CentralInstallation) || !CentralPayloadAssembled ||
                CentralPayloadSecured || Outcome != RunOutcome.Running)
            {
                return false;
            }

            CentralPayloadSecured = true;
            return true;
        }

        public bool TryActivateSpineTower()
        {
            if (!_isCurrentObjective(MissionObjectiveId.SpineTower) || !RelayTowerOnline || !RelayPayloadSecured ||
                !SpineBerthVented || SpineRelayResultInstalled || Outcome != RunOutcome.Running)
            {
                return false;
            }

            Signal = Math.Min(MaximumSignal, Signal + SpineTowerRefill);
            SpineRelayResultInstalled = true;
            CriticalRecoveryRemaining = 0f;
            return true;
        }

        public bool TryVentSpineBerth()
        {
            if (!_isCurrentObjective(MissionObjectiveId.SpineVenting) || !RelayPayloadSecured ||
                SpineBerthVented || Outcome != RunOutcome.Running)
            {
                return false;
            }

            SpineBerthVented = true;
            return true;
        }

        public bool TryInstallRelayPayload()
        {
            if (!_isCurrentObjective(MissionObjectiveId.RelayInstallation) || !RelayPayloadStabilized ||
                RelayPayloadSecured || Outcome != RunOutcome.Running)
            {
                return false;
            }

            RelayPayloadSecured = true;
            return true;
        }

        public void TakeSecurityHit()
        {
            if (Outcome != RunOutcome.Running)
            {
                return;
            }

            Signal = Math.Max(0f, Signal - SecurityHitCost);
            _evaluateSignal();
        }

        public void TakeSapperPulse()
        {
            if (Outcome != RunOutcome.Running)
            {
                return;
            }

            Signal = Math.Max(0f, Signal - SapperPulseCost);
            _evaluateSignal();
        }

        public void TakeSuppressionPulse(float amount)
        {
            if (Outcome != RunOutcome.Running || amount <= 0f)
            {
                return;
            }

            Signal = Math.Max(0f, Signal - amount);
            _evaluateSignal();
        }

        public bool CanCollectPayload(SignalRegion region, CentralComponentKind centralComponent = CentralComponentKind.None)
        {
            if (Outcome != RunOutcome.Running)
            {
                return false;
            }

            return region switch
            {
                SignalRegion.Central => _canCollectCentralComponent(centralComponent),
                SignalRegion.Relay => _isCurrentObjective(MissionObjectiveId.RelayPayload) && RelayTowerOnline &&
                                      CentralPayloadSecured && !RelayPayloadStabilized,
                SignalRegion.Spine => _isCurrentObjective(MissionObjectiveId.SpinePayload) && SpineTowerOnline &&
                                      RelayPayloadSecured && !SpinePayloadSecured,
                _ => false
            };
        }

        public bool CollectPayload(SignalRegion region, CentralComponentKind centralComponent = CentralComponentKind.None)
        {
            if (!CanCollectPayload(region, centralComponent))
            {
                return false;
            }

            switch (region)
            {
                case SignalRegion.Central:
                    if (centralComponent == CentralComponentKind.None)
                    {
                        CargoCouplingSecured = true;
                        CoolantSealSecured = true;
                    }
                    else if (centralComponent == CentralComponentKind.CoolantSeal)
                    {
                        CoolantSealSecured = true;
                    }
                    else
                    {
                        CargoCouplingSecured = true;
                    }

                    break;
                case SignalRegion.Relay:
                    RelayPayloadStabilized = true;
                    break;
                case SignalRegion.Spine:
                    SpinePayloadSecured = true;
                    break;
                default:
                    return false;
            }

            if (region != SignalRegion.Central || Salvage == 0)
            {
                Salvage++;
            }
            return true;
        }

        public void CollectSalvage()
        {
            if (CanCollectPayload(SignalRegion.Central))
            {
                CollectPayload(SignalRegion.Central);
            }
            else if (CanCollectPayload(SignalRegion.Relay))
            {
                CollectPayload(SignalRegion.Relay);
            }
            else if (CanCollectPayload(SignalRegion.Spine))
            {
                CollectPayload(SignalRegion.Spine);
            }
        }

        public float CollectOptionalSalvage(float signalReward)
        {
            if (!CanExtract || OptionalSalvageSecured)
            {
                return 0f;
            }

            OptionalSalvageSecured = true;
            return RestoreSignal(signalReward);
        }

        public float RestoreSignal(float amount)
        {
            if (Outcome != RunOutcome.Running || amount <= 0f)
            {
                return 0f;
            }

            var previousSignal = Signal;
            Signal = Math.Min(MaximumSignal, Signal + amount);
            if (Signal > 0f)
            {
                CriticalRecoveryRemaining = 0f;
            }
            return Signal - previousSignal;
        }

        internal void SetSignalForDebug(float signal)
        {
            if (Outcome != RunOutcome.Running)
            {
                return;
            }

            Signal = Math.Max(0f, Math.Min(MaximumSignal, signal));
            CriticalRecoveryRemaining = Signal <= 0f ? CriticalRecoveryDuration : 0f;
        }

        public bool TryExtract()
        {
            if (!_isCurrentObjective(MissionObjectiveId.Extraction) || !CanExtract)
            {
                return false;
            }

            Outcome = RunOutcome.Victory;
            return true;
        }

        private bool _isObjectiveComplete(MissionCompletionRule rule)
        {
            return rule switch
            {
                MissionCompletionRule.CentralTowerOnline => TowerOnline,
                MissionCompletionRule.CentralPayloadSecured => CentralPayloadSecured,
                MissionCompletionRule.RelayTowerOnline => RelayTowerOnline,
                MissionCompletionRule.RelayPayloadSecured => RelayPayloadSecured,
                MissionCompletionRule.SpineTowerOnline => SpineTowerOnline,
                MissionCompletionRule.SpinePayloadSecured => SpinePayloadSecured,
                MissionCompletionRule.ExtractionComplete => Outcome == RunOutcome.Victory,
                MissionCompletionRule.CargoCouplingSecured => CargoCouplingSecured,
                MissionCompletionRule.CoolantSealSecured => CoolantSealSecured,
                MissionCompletionRule.RelayFeedsRouted => RelayFeedsRouted,
                MissionCompletionRule.CentralPayloadAssembled => CentralPayloadAssembled,
                MissionCompletionRule.CentralPayloadInstalled => CentralPayloadSecured,
                MissionCompletionRule.RelayPayloadStabilized => RelayPayloadStabilized,
                MissionCompletionRule.SpineBerthVented => SpineBerthVented,
                MissionCompletionRule.SpineRelayResultInstalled => SpineRelayResultInstalled,
                _ => false
            };
        }

        private bool _canCollectCentralComponent(CentralComponentKind component)
        {
            if (!TowerOnline || CentralPayloadSecured)
            {
                return false;
            }

            if (component == CentralComponentKind.None)
            {
                return m_objectiveGraph.IsAvailable(MissionObjectiveId.CargoCoupling, _isObjectiveComplete) ||
                       m_objectiveGraph.IsAvailable(MissionObjectiveId.CoolantSeal, _isObjectiveComplete);
            }

            return component switch
            {
                CentralComponentKind.PowerCoupling => !CargoCouplingSecured &&
                                                      m_objectiveGraph.IsAvailable(MissionObjectiveId.CargoCoupling,
                                                          _isObjectiveComplete),
                CentralComponentKind.CoolantSeal => !CoolantSealSecured &&
                                                   m_objectiveGraph.IsAvailable(MissionObjectiveId.CoolantSeal,
                                                       _isObjectiveComplete),
                _ => false
            };
        }

        private bool _isCurrentObjective(MissionObjectiveId objectiveId)
        {
            return CurrentObjective.Id == objectiveId;
        }

        private void _evaluateSignal(float elapsedSeconds = 0f)
        {
            if (Signal > 0f || Outcome != RunOutcome.Running)
            {
                return;
            }

            if (CriticalRecoveryRemaining <= 0f && elapsedSeconds <= 0f)
            {
                CriticalRecoveryRemaining = CriticalRecoveryDuration;
                return;
            }

            if (CriticalRecoveryRemaining <= 0f)
            {
                CriticalRecoveryRemaining = CriticalRecoveryDuration;
            }

            CriticalRecoveryRemaining = Math.Max(0f, CriticalRecoveryRemaining - elapsedSeconds);
            if (CriticalRecoveryRemaining <= 0f)
            {
                Outcome = RunOutcome.Destroyed;
            }
        }

        private readonly MissionObjectiveGraph m_objectiveGraph;
    }
}
