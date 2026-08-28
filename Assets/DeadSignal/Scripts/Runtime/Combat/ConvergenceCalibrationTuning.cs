using UnityEngine;

namespace DeadSignal.Combat
{
    [CreateAssetMenu(menuName = "DEAD SIGNAL/Convergence Calibration Tuning")]
    public sealed class ConvergenceCalibrationTuning : ScriptableObject
    {
        [SerializeField, Min(1f)] private float m_holdDuration = 12f;
        [SerializeField] private SecurityReinforcement m_pressureRole = SecurityReinforcement.Interceptor;

        public float HoldDuration => Mathf.Max(1f, m_holdDuration);
        public SecurityReinforcement PressureRole => m_pressureRole;

        public void Configure(float holdDuration, SecurityReinforcement pressureRole)
        {
            m_holdDuration = Mathf.Max(1f, holdDuration);
            m_pressureRole = pressureRole;
        }

        private void OnValidate()
        {
            m_holdDuration = Mathf.Max(1f, m_holdDuration);
            if (m_pressureRole == SecurityReinforcement.None)
            {
                m_pressureRole = SecurityReinforcement.Interceptor;
            }
        }
    }
}
