using System;

namespace DeadSignal
{
    public enum RunOutcome
    {
        Running,
        Victory,
        Destroyed
    }

    /// <summary>
    /// Small deterministic run report used by the result screen and future balance sessions.
    /// It deliberately stores no personal or persistent data.
    /// </summary>
    public sealed class RunMetrics
    {
        public float ElapsedSeconds { get; private set; }
        public float DeadZoneSeconds { get; private set; }
        public int ShotsFired { get; private set; }
        public int SecurityHits { get; private set; }
        public int SapperPulses { get; private set; }
        public int ThreatsPurged { get; private set; }
        public float SignalRecovered { get; private set; }
        public int BestSalvageChain { get; private set; }
        public float SalvageSignalRecovered { get; private set; }
        public float PassiveSignalSpent { get; private set; }
        public float MovementSignalSpent { get; private set; }
        public float WeaponSignalSpent { get; private set; }

        public void Advance(float seconds, bool isPowered)
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

        public void RecordSalvageChain(int chainCount, float signalRecovered)
        {
            BestSalvageChain = Math.Max(BestSalvageChain, chainCount);
            RecordSalvageSignalRecovered(signalRecovered);
        }

        public void RecordSalvageSignalRecovered(float signalRecovered)
        {
            SalvageSignalRecovered += Math.Max(0f, signalRecovered);
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
        public const float ShotCost = 5f;
        public const float TowerCost = 10f;
        public const float TowerRefill = 62f;
        public const float ShortcutCost = 16f;
        public const float SecurityHitCost = 18f;
        public const float SapperPulseCost = 8f;
        public const int SalvageRequired = 3;
        public const float CriticalRecoveryDuration = 5f;

        public float Signal { get; private set; } = StartingSignal;
        public int Salvage { get; private set; }
        public bool TowerOnline { get; private set; }
        public bool ShortcutOpen { get; private set; }
        public bool OptionalSalvageSecured { get; private set; }
        public RunOutcome Outcome { get; private set; } = RunOutcome.Running;
        public float CriticalRecoveryRemaining { get; private set; }
        public bool IsCriticalRecovery => CriticalRecoveryRemaining > 0f && Outcome == RunOutcome.Running;

        public bool CanExtract => Outcome == RunOutcome.Running && Salvage >= SalvageRequired;

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
            if (TowerOnline || Outcome != RunOutcome.Running || (Signal < TowerCost && !IsCriticalRecovery))
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

        public void CollectSalvage()
        {
            if (Outcome == RunOutcome.Running && Salvage < SalvageRequired)
            {
                Salvage++;
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

        public bool TryExtract()
        {
            if (!CanExtract)
            {
                return false;
            }

            Outcome = RunOutcome.Victory;
            return true;
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
    }
}
