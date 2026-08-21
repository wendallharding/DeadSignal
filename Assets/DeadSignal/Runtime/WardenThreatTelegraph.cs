using UnityEngine;

namespace DeadSignal
{
    public sealed class WardenThreatTelegraph : MonoBehaviour
    {
        private const string TEXTURE_RESOURCE = "VFX/WardenStrikeWarning";
        private const string TUNING_RESOURCE = "Tuning/WardenThreatTelegraphTuning";

        private Transform m_warden;
        private Transform m_player;
        private IComfortSettings m_comfortSettings;
        private WardenThreatTelegraphTuning m_tuning;
        private Texture2D m_texture;
        private Sprite m_sprite;
        private SpriteRenderer m_renderer;
        private bool m_ownsFallbackTuning;

        public bool HasTexture => m_texture != null;
        public bool IsWarningVisible => m_renderer != null && m_renderer.enabled;
        public bool IsMotionSuppressed => IsWarningVisible && m_comfortSettings.ReducedFlashesEnabled;

        internal void Configure(Transform warden, Transform player, IComfortSettings comfortSettings)
        {
            m_warden = warden;
            m_player = player;
            m_comfortSettings = comfortSettings;
            m_tuning = Resources.Load<WardenThreatTelegraphTuning>(TUNING_RESOURCE);
            if (m_tuning == null)
            {
                m_tuning = ScriptableObject.CreateInstance<WardenThreatTelegraphTuning>();
                m_ownsFallbackTuning = true;
                Debug.LogWarning($"Warden telegraph tuning was not found at Resources/{TUNING_RESOURCE}.", this);
            }

            m_texture = Resources.Load<Texture2D>(TEXTURE_RESOURCE);
            if (m_texture == null)
            {
                Debug.LogWarning($"Warden strike warning was not found at Resources/{TEXTURE_RESOURCE}.", this);
                return;
            }

            m_sprite = Sprite.Create(
                m_texture,
                new Rect(0f, 0f, m_texture.width, m_texture.height),
                new Vector2(0.5f, 0.5f),
                m_texture.width);
            m_sprite.name = "Warden Strike Warning Sprite";
            m_renderer = gameObject.AddComponent<SpriteRenderer>();
            m_renderer.sprite = m_sprite;
            m_renderer.sortingOrder = 22;
            m_renderer.enabled = false;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void Update()
        {
            if (m_renderer == null || m_warden == null || m_player == null || !m_warden.gameObject.activeInHierarchy)
            {
                _setVisible(false);
                return;
            }

            float distance = DeadSignalWorld.FlatDistance(m_warden.position, m_player.position);
            if (distance > m_tuning.WarningDistance)
            {
                _setVisible(false);
                return;
            }

            _setVisible(true);
            float proximity = 1f - Mathf.Clamp01(distance / m_tuning.WarningDistance);
            bool reducedFlashes = m_comfortSettings.ReducedFlashesEnabled;
            float pulse = reducedFlashes ? 0f : Mathf.Sin(Time.time * m_tuning.PulseSpeed) * m_tuning.PulseScale;
            float diameter = m_tuning.RingDiameter * (1f + pulse);
            transform.position = m_warden.position + Vector3.up * 0.065f;
            transform.localScale = Vector3.one * diameter;
            if (!reducedFlashes)
            {
                transform.rotation = Quaternion.Euler(90f, Time.time * m_tuning.RotationSpeed, 0f);
            }

            float alpha = Mathf.Lerp(m_tuning.MinimumAlpha, m_tuning.MaximumAlpha, proximity);
            m_renderer.color = new Color(1f, 1f, 1f, alpha);
        }

        private void OnDestroy()
        {
            if (m_sprite != null)
            {
                Destroy(m_sprite);
            }

            if (m_ownsFallbackTuning && m_tuning != null)
            {
                Destroy(m_tuning);
            }
        }

        private void _setVisible(bool visible)
        {
            if (m_renderer != null)
            {
                m_renderer.enabled = visible;
            }
        }
    }
}
