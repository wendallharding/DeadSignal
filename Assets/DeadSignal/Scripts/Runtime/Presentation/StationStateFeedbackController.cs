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
        Machinery,
        TrialCommitment,
        LockdownEntry,
        PhaseTransition,
        RoomClear,
        Recovery,
        RewardRelease,
        DepartureSurge
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
        public int SuppressedDuplicateCount { get; private set; }
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
            var lineShader = Shader.Find("Sprites/Default");
            if (lineShader != null)
            {
                m_lineMaterial = new Material(lineShader)
                {
                    name = "Station State Transition Line Material",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            m_slots = new FeedbackSlot[m_tuning.PoolSize];
            for (var index = 0; index < m_slots.Length; index++)
            {
                var slotRoot = new GameObject($"{SLOT_NAME} {index + 1}");
                slotRoot.transform.SetParent(transform, false);
                slotRoot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var renderer = slotRoot.AddComponent<SpriteRenderer>();
                renderer.sprite = m_sprite;
                renderer.sortingOrder = 19;
                var primaryLine = _createLineRenderer(
                    slotRoot.transform,
                    "Primary Transition Shape",
                    m_tuning.PrimaryLineWidth,
                    20);
                var detailLine = _createLineRenderer(
                    slotRoot.transform,
                    "Detail Transition Shape",
                    m_tuning.DetailLineWidth,
                    21);
                slotRoot.SetActive(false);
                m_slots[index] = new FeedbackSlot(slotRoot, renderer, primaryLine, detailLine);
            }
        }

        public void Play(Vector3 position, StationStateFeedbackKind kind)
        {
            if (m_slots == null || m_slots.Length == 0 || m_paused)
            {
                return;
            }

            if (_isDuplicate(position, kind))
            {
                SuppressedDuplicateCount++;
                return;
            }

            var slot = m_slots[m_nextSlot];
            m_nextSlot = (m_nextSlot + 1) % m_slots.Length;
            slot.Elapsed = 0f;
            slot.Diameter = _diameterFor(kind);
            slot.Kind = kind;
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

            if (m_lineMaterial != null)
            {
                Destroy(m_lineMaterial);
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
            _colorsFor(slot.Kind, out var startingColor, out var endingColor);
            var color = Color.Lerp(startingColor, endingColor, eased);
            color.a = fadeIn * fadeOut * maximumAlpha;
            slot.Renderer.color = color;
            _applyTransitionShape(slot, progress, eased, color);
            CurrentAlpha = color.a;
            CurrentColor = color;
        }

        private LineRenderer _createLineRenderer(
            Transform parent,
            string objectName,
            float width,
            int sortingOrder)
        {
            var root = new GameObject(objectName);
            root.transform.SetParent(parent, false);
            var line = root.AddComponent<LineRenderer>();
            line.sharedMaterial = m_lineMaterial != null ? m_lineMaterial : parent.GetComponent<SpriteRenderer>().sharedMaterial;
            line.useWorldSpace = false;
            line.positionCount = MAXIMUM_LINE_POINTS;
            line.widthMultiplier = width;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.textureMode = LineTextureMode.Stretch;
            line.sortingOrder = sortingOrder;
            line.enabled = false;
            return line;
        }

        private bool _isDuplicate(Vector3 position, StationStateFeedbackKind kind)
        {
            for (var index = 0; index < m_slots.Length; index++)
            {
                var slot = m_slots[index];
                if (slot.Root.activeSelf &&
                    slot.Kind == kind &&
                    slot.Elapsed <= m_tuning.DuplicateSuppressionWindow &&
                    (slot.Root.transform.position - position - Vector3.up * WORLD_HEIGHT).sqrMagnitude < 0.0025f)
                {
                    return true;
                }
            }

            return false;
        }

        private static void _applyTransitionShape(FeedbackSlot slot, float progress, float eased, Color color)
        {
            switch (slot.Kind)
            {
                case StationStateFeedbackKind.Tower:
                    _setTowerShape(slot, progress, eased);
                    break;
                case StationStateFeedbackKind.Installation:
                    _setInstallationShape(slot, eased);
                    break;
                case StationStateFeedbackKind.Passage:
                    _setPassageShape(slot, eased);
                    break;
                case StationStateFeedbackKind.Machinery:
                    _setMachineryShape(slot, progress, eased);
                    break;
                case StationStateFeedbackKind.TrialCommitment:
                    _setTrialCommitmentShape(slot, eased);
                    break;
                case StationStateFeedbackKind.LockdownEntry:
                    _setLockdownShape(slot, eased);
                    break;
                case StationStateFeedbackKind.PhaseTransition:
                    _setPhaseShape(slot, progress, eased);
                    break;
                case StationStateFeedbackKind.RoomClear:
                    _setRoomClearShape(slot, eased);
                    break;
                case StationStateFeedbackKind.Recovery:
                case StationStateFeedbackKind.RewardRelease:
                    _setRewardShape(slot, eased);
                    break;
                case StationStateFeedbackKind.DepartureSurge:
                    _setDepartureSurgeShape(slot, eased);
                    break;
                default:
                    slot.PrimaryLine.enabled = false;
                    slot.DetailLine.enabled = false;
                    return;
            }

            _setLineColor(slot.PrimaryLine, color, 0.92f);
            _setLineColor(slot.DetailLine, color, 0.58f);
            slot.PrimaryLine.enabled = color.a > 0.001f;
            slot.DetailLine.enabled = color.a > 0.001f;
        }

        private static void _setTowerShape(FeedbackSlot slot, float progress, float eased)
        {
            slot.PrimaryLine.loop = true;
            slot.PrimaryLine.positionCount = 12;
            var radius = Mathf.Lerp(0.23f, 0.48f, eased);
            var rotation = progress * Mathf.PI * 0.75f;
            for (var index = 0; index < slot.PrimaryLine.positionCount; index++)
            {
                var angle = rotation + index * Mathf.PI * 2f / slot.PrimaryLine.positionCount;
                slot.PrimaryLine.SetPosition(index, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
            }

            slot.DetailLine.loop = true;
            slot.DetailLine.positionCount = 4;
            var detailRadius = Mathf.Lerp(0.13f, 0.27f, eased);
            for (var index = 0; index < slot.DetailLine.positionCount; index++)
            {
                var angle = -rotation + Mathf.PI * 0.25f + index * Mathf.PI * 0.5f;
                slot.DetailLine.SetPosition(index, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * detailRadius);
            }
        }

        private static void _setInstallationShape(FeedbackSlot slot, float eased)
        {
            slot.PrimaryLine.loop = false;
            slot.PrimaryLine.positionCount = 8;
            var outer = Mathf.Lerp(0.48f, 0.3f, eased);
            var inner = Mathf.Lerp(0.24f, 0.08f, eased);
            slot.PrimaryLine.SetPosition(0, new Vector3(-outer, -0.28f, 0f));
            slot.PrimaryLine.SetPosition(1, new Vector3(-outer, 0.28f, 0f));
            slot.PrimaryLine.SetPosition(2, new Vector3(-inner, 0.16f, 0f));
            slot.PrimaryLine.SetPosition(3, new Vector3(-inner, -0.16f, 0f));
            slot.PrimaryLine.SetPosition(4, new Vector3(inner, -0.16f, 0f));
            slot.PrimaryLine.SetPosition(5, new Vector3(inner, 0.16f, 0f));
            slot.PrimaryLine.SetPosition(6, new Vector3(outer, 0.28f, 0f));
            slot.PrimaryLine.SetPosition(7, new Vector3(outer, -0.28f, 0f));

            slot.DetailLine.loop = true;
            slot.DetailLine.positionCount = 4;
            var payloadRadius = Mathf.Lerp(0.18f, 0.1f, eased);
            slot.DetailLine.SetPosition(0, new Vector3(0f, payloadRadius, 0f));
            slot.DetailLine.SetPosition(1, new Vector3(payloadRadius, 0f, 0f));
            slot.DetailLine.SetPosition(2, new Vector3(0f, -payloadRadius, 0f));
            slot.DetailLine.SetPosition(3, new Vector3(-payloadRadius, 0f, 0f));
        }

        private static void _setPassageShape(FeedbackSlot slot, float eased)
        {
            var offset = Mathf.Lerp(0.14f, 0.44f, eased);
            _setDoorRail(slot.PrimaryLine, -offset, -1f);
            _setDoorRail(slot.DetailLine, offset, 1f);
        }

        private static void _setDoorRail(LineRenderer line, float offset, float direction)
        {
            line.loop = false;
            line.positionCount = 5;
            line.SetPosition(0, new Vector3(offset + direction * 0.12f, -0.34f, 0f));
            line.SetPosition(1, new Vector3(offset, -0.34f, 0f));
            line.SetPosition(2, new Vector3(offset, 0.34f, 0f));
            line.SetPosition(3, new Vector3(offset + direction * 0.12f, 0.34f, 0f));
            line.SetPosition(4, new Vector3(offset + direction * 0.2f, 0.24f, 0f));
        }

        private static void _setMachineryShape(FeedbackSlot slot, float progress, float eased)
        {
            slot.PrimaryLine.loop = true;
            slot.PrimaryLine.positionCount = 6;
            var rotation = progress * Mathf.PI * 1.5f;
            var radius = Mathf.Lerp(0.22f, 0.42f, eased);
            for (var index = 0; index < slot.PrimaryLine.positionCount; index++)
            {
                var angle = rotation + index * Mathf.PI / 3f;
                slot.PrimaryLine.SetPosition(index, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
            }

            slot.DetailLine.loop = false;
            slot.DetailLine.positionCount = 5;
            var throwOffset = Mathf.Lerp(-0.22f, 0.22f, eased);
            slot.DetailLine.SetPosition(0, new Vector3(-0.28f, -0.12f, 0f));
            slot.DetailLine.SetPosition(1, new Vector3(throwOffset, -0.12f, 0f));
            slot.DetailLine.SetPosition(2, new Vector3(throwOffset + 0.1f, 0f, 0f));
            slot.DetailLine.SetPosition(3, new Vector3(throwOffset, 0.12f, 0f));
            slot.DetailLine.SetPosition(4, new Vector3(-0.28f, 0.12f, 0f));
        }

        private static void _setTrialCommitmentShape(FeedbackSlot slot, float eased)
        {
            var gap = Mathf.Lerp(0.48f, 0.18f, eased);
            _setDoorRail(slot.PrimaryLine, -gap, 1f);
            _setDoorRail(slot.DetailLine, gap, -1f);
        }

        private static void _setLockdownShape(FeedbackSlot slot, float eased)
        {
            slot.PrimaryLine.loop = true;
            slot.PrimaryLine.positionCount = 8;
            var radius = Mathf.Lerp(0.52f, 0.34f, eased);
            for (var index = 0; index < slot.PrimaryLine.positionCount; index++)
            {
                var angle = Mathf.PI * 0.125f + index * Mathf.PI * 0.25f;
                slot.PrimaryLine.SetPosition(index, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
            }

            slot.DetailLine.loop = false;
            slot.DetailLine.positionCount = 4;
            var cross = Mathf.Lerp(0.12f, 0.32f, eased);
            slot.DetailLine.SetPosition(0, new Vector3(-cross, 0f, 0f));
            slot.DetailLine.SetPosition(1, new Vector3(cross, 0f, 0f));
            slot.DetailLine.SetPosition(2, new Vector3(0f, -cross, 0f));
            slot.DetailLine.SetPosition(3, new Vector3(0f, cross, 0f));
        }

        private static void _setPhaseShape(FeedbackSlot slot, float progress, float eased)
        {
            slot.PrimaryLine.loop = true;
            slot.PrimaryLine.positionCount = 6;
            var radius = Mathf.Lerp(0.28f, 0.48f, eased);
            for (var index = 0; index < slot.PrimaryLine.positionCount; index++)
            {
                var angle = progress * Mathf.PI + index * Mathf.PI / 3f;
                slot.PrimaryLine.SetPosition(index, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
            }

            slot.DetailLine.loop = false;
            slot.DetailLine.positionCount = 3;
            slot.DetailLine.SetPosition(0, new Vector3(-0.22f, -0.2f, 0f));
            slot.DetailLine.SetPosition(1, new Vector3(0f, 0.24f, 0f));
            slot.DetailLine.SetPosition(2, new Vector3(0.22f, -0.2f, 0f));
        }

        private static void _setRoomClearShape(FeedbackSlot slot, float eased)
        {
            slot.PrimaryLine.loop = false;
            slot.PrimaryLine.positionCount = 5;
            var spread = Mathf.Lerp(0.18f, 0.5f, eased);
            slot.PrimaryLine.SetPosition(0, new Vector3(-spread, 0f, 0f));
            slot.PrimaryLine.SetPosition(1, new Vector3(-0.12f, -0.16f, 0f));
            slot.PrimaryLine.SetPosition(2, new Vector3(0f, 0.2f, 0f));
            slot.PrimaryLine.SetPosition(3, new Vector3(0.12f, -0.16f, 0f));
            slot.PrimaryLine.SetPosition(4, new Vector3(spread, 0f, 0f));
            _setDiamond(slot.DetailLine, Mathf.Lerp(0.1f, 0.26f, eased));
        }

        private static void _setRewardShape(FeedbackSlot slot, float eased)
        {
            _setDiamond(slot.PrimaryLine, Mathf.Lerp(0.36f, 0.18f, eased));
            _setDiamond(slot.DetailLine, Mathf.Lerp(0.12f, 0.32f, eased));
        }

        private static void _setDepartureSurgeShape(FeedbackSlot slot, float eased)
        {
            slot.PrimaryLine.loop = false;
            slot.PrimaryLine.positionCount = 6;
            var reach = Mathf.Lerp(0.2f, 0.55f, eased);
            slot.PrimaryLine.SetPosition(0, new Vector3(-reach, -0.18f, 0f));
            slot.PrimaryLine.SetPosition(1, new Vector3(-0.12f, -0.18f, 0f));
            slot.PrimaryLine.SetPosition(2, new Vector3(0.08f, 0f, 0f));
            slot.PrimaryLine.SetPosition(3, new Vector3(-0.08f, 0f, 0f));
            slot.PrimaryLine.SetPosition(4, new Vector3(0.12f, 0.18f, 0f));
            slot.PrimaryLine.SetPosition(5, new Vector3(reach, 0.18f, 0f));
            _setDiamond(slot.DetailLine, Mathf.Lerp(0.1f, 0.22f, eased));
        }

        private static void _setDiamond(LineRenderer line, float radius)
        {
            line.loop = true;
            line.positionCount = 4;
            line.SetPosition(0, new Vector3(0f, radius, 0f));
            line.SetPosition(1, new Vector3(radius, 0f, 0f));
            line.SetPosition(2, new Vector3(0f, -radius, 0f));
            line.SetPosition(3, new Vector3(-radius, 0f, 0f));
        }

        private static void _setLineColor(LineRenderer line, Color color, float alphaMultiplier)
        {
            color.a *= alphaMultiplier;
            line.startColor = color;
            color.a *= 0.45f;
            line.endColor = color;
        }

        private void _clear()
        {
            if (m_slots != null)
            {
                for (var index = 0; index < m_slots.Length; index++)
                {
                    m_slots[index].Root.SetActive(false);
                    m_slots[index].PrimaryLine.enabled = false;
                    m_slots[index].DetailLine.enabled = false;
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
                StationStateFeedbackKind.TrialCommitment => 3f,
                StationStateFeedbackKind.LockdownEntry => 4.1f,
                StationStateFeedbackKind.PhaseTransition => 3.2f,
                StationStateFeedbackKind.RoomClear => 4.5f,
                StationStateFeedbackKind.Recovery => 2.4f,
                StationStateFeedbackKind.RewardRelease => 2.8f,
                StationStateFeedbackKind.DepartureSurge => 3.6f,
                _ => 1.8f
            };
        }

        private void _colorsFor(StationStateFeedbackKind kind, out Color startingColor, out Color endingColor)
        {
            switch (kind)
            {
                case StationStateFeedbackKind.LockdownEntry:
                case StationStateFeedbackKind.TrialCommitment:
                    startingColor = m_tuning.LockdownColor;
                    endingColor = m_tuning.PhaseColor;
                    return;
                case StationStateFeedbackKind.PhaseTransition:
                    startingColor = m_tuning.PhaseColor;
                    endingColor = m_tuning.LockdownColor;
                    return;
                case StationStateFeedbackKind.RoomClear:
                    startingColor = m_tuning.LockdownColor;
                    endingColor = m_tuning.CompleteColor;
                    return;
                case StationStateFeedbackKind.Recovery:
                    startingColor = m_tuning.RecoveryColor;
                    endingColor = m_tuning.CompleteColor;
                    return;
                case StationStateFeedbackKind.RewardRelease:
                    startingColor = m_tuning.AvailableColor;
                    endingColor = m_tuning.RecoveryColor;
                    return;
                case StationStateFeedbackKind.DepartureSurge:
                    startingColor = m_tuning.RecoveryColor;
                    endingColor = m_tuning.CompleteColor;
                    return;
                default:
                    startingColor = m_tuning.AvailableColor;
                    endingColor = m_tuning.CompleteColor;
                    return;
            }
        }

        private sealed class FeedbackSlot
        {
            public FeedbackSlot(
                GameObject root,
                SpriteRenderer renderer,
                LineRenderer primaryLine,
                LineRenderer detailLine)
            {
                Root = root;
                Renderer = renderer;
                PrimaryLine = primaryLine;
                DetailLine = detailLine;
            }

            public GameObject Root { get; }
            public SpriteRenderer Renderer { get; }
            public LineRenderer PrimaryLine { get; }
            public LineRenderer DetailLine { get; }
            public float Elapsed { get; set; }
            public float Diameter { get; set; }
            public StationStateFeedbackKind Kind { get; set; }
        }

        private const string GLYPH_TEXTURE_PATH = "VFX/MachineryStateTransitionGlyph";
        private const string TUNING_PATH = "Tuning/StationStateFeedbackTuning";
        private const string SLOT_NAME = "Station State Transition";
        private const float WORLD_HEIGHT = 0.2f;
        private const int MAXIMUM_LINE_POINTS = 12;

        private IComfortSettings m_comfortSettings;
        private StationStateFeedbackTuning m_tuning;
        private Texture2D m_texture;
        private Sprite m_sprite;
        private Material m_lineMaterial;
        private FeedbackSlot[] m_slots;
        private int m_nextSlot;
        private bool m_paused;
    }
}
