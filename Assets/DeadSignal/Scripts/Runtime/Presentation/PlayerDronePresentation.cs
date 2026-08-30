using DeadSignal.Missions;
using DeadSignal.Player;
using UnityEngine;

namespace DeadSignal.Presentation
{
    /// <summary>Owns presentation-only motion grammar for the authored maintenance drone.</summary>
    public sealed class PlayerDronePresentation : MonoBehaviour
    {
        private const float DAMAGE_DURATION = 0.24f;
        private const float DASH_LEAN_DURATION = 0.2f;
        private const float FIRE_PULSE_DURATION = 0.14f;
        private const float RECOVERY_DURATION = 0.5f;

        private Transform m_presentation;
        private Transform m_body;
        private Transform m_turret;
        private Transform m_signalRing;
        private Transform m_tool;
        private Vector3 m_signalRingRestScale;
        private Vector3 m_toolRestScale;
        private float m_damageRemaining;
        private float m_dashRemaining;
        private float m_fireRemaining;
        private float m_recoveryRemaining;
        private Vector3 m_damageDirection;
        private bool m_wasCritical;
        private bool m_evolvedFire;
        private RunOutcome m_outcome = RunOutcome.Running;

        public bool IsConfigured => m_presentation != null && m_body != null && m_turret != null &&
                                    m_signalRing != null && m_tool != null;
        public bool IsDamageReacting => m_damageRemaining > 0f;
        public bool IsCritical => m_wasCritical;
        public bool IsDefeated => m_outcome == RunOutcome.Destroyed;

        internal void Configure(
            Transform presentation,
            Transform body,
            Transform turret,
            Transform signalRing,
            Transform tool)
        {
            m_presentation = presentation;
            m_body = body;
            m_turret = turret;
            m_signalRing = signalRing;
            m_tool = tool;
            m_signalRingRestScale = signalRing.localScale;
            m_toolRestScale = tool.localScale;
            _resetPresentation();
        }

        public void Tick(
            float dt,
            Vector3 acceleration,
            Vector3 velocity,
            Vector3 aimDirection,
            PlayerDroneMovementTuning tuning,
            float signalRatio,
            bool isCriticalRecovery)
        {
            if (!IsConfigured || dt < 0f)
            {
                return;
            }

            if (m_wasCritical && !isCriticalRecovery && m_outcome == RunOutcome.Running)
            {
                m_recoveryRemaining = RECOVERY_DURATION;
            }
            m_wasCritical = isCriticalRecovery;

            m_damageRemaining = Mathf.Max(0f, m_damageRemaining - dt);
            m_dashRemaining = Mathf.Max(0f, m_dashRemaining - dt);
            m_fireRemaining = Mathf.Max(0f, m_fireRemaining - dt);
            m_recoveryRemaining = Mathf.Max(0f, m_recoveryRemaining - dt);

            _tickFacing(dt, acceleration, velocity, aimDirection, tuning);
            _tickBodyState(dt, tuning, Mathf.Clamp01(signalRatio));
        }

        public void PlayDamage(Vector3 sourcePosition)
        {
            if (!IsConfigured || m_outcome != RunOutcome.Running)
            {
                return;
            }

            m_damageDirection = transform.position - sourcePosition;
            m_damageDirection.y = 0f;
            if (m_damageDirection.sqrMagnitude < 0.01f)
            {
                m_damageDirection = -m_body.forward;
            }
            m_damageDirection.Normalize();
            m_damageRemaining = DAMAGE_DURATION;
        }

        public void PlayDash()
        {
            if (m_outcome == RunOutcome.Running)
            {
                m_dashRemaining = DASH_LEAN_DURATION;
            }
        }

        public void PlayFire(bool evolved)
        {
            if (m_outcome != RunOutcome.Running)
            {
                return;
            }

            m_evolvedFire = evolved;
            m_fireRemaining = FIRE_PULSE_DURATION;
        }

        public void SetOutcome(RunOutcome outcome)
        {
            m_outcome = outcome;
            if (outcome == RunOutcome.Running)
            {
                _resetPresentation();
                return;
            }

            if (outcome == RunOutcome.Destroyed && IsConfigured)
            {
                m_presentation.localPosition = Vector3.down * 0.22f;
                m_body.localRotation *= Quaternion.Euler(0f, 0f, 28f);
            }
        }

        private void OnDisable()
        {
            _resetPresentation();
        }

        private void _tickFacing(
            float dt,
            Vector3 acceleration,
            Vector3 velocity,
            Vector3 aimDirection,
            PlayerDroneMovementTuning tuning)
        {
            var bodyForward = velocity.sqrMagnitude > 0.01f ? velocity.normalized : m_body.forward;
            bodyForward.y = 0f;
            var bodyYaw = Quaternion.LookRotation(bodyForward, Vector3.up);
            var localAcceleration = Quaternion.Inverse(bodyYaw) * acceleration;
            var bankScale = tuning.MaximumBankDegrees / tuning.Acceleration;
            var targetBank = Quaternion.Euler(
                Mathf.Clamp(localAcceleration.z * bankScale, -tuning.MaximumBankDegrees, tuning.MaximumBankDegrees),
                0f,
                Mathf.Clamp(-localAcceleration.x * bankScale, -tuning.MaximumBankDegrees, tuning.MaximumBankDegrees));

            if (m_dashRemaining > 0f)
            {
                targetBank *= Quaternion.Euler(8f, 0f, 0f);
            }
            if (m_damageRemaining > 0f)
            {
                var localDamage = Quaternion.Inverse(bodyYaw) * m_damageDirection;
                var damagePulse = Mathf.Sin(m_damageRemaining / DAMAGE_DURATION * Mathf.PI);
                targetBank *= Quaternion.Euler(localDamage.z * 12f * damagePulse, 0f, -localDamage.x * 12f * damagePulse);
            }
            if (m_outcome == RunOutcome.Destroyed)
            {
                targetBank = Quaternion.Euler(0f, 0f, 28f);
            }

            var bodyTurnBlend = 1f - Mathf.Exp(-tuning.BodyTurnSharpness * dt);
            var bankBlend = 1f - Mathf.Exp(-tuning.BankSharpness * dt);
            var currentYaw = Quaternion.Euler(0f, m_body.localEulerAngles.y, 0f);
            var smoothedYaw = Quaternion.Slerp(currentYaw, bodyYaw, bodyTurnBlend);
            var currentBank = Quaternion.Inverse(currentYaw) * m_body.localRotation;
            m_body.localRotation = smoothedYaw * Quaternion.Slerp(currentBank, targetBank, bankBlend);

            if (aimDirection.sqrMagnitude > 0.01f && m_outcome == RunOutcome.Running)
            {
                var turretTarget = Quaternion.LookRotation(aimDirection.normalized, Vector3.up);
                var turretBlend = 1f - Mathf.Exp(-tuning.TurretTurnSharpness * dt);
                m_turret.rotation = Quaternion.Slerp(m_turret.rotation, turretTarget, turretBlend);
            }
            m_turret.localPosition = Vector3.up * tuning.TurretMountHeight;
        }

        private void _tickBodyState(float dt, PlayerDroneMovementTuning tuning, float signalRatio)
        {
            var hover = Mathf.Sin(Time.time * tuning.HoverFrequency * Mathf.PI * 2f) * tuning.HoverAmplitude;
            var criticalPulse = m_wasCritical ? Mathf.Sin(Time.time * 18f) * 0.018f : 0f;
            var recoveryLift = m_recoveryRemaining > 0f
                ? Mathf.Sin((1f - m_recoveryRemaining / RECOVERY_DURATION) * Mathf.PI) * 0.07f
                : 0f;
            var defeatDrop = m_outcome == RunOutcome.Destroyed ? -0.22f : 0f;
            m_presentation.localPosition = new Vector3(criticalPulse, hover + recoveryLift + defeatDrop, -criticalPulse * 0.5f);

            var ringSpeed = m_outcome == RunOutcome.Destroyed ? 0f : Mathf.Lerp(55f, 150f, 1f - signalRatio);
            m_signalRing.Rotate(Vector3.up, ringSpeed * dt, Space.Self);
            var ringPulse = m_wasCritical ? 1f + Mathf.Sin(Time.time * 16f) * 0.1f : 1f;
            m_signalRing.localScale = m_signalRingRestScale * ringPulse;

            var fireProgress = m_fireRemaining > 0f ? m_fireRemaining / FIRE_PULSE_DURATION : 0f;
            var firePulse = Mathf.Sin(fireProgress * Mathf.PI) * (m_evolvedFire ? 0.22f : 0.1f);
            m_tool.localScale = m_toolRestScale * (1f + firePulse);
        }

        private void _resetPresentation()
        {
            m_damageRemaining = 0f;
            m_dashRemaining = 0f;
            m_fireRemaining = 0f;
            m_recoveryRemaining = 0f;
            m_wasCritical = false;
            m_evolvedFire = false;
            m_outcome = RunOutcome.Running;
            if (m_presentation != null)
            {
                m_presentation.localPosition = Vector3.zero;
            }
            if (m_signalRing != null)
            {
                m_signalRing.localScale = m_signalRingRestScale;
            }
            if (m_tool != null)
            {
                m_tool.localScale = m_toolRestScale;
            }
        }
    }
}
