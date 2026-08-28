using System.Collections.Generic;
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.UI;
using DeadSignal.Combat;
using DeadSignal.Missions;
using DeadSignal.World;

namespace DeadSignal.Presentation
{
    public enum ObjectiveBeaconPhase
    {
        Tower,
        Salvage,
        Extraction
    }

    internal interface IObjectiveBeacon
    {
        bool HasIcon { get; }
        ObjectiveBeaconPhase CurrentPhase { get; }
        Vector3 CurrentTarget { get; }
        bool IsObjectiveIndicatorVisible { get; }
        bool IsObjectiveIndicatorCompact { get; }
        int ActiveEnemyIndicatorCount { get; }
        string CurrentLabel { get; }
        string CurrentHint { get; }

        void Configure(RunModel model, DeadSignalWorld world, DeadSignalThreatController threats);
        void SetGuidanceStrength(float strength);
    }

    /// <summary>
    /// Presents the next critical run objective as a directional HUD beacon.
    /// </summary>
    public sealed class ObjectiveBeaconHud : MonoBehaviour, IObjectiveBeacon
    {
        private const string ICON_PATH = "UI/ObjectiveBeaconIcon";
        private const string TUNING_PATH = "Tuning/EdgeIndicatorTuning";
        private const float OBJECTIVE_ICON_ROTATION_OFFSET = 180f;

        private RunModel m_model;
        private DeadSignalWorld m_world;
        private DeadSignalThreatController m_threats;
        private ICombatFeedback m_combatFeedback;
        private EdgeIndicatorTuning m_tuning;
        private RectTransform m_canvasRect;
        private RectTransform m_panelRect;
        private RectTransform m_objectiveTail;
        private Vector2 m_edgeIconAnchorMin;
        private Vector2 m_edgeIconAnchorMax;
        private Vector2 m_edgeIconPosition;
        private bool m_wasObjectiveIndicatorCompact;
        private readonly List<ThreatCandidate> m_candidates = new();
        private readonly List<ThreatIndicator> m_threatIndicators = new();
        private float m_guidanceStrength = 0.7f;
        [SerializeField] private GameObject m_panel;
        [SerializeField] private RawImage m_icon;
        [SerializeField] private Text m_label;
        [SerializeField] private Text m_hint;
        [SerializeField] private Text m_distance;

        public bool HasIcon => m_icon != null && m_icon.texture != null;
        public ObjectiveBeaconPhase CurrentPhase { get; private set; }
        public Vector3 CurrentTarget { get; private set; }
        public bool IsObjectiveIndicatorVisible => m_panel != null && m_panel.activeSelf;
        public bool IsObjectiveIndicatorCompact { get; private set; }
        internal RectTransform ObjectiveIndicatorIconRect => m_icon == null ? null : m_icon.rectTransform;
        public int ActiveEnemyIndicatorCount { get; private set; }
        public string CurrentLabel => _currentLabel();
        public string CurrentHint => _currentHint();

        [Inject]
        private void _construct(ICombatFeedback combatFeedback)
        {
            m_combatFeedback = combatFeedback;
        }

        void IObjectiveBeacon.Configure(RunModel model, DeadSignalWorld world, DeadSignalThreatController threats)
        {
            m_model = model;
            m_world = world;
            m_threats = threats;
            m_tuning = Resources.Load<EdgeIndicatorTuning>(TUNING_PATH);
            m_canvasRect = m_panel.transform.parent as RectTransform;
            m_panelRect = m_panel.transform as RectTransform;
            m_edgeIconAnchorMin = m_icon.rectTransform.anchorMin;
            m_edgeIconAnchorMax = m_icon.rectTransform.anchorMax;
            m_edgeIconPosition = m_icon.rectTransform.anchoredPosition + Vector2.Scale(
                m_icon.rectTransform.sizeDelta, Vector2.one * 0.5f - m_icon.rectTransform.pivot);
            m_wasObjectiveIndicatorCompact = false;
            _configureObjectivePanel();
            _createThreatIndicators();
            if (!HasIcon)
            {
                Debug.LogWarning($"Objective beacon icon was not found at Resources/{ICON_PATH}.", this);
            }

            _refreshTarget();
        }

        void IObjectiveBeacon.SetGuidanceStrength(float strength)
        {
            m_guidanceStrength = Mathf.Clamp01(strength);
        }

        private void Update()
        {
            if (m_model != null)
            {
                _refreshTarget();
                _refreshPresentation();
                _refreshThreatIndicators();
            }
        }

        private void _refreshPresentation()
        {
            var visible = m_model != null && m_world != null && m_tuning != null &&
                          m_model.Outcome == RunOutcome.Running && !m_combatFeedback.IsPaused &&
                          m_guidanceStrength > 0.01f;
            m_panel.SetActive(visible);
            if (!visible)
            {
                IsObjectiveIndicatorCompact = false;
                m_wasObjectiveIndicatorCompact = false;
                if (m_objectiveTail != null) m_objectiveTail.gameObject.SetActive(false);
                return;
            }

            IsObjectiveIndicatorCompact = _isComfortablyVisible(CurrentTarget);
            _setObjectiveDetailsVisible(!IsObjectiveIndicatorCompact);
            if (IsObjectiveIndicatorCompact)
            {
                var screenPosition = m_world.Camera.WorldToScreenPoint(CurrentTarget + Vector3.up * 1.6f);
                var canvas = m_canvasRect.GetComponentInParent<Canvas>();
                var eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? canvas.worldCamera
                    : null;
                var previousIconScreenPosition = RectTransformUtility.WorldToScreenPoint(
                    eventCamera, m_icon.rectTransform.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    m_canvasRect, previousIconScreenPosition, eventCamera, out var previousIconLocalPosition);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    m_canvasRect, screenPosition, eventCamera, out var localPosition);
                var anchorReference = new Vector2(
                    Mathf.Lerp(m_canvasRect.rect.xMin, m_canvasRect.rect.xMax, m_panelRect.anchorMin.x),
                    Mathf.Lerp(m_canvasRect.rect.yMin, m_canvasRect.rect.yMax, m_panelRect.anchorMin.y));
                var targetPosition = localPosition - anchorReference;
                m_panelRect.anchoredPosition = targetPosition;
                m_icon.rectTransform.anchorMin = Vector2.one * 0.5f;
                m_icon.rectTransform.anchorMax = Vector2.one * 0.5f;
                m_icon.rectTransform.pivot = Vector2.one * 0.5f;
                if (!m_wasObjectiveIndicatorCompact)
                {
                    m_icon.rectTransform.anchoredPosition = previousIconLocalPosition - localPosition;
                }

                var interpolation = 1f - Mathf.Exp(-m_tuning.ObjectiveTransitionSpeed * Time.unscaledDeltaTime);
                m_icon.rectTransform.anchoredPosition = Vector2.Lerp(
                    m_icon.rectTransform.anchoredPosition, Vector2.zero, interpolation);
                m_icon.rectTransform.localRotation = Quaternion.identity;
                m_wasObjectiveIndicatorCompact = true;
                return;
            }

            var direction = _edgeDirection(CurrentTarget);
            var edgePosition = _edgePosition(direction, m_tuning.ObjectiveSize);
            m_panelRect.anchoredPosition = edgePosition;
            m_wasObjectiveIndicatorCompact = false;
            var panelImage = m_panel.GetComponent<Image>();
            if (panelImage != null)
            {
                var color = panelImage.color;
                color.a = Mathf.Lerp(0.72f, 0.96f, m_guidanceStrength);
                panelImage.color = color;
            }
            m_icon.rectTransform.localRotation = Quaternion.Euler(
                0f, 0f, _directionAngle(direction) + OBJECTIVE_ICON_ROTATION_OFFSET);
            m_objectiveTail.anchoredPosition = edgePosition - direction * 28f;
            m_objectiveTail.localRotation = Quaternion.Euler(0f, 0f, _directionAngle(direction));
            m_label.text = $"NEXT  {_currentLabel()}";
            m_hint.text = _currentHint();
            m_distance.text = $"{Mathf.CeilToInt(DeadSignalWorld.FlatDistance(m_world.Player.position, CurrentTarget))}m";
        }

        private void _setObjectiveDetailsVisible(bool visible)
        {
            var panelImage = m_panel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.enabled = visible;
            }

            m_label.gameObject.SetActive(visible);
            m_hint.gameObject.SetActive(visible);
            m_distance.gameObject.SetActive(visible);
            if (m_objectiveTail != null)
            {
                m_objectiveTail.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            m_icon.rectTransform.anchorMin = m_edgeIconAnchorMin;
            m_icon.rectTransform.anchorMax = m_edgeIconAnchorMax;
            m_icon.rectTransform.pivot = Vector2.one * 0.5f;
            m_icon.rectTransform.anchoredPosition = m_edgeIconPosition;
        }

        private void _refreshTarget()
        {
            CurrentTarget = m_world.GetObjectiveTarget(m_model);
            CurrentPhase = m_model.CurrentObjective.Id switch
            {
                MissionObjectiveId.CentralTower or MissionObjectiveId.CentralInstallation or
                    MissionObjectiveId.RelayTower or MissionObjectiveId.SpineVenting or MissionObjectiveId.SpineTower =>
                    ObjectiveBeaconPhase.Tower,
                MissionObjectiveId.Extraction => ObjectiveBeaconPhase.Extraction,
                _ => ObjectiveBeaconPhase.Salvage
            };
        }

        private void _configureObjectivePanel()
        {
            if (m_tuning == null || m_canvasRect == null || m_panelRect == null)
            {
                return;
            }

            m_panelRect.anchorMin = Vector2.one * 0.5f;
            m_panelRect.anchorMax = Vector2.one * 0.5f;
            m_panelRect.pivot = Vector2.one * 0.5f;
            m_panelRect.sizeDelta = m_tuning.ObjectiveSize;
            var tailObject = new GameObject("Objective Indicator Tail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tailObject.transform.SetParent(m_canvasRect, false);
            m_objectiveTail = tailObject.GetComponent<RectTransform>();
            m_objectiveTail.anchorMin = Vector2.one * 0.5f;
            m_objectiveTail.anchorMax = Vector2.one * 0.5f;
            m_objectiveTail.pivot = new Vector2(0.5f, 1f);
            m_objectiveTail.sizeDelta = new Vector2(3f, 20f);
            tailObject.GetComponent<Image>().color = new Color(1f, 0.58f, 0.08f, 0.85f);
            tailObject.transform.SetAsFirstSibling();
        }

        private void _createThreatIndicators()
        {
            if (m_tuning == null || m_canvasRect == null)
            {
                return;
            }

            for (var index = 0; index < m_tuning.MaximumThreatIndicators; index++)
            {
                m_threatIndicators.Add(_createThreatIndicator(index));
            }
        }

        private ThreatIndicator _createThreatIndicator(int index)
        {
            var root = new GameObject($"Enemy Edge Indicator {index + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(m_canvasRect, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.one * 0.5f;
            rect.anchorMax = Vector2.one * 0.5f;
            rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = m_tuning.ThreatSize;
            root.GetComponent<Image>().color = new Color(0.16f, 0.015f, 0.025f, 0.9f);

            var arrow = _createText(root.transform, "Arrow", 22, TextAnchor.MiddleCenter, new Color(1f, 0.18f, 0.12f));
            arrow.text = "▲";
            arrow.rectTransform.anchorMin = new Vector2(0f, 0f);
            arrow.rectTransform.anchorMax = new Vector2(0f, 1f);
            arrow.rectTransform.pivot = new Vector2(0f, 0.5f);
            arrow.rectTransform.anchoredPosition = new Vector2(6f, 0f);
            arrow.rectTransform.sizeDelta = new Vector2(30f, 0f);

            var label = _createText(root.transform, "Label", 13, TextAnchor.MiddleLeft, new Color(1f, 0.72f, 0.66f));
            label.fontStyle = FontStyle.Bold;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(38f, 0f);
            label.rectTransform.offsetMax = new Vector2(-5f, 0f);
            root.SetActive(false);
            return new ThreatIndicator(root, rect, arrow, label);
        }

        private static Text _createText(
            Transform parent, string name, int size, TextAnchor alignment, Color color)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            root.transform.SetParent(parent, false);
            var text = root.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private void _refreshThreatIndicators()
        {
            ActiveEnemyIndicatorCount = 0;
            foreach (var indicator in m_threatIndicators) indicator.Root.SetActive(false);
            if (m_model == null || m_world == null || m_threats == null || m_tuning == null ||
                m_model.Outcome != RunOutcome.Running || m_combatFeedback.IsPaused)
            {
                return;
            }

            m_candidates.Clear();
            _addSpecialist(m_world.Warden, m_threats.IsWardenAlive, "WARDEN", m_threats.IsWardenScreeningSapper, 40);
            _addSpecialist(m_world.Sapper, m_threats.IsSapperAlive, "SAPPER", m_threats.IsSapperLatched, 80);
            _addSpecialist(m_world.Interceptor, m_threats.IsInterceptorAlive, "INTERCEPTOR", m_threats.IsInterceptorCharging, 70);
            _addSpecialist(m_world.Suppressor, m_threats.IsSuppressorAlive, "SUPPRESSOR",
                m_threats.IsSuppressorFieldWarningActive || m_threats.IsSuppressorFieldActive, 60);
            _addSwarmerGroup();
            m_candidates.Sort((first, second) => second.Score.CompareTo(first.Score));

            var usedPositions = new List<Vector2>();
            var count = Mathf.Min(m_tuning.MaximumThreatIndicators, m_candidates.Count);
            for (var index = 0; index < count; index++)
            {
                var candidate = m_candidates[index];
                var direction = _edgeDirection(candidate.Position);
                var position = _separate(_edgePosition(direction, m_tuning.ThreatSize), direction, usedPositions);
                usedPositions.Add(position);
                var indicator = m_threatIndicators[index];
                indicator.Root.SetActive(true);
                indicator.Rect.anchoredPosition = position;
                indicator.Arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _directionAngle(direction));
                indicator.Label.text = candidate.Label;
                var pulse = candidate.Imminent ? 1f + Mathf.Sin(Time.unscaledTime * m_tuning.ImminentPulseSpeed) * 0.12f : 1f;
                indicator.Rect.localScale = Vector3.one * pulse;
                indicator.Background.color = candidate.Imminent
                    ? new Color(0.5f, 0.015f, 0.025f, 0.96f)
                    : new Color(0.16f, 0.015f, 0.025f, 0.9f);
                ActiveEnemyIndicatorCount++;
            }
        }

        private void _addSpecialist(Transform target, bool alive, string label, bool imminent, int priority)
        {
            if (!alive || target == null || !target.gameObject.activeInHierarchy ||
                _isComfortablyVisible(target.position))
            {
                return;
            }
            var distance = DeadSignalWorld.FlatDistance(m_world.Player.position, target.position);
            m_candidates.Add(new ThreatCandidate(target.position, label, imminent, priority + (imminent ? 100 : 0) - distance * 0.1f));
        }

        private void _addSwarmerGroup()
        {
            var position = Vector3.zero;
            var count = 0;
            foreach (var swarmer in m_threats.ActiveSwarmers)
            {
                if (swarmer == null || _isComfortablyVisible(swarmer.position))
                {
                    continue;
                }
                position += swarmer.position;
                count++;
            }
            if (count > 0)
            {
                m_candidates.Add(new ThreatCandidate(position / count, $"SWARM ×{count}", false, 30));
            }
        }

        private bool _isComfortablyVisible(Vector3 position)
        {
            if (m_world?.Camera == null || m_tuning == null)
            {
                return false;
            }
            var viewport = m_world.Camera.WorldToViewportPoint(position + Vector3.up * 0.5f);
            return viewport.z > 0f && viewport.x >= m_tuning.VisibleInset.x &&
                   viewport.x <= 1f - m_tuning.VisibleInset.x && viewport.y >= m_tuning.VisibleInset.y &&
                   viewport.y <= 1f - m_tuning.VisibleInset.y;
        }

        private Vector2 _edgeDirection(Vector3 position)
        {
            var viewport = m_world.Camera.WorldToViewportPoint(position + Vector3.up * 0.5f);
            var direction = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
            if (viewport.z < 0f) direction = -direction;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.down;
        }

        private Vector2 _edgePosition(Vector2 direction, Vector2 indicatorSize)
        {
            var available = m_canvasRect.rect.size * 0.5f - new Vector2(
                m_canvasRect.rect.width * m_tuning.ViewportMargin.x,
                m_canvasRect.rect.height * m_tuning.ViewportMargin.y) - indicatorSize * 0.5f;
            available = Vector2.Max(available, Vector2.one * 8f);
            var scaleX = Mathf.Abs(direction.x) > 0.0001f ? available.x / Mathf.Abs(direction.x) : float.MaxValue;
            var scaleY = Mathf.Abs(direction.y) > 0.0001f ? available.y / Mathf.Abs(direction.y) : float.MaxValue;
            return direction * Mathf.Min(scaleX, scaleY);
        }

        private Vector2 _separate(Vector2 position, Vector2 direction, IReadOnlyList<Vector2> usedPositions)
        {
            var tangent = new Vector2(-direction.y, direction.x);
            foreach (var used in usedPositions)
            {
                if (Vector2.Distance(position, used) < m_tuning.Separation)
                {
                    position += tangent * m_tuning.Separation;
                }
            }
            return position;
        }

        private static float _directionAngle(Vector2 direction)
        {
            return Vector2.SignedAngle(Vector2.up, direction);
        }

        private string _currentLabel()
        {
            return m_model?.CurrentObjective.Guidance.Action ?? string.Empty;
        }

        private string _currentHint()
        {
            return m_model?.CurrentObjective.Guidance.Advisory ?? string.Empty;
        }

    }

    internal readonly struct ThreatCandidate
    {
        public ThreatCandidate(Vector3 position, string label, bool imminent, float score)
        {
            Position = position;
            Label = label;
            Imminent = imminent;
            Score = score;
        }

        public Vector3 Position { get; }
        public string Label { get; }
        public bool Imminent { get; }
        public float Score { get; }
    }

    internal sealed class ThreatIndicator
    {
        public ThreatIndicator(GameObject root, RectTransform rect, Text arrow, Text label)
        {
            Root = root;
            Rect = rect;
            Arrow = arrow;
            Label = label;
            Background = root.GetComponent<Image>();
        }

        public GameObject Root { get; }
        public RectTransform Rect { get; }
        public Text Arrow { get; }
        public Text Label { get; }
        public Image Background { get; }
    }
}
