using UnityEngine;

namespace DeadSignal
{
    public sealed class SignalSapperTelegraph : MonoBehaviour
    {
        private const string PULSE_TEXTURE_RESOURCE = "VFX/SapperDrainGlyph";
        private const string TUNING_RESOURCE = "Tuning/SignalSapperTelegraphTuning";

        private readonly Transform[] m_reticleBrackets = new Transform[4];

        private Transform m_sapper;
        private IComfortSettings m_comfortSettings;
        private SignalSapperTelegraphTuning m_tuning;
        private Vector3 m_towerPosition;
        private LineRenderer m_tether;
        private GameObject m_pulseFlashRoot;
        private Texture2D m_pulseTexture;
        private Sprite m_pulseSprite;
        private SpriteRenderer m_pulseRenderer;
        private float m_pulseSecondsRemaining;
        private float m_pulseInterval;
        private float m_flashTimer;
        private bool m_isLatched;
        private bool m_ownsFallbackTuning;

        public bool IsVisible { get; private set; }
        public bool IsLatched => m_isLatched;
        public float DisplayedCountdown => m_pulseSecondsRemaining;
        public bool HasPulseTexture => m_pulseTexture != null;
        public bool PulseFlashVisible => m_pulseFlashRoot != null && m_pulseFlashRoot.activeSelf;

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
            m_tuning = Resources.Load<SignalSapperTelegraphTuning>(TUNING_RESOURCE);
            if (m_tuning == null)
            {
                m_tuning = ScriptableObject.CreateInstance<SignalSapperTelegraphTuning>();
                m_ownsFallbackTuning = true;
                Debug.LogWarning($"Sapper telegraph tuning was not found at Resources/{TUNING_RESOURCE}.", this);
            }

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

            _createPulseFlash(brightMaterial);
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

            m_flashTimer = m_tuning.FlashDuration;
            m_pulseFlashRoot.SetActive(true);
        }

        private void Update()
        {
            if (!IsVisible || m_sapper == null)
            {
                return;
            }

            m_tether.SetPosition(0, m_sapper.position + Vector3.up * 0.55f);
            m_tether.SetPosition(1, m_towerPosition + Vector3.up * 0.24f);

            float rotationSpeed = m_isLatched ? m_tuning.LatchedRotationSpeed : m_tuning.ApproachRotationSpeed;
            float rotationOffset = Time.time * rotationSpeed;
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

            if (m_pulseSprite != null)
            {
                Destroy(m_pulseSprite);
            }

            if (m_ownsFallbackTuning && m_tuning != null)
            {
                Destroy(m_tuning);
            }
        }

        private float _reticleRadius()
        {
            if (!m_isLatched)
            {
                return m_tuning.ApproachRadius + Mathf.Sin(Time.time * 5f) * 0.12f;
            }

            float remainingRatio = Mathf.Clamp01(m_pulseSecondsRemaining / m_pulseInterval);
            return Mathf.Lerp(m_tuning.CountdownEndRadius, m_tuning.CountdownStartRadius, remainingRatio);
        }

        private void _updatePulseFlash()
        {
            if (m_flashTimer <= 0f)
            {
                m_pulseFlashRoot.SetActive(false);
                return;
            }

            m_flashTimer = Mathf.Max(0f, m_flashTimer - Time.deltaTime);
            float progress = 1f - m_flashTimer / m_tuning.FlashDuration;
            float easedProgress = 1f - Mathf.Pow(1f - progress, 2f);
            float diameter = Mathf.Lerp(m_tuning.FlashStartingDiameter, m_tuning.FlashEndingDiameter, easedProgress);
            m_pulseFlashRoot.transform.localScale = Vector3.one * diameter;
            if (m_pulseRenderer != null)
            {
                float alpha = (1f - progress) * m_tuning.FlashMaximumAlpha;
                m_pulseRenderer.color = new Color(1f, 1f, 1f, alpha);
                m_pulseFlashRoot.transform.rotation = Quaternion.Euler(90f, progress * 110f, 0f);
            }

            if (m_flashTimer <= 0f)
            {
                m_pulseFlashRoot.SetActive(false);
            }
        }

        private void _createPulseFlash(Material fallbackMaterial)
        {
            m_pulseTexture = Resources.Load<Texture2D>(PULSE_TEXTURE_RESOURCE);
            if (m_pulseTexture == null)
            {
                Debug.LogWarning($"Sapper drain glyph was not found at Resources/{PULSE_TEXTURE_RESOURCE}.", this);
                m_pulseFlashRoot = _createPrimitive(
                    "Sapper Pulse Flash",
                    PrimitiveType.Cylinder,
                    new Vector3(1f, 0.012f, 1f),
                    fallbackMaterial);
                m_pulseFlashRoot.transform.position = m_towerPosition + Vector3.up * 0.025f;
                m_pulseFlashRoot.SetActive(false);
                return;
            }

            m_pulseSprite = Sprite.Create(
                m_pulseTexture,
                new Rect(0f, 0f, m_pulseTexture.width, m_pulseTexture.height),
                new Vector2(0.5f, 0.5f),
                m_pulseTexture.width);
            m_pulseSprite.name = "Sapper Drain Glyph Sprite";
            m_pulseFlashRoot = new GameObject("Sapper Pulse Flash");
            m_pulseFlashRoot.transform.SetParent(transform, false);
            m_pulseFlashRoot.transform.position = m_towerPosition + Vector3.up * 0.075f;
            m_pulseFlashRoot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            m_pulseRenderer = m_pulseFlashRoot.AddComponent<SpriteRenderer>();
            m_pulseRenderer.sprite = m_pulseSprite;
            m_pulseRenderer.sortingOrder = 24;
            m_pulseFlashRoot.SetActive(false);
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
            if (!enabled || m_pulseFlashRoot == null)
            {
                return;
            }

            m_flashTimer = 0f;
            m_pulseFlashRoot.SetActive(false);
        }
    }
}
