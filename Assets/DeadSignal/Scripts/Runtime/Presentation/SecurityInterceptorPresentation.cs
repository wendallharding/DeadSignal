using UnityEngine;

namespace DeadSignal.Presentation
{
    /// <summary>Owns presentation-only motion for the authored Security Interceptor without moving its combat root.</summary>
    public sealed class SecurityInterceptorPresentation : MonoBehaviour
    {
        private const float HIT_DURATION = 0.2f;
        private const float PURGE_DURATION = 0.36f;
        private const float WAKE_DURATION = 0.42f;

        private Transform m_interceptor;
        private Transform m_chassis;
        private Transform m_leftBlade;
        private Transform m_rightBlade;
        private Transform m_core;
        private Transform m_purgeEcho;
        private Vector3[] m_restPositions;
        private Quaternion[] m_restRotations;
        private Vector3[] m_restScales;
        private Vector3 m_previousRootPosition;
        private Vector3 m_hitDirection;
        private Vector3 m_purgePosition;
        private Quaternion m_purgeRotation;
        private Vector3 m_purgeScale;
        private float m_wakeRemaining;
        private float m_hitRemaining;
        private float m_purgeRemaining;
        private float m_recoveryRemaining;
        private float m_recoveryDuration = 1f;
        private bool m_charging;
        private bool m_dashing;
        private bool m_crashed;

        public bool IsConfigured => m_interceptor != null && m_chassis != null && m_leftBlade != null &&
                                    m_rightBlade != null && m_core != null && m_purgeEcho != null;
        public bool IsWaking => m_wakeRemaining > 0f;
        public bool IsChargeLocked => m_charging;
        public bool IsDashCommitted => m_dashing;
        public bool IsRecovering => m_recoveryRemaining > 0f;
        public bool IsCoverCrash => IsRecovering && m_crashed;
        public bool IsHitReacting => m_hitRemaining > 0f;
        public bool IsPurgeVisible => m_purgeRemaining > 0f && m_purgeEcho != null && m_purgeEcho.gameObject.activeSelf;

        public void Configure(Transform interceptor, Transform chassis, Transform leftBlade, Transform rightBlade, Transform core)
        {
            m_interceptor = interceptor;
            m_chassis = chassis;
            m_leftBlade = leftBlade;
            m_rightBlade = rightBlade;
            m_core = core;
            var parts = _parts();
            m_restPositions = new Vector3[parts.Length];
            m_restRotations = new Quaternion[parts.Length];
            m_restScales = new Vector3[parts.Length];
            for (var index = 0; index < parts.Length; index++)
            {
                m_restPositions[index] = parts[index].localPosition;
                m_restRotations[index] = parts[index].localRotation;
                m_restScales[index] = parts[index].localScale;
            }
            m_purgeEcho = _createPurgeEcho();
            ResetPresentation();
        }

        public void SetThreatState(bool charging, bool dashing)
        {
            m_charging = charging;
            m_dashing = dashing;
        }

        public void PlayWake()
        {
            if (!IsConfigured) return;
            ResetPresentation();
            m_previousRootPosition = m_interceptor.position;
            m_wakeRemaining = WAKE_DURATION;
        }

        public void PlayRecovery(bool hitCover, float duration)
        {
            if (!IsConfigured) return;
            m_crashed = hitCover;
            m_recoveryDuration = Mathf.Max(0.01f, duration);
            m_recoveryRemaining = m_recoveryDuration;
            m_charging = false;
            m_dashing = false;
        }

        public void PlayHit(Vector3 sourcePosition)
        {
            if (!IsConfigured) return;
            m_hitDirection = m_interceptor.position - sourcePosition;
            m_hitDirection.y = 0f;
            if (m_hitDirection.sqrMagnitude < 0.01f) m_hitDirection = -m_interceptor.forward;
            m_hitDirection.Normalize();
            m_hitRemaining = HIT_DURATION;
        }

        public void PlayPurge()
        {
            if (!IsConfigured) return;
            m_purgeEcho.SetPositionAndRotation(m_interceptor.position, m_interceptor.rotation);
            m_purgeEcho.localScale = m_interceptor.lossyScale;
            m_purgePosition = m_purgeEcho.localPosition;
            m_purgeRotation = m_purgeEcho.localRotation;
            m_purgeScale = m_purgeEcho.localScale;
            var parts = _parts();
            for (var index = 0; index < parts.Length; index++) _copyPose(parts[index], m_purgeEcho.GetChild(index));
            m_purgeEcho.gameObject.SetActive(true);
            m_purgeRemaining = PURGE_DURATION;
            _resetLivePose();
        }

        public void ResetPresentation()
        {
            m_wakeRemaining = 0f;
            m_hitRemaining = 0f;
            m_purgeRemaining = 0f;
            m_recoveryRemaining = 0f;
            m_charging = false;
            m_dashing = false;
            m_crashed = false;
            if (m_purgeEcho != null) m_purgeEcho.gameObject.SetActive(false);
            _resetLivePose();
        }

        private void Update()
        {
            if (!IsConfigured) return;
            var dt = Time.deltaTime;
            _tickPurge(dt);
            if (!m_interceptor.gameObject.activeInHierarchy) return;
            m_wakeRemaining = Mathf.Max(0f, m_wakeRemaining - dt);
            m_hitRemaining = Mathf.Max(0f, m_hitRemaining - dt);
            m_recoveryRemaining = Mathf.Max(0f, m_recoveryRemaining - dt);

            var rootDelta = m_interceptor.position - m_previousRootPosition;
            rootDelta.y = 0f;
            m_previousRootPosition = m_interceptor.position;
            var speedWeight = dt > 0f ? Mathf.Clamp01(rootDelta.magnitude / (dt * 5f)) : 0f;
            var wakeProgress = m_wakeRemaining > 0f ? 1f - m_wakeRemaining / WAKE_DURATION : 1f;
            var wakeEase = 1f - Mathf.Pow(1f - wakeProgress, 3f);
            var chargePulse = m_charging ? 0.5f + Mathf.Sin(Time.time * 16f) * 0.5f : 0f;
            var recoveryProgress = IsRecovering ? 1f - m_recoveryRemaining / m_recoveryDuration : 1f;
            var recoveryArc = IsRecovering ? Mathf.Sin(recoveryProgress * Mathf.PI) : 0f;
            var hit = m_hitRemaining > 0f ? Mathf.Sin(m_hitRemaining / HIT_DURATION * Mathf.PI) : 0f;
            var localHit = Quaternion.Inverse(m_interceptor.rotation) * m_hitDirection;
            var bank = Mathf.Clamp(rootDelta.x * 12f, -10f, 10f);

            m_chassis.localPosition = m_restPositions[0] + new Vector3(localHit.x * hit * 0.07f,
                Mathf.Lerp(-0.16f, 0f, wakeEase) + speedWeight * 0.025f - recoveryArc * (m_crashed ? 0.1f : 0.035f),
                localHit.z * hit * 0.07f + (m_dashing ? 0.08f : 0f));
            m_chassis.localRotation = m_restRotations[0] * Quaternion.Euler(
                m_charging ? 8f : m_dashing ? -11f : recoveryArc * (m_crashed ? 17f : 7f), 0f, -bank - localHit.x * hit * 12f);
            m_chassis.localScale = Vector3.Scale(m_restScales[0], new Vector3(1f, Mathf.Lerp(0.5f, 1f, wakeEase), 1f));
            _poseBlade(m_leftBlade, 1, -1f, wakeEase, chargePulse, recoveryArc);
            _poseBlade(m_rightBlade, 2, 1f, wakeEase, chargePulse, recoveryArc);
            m_core.localPosition = m_restPositions[3] + Vector3.up * (chargePulse * 0.055f - recoveryArc * 0.04f);
            m_core.localRotation = m_restRotations[3] * Quaternion.Euler(0f, Time.time * (m_charging ? 520f : 220f), 0f);
            m_core.localScale = m_restScales[3] * Mathf.Lerp(0.45f, 1f + chargePulse * 0.22f, wakeEase);
        }

        private void OnDisable() => ResetPresentation();

        private void _poseBlade(Transform blade, int index, float side, float wakeEase, float chargePulse, float recoveryArc)
        {
            var spread = m_charging ? 0.12f + chargePulse * 0.035f : m_dashing ? -0.055f : 0f;
            blade.localPosition = m_restPositions[index] + new Vector3(side * spread, Mathf.Lerp(-0.12f, 0f, wakeEase), 0f);
            blade.localRotation = m_restRotations[index] * Quaternion.Euler(
                m_dashing ? -7f : recoveryArc * (m_crashed ? 14f : 5f), side * (m_charging ? 9f : 0f), side * recoveryArc * 9f);
            blade.localScale = Vector3.Scale(m_restScales[index], new Vector3(1f, Mathf.Lerp(0.45f, 1f, wakeEase), 1f));
        }

        private Transform[] _parts() => new[] { m_chassis, m_leftBlade, m_rightBlade, m_core };

        private Transform _createPurgeEcho()
        {
            var echo = new GameObject("Security Interceptor Purge Echo").transform;
            echo.SetParent(transform, false);
            foreach (var source in _parts())
            {
                var part = new GameObject($"{source.name} Echo");
                part.transform.SetParent(echo, false);
                part.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
                var renderer = part.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
                renderer.shadowCastingMode = source.GetComponent<MeshRenderer>().shadowCastingMode;
                renderer.receiveShadows = source.GetComponent<MeshRenderer>().receiveShadows;
            }
            echo.gameObject.SetActive(false);
            return echo;
        }

        private void _tickPurge(float dt)
        {
            if (m_purgeRemaining <= 0f || m_purgeEcho == null) return;
            m_purgeRemaining = Mathf.Max(0f, m_purgeRemaining - dt);
            var progress = 1f - m_purgeRemaining / PURGE_DURATION;
            m_purgeEcho.localPosition = m_purgePosition + Vector3.up * (progress * 0.2f);
            m_purgeEcho.localRotation = m_purgeRotation * Quaternion.Euler(progress * 65f, progress * 320f, 0f);
            m_purgeEcho.localScale = Vector3.Scale(m_purgeScale,
                new Vector3(1f + progress * 0.35f, 1f - progress * 0.92f, 1f + progress * 0.55f));
            if (m_purgeRemaining <= 0f) m_purgeEcho.gameObject.SetActive(false);
        }

        private static void _copyPose(Transform source, Transform destination)
        {
            destination.localPosition = source.localPosition;
            destination.localRotation = source.localRotation;
            destination.localScale = source.localScale;
        }

        private void _resetLivePose()
        {
            if (m_chassis == null || m_restPositions == null) return;
            var parts = _parts();
            for (var index = 0; index < parts.Length; index++)
            {
                parts[index].localPosition = m_restPositions[index];
                parts[index].localRotation = m_restRotations[index];
                parts[index].localScale = m_restScales[index];
            }
        }
    }
}
