using UnityEngine;
using DeadSignal.Missions;

namespace DeadSignal.World
{
    public sealed class AuthoredWithdrawalPursuitLandmark : MonoBehaviour
    {
        [SerializeField] private PoweredWithdrawalPhase m_phase;

        public PoweredWithdrawalPhase Phase => m_phase;
        public Vector3 Position => transform.position;
        public bool IsConfigured => m_phase is PoweredWithdrawalPhase.WardenBay or PoweredWithdrawalPhase.SapperCradle;

        public void Configure(PoweredWithdrawalPhase phase)
        {
            m_phase = phase;
        }
    }
}
