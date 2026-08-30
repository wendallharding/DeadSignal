using UnityEngine;

namespace DeadSignal.Presentation
{
    /// <summary>Owns presentation-only motion for the authored Security Suppressor without moving its combat root.</summary>
    public sealed class SecuritySuppressorPresentation : MonoBehaviour
    {
        private const float HIT_DURATION = 0.2f;
        private const float PURGE_DURATION = 0.38f;
        private const float SHUTDOWN_DURATION = 0.3f;
        private const float WAKE_DURATION = 0.45f;

        public bool IsConfigured => m_suppressor != null && m_chassis != null && m_leftEmitter != null &&
                                    m_rightEmitter != null && m_core != null && m_purgeEcho != null;
        public bool IsWaking => m_wakeRemaining > 0f;
        public bool IsWarning => m_fieldVisible && !m_fieldActive;
        public bool IsProjecting => m_fieldVisible && m_fieldActive;
        public bool IsShuttingDown => m_shutdownRemaining > 0f;
        public bool IsHitReacting => m_hitRemaining > 0f;
        public bool IsPurgeVisible => m_purgeRemaining > 0f && m_purgeEcho != null && m_purgeEcho.gameObject.activeSelf;

        public void Configure(
            Transform suppressor,
            Transform chassis,
            Transform leftEmitter,
            Transform rightEmitter,
            Transform core)
        {
            m_suppressor = suppressor;
            m_chassis = chassis;
            m_leftEmitter = leftEmitter;
            m_rightEmitter = rightEmitter;
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

        public void PlayWake()
        {
            if (!IsConfigured) return;
            ResetPresentation();
            m_previousRootPosition = m_suppressor.position;
            m_wakeRemaining = WAKE_DURATION;
        }

        public void SetFieldState(bool visible, bool active)
        {
            if (m_fieldActive && (!visible || !active)) m_shutdownRemaining = SHUTDOWN_DURATION;
            m_fieldVisible = visible;
            m_fieldActive = visible && active;
        }

        public void PlayHit(Vector3 sourcePosition)
        {
            if (!IsConfigured) return;
            m_hitDirection = m_suppressor.position - sourcePosition;
            m_hitDirection.y = 0f;
            if (m_hitDirection.sqrMagnitude < 0.01f) m_hitDirection = -m_suppressor.forward;
            m_hitDirection.Normalize();
            m_hitRemaining = HIT_DURATION;
        }

        public void PlayPurge()
        {
            if (!IsConfigured) return;
            m_purgeEcho.SetPositionAndRotation(m_suppressor.position, m_suppressor.rotation);
            m_purgeEcho.localScale = m_suppressor.lossyScale;
            m_purgePosition = m_purgeEcho.localPosition;
            m_purgeRotation = m_purgeEcho.localRotation;
            m_purgeScale = m_purgeEcho.localScale;
            var parts = _parts();
            for (var index = 0; index < parts.Length; index++) _copyPose(parts[index], m_purgeEcho.GetChild(index));
            m_purgeEcho.gameObject.SetActive(true);
            m_purgeRemaining = PURGE_DURATION;
            m_fieldVisible = false;
            m_fieldActive = false;
            _resetLivePose();
        }

        public void ResetPresentation()
        {
            m_wakeRemaining = 0f;
            m_hitRemaining = 0f;
            m_purgeRemaining = 0f;
            m_shutdownRemaining = 0f;
            m_fieldVisible = false;
            m_fieldActive = false;
            if (m_purgeEcho != null) m_purgeEcho.gameObject.SetActive(false);
            _resetLivePose();
        }

        private void Update()
        {
            if (!IsConfigured) return;
            var dt = Time.deltaTime;
            _tickPurge(dt);
            if (!m_suppressor.gameObject.activeInHierarchy) return;

            m_wakeRemaining = Mathf.Max(0f, m_wakeRemaining - dt);
            m_hitRemaining = Mathf.Max(0f, m_hitRemaining - dt);
            m_shutdownRemaining = Mathf.Max(0f, m_shutdownRemaining - dt);
            var rootDelta = m_suppressor.position - m_previousRootPosition;
            rootDelta.y = 0f;
            m_previousRootPosition = m_suppressor.position;
            var movementWeight = dt > 0f ? Mathf.Clamp01(rootDelta.magnitude / (dt * 3f)) : 0f;
            var wakeProgress = m_wakeRemaining > 0f ? 1f - m_wakeRemaining / WAKE_DURATION : 1f;
            var wakeEase = 1f - Mathf.Pow(1f - wakeProgress, 3f);
            var warningPulse = IsWarning ? 0.5f + Mathf.Sin(Time.time * 12f) * 0.5f : 0f;
            var sustainPulse = IsProjecting ? 0.5f + Mathf.Sin(Time.time * 18f) * 0.5f : 0f;
            var shutdownProgress = IsShuttingDown ? 1f - m_shutdownRemaining / SHUTDOWN_DURATION : 1f;
            var shutdownArc = IsShuttingDown ? Mathf.Sin(shutdownProgress * Mathf.PI) : 0f;
            var hit = IsHitReacting ? Mathf.Sin(m_hitRemaining / HIT_DURATION * Mathf.PI) : 0f;
            var localHit = Quaternion.Inverse(m_suppressor.rotation) * m_hitDirection;
            var bank = Mathf.Clamp(rootDelta.x * 10f, -7f, 7f);

            m_chassis.localPosition = m_restPositions[0] + new Vector3(
                localHit.x * hit * 0.06f,
                Mathf.Lerp(-0.13f, 0f, wakeEase) + movementWeight * 0.025f - shutdownArc * 0.035f,
                localHit.z * hit * 0.06f);
            m_chassis.localRotation = m_restRotations[0] * Quaternion.Euler(
                movementWeight * -3f + shutdownArc * 8f,
                0f,
                -bank - localHit.x * hit * 10f);
            m_chassis.localScale = Vector3.Scale(
                m_restScales[0],
                new Vector3(1f, Mathf.Lerp(0.55f, 1f, wakeEase), 1f));
            _poseEmitter(m_leftEmitter, 1, -1f, wakeEase, warningPulse, sustainPulse, shutdownArc);
            _poseEmitter(m_rightEmitter, 2, 1f, wakeEase, warningPulse, sustainPulse, shutdownArc);
            m_core.localPosition = m_restPositions[3] + Vector3.up * (warningPulse * 0.03f + sustainPulse * 0.055f);
            m_core.localRotation = m_restRotations[3] * Quaternion.Euler(0f, Time.time * (IsProjecting ? 300f : IsWarning ? 150f : 55f), 0f);
            m_core.localScale = m_restScales[3] * Mathf.Lerp(0.5f, 1f + warningPulse * 0.12f + sustainPulse * 0.2f, wakeEase);
        }

        private void OnDisable() => ResetPresentation();

        private void _poseEmitter(
            Transform emitter,
            int index,
            float side,
            float wakeEase,
            float warningPulse,
            float sustainPulse,
            float shutdownArc)
        {
            var deployment = IsProjecting ? 0.16f : IsWarning ? 0.08f + warningPulse * 0.035f : 0f;
            emitter.localPosition = m_restPositions[index] + new Vector3(side * deployment, Mathf.Lerp(-0.1f, 0f, wakeEase), 0f);
            emitter.localRotation = m_restRotations[index] * Quaternion.Euler(
                sustainPulse * -5f + shutdownArc * 12f,
                side * (IsProjecting ? 12f : IsWarning ? 5f : 0f),
                side * (deployment * 25f));
            emitter.localScale = Vector3.Scale(m_restScales[index], new Vector3(
                Mathf.Lerp(0.5f, 1f + sustainPulse * 0.08f, wakeEase),
                Mathf.Lerp(0.5f, 1f, wakeEase),
                1f));
        }

        private Transform[] _parts() => new[] { m_chassis, m_leftEmitter, m_rightEmitter, m_core };

        private Transform _createPurgeEcho()
        {
            var echo = new GameObject("Security Suppressor Purge Echo").transform;
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
            m_purgeEcho.localPosition = m_purgePosition + Vector3.up * (progress * 0.16f);
            m_purgeEcho.localRotation = m_purgeRotation * Quaternion.Euler(progress * -28f, progress * 240f, 0f);
            m_purgeEcho.localScale = Vector3.Scale(m_purgeScale,
                new Vector3(1f + progress * 0.55f, 1f - progress * 0.9f, 1f + progress * 0.55f));
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

        private Transform m_suppressor;
        private Transform m_chassis;
        private Transform m_leftEmitter;
        private Transform m_rightEmitter;
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
        private float m_shutdownRemaining;
        private bool m_fieldVisible;
        private bool m_fieldActive;
    }
}
