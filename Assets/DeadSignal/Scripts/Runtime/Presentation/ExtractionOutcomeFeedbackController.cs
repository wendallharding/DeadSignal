using DeadSignal.Missions;
using DeadSignal.Player;
using Reflex.Attributes;
using UnityEngine;

namespace DeadSignal.Presentation
{
    public enum ExtractionOutcomeFeedbackKind
    {
        ExtractionStart,
        ExtractionComplete,
        Victory,
        Defeat
    }

    internal interface IExtractionOutcomeFeedback
    {
        bool HasTexture { get; }
        int EventPoolSize { get; }

        void BeginExtraction(Vector3 position, ExtractionUplinkMode mode, float duration);
        void UpdateExtraction(float secondsRemaining);
        void CompleteExtraction(Vector3 position);
        void PlayOutcome(RunOutcome outcome, Vector3 position);
        void SetPaused(bool paused);
    }

    /// <summary>
    /// Presents extraction and terminal outcomes without owning countdown, progression, Signal, or pause state.
    /// </summary>
    public sealed class ExtractionOutcomeFeedbackController : MonoBehaviour, IExtractionOutcomeFeedback
    {
        public bool HasTexture => m_texture != null;
        public int EventPoolSize => m_eventSlots?.Length ?? 0;
        public int OwnedObjectCount => (m_progressRoot == null ? 0 : 1) + EventPoolSize;
        public bool IsProgressActive => m_progressRoot != null && m_progressRoot.activeSelf;
        public int ActiveEventCount { get; private set; }
        public int PlayCount { get; private set; }
        public float ProgressNormalized { get; private set; }
        public float CurrentMaximumAlpha { get; private set; }
        public ExtractionOutcomeFeedbackKind LastKind { get; private set; }
        public RunOutcome LastOutcome { get; private set; }
        public Vector3 LastPosition { get; private set; }

        [Inject]
        private void _construct(IComfortSettings comfortSettings)
        {
            m_comfortSettings = comfortSettings;
        }

        private void Awake()
        {
            m_tuning = Resources.Load<ExtractionOutcomeFeedbackTuning>(TUNING_PATH);
            m_texture = Resources.Load<Texture2D>(GLYPH_TEXTURE_PATH);
            if (m_tuning == null || m_texture == null)
            {
                Debug.LogWarning("Extraction outcome feedback tuning or glyph was not found in Resources.", this);
                return;
            }

            m_sprite = Sprite.Create(
                m_texture,
                new Rect(0f, 0f, m_texture.width, m_texture.height),
                new Vector2(0.5f, 0.5f),
                m_texture.width);
            m_sprite.name = "Extraction Outcome Glyph Sprite";
            m_progressRoot = _createRendererOwner(PROGRESS_NAME, 17, out m_progressRenderer);
            m_eventSlots = new EventSlot[m_tuning.EventPoolSize];
            for (var index = 0; index < m_eventSlots.Length; index++)
            {
                var root = _createRendererOwner($"{EVENT_NAME} {index + 1}", 22 + index, out var renderer);
                m_eventSlots[index] = new EventSlot(root, renderer);
            }
        }

        public void BeginExtraction(Vector3 position, ExtractionUplinkMode mode, float duration)
        {
            if (mode == ExtractionUplinkMode.None || m_progressRoot == null || m_paused)
            {
                return;
            }

            m_extractionActive = true;
            m_extractionMode = mode;
            m_extractionDuration = Mathf.Max(0.1f, duration);
            ProgressNormalized = 0f;
            m_progressRoot.transform.position = position + Vector3.up * WORLD_HEIGHT;
            m_progressRoot.SetActive(true);
            LastKind = ExtractionOutcomeFeedbackKind.ExtractionStart;
            LastPosition = position;
            PlayCount++;
            _applyProgress();
        }

        public void UpdateExtraction(float secondsRemaining)
        {
            if (!m_extractionActive || m_progressRoot == null || m_paused)
            {
                return;
            }

            ProgressNormalized = 1f - Mathf.Clamp01(secondsRemaining / m_extractionDuration);
            if (!m_progressRoot.activeSelf)
            {
                m_progressRoot.SetActive(true);
            }
            _applyProgress();
        }

        public void CompleteExtraction(Vector3 position)
        {
            m_extractionActive = false;
            if (m_progressRoot != null)
            {
                m_progressRoot.SetActive(false);
            }
            _playEvent(position, ExtractionOutcomeFeedbackKind.ExtractionComplete);
        }

        public void PlayOutcome(RunOutcome outcome, Vector3 position)
        {
            if (outcome == RunOutcome.Running)
            {
                return;
            }

            LastOutcome = outcome;
            _playEvent(
                position,
                outcome == RunOutcome.Victory
                    ? ExtractionOutcomeFeedbackKind.Victory
                    : ExtractionOutcomeFeedbackKind.Defeat);
        }

        public void SetPaused(bool paused)
        {
            m_paused = paused;
            if (paused)
            {
                _hideAll();
            }
        }

        private void Update()
        {
            if (m_paused || m_tuning == null)
            {
                return;
            }

            m_animationTime += Time.deltaTime;
            if (m_extractionActive && m_progressRoot != null && m_progressRoot.activeSelf)
            {
                _applyProgress();
            }

            for (var index = 0; index < m_eventSlots.Length; index++)
            {
                var slot = m_eventSlots[index];
                if (!slot.Root.activeSelf)
                {
                    continue;
                }

                slot.Elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(slot.Elapsed / m_tuning.EventDuration);
                _applyEvent(slot, progress);
                if (progress >= 1f)
                {
                    slot.Root.SetActive(false);
                }
            }

            _updateActiveEventCount();
        }

        private void OnDisable()
        {
            _hideAll();
        }

        private void OnDestroy()
        {
            if (m_sprite != null)
            {
                Destroy(m_sprite);
            }
        }

        private void _playEvent(Vector3 position, ExtractionOutcomeFeedbackKind kind)
        {
            if (m_eventSlots == null || m_eventSlots.Length == 0 || m_paused)
            {
                return;
            }

            var slot = m_eventSlots[m_nextEventSlot];
            m_nextEventSlot = (m_nextEventSlot + 1) % m_eventSlots.Length;
            slot.Elapsed = 0f;
            slot.Kind = kind;
            slot.Root.transform.position = position + Vector3.up * WORLD_HEIGHT;
            slot.Root.SetActive(true);
            LastKind = kind;
            LastPosition = position;
            PlayCount++;
            _applyEvent(slot, 0f);
            _updateActiveEventCount();
        }

        private void _applyProgress()
        {
            var diameter = m_tuning.ProgressDiameter * Mathf.Lerp(0.96f, 1.12f, ProgressNormalized);
            m_progressRoot.transform.localScale = Vector3.one * diameter;
            m_progressRoot.transform.rotation = Quaternion.Euler(90f, m_animationTime * 18f, 0f);
            var maximumAlpha = m_comfortSettings.ReducedFlashesEnabled
                ? m_tuning.ReducedFlashesProgressMaximumAlpha
                : m_tuning.ProgressMaximumAlpha;
            var pulse = m_comfortSettings.ReducedFlashesEnabled
                ? 1f
                : 0.88f + Mathf.Sin(m_animationTime * 3.2f) * 0.12f;
            var modeColor = m_extractionMode == ExtractionUplinkMode.Overdrive
                ? new Color(1f, 0.58f, 0.08f, 1f)
                : new Color(0.1f, 0.88f, 1f, 1f);
            modeColor.a = maximumAlpha * pulse;
            m_progressRenderer.color = modeColor;
            CurrentMaximumAlpha = Mathf.Max(CurrentMaximumAlpha, modeColor.a);
        }

        private void _applyEvent(EventSlot slot, float progress)
        {
            var eased = 1f - Mathf.Pow(1f - progress, 3f);
            _eventStyle(slot.Kind, out var diameter, out var startingColor, out var endingColor, out var contracts);
            var scale = contracts
                ? Mathf.Lerp(1.15f, 0.52f, eased)
                : Mathf.Lerp(0.58f, 1.18f, eased);
            slot.Root.transform.localScale = Vector3.one * diameter * scale;
            slot.Root.transform.rotation = Quaternion.Euler(90f, progress * (contracts ? -35f : 35f), 0f);
            var maximumAlpha = m_comfortSettings.ReducedFlashesEnabled
                ? m_tuning.ReducedFlashesEventMaximumAlpha
                : m_tuning.EventMaximumAlpha;
            var fadeIn = Mathf.Clamp01(progress / 0.16f);
            var fadeOut = 1f - Mathf.Clamp01((progress - 0.46f) / 0.54f);
            var color = Color.Lerp(startingColor, endingColor, eased);
            color.a = maximumAlpha * fadeIn * fadeOut;
            slot.Renderer.color = color;
            CurrentMaximumAlpha = Mathf.Max(CurrentMaximumAlpha, color.a);
        }

        private void _eventStyle(
            ExtractionOutcomeFeedbackKind kind,
            out float diameter,
            out Color startingColor,
            out Color endingColor,
            out bool contracts)
        {
            contracts = false;
            switch (kind)
            {
                case ExtractionOutcomeFeedbackKind.ExtractionComplete:
                    diameter = m_tuning.CompletionDiameter;
                    startingColor = new Color(1f, 0.58f, 0.08f, 1f);
                    endingColor = new Color(0.1f, 0.88f, 1f, 1f);
                    return;
                case ExtractionOutcomeFeedbackKind.Victory:
                    diameter = m_tuning.VictoryDiameter;
                    startingColor = new Color(0.1f, 0.88f, 1f, 1f);
                    endingColor = Color.white;
                    return;
                case ExtractionOutcomeFeedbackKind.Defeat:
                    diameter = m_tuning.DefeatDiameter;
                    startingColor = new Color(1f, 0.08f, 0.22f, 1f);
                    endingColor = new Color(0.75f, 0.08f, 0.42f, 1f);
                    contracts = true;
                    return;
                default:
                    diameter = m_tuning.ProgressDiameter;
                    startingColor = new Color(1f, 0.58f, 0.08f, 1f);
                    endingColor = new Color(0.1f, 0.88f, 1f, 1f);
                    return;
            }
        }

        private GameObject _createRendererOwner(string ownerName, int sortingOrder, out SpriteRenderer renderer)
        {
            var owner = new GameObject(ownerName);
            owner.transform.SetParent(transform, false);
            owner.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            renderer = owner.AddComponent<SpriteRenderer>();
            renderer.sprite = m_sprite;
            renderer.sortingOrder = sortingOrder;
            owner.SetActive(false);
            return owner;
        }

        private void _hideAll()
        {
            if (m_progressRoot != null)
            {
                m_progressRoot.SetActive(false);
            }

            if (m_eventSlots != null)
            {
                for (var index = 0; index < m_eventSlots.Length; index++)
                {
                    m_eventSlots[index].Root.SetActive(false);
                }
            }

            ActiveEventCount = 0;
            CurrentMaximumAlpha = 0f;
        }

        private void _updateActiveEventCount()
        {
            ActiveEventCount = 0;
            for (var index = 0; index < m_eventSlots.Length; index++)
            {
                if (m_eventSlots[index].Root.activeSelf)
                {
                    ActiveEventCount++;
                }
            }
        }

        private sealed class EventSlot
        {
            public EventSlot(GameObject root, SpriteRenderer renderer)
            {
                Root = root;
                Renderer = renderer;
            }

            public GameObject Root { get; }
            public SpriteRenderer Renderer { get; }
            public float Elapsed { get; set; }
            public ExtractionOutcomeFeedbackKind Kind { get; set; }
        }

        private const string GLYPH_TEXTURE_PATH = "VFX/MachineryStateTransitionGlyph";
        private const string TUNING_PATH = "Tuning/ExtractionOutcomeFeedbackTuning";
        private const string PROGRESS_NAME = "Extraction Uplink Progress";
        private const string EVENT_NAME = "Extraction Outcome Event";
        private const float WORLD_HEIGHT = 0.22f;

        private IComfortSettings m_comfortSettings;
        private ExtractionOutcomeFeedbackTuning m_tuning;
        private Texture2D m_texture;
        private Sprite m_sprite;
        private GameObject m_progressRoot;
        private SpriteRenderer m_progressRenderer;
        private EventSlot[] m_eventSlots;
        private ExtractionUplinkMode m_extractionMode;
        private float m_extractionDuration;
        private float m_animationTime;
        private int m_nextEventSlot;
        private bool m_extractionActive;
        private bool m_paused;
    }
}
