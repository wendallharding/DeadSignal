using UnityEngine;

namespace DeadSignal
{
    public sealed class SignalSapperTelegraph : MonoBehaviour
    {
        private const float FLASH_DURATION = 0.32f;
        private const float APPROACH_RADIUS = 1.65f;
        private const float COUNTDOWN_START_RADIUS = 2.25f;
        private const float COUNTDOWN_END_RADIUS = 0.82f;

        private readonly Transform[] m_reticleBrackets = new Transform[4];

        private Transform m_sapper;
        private IComfortSettings m_comfortSettings;
        private Vector3 m_towerPosition;
        private LineRenderer m_tether;
        private GameObject m_pulseFlash;
        private float m_pulseSecondsRemaining;
        private float m_pulseInterval;
        private float m_flashTimer;
        private bool m_isLatched;

        public bool IsVisible { get; private set; }
        public bool IsLatched => m_isLatched;
        public float DisplayedCountdown => m_pulseSecondsRemaining;
        public bool PulseFlashVisible => m_pulseFlash != null && m_pulseFlash.activeSelf;

        internal void Configure(
            Transform sapper,
            Vector3 towerPosition,
            Material brightMaterial,
            Material dimMaterial,
            IComfortSettings comfortSettings)
        {
            m_sapper = sapper;
            m_towerPosition = towerPosition;
            m_comfortSettings = comfortSettings;
            m_comfortSettings.ReducedFlashesChanged += _handleReducedFlashesChanged;

            var tetherObject = new GameObject("Sapper Target Tether");
            tetherObject.transform.SetParent(transform, false);
            m_tether = tetherObject.AddComponent<LineRenderer>();
            m_tether.sharedMaterial = dimMaterial;
            m_tether.positionCount = 2;
            m_tether.startWidth = 0.045f;
            m_tether.endWidth = 0.14f;
            m_tether.startColor = new Color(1f, 0.08f, 0.72f, 0.6f);
            m_tether.endColor = new Color(1f, 0.08f, 0.72f, 1f);
            m_tether.useWorldSpace = true;
            m_tether.numCapVertices = 3;

            for (int index = 0; index < m_reticleBrackets.Length; index++)
            {
                m_reticleBrackets[index] = _createPrimitive(
                    $"Sapper Countdown Bracket {index + 1}",
                    PrimitiveType.Cube,
                    new Vector3(0.78f, 0.055f, 0.16f),
                    brightMaterial).transform;
            }

            m_pulseFlash = _createPrimitive(
                "Sapper Pulse Flash",
                PrimitiveType.Cylinder,
                new Vector3(1f, 0.012f, 1f),
                brightMaterial);
            m_pulseFlash.transform.position = m_towerPosition + Vector3.up * 0.025f;
            m_pulseFlash.SetActive(false);
            SetThreatState(false, false, 0f, 1f);
        }

        public void SetThreatState(bool isActive, bool isLatched, float pulseSecondsRemaining, float pulseInterval)
        {
            IsVisible = isActive;
            m_isLatched = isLatched;
            m_pulseSecondsRemaining = Mathf.Max(0f, pulseSecondsRemaining);
            m_pulseInterval = Mathf.Max(0.01f, pulseInterval);

            if (gameObject.activeSelf != isActive)
            {
                gameObject.SetActive(isActive);
            }
        }

        public void NotifyPulse()
        {
            if (!IsVisible || m_comfortSettings.ReducedFlashesEnabled)
            {
                return;
            }

            m_flashTimer = FLASH_DURATION;
            m_pulseFlash.SetActive(true);
        }

        private void Update()
        {
            if (!IsVisible || m_sapper == null)
            {
                return;
            }

            m_tether.SetPosition(0, m_sapper.position + Vector3.up * 0.55f);
            m_tether.SetPosition(1, m_towerPosition + Vector3.up * 0.24f);

            float rotationOffset = Time.time * (m_isLatched ? 34f : 58f);
            float radius = _reticleRadius();
            for (int index = 0; index < m_reticleBrackets.Length; index++)
            {
                float angle = rotationOffset + index * 90f;
                float radians = angle * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
                Transform bracket = m_reticleBrackets[index];
                bracket.position = m_towerPosition + direction * radius + Vector3.up * 0.055f;
                bracket.rotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);
            }

            _updatePulseFlash();
        }

        private void OnDestroy()
        {
            if (m_comfortSettings != null)
            {
                m_comfortSettings.ReducedFlashesChanged -= _handleReducedFlashesChanged;
            }
        }

        private float _reticleRadius()
        {
            if (!m_isLatched)
            {
                return APPROACH_RADIUS + Mathf.Sin(Time.time * 5f) * 0.12f;
            }

            float remainingRatio = Mathf.Clamp01(m_pulseSecondsRemaining / m_pulseInterval);
            return Mathf.Lerp(COUNTDOWN_END_RADIUS, COUNTDOWN_START_RADIUS, remainingRatio);
        }

        private void _updatePulseFlash()
        {
            if (m_flashTimer <= 0f)
            {
                m_pulseFlash.SetActive(false);
                return;
            }

            m_flashTimer = Mathf.Max(0f, m_flashTimer - Time.deltaTime);
            float progress = 1f - m_flashTimer / FLASH_DURATION;
            float diameter = Mathf.Lerp(0.9f, 4.8f, progress);
            m_pulseFlash.transform.localScale = new Vector3(diameter, 0.012f, diameter);
            if (m_flashTimer <= 0f)
            {
                m_pulseFlash.SetActive(false);
            }
        }

        private GameObject _createPrimitive(string objectName, PrimitiveType type, Vector3 scale, Material material)
        {
            var visual = GameObject.CreatePrimitive(type);
            visual.name = objectName;
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = scale;
            visual.GetComponent<Renderer>().sharedMaterial = material;

            var primitiveCollider = visual.GetComponent<Collider>();
            if (primitiveCollider != null)
            {
                Destroy(primitiveCollider);
            }

            return visual;
        }

        private void _handleReducedFlashesChanged(bool enabled)
        {
            if (!enabled || m_pulseFlash == null)
            {
                return;
            }

            m_flashTimer = 0f;
            m_pulseFlash.SetActive(false);
        }
    }
}
