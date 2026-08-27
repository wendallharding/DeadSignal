using System;

namespace DeadSignal.Missions
{
    public enum CargoCouplingRetrievalPhase
    {
        AwaitingCommit,
        CouplingAvailable,
        Withdrawing,
        Complete
    }

    /// <summary>
    /// Deterministic state for the Cargo Annex inward commitment and outward recovery crossing.
    /// Progress is measured along the authored withdrawal-to-commit axis.
    /// </summary>
    public sealed class CargoCouplingRetrieval
    {
        public CargoCouplingRetrievalPhase Phase { get; private set; }

        public CargoCouplingRetrieval(float commitmentProgress, float withdrawalProgress)
        {
            if (commitmentProgress <= withdrawalProgress)
            {
                throw new ArgumentException("Cargo commitment must be deeper than its withdrawal threshold.");
            }

            m_commitmentProgress = commitmentProgress;
            m_withdrawalProgress = withdrawalProgress;
            Reset();
        }

        public void Observe(float progress, bool objectiveAvailable)
        {
            if (!objectiveAvailable || Phase != CargoCouplingRetrievalPhase.AwaitingCommit ||
                progress < m_commitmentProgress)
            {
                return;
            }

            Phase = CargoCouplingRetrievalPhase.CouplingAvailable;
        }

        public bool TryTakeCoupling(float progress, bool objectiveAvailable)
        {
            Observe(progress, objectiveAvailable);
            if (!objectiveAvailable || Phase != CargoCouplingRetrievalPhase.CouplingAvailable)
            {
                return false;
            }

            Phase = CargoCouplingRetrievalPhase.Withdrawing;
            return true;
        }

        public bool CanCompleteWithdrawal(float progress, bool objectiveAvailable)
        {
            return objectiveAvailable && Phase == CargoCouplingRetrievalPhase.Withdrawing &&
                   progress <= m_withdrawalProgress;
        }

        public void CompleteWithdrawal()
        {
            if (Phase != CargoCouplingRetrievalPhase.Withdrawing)
            {
                throw new InvalidOperationException("The coupling must be carried before withdrawal can complete.");
            }

            Phase = CargoCouplingRetrievalPhase.Complete;
        }

        public void Reset()
        {
            Phase = CargoCouplingRetrievalPhase.AwaitingCommit;
        }

        private readonly float m_commitmentProgress;
        private readonly float m_withdrawalProgress;
    }
}
