using UnityEngine;

namespace DeadSignal.Presentation
{
    /// <summary>Keeps persistent run-HUD panels inside the display safe area without compressing edge indicators.</summary>
    public sealed class HudCompositionLayout : MonoBehaviour
    {
        private const float DEFAULT_REFERENCE_HEIGHT = 1080f;
        private const float DEFAULT_MAXIMUM_CONTENT_WIDTH = 2160f;

        [SerializeField] private RectTransform m_safeArea;
        [SerializeField] private RectTransform m_compositionFrame;
        [SerializeField] private float m_referenceHeight = DEFAULT_REFERENCE_HEIGHT;
        [SerializeField] private float m_maximumContentWidth = DEFAULT_MAXIMUM_CONTENT_WIDTH;

        private Rect m_lastSafeArea;
        private Vector2Int m_lastScreenSize;

        public bool IsConfigured => m_safeArea != null && m_compositionFrame != null;
        public RectTransform SafeArea => m_safeArea;
        public RectTransform CompositionFrame => m_compositionFrame;
        public float MaximumContentWidth => m_maximumContentWidth;

        private void OnEnable()
        {
            _applyCurrentDisplay();
        }

        private void LateUpdate()
        {
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (m_lastScreenSize == screenSize && m_lastSafeArea == Screen.safeArea)
            {
                return;
            }

            _applyCurrentDisplay();
        }

        public void Configure(RectTransform safeArea, RectTransform compositionFrame)
        {
            m_safeArea = safeArea;
            m_compositionFrame = compositionFrame;
            _applyCurrentDisplay();
        }

        public void ApplyLayout(Rect safeArea, Vector2 screenSize)
        {
            if (!IsConfigured || screenSize.x <= 0f || screenSize.y <= 0f)
            {
                return;
            }

            safeArea = _validatedSafeArea(safeArea, screenSize);
            m_safeArea.anchorMin = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            m_safeArea.anchorMax = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
            m_safeArea.anchoredPosition = Vector2.zero;
            m_safeArea.sizeDelta = Vector2.zero;

            var safeWidth = safeArea.width * Mathf.Max(1f, m_referenceHeight) / screenSize.y;
            m_compositionFrame.anchorMin = new Vector2(0.5f, 0f);
            m_compositionFrame.anchorMax = new Vector2(0.5f, 1f);
            m_compositionFrame.pivot = new Vector2(0.5f, 0.5f);
            m_compositionFrame.anchoredPosition = Vector2.zero;
            m_compositionFrame.sizeDelta = new Vector2(
                Mathf.Min(Mathf.Max(1f, m_maximumContentWidth), safeWidth),
                0f);

            m_lastScreenSize = new Vector2Int(Mathf.RoundToInt(screenSize.x), Mathf.RoundToInt(screenSize.y));
            m_lastSafeArea = safeArea;
        }

        private void _applyCurrentDisplay()
        {
            ApplyLayout(Screen.safeArea, new Vector2(Screen.width, Screen.height));
        }

        private static Rect _validatedSafeArea(Rect safeArea, Vector2 screenSize)
        {
            var screenRect = new Rect(Vector2.zero, screenSize);
            if (safeArea.width <= 0f || safeArea.height <= 0f)
            {
                return screenRect;
            }

            var xMin = Mathf.Clamp(safeArea.xMin, 0f, screenSize.x);
            var yMin = Mathf.Clamp(safeArea.yMin, 0f, screenSize.y);
            var xMax = Mathf.Clamp(safeArea.xMax, xMin, screenSize.x);
            var yMax = Mathf.Clamp(safeArea.yMax, yMin, screenSize.y);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
