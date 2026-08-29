using Reflex.Attributes;
using UnityEngine;
using DeadSignal.Player;

namespace DeadSignal.Presentation
{
    public enum StationStateFeedbackKind
    {
        Tower,
        Installation,
        Passage,
        Machinery
    }

    internal interface IStationStateFeedback
    {
        bool HasTexture { get; }
        int PoolSize { get; }

        void Play(Vector3 position, StationStateFeedbackKind kind);
        void SetPaused(bool paused);
    }

    /// <summary>
    /// Presents resolved station mutations without owning progression, doors, rewards, or timing.
    /// </summary>
    public sealed class StationStateFeedbackController : MonoBehaviour, IStationStateFeedback
    {
        public bool HasTexture => m_texture != null;
        public int PoolSize => m_slots?.Length ?? 0;
        public int ActiveCount { get; private set; }
        public int PlayCount { get; private set; }
        public StationStateFeedbackKind LastKind { get; private set; }
        public Vector3 LastPosition { get; private set; }
        public float CurrentAlpha { get; private set; }
        public Color CurrentColor { get; private set; }

        [Inject]
        private void _construct(IComfortSettings comfortSettings)
        {
            m_comfortSettings = comfortSettings;
        }

        private void Awake()
        {
            m_tuning = Resources.Load<StationStateFeedbackTuning>(TUNING_PATH);
            m_texture = Resources.Load<Texture2D>(GLYPH_TEXTURE_PATH);
            if (m_tuning == null || m_texture == null)
            {
                Debug.LogWarning("Station state feedback tuning or glyph was not found in Resources.", this);
                return;
            }

            m_sprite = Sprite.Create(
                m_texture,
                new Rect(0f, 0f, m_texture.width, m_texture.height),
                new Vector2(0.5f, 0.5f),
                m_texture.width);
            m_sprite.name = "Station State Transition Glyph Sprite";
            m_slots = new FeedbackSlot[m_tuning.PoolSize];
            for (var index = 0; index < m_slots.Length; index++)
            {
                var slotRoot = new GameObject($"{SLOT_NAME} {index + 1}");
                slotRoot.transform.SetParent(transform, false);
                slotRoot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var renderer = slotRoot.AddComponent<SpriteRenderer>();
                renderer.sprite = m_sprite;
                renderer.sortingOrder = 19;
                slotRoot.SetActive(false);
                m_slots[index] = new FeedbackSlot(slotRoot, renderer);
            }
        }

        public void Play(Vector3 position, StationStateFeedbackKind kind)
        {
            if (m_slots == null || m_slots.Length == 0 || m_paused)
            {
                return;
            }

            var slot = m_slots[m_nextSlot];
            m_nextSlot = (m_nextSlot + 1) % m_slots.Length;
            slot.Elapsed = 0f;
            slot.Diameter = _diameterFor(kind);
            slot.Root.transform.position = position + Vector3.up * WORLD_HEIGHT;
            slot.Root.SetActive(true);
            LastKind = kind;
            LastPosition = position;
            PlayCount++;
            _apply(slot, 0f);
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
                var progress = Mathf.Clamp01(slot.Elapsed / m_tuning.Duration);
                _apply(slot, progress);
                if (progress >= 1f)
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
            if (m_sprite != null)
            {
                Destroy(m_sprite);
            }
        }

        private void _apply(FeedbackSlot slot, float progress)
        {
            var eased = 1f - Mathf.Pow(1f - progress, 3f);
            var diameter = slot.Diameter * Mathf.Lerp(
                m_tuning.StartingDiameterMultiplier,
                m_tuning.EndingDiameterMultiplier,
                eased);
            slot.Root.transform.localScale = Vector3.one * diameter;

            var maximumAlpha = m_comfortSettings.ReducedFlashesEnabled
                ? m_tuning.ReducedFlashesMaximumAlpha
                : m_tuning.MaximumAlpha;
            var fadeIn = Mathf.Clamp01(progress / 0.18f);
            var fadeOut = 1f - Mathf.Clamp01((progress - 0.42f) / 0.58f);
            var color = Color.Lerp(m_tuning.AvailableColor, m_tuning.CompleteColor, eased);
            color.a = fadeIn * fadeOut * maximumAlpha;
            slot.Renderer.color = color;
            CurrentAlpha = color.a;
            CurrentColor = color;
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
            for (var index = 0; index < m_slots.Length; index++)
            {
                if (m_slots[index].Root.activeSelf)
                {
                    ActiveCount++;
                }
            }
        }

        private static float _diameterFor(StationStateFeedbackKind kind)
        {
            return kind switch
            {
                StationStateFeedbackKind.Tower => 3.4f,
                StationStateFeedbackKind.Installation => 2.2f,
                StationStateFeedbackKind.Passage => 2.7f,
                _ => 1.8f
            };
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
            public float Diameter { get; set; }
        }

        private const string GLYPH_TEXTURE_PATH = "VFX/MachineryStateTransitionGlyph";
        private const string TUNING_PATH = "Tuning/StationStateFeedbackTuning";
        private const string SLOT_NAME = "Station State Transition";
        private const float WORLD_HEIGHT = 0.2f;

        private IComfortSettings m_comfortSettings;
        private StationStateFeedbackTuning m_tuning;
        private Texture2D m_texture;
        private Sprite m_sprite;
        private FeedbackSlot[] m_slots;
        private int m_nextSlot;
        private bool m_paused;
    }
}
