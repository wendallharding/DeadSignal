using DeadSignal.Missions;
using DeadSignal.Player;
using Reflex.Attributes;
using UnityEngine;

namespace DeadSignal.Presentation
{
    internal interface IWeaponTransformationFeedback
    {
        bool HasTextures { get; }
        int PoolSize { get; }

        void Play(Vector3 position, SignalWeaponOverclock weapon, bool evolved);
        void SetPaused(bool paused);
    }

    /// <summary>
    /// Presents weapon selection and evolution without owning weapon rules, projectile behavior, or choice state.
    /// </summary>
    public sealed class WeaponTransformationFeedbackController : MonoBehaviour, IWeaponTransformationFeedback
    {
        public bool HasTextures => m_piercingTexture != null && m_ricochetTexture != null;
        public int PoolSize => m_slots?.Length ?? 0;
        public int ActiveCount { get; private set; }
        public int PlayCount { get; private set; }
        public SignalWeaponOverclock LastWeapon { get; private set; }
        public bool LastWasEvolution { get; private set; }
        public float CurrentAlpha { get; private set; }
        public Texture2D LastTexture { get; private set; }

        [Inject]
        private void _construct(IComfortSettings comfortSettings)
        {
            m_comfortSettings = comfortSettings;
        }

        private void Awake()
        {
            m_tuning = Resources.Load<WeaponTransformationFeedbackTuning>(TUNING_PATH);
            m_piercingTexture = Resources.Load<Texture2D>(PIERCING_TEXTURE_PATH);
            m_ricochetTexture = Resources.Load<Texture2D>(RICOCHET_TEXTURE_PATH);
            if (m_tuning == null || !HasTextures)
            {
                Debug.LogWarning("Weapon transformation feedback tuning or glyphs were not found in Resources.", this);
                return;
            }

            m_piercingSprite = _createSprite(m_piercingTexture, "Piercing Pulse Transformation Glyph Sprite");
            m_ricochetSprite = _createSprite(m_ricochetTexture, "Controlled Ricochet Transformation Glyph Sprite");
            m_slots = new FeedbackSlot[m_tuning.PoolSize];
            for (var index = 0; index < m_slots.Length; index++)
            {
                var slotRoot = new GameObject($"{SLOT_NAME} {index + 1}");
                slotRoot.transform.SetParent(transform, false);
                slotRoot.transform.rotation = Quaternion.Euler(90f, index * 45f, 0f);
                var renderer = slotRoot.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 21 + index;
                slotRoot.SetActive(false);
                m_slots[index] = new FeedbackSlot(slotRoot, renderer);
            }
        }

        public void Play(Vector3 position, SignalWeaponOverclock weapon, bool evolved)
        {
            if (weapon == SignalWeaponOverclock.None || m_slots == null || m_slots.Length == 0 || m_paused)
            {
                return;
            }

            var sprite = weapon == SignalWeaponOverclock.PiercingPulse ? m_piercingSprite : m_ricochetSprite;
            _activateSlot(m_slots[0], position, sprite, evolved, 0f);
            if (evolved && m_slots.Length > 1)
            {
                _activateSlot(m_slots[1], position, sprite, true, 0.16f);
            }

            LastWeapon = weapon;
            LastWasEvolution = evolved;
            LastTexture = weapon == SignalWeaponOverclock.PiercingPulse ? m_piercingTexture : m_ricochetTexture;
            PlayCount++;
            _updateActiveCount();
        }

        public void SetPaused(bool paused)
        {
            m_paused = paused;
            if (paused)
            {
                _clear();
            }
        }

        private void Update()
        {
            if (m_paused || m_slots == null)
            {
                return;
            }

            for (var index = 0; index < m_slots.Length; index++)
            {
                var slot = m_slots[index];
                if (!slot.Root.activeSelf)
                {
                    continue;
                }

                slot.Elapsed += Time.deltaTime;
                _apply(slot);
                if (slot.Elapsed >= m_tuning.Duration + slot.Delay)
                {
                    slot.Root.SetActive(false);
                }
            }

            _updateActiveCount();
        }

        private void OnDisable()
        {
            _clear();
        }

        private void OnDestroy()
        {
            if (m_piercingSprite != null)
            {
                Destroy(m_piercingSprite);
            }
            if (m_ricochetSprite != null)
            {
                Destroy(m_ricochetSprite);
            }
        }

        private void _activateSlot(FeedbackSlot slot, Vector3 position, Sprite sprite, bool evolved, float delay)
        {
            slot.Elapsed = 0f;
            slot.Delay = delay;
            slot.Diameter = evolved ? m_tuning.EvolutionDiameter : m_tuning.TransformationDiameter;
            slot.Renderer.sprite = sprite;
            slot.Root.transform.position = position + Vector3.up * WORLD_HEIGHT;
            slot.Root.SetActive(true);
            _apply(slot);
        }

        private void _apply(FeedbackSlot slot)
        {
            if (slot.Elapsed < slot.Delay)
            {
                slot.Renderer.color = Color.clear;
                return;
            }

            var progress = Mathf.Clamp01((slot.Elapsed - slot.Delay) / m_tuning.Duration);
            var eased = 1f - Mathf.Pow(1f - progress, 3f);
            var diameter = slot.Diameter * Mathf.Lerp(
                m_tuning.StartingDiameterMultiplier,
                m_tuning.EndingDiameterMultiplier,
                eased);
            slot.Root.transform.localScale = Vector3.one * diameter;

            var maximumAlpha = m_comfortSettings.ReducedFlashesEnabled
                ? m_tuning.ReducedFlashesMaximumAlpha
                : m_tuning.MaximumAlpha;
            var fadeIn = Mathf.Clamp01(progress / 0.16f);
            var fadeOut = 1f - Mathf.Clamp01((progress - 0.48f) / 0.52f);
            var color = Color.white;
            color.a = fadeIn * fadeOut * maximumAlpha;
            slot.Renderer.color = color;
            CurrentAlpha = Mathf.Max(CurrentAlpha, color.a);
        }

        private void _clear()
        {
            if (m_slots != null)
            {
                for (var index = 0; index < m_slots.Length; index++)
                {
                    m_slots[index].Root.SetActive(false);
                }
            }

            ActiveCount = 0;
            CurrentAlpha = 0f;
        }

        private void _updateActiveCount()
        {
            ActiveCount = 0;
            CurrentAlpha = 0f;
            for (var index = 0; index < m_slots.Length; index++)
            {
                if (!m_slots[index].Root.activeSelf)
                {
                    continue;
                }

                ActiveCount++;
                CurrentAlpha = Mathf.Max(CurrentAlpha, m_slots[index].Renderer.color.a);
            }
        }

        private static Sprite _createSprite(Texture2D texture, string name)
        {
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            sprite.name = name;
            return sprite;
        }

        private sealed class FeedbackSlot
        {
            public FeedbackSlot(GameObject root, SpriteRenderer renderer)
            {
                Root = root;
                Renderer = renderer;
            }

            public GameObject Root { get; }
            public SpriteRenderer Renderer { get; }
            public float Elapsed { get; set; }
            public float Delay { get; set; }
            public float Diameter { get; set; }
        }

        private const string PIERCING_TEXTURE_PATH = "VFX/PiercingPulseTransformationGlyph";
        private const string RICOCHET_TEXTURE_PATH = "VFX/ControlledRicochetTransformationGlyph";
        private const string TUNING_PATH = "Tuning/WeaponTransformationFeedbackTuning";
        private const string SLOT_NAME = "Weapon Transformation";
        private const float WORLD_HEIGHT = 0.24f;

        private IComfortSettings m_comfortSettings;
        private WeaponTransformationFeedbackTuning m_tuning;
        private Texture2D m_piercingTexture;
        private Texture2D m_ricochetTexture;
        private Sprite m_piercingSprite;
        private Sprite m_ricochetSprite;
        private FeedbackSlot[] m_slots;
        private bool m_paused;
    }
}
