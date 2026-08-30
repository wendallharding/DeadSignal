using UnityEngine;

namespace DeadSignal.Presentation
{
    /// <summary>Owns presentation-only motion for one fragile Swarmer without moving its combat root.</summary>
    public sealed class SecuritySwarmerPresentation : MonoBehaviour
    {
        private const float CONTACT_REACTION_DURATION = 0.16f;
        private const float PURGE_DURATION = 0.26f;
        private const float WAKE_DURATION = 0.28f;

        public bool IsConfigured => m_root != null && m_body != null && m_core != null && m_needle != null && m_tail != null;
        public bool IsWaking => m_wakeRemaining > 0f;
        public bool IsContactReacting => m_contactRemaining > 0f;
        public bool IsPurgeVisible => m_purgeRemaining > 0f && gameObject.activeSelf;
        public float Pressure => m_pressure;

        public void Configure(Transform root, Transform body, Transform core, Transform needle, Transform tail)
        {
            m_root = root;
            m_body = body;
            m_core = core;
            m_needle = needle;
            m_tail = tail;
            m_parts = new[] { body, core, needle, tail };
            m_restPositions = new Vector3[m_parts.Length];
            m_restRotations = new Quaternion[m_parts.Length];
            m_restScales = new Vector3[m_parts.Length];
            for (var index = 0; index < m_parts.Length; index++)
            {
                m_restPositions[index] = m_parts[index].localPosition;
                m_restRotations[index] = m_parts[index].localRotation;
                m_restScales[index] = m_parts[index].localScale;
            }
            m_phase = Mathf.Abs(GetInstanceID() % 31) * 0.37f;
            ResetPresentation();
        }

        public void PlayWake()
        {
            if (!IsConfigured) return;
            ResetPresentation();
            m_previousRootPosition = m_root.position;
            m_wakeRemaining = WAKE_DURATION;
        }

        public void SetPressure(float normalizedPressure)
        {
            m_pressureTarget = Mathf.Clamp01(normalizedPressure);
        }

        public void PlayContact()
        {
            if (!IsConfigured || m_purgeRemaining > 0f) return;
            m_contactRemaining = CONTACT_REACTION_DURATION;
        }

        public void PlayHitAndPurge(Vector3 sourcePosition)
        {
            if (!IsConfigured) return;
            var worldDirection = m_root.position - sourcePosition;
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.01f) worldDirection = -m_root.forward;
            m_hitDirection = m_root.InverseTransformDirection(worldDirection.normalized);
            m_purgeRemaining = PURGE_DURATION;
            m_wakeRemaining = 0f;
            m_contactRemaining = 0f;
            m_pressureTarget = 0f;
        }

        public void ResetPresentation()
        {
            m_wakeRemaining = 0f;
            m_contactRemaining = 0f;
            m_purgeRemaining = 0f;
            m_pressure = 0f;
            m_pressureTarget = 0f;
            if (IsConfigured) _resetPose();
        }

        private void Update()
        {
            if (!IsConfigured) return;
            var dt = Time.deltaTime;
            if (m_purgeRemaining > 0f)
            {
                _tickPurge(dt);
                return;
            }

            m_wakeRemaining = Mathf.Max(0f, m_wakeRemaining - dt);
            m_contactRemaining = Mathf.Max(0f, m_contactRemaining - dt);
            m_pressure = Mathf.MoveTowards(m_pressure, m_pressureTarget, dt * 5.5f);
            var rootDelta = m_root.position - m_previousRootPosition;
            m_previousRootPosition = m_root.position;
            var localDelta = m_root.InverseTransformDirection(rootDelta);
            var movement = dt > 0f ? Mathf.Clamp01(rootDelta.magnitude / (dt * 5f)) : 0f;
            var hover = Mathf.Sin(Time.time * 8.5f + m_phase);
            var wake = m_wakeRemaining > 0f ? 1f - m_wakeRemaining / WAKE_DURATION : 1f;
            var contact = m_contactRemaining > 0f ? m_contactRemaining / CONTACT_REACTION_DURATION : 0f;

            m_body.localPosition = m_restPositions[0] + Vector3.up * (hover * 0.025f - (1f - wake) * 0.14f);
            m_body.localRotation = m_restRotations[0] * Quaternion.Euler(
                -movement * 10f - m_pressure * 13f + contact * 12f,
                0f,
                Mathf.Clamp(-localDelta.x * 90f, -12f, 12f));
            m_body.localScale = Vector3.Scale(m_restScales[0], new Vector3(0.76f + wake * 0.24f, wake, 0.82f + wake * 0.18f));

            m_core.localPosition = m_restPositions[1] + Vector3.up * (hover * 0.04f + m_pressure * 0.035f);
            m_core.localRotation = m_restRotations[1] * Quaternion.Euler(0f, Time.time * (45f + movement * 55f), 0f);
            m_core.localScale = m_restScales[1] * (0.9f + wake * 0.1f + m_pressure * 0.12f);

            m_needle.localPosition = m_restPositions[2] + Vector3.forward * (m_pressure * 0.16f - contact * 0.12f);
            m_needle.localRotation = m_restRotations[2] * Quaternion.Euler(-m_pressure * 8f + contact * 16f, 0f, 0f);
            m_tail.localPosition = m_restPositions[3] - Vector3.forward * (movement * 0.08f);
            m_tail.localRotation = m_restRotations[3] * Quaternion.Euler(movement * 10f, 0f, hover * 5f);
        }

        private void _tickPurge(float dt)
        {
            m_purgeRemaining = Mathf.Max(0f, m_purgeRemaining - dt);
            var normalized = 1f - m_purgeRemaining / PURGE_DURATION;
            var recoil = Mathf.Sin(normalized * Mathf.PI) * 0.12f;
            for (var index = 0; index < m_parts.Length; index++)
            {
                m_parts[index].localPosition = m_restPositions[index] + m_hitDirection * recoil * (index == 2 ? 1.4f : 1f);
                m_parts[index].localRotation = m_restRotations[index] * Quaternion.Euler(normalized * 75f, normalized * 95f, 0f);
                m_parts[index].localScale = m_restScales[index] * Mathf.Max(0.05f, 1f - normalized);
            }
            if (m_purgeRemaining <= 0f) gameObject.SetActive(false);
        }

        private void _resetPose()
        {
            for (var index = 0; index < m_parts.Length; index++)
            {
                m_parts[index].localPosition = m_restPositions[index];
                m_parts[index].localRotation = m_restRotations[index];
                m_parts[index].localScale = m_restScales[index];
            }
            m_previousRootPosition = m_root.position;
        }

        private Transform m_root;
        private Transform m_body;
        private Transform m_core;
        private Transform m_needle;
        private Transform m_tail;
        private Transform[] m_parts;
        private Vector3[] m_restPositions;
        private Quaternion[] m_restRotations;
        private Vector3[] m_restScales;
        private Vector3 m_previousRootPosition;
        private Vector3 m_hitDirection;
        private float m_phase;
        private float m_wakeRemaining;
        private float m_contactRemaining;
        private float m_purgeRemaining;
        private float m_pressure;
        private float m_pressureTarget;
    }
}
