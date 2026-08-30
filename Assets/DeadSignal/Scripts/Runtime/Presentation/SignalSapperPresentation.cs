using UnityEngine;

namespace DeadSignal.Presentation
{
    /// <summary>Owns presentation-only motion for the authored Signal Sapper without moving its combat root.</summary>
    public sealed class SignalSapperPresentation : MonoBehaviour
    {
        private const float HIT_DURATION = 0.22f;
        private const float INTERRUPT_DURATION = 0.46f;
        private const float LATCH_DURATION = 0.4f;
        private const float PULSE_DURATION = 0.34f;
        private const float PURGE_DURATION = 0.42f;
        private const float WAKE_DURATION = 0.48f;

        private Transform m_sapper;
        private Transform m_chassis;
        private Transform m_leftFork;
        private Transform m_rightFork;
        private Transform m_core;
        private Transform m_purgeEcho;
        private Vector3 m_chassisRestPosition;
        private Quaternion m_chassisRestRotation;
        private Vector3 m_chassisRestScale;
        private Vector3 m_leftForkRestPosition;
        private Quaternion m_leftForkRestRotation;
        private Vector3 m_leftForkRestScale;
        private Vector3 m_rightForkRestPosition;
        private Quaternion m_rightForkRestRotation;
        private Vector3 m_rightForkRestScale;
        private Vector3 m_coreRestPosition;
        private Quaternion m_coreRestRotation;
        private Vector3 m_coreRestScale;
        private Vector3 m_previousRootPosition;
        private Vector3 m_hitDirection;
        private Vector3 m_purgeRestPosition;
        private Quaternion m_purgeRestRotation;
        private Vector3 m_purgeRestScale;
        private float m_wakeRemaining;
        private float m_latchRemaining;
        private float m_pulseRemaining;
        private float m_interruptRemaining;
        private float m_hitRemaining;
        private float m_purgeRemaining;
        private float m_pulseSecondsRemaining;
        private float m_pulseInterval = 1f;
        private bool m_latched;

        public bool IsConfigured => m_sapper != null && m_chassis != null && m_leftFork != null &&
                                    m_rightFork != null && m_core != null && m_purgeEcho != null;
        public bool IsWaking => m_wakeRemaining > 0f;
        public bool IsLatchDeploying => m_latchRemaining > 0f;
        public bool IsSiphonPulsing => m_pulseRemaining > 0f;
        public bool IsInterrupted => m_interruptRemaining > 0f;
        public bool IsHitReacting => m_hitRemaining > 0f;
        public bool IsTetherOwned => m_latched && m_sapper != null && m_sapper.gameObject.activeSelf;
        public bool IsPurgeVisible => m_purgeRemaining > 0f && m_purgeEcho != null && m_purgeEcho.gameObject.activeSelf;

        public void Configure(
            Transform sapper,
            Transform chassis,
            Transform leftFork,
            Transform rightFork,
            Transform core)
        {
            m_sapper = sapper;
            m_chassis = chassis;
            m_leftFork = leftFork;
            m_rightFork = rightFork;
            m_core = core;
            m_chassisRestPosition = chassis.localPosition;
            m_chassisRestRotation = chassis.localRotation;
            m_chassisRestScale = chassis.localScale;
            m_leftForkRestPosition = leftFork.localPosition;
            m_leftForkRestRotation = leftFork.localRotation;
            m_leftForkRestScale = leftFork.localScale;
            m_rightForkRestPosition = rightFork.localPosition;
            m_rightForkRestRotation = rightFork.localRotation;
            m_rightForkRestScale = rightFork.localScale;
            m_coreRestPosition = core.localPosition;
            m_coreRestRotation = core.localRotation;
            m_coreRestScale = core.localScale;
            m_purgeEcho = _createPurgeEcho();
            ResetPresentation();
        }

        public void SetThreatState(bool latched, float pulseSecondsRemaining, float pulseInterval)
        {
            if (latched && !m_latched)
            {
                m_latchRemaining = LATCH_DURATION;
            }

            m_latched = latched;
            m_pulseSecondsRemaining = Mathf.Max(0f, pulseSecondsRemaining);
            m_pulseInterval = Mathf.Max(0.01f, pulseInterval);
        }

        public void PlayWake()
        {
            if (!IsConfigured)
            {
                return;
            }

            ResetPresentation();
            m_previousRootPosition = m_sapper.position;
            m_wakeRemaining = WAKE_DURATION;
        }

        public void PlayPulse()
        {
            if (IsConfigured)
            {
                m_pulseRemaining = PULSE_DURATION;
            }
        }

        public void PlayHit(Vector3 sourcePosition, bool interrupted)
        {
            if (!IsConfigured)
            {
                return;
            }

            m_hitDirection = m_sapper.position - sourcePosition;
            m_hitDirection.y = 0f;
            if (m_hitDirection.sqrMagnitude < 0.01f)
            {
                m_hitDirection = -m_sapper.forward;
            }
            m_hitDirection.Normalize();
            m_hitRemaining = HIT_DURATION;
            if (interrupted)
            {
                m_interruptRemaining = INTERRUPT_DURATION;
            }
        }

        public void PlayPurge()
        {
            if (!IsConfigured)
            {
                return;
            }

            m_purgeEcho.SetPositionAndRotation(m_sapper.position, m_sapper.rotation);
            m_purgeEcho.localScale = m_sapper.lossyScale;
            m_purgeRestPosition = m_purgeEcho.localPosition;
            m_purgeRestRotation = m_purgeEcho.localRotation;
            m_purgeRestScale = m_purgeEcho.localScale;
            _copyPose(m_chassis, m_purgeEcho.GetChild(0));
            _copyPose(m_leftFork, m_purgeEcho.GetChild(1));
            _copyPose(m_rightFork, m_purgeEcho.GetChild(2));
            _copyPose(m_core, m_purgeEcho.GetChild(3));
            m_purgeEcho.gameObject.SetActive(true);
            m_purgeRemaining = PURGE_DURATION;
            _resetLivePose();
        }

        public void ResetPresentation()
        {
            m_wakeRemaining = 0f;
            m_latchRemaining = 0f;
            m_pulseRemaining = 0f;
            m_interruptRemaining = 0f;
            m_hitRemaining = 0f;
            m_purgeRemaining = 0f;
            m_pulseSecondsRemaining = 0f;
            m_pulseInterval = 1f;
            m_latched = false;
            if (m_purgeEcho != null)
            {
                m_purgeEcho.gameObject.SetActive(false);
            }
            _resetLivePose();
        }

        private void Update()
        {
            if (!IsConfigured)
            {
                return;
            }

            var dt = Time.deltaTime;
            _tickPurge(dt);
            if (!m_sapper.gameObject.activeInHierarchy)
            {
                return;
            }

            m_wakeRemaining = Mathf.Max(0f, m_wakeRemaining - dt);
            m_latchRemaining = Mathf.Max(0f, m_latchRemaining - dt);
            m_pulseRemaining = Mathf.Max(0f, m_pulseRemaining - dt);
            m_interruptRemaining = Mathf.Max(0f, m_interruptRemaining - dt);
            m_hitRemaining = Mathf.Max(0f, m_hitRemaining - dt);

            var rootDelta = m_sapper.position - m_previousRootPosition;
            rootDelta.y = 0f;
            m_previousRootPosition = m_sapper.position;
            var movementWeight = dt > 0f ? Mathf.Clamp01(rootDelta.magnitude / (dt * 2.4f)) : 0f;
            var stride = Mathf.Sin(Time.time * 10f) * movementWeight;
            var wakeProgress = m_wakeRemaining > 0f ? 1f - m_wakeRemaining / WAKE_DURATION : 1f;
            var wakeEase = 1f - Mathf.Pow(1f - wakeProgress, 3f);
            var latchProgress = m_latched
                ? m_latchRemaining > 0f ? 1f - m_latchRemaining / LATCH_DURATION : 1f
                : 0f;
            var latchEase = latchProgress * latchProgress * (3f - 2f * latchProgress);
            var buildup = m_latched ? 1f - Mathf.Clamp01(m_pulseSecondsRemaining / m_pulseInterval) : 0f;
            var pulse = m_pulseRemaining > 0f ? Mathf.Sin(m_pulseRemaining / PULSE_DURATION * Mathf.PI) : 0f;
            var interruption = m_interruptRemaining > 0f
                ? Mathf.Sin(m_interruptRemaining / INTERRUPT_DURATION * Mathf.PI)
                : 0f;
            var hit = m_hitRemaining > 0f ? Mathf.Sin(m_hitRemaining / HIT_DURATION * Mathf.PI) : 0f;
            var localHit = Quaternion.Inverse(m_sapper.rotation) * m_hitDirection;

            m_chassis.localPosition = m_chassisRestPosition + new Vector3(
                localHit.x * hit * 0.08f,
                Mathf.Lerp(-0.18f, -0.035f * latchEase, wakeEase) + Mathf.Abs(stride) * 0.02f,
                -movementWeight * 0.045f + localHit.z * hit * 0.08f);
            m_chassis.localRotation = m_chassisRestRotation * Quaternion.Euler(
                movementWeight * 8f - latchEase * 5f,
                0f,
                -stride * 5f - localHit.x * hit * 12f);
            m_chassis.localScale = Vector3.Scale(
                m_chassisRestScale,
                new Vector3(1f, Mathf.Lerp(0.55f, 1f, wakeEase), 1f));

            _poseFork(m_leftFork, m_leftForkRestPosition, m_leftForkRestRotation, m_leftForkRestScale, -1f,
                wakeEase, latchEase, buildup, pulse, interruption, stride);
            _poseFork(m_rightFork, m_rightForkRestPosition, m_rightForkRestRotation, m_rightForkRestScale, 1f,
                wakeEase, latchEase, buildup, pulse, interruption, stride);

            var coreSpin = Time.time * (m_latched ? 260f : 120f);
            m_core.localPosition = m_coreRestPosition + Vector3.up * (latchEase * 0.08f + pulse * 0.05f - interruption * 0.06f);
            m_core.localRotation = m_coreRestRotation * Quaternion.Euler(0f, coreSpin, 0f);
            var coreScale = Mathf.Lerp(0.45f, 1f, wakeEase) *
                            (1f + buildup * 0.14f + pulse * 0.2f - interruption * 0.22f);
            m_core.localScale = m_coreRestScale * Mathf.Max(0.3f, coreScale);
        }

        private void OnDisable()
        {
            ResetPresentation();
        }

        private static void _poseFork(
            Transform fork,
            Vector3 restPosition,
            Quaternion restRotation,
            Vector3 restScale,
            float side,
            float wakeEase,
            float latchEase,
            float buildup,
            float pulse,
            float interruption,
            float stride)
        {
            var deployment = Mathf.Max(0f, latchEase - interruption * 0.35f);
            fork.localPosition = restPosition + new Vector3(
                side * (deployment * 0.12f + pulse * 0.025f),
                Mathf.Lerp(-0.12f, deployment * 0.035f, wakeEase),
                deployment * 0.09f - interruption * 0.05f);
            fork.localRotation = restRotation * Quaternion.Euler(
                -deployment * (18f + buildup * 5f) + interruption * 15f,
                side * deployment * 16f,
                side * (stride * 3f + pulse * 5f));
            fork.localScale = Vector3.Scale(restScale, new Vector3(1f, Mathf.Lerp(0.5f, 1f, wakeEase), 1f));
        }

        private Transform _createPurgeEcho()
        {
            var echo = new GameObject("Signal Sapper Purge Echo").transform;
            echo.SetParent(transform, false);
            _createEchoPart(m_chassis, echo);
            _createEchoPart(m_leftFork, echo);
            _createEchoPart(m_rightFork, echo);
            _createEchoPart(m_core, echo);
            echo.gameObject.SetActive(false);
            return echo;
        }

        private static void _createEchoPart(Transform source, Transform parent)
        {
            var echoPart = new GameObject($"{source.name} Echo");
            echoPart.transform.SetParent(parent, false);
            var sourceFilter = source.GetComponent<MeshFilter>();
            var sourceRenderer = source.GetComponent<MeshRenderer>();
            echoPart.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
            var echoRenderer = echoPart.AddComponent<MeshRenderer>();
            echoRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
            echoRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
            echoRenderer.receiveShadows = sourceRenderer.receiveShadows;
        }

        private static void _copyPose(Transform source, Transform destination)
        {
            destination.localPosition = source.localPosition;
            destination.localRotation = source.localRotation;
            destination.localScale = source.localScale;
        }

        private void _tickPurge(float dt)
        {
            if (m_purgeRemaining <= 0f || m_purgeEcho == null)
            {
                return;
            }

            m_purgeRemaining = Mathf.Max(0f, m_purgeRemaining - dt);
            var progress = 1f - m_purgeRemaining / PURGE_DURATION;
            var collapse = 1f - progress * 0.9f;
            m_purgeEcho.localPosition = m_purgeRestPosition + Vector3.up * (progress * 0.24f);
            m_purgeEcho.localRotation = m_purgeRestRotation * Quaternion.Euler(progress * 70f, progress * 260f, 0f);
            m_purgeEcho.localScale = Vector3.Scale(
                m_purgeRestScale,
                new Vector3(1f + progress * 0.28f, collapse, 1f + progress * 0.28f));
            if (m_purgeRemaining <= 0f)
            {
                m_purgeEcho.gameObject.SetActive(false);
            }
        }

        private void _resetLivePose()
        {
            if (m_chassis == null)
            {
                return;
            }

            m_chassis.localPosition = m_chassisRestPosition;
            m_chassis.localRotation = m_chassisRestRotation;
            m_chassis.localScale = m_chassisRestScale;
            m_leftFork.localPosition = m_leftForkRestPosition;
            m_leftFork.localRotation = m_leftForkRestRotation;
            m_leftFork.localScale = m_leftForkRestScale;
            m_rightFork.localPosition = m_rightForkRestPosition;
            m_rightFork.localRotation = m_rightForkRestRotation;
            m_rightFork.localScale = m_rightForkRestScale;
            m_core.localPosition = m_coreRestPosition;
            m_core.localRotation = m_coreRestRotation;
            m_core.localScale = m_coreRestScale;
        }
    }
}
