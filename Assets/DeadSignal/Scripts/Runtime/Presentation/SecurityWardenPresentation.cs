using UnityEngine;

namespace DeadSignal.Presentation
{
    /// <summary>Owns presentation-only motion for the authored Security Warden without moving its combat root.</summary>
    public sealed class SecurityWardenPresentation : MonoBehaviour
    {
        private const float HIT_DURATION = 0.22f;
        private const float PURGE_DURATION = 0.42f;
        private const float STRIKE_DURATION = 0.44f;
        private const float WAKE_DURATION = 0.48f;

        private Transform m_warden;
        private Transform m_player;
        private Transform m_chassis;
        private Transform m_eye;
        private Transform m_crown;
        private Transform m_purgeEcho;
        private Vector3 m_chassisRestPosition;
        private Quaternion m_chassisRestRotation;
        private Vector3 m_chassisRestScale;
        private Vector3 m_eyeRestPosition;
        private Quaternion m_eyeRestRotation;
        private Vector3 m_eyeRestScale;
        private Vector3 m_crownRestPosition;
        private Quaternion m_crownRestRotation;
        private Vector3 m_crownRestScale;
        private Vector3 m_previousRootPosition;
        private Vector3 m_hitDirection;
        private Vector3 m_purgeRestPosition;
        private Quaternion m_purgeRestRotation;
        private Vector3 m_purgeRestScale;
        private float m_wakeRemaining;
        private float m_strikeRemaining;
        private float m_hitRemaining;
        private float m_purgeRemaining;
        private float m_attackDistance;
        private bool m_screening;

        public bool IsConfigured => m_warden != null && m_chassis != null && m_eye != null && m_crown != null &&
                                    m_purgeEcho != null;
        public bool IsWaking => m_wakeRemaining > 0f;
        public bool IsStriking => m_strikeRemaining > 0f;
        public bool IsHitReacting => m_hitRemaining > 0f;
        public bool IsPurgeVisible => m_purgeRemaining > 0f && m_purgeEcho != null && m_purgeEcho.gameObject.activeSelf;
        public bool IsScreening => m_screening;

        public void Configure(Transform warden, Transform player, Transform chassis, Transform eye, Transform crown)
        {
            m_warden = warden;
            m_player = player;
            m_chassis = chassis;
            m_eye = eye;
            m_crown = crown;
            m_chassisRestPosition = chassis.localPosition;
            m_chassisRestRotation = chassis.localRotation;
            m_chassisRestScale = chassis.localScale;
            m_eyeRestPosition = eye.localPosition;
            m_eyeRestRotation = eye.localRotation;
            m_eyeRestScale = eye.localScale;
            m_crownRestPosition = crown.localPosition;
            m_crownRestRotation = crown.localRotation;
            m_crownRestScale = crown.localScale;
            m_purgeEcho = _createPurgeEcho();
            ResetPresentation();
        }

        public void SetThreatState(bool screening, float attackDistance)
        {
            m_screening = screening;
            m_attackDistance = Mathf.Max(0f, attackDistance);
        }

        public void PlayWake()
        {
            if (!IsConfigured)
            {
                return;
            }

            ResetPresentation();
            m_previousRootPosition = m_warden.position;
            m_wakeRemaining = WAKE_DURATION;
        }

        public void PlayStrike()
        {
            if (IsConfigured)
            {
                m_strikeRemaining = STRIKE_DURATION;
            }
        }

        public void PlayHit(Vector3 sourcePosition)
        {
            if (!IsConfigured)
            {
                return;
            }

            m_hitDirection = m_warden.position - sourcePosition;
            m_hitDirection.y = 0f;
            if (m_hitDirection.sqrMagnitude < 0.01f)
            {
                m_hitDirection = -m_warden.forward;
            }
            m_hitDirection.Normalize();
            m_hitRemaining = HIT_DURATION;
        }

        public void PlayPurge()
        {
            if (!IsConfigured)
            {
                return;
            }

            m_purgeEcho.SetPositionAndRotation(m_warden.position, m_warden.rotation);
            m_purgeEcho.localScale = m_warden.lossyScale;
            m_purgeRestPosition = m_purgeEcho.localPosition;
            m_purgeRestRotation = m_purgeEcho.localRotation;
            m_purgeRestScale = m_purgeEcho.localScale;
            _copyPose(m_chassis, m_purgeEcho.GetChild(0));
            _copyPose(m_eye, m_purgeEcho.GetChild(1));
            _copyPose(m_crown, m_purgeEcho.GetChild(2));
            m_purgeEcho.gameObject.SetActive(true);
            m_purgeRemaining = PURGE_DURATION;
            _resetLivePose();
        }

        public void ResetPresentation()
        {
            m_wakeRemaining = 0f;
            m_strikeRemaining = 0f;
            m_hitRemaining = 0f;
            m_purgeRemaining = 0f;
            m_screening = false;
            m_attackDistance = 0f;
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
            if (!m_warden.gameObject.activeInHierarchy)
            {
                return;
            }

            m_wakeRemaining = Mathf.Max(0f, m_wakeRemaining - dt);
            m_strikeRemaining = Mathf.Max(0f, m_strikeRemaining - dt);
            m_hitRemaining = Mathf.Max(0f, m_hitRemaining - dt);

            var rootDelta = m_warden.position - m_previousRootPosition;
            rootDelta.y = 0f;
            m_previousRootPosition = m_warden.position;
            var movementWeight = dt > 0f ? Mathf.Clamp01(rootDelta.magnitude / (dt * 2.2f)) : 0f;
            var stride = Mathf.Sin(Time.time * 9f) * movementWeight;

            var wakeProgress = m_wakeRemaining > 0f ? 1f - m_wakeRemaining / WAKE_DURATION : 1f;
            var wakeEase = 1f - Mathf.Pow(1f - wakeProgress, 3f);
            var distanceToPlayer = m_player != null
                ? Vector3.Distance(m_warden.position, m_player.position)
                : float.PositiveInfinity;
            var anticipation = m_attackDistance > 0f
                ? 1f - Mathf.Clamp01((distanceToPlayer - m_attackDistance) / Mathf.Max(0.01f, m_attackDistance * 0.9f))
                : 0f;
            var strikeProgress = m_strikeRemaining > 0f ? 1f - m_strikeRemaining / STRIKE_DURATION : -1f;
            var commit = strikeProgress >= 0f && strikeProgress < 0.28f
                ? Mathf.Sin(strikeProgress / 0.28f * Mathf.PI * 0.5f)
                : 0f;
            var recovery = strikeProgress >= 0.28f
                ? Mathf.Sin(Mathf.Clamp01((strikeProgress - 0.28f) / 0.72f) * Mathf.PI)
                : 0f;
            var hitPulse = m_hitRemaining > 0f ? Mathf.Sin(m_hitRemaining / HIT_DURATION * Mathf.PI) : 0f;
            var localHit = Quaternion.Inverse(m_warden.rotation) * m_hitDirection;

            m_chassis.localPosition = m_chassisRestPosition + new Vector3(
                localHit.x * hitPulse * 0.08f,
                Mathf.Lerp(-0.2f, 0f, wakeEase) + Mathf.Abs(stride) * 0.025f,
                -commit * 0.16f + recovery * 0.05f + localHit.z * hitPulse * 0.08f);
            m_chassis.localRotation = m_chassisRestRotation * Quaternion.Euler(
                anticipation * 7f - commit * 18f + recovery * 8f,
                0f,
                -stride * 4f - localHit.x * hitPulse * 11f);
            m_chassis.localScale = Vector3.Scale(m_chassisRestScale, new Vector3(1f, Mathf.Lerp(0.55f, 1f, wakeEase), 1f));

            m_eye.localPosition = m_eyeRestPosition + Vector3.forward * (anticipation * 0.03f + commit * 0.08f);
            m_eye.localRotation = m_eyeRestRotation;
            m_eye.localScale = m_eyeRestScale * Mathf.Lerp(0.45f, 1f + anticipation * 0.12f, wakeEase);

            var screenWidth = m_screening ? 1.13f : 1f;
            m_crown.localPosition = m_crownRestPosition + Vector3.up * (anticipation * 0.035f + hitPulse * 0.025f);
            m_crown.localRotation = m_crownRestRotation * Quaternion.Euler(-anticipation * 9f, 0f, stride * 3f);
            m_crown.localScale = Vector3.Scale(m_crownRestScale, new Vector3(screenWidth, 1f, screenWidth));
        }

        private void OnDisable()
        {
            ResetPresentation();
        }

        private Transform _createPurgeEcho()
        {
            var echo = new GameObject("Security Warden Purge Echo").transform;
            echo.SetParent(transform, false);
            _createEchoPart(m_chassis, echo);
            _createEchoPart(m_eye, echo);
            _createEchoPart(m_crown, echo);
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
            var squash = 1f - progress * 0.88f;
            m_purgeEcho.localPosition = m_purgeRestPosition + Vector3.up * (progress * 0.32f);
            m_purgeEcho.localRotation = m_purgeRestRotation * Quaternion.Euler(0f, progress * 220f, progress * 90f);
            m_purgeEcho.localScale = Vector3.Scale(
                m_purgeRestScale,
                new Vector3(1f + progress * 0.22f, squash, 1f + progress * 0.22f));
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
            m_eye.localPosition = m_eyeRestPosition;
            m_eye.localRotation = m_eyeRestRotation;
            m_eye.localScale = m_eyeRestScale;
            m_crown.localPosition = m_crownRestPosition;
            m_crown.localRotation = m_crownRestRotation;
            m_crown.localScale = m_crownRestScale;
        }
    }
}
