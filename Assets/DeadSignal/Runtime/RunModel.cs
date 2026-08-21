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
        }

        public void RecordSecurityHit()
        {
            SecurityHits++;
        }

        public void RecordSapperPulse()
        {
            SapperPulses++;
        }
    }

    /// <summary>
    /// Deterministic, engine-independent rules for the vertical slice.
    /// Presentation and input live in DeadSignalGame; tests can exercise this model directly.
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

        public float Signal { get; private set; } = StartingSignal;
        public int Salvage { get; private set; }
        public bool TowerOnline { get; private set; }
        public bool ShortcutOpen { get; private set; }
        public RunOutcome Outcome { get; private set; } = RunOutcome.Running;

        public bool CanExtract => Outcome == RunOutcome.Running && Salvage >= SalvageRequired;

        public bool TrySpend(float amount)
        {
            if (Outcome != RunOutcome.Running || amount < 0f || Signal < amount)
            {
                return false;
            }

            Signal -= amount;
            EvaluateSignal();
            return true;
        }

        public void Advance(float seconds, bool isMoving, bool isPowered)
        {
            if (Outcome != RunOutcome.Running || seconds <= 0f)
            {
                return;
            }

            float passiveDrain = isPowered ? 0f : 2.8f;
            float movementDrain = isMoving ? (isPowered ? 0.38f : 3.2f) : 0f;
            Signal -= (passiveDrain + movementDrain) * seconds;
            Signal = Math.Max(0f, Signal);
            EvaluateSignal();
        }

        public bool TryActivateTower()
        {
            if (TowerOnline || Outcome != RunOutcome.Running || Signal < TowerCost)
            {
                return false;
            }

            TowerOnline = true;
            // Activation is one atomic transaction: the tower refill lands before a zero-Signal
            // death evaluation, so spending the drone's last 10 Signal on rescue is valid.
            Signal = Math.Min(MaximumSignal, Signal - TowerCost + TowerRefill);
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
            EvaluateSignal();
        }

        public void TakeSapperPulse()
        {
            if (Outcome != RunOutcome.Running)
            {
                return;
            }

            Signal = Math.Max(0f, Signal - SapperPulseCost);
            EvaluateSignal();
        }

        public void CollectSalvage()
        {
            if (Outcome == RunOutcome.Running && Salvage < SalvageRequired)
            {
                Salvage++;
            }
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

        private void EvaluateSignal()
        {
            if (Signal <= 0f && Outcome == RunOutcome.Running)
            {
                Outcome = RunOutcome.Destroyed;
            }
        }
    }
}
