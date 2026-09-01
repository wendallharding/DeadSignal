using DeadSignal.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadSignal.Presentation
{
    /// <summary>Owns presentation-only motion and focus feedback for the authored main menu.</summary>
    public sealed class MainMenuPresentation : MonoBehaviour
    {
        [SerializeField] private RawImage m_backdrop;
        [SerializeField] private RectTransform m_mainPanel;
        [SerializeField] private RectTransform m_selectionRail;
        [SerializeField] private Text m_selectionDetail;
        [SerializeField] private RectTransform m_signalSweep;
        [SerializeField] private Text m_stationReadout;
        [SerializeField] private RectTransform m_settingsPanel;
        [SerializeField] private RectTransform m_controlsPanel;
        [SerializeField] private Button[] m_settingButtons;
        [SerializeField] private Button[] m_controlButtons;
        [SerializeField] private RectTransform m_settingsSelectionRail;
        [SerializeField] private RectTransform m_controlsSelectionRail;
        [SerializeField] private Text m_utilityDetailTitle;
        [SerializeField] private Text m_utilityDetail;
        [SerializeField] private Text m_utilityConfirmation;
        [SerializeField] private Text m_utilityInputHint;
        [SerializeField] private Text m_controlsDetailTitle;
        [SerializeField] private Text m_controlsDetail;
        [SerializeField] private Text m_controlsInputHint;

        private IComfortSettings m_comfortSettings;
        private Vector2 m_backdropTextureSize = new Vector2(16f, 9f);
        private float m_railVelocity;
        private float m_utilityRailVelocity;
        private ProductShellPage m_page;
        private InputPromptDevice m_inputDevice;

        public RectTransform MainPanel => m_mainPanel;
        public RectTransform SelectionRail => m_selectionRail;
        public Text SelectionDetail => m_selectionDetail;
        public RectTransform SignalSweep => m_signalSweep;
        public RectTransform SettingsPanel => m_settingsPanel;
        public RectTransform ControlsPanel => m_controlsPanel;
        public RectTransform SettingsSelectionRail => m_settingsSelectionRail;
        public RectTransform ControlsSelectionRail => m_controlsSelectionRail;
        public Text UtilityDetail => m_utilityDetail;
        public Text UtilityConfirmation => m_utilityConfirmation;
        public Text UtilityInputHint => m_utilityInputHint;

        internal void Configure(IComfortSettings comfortSettings)
        {
            m_comfortSettings = comfortSettings;
            if (m_backdrop != null && m_backdrop.texture != null)
            {
                m_backdropTextureSize = new Vector2(m_backdrop.texture.width, m_backdrop.texture.height);
            }

            ApplyPresentationForViewport(new Vector2(Screen.width, Screen.height), true, 0f);
        }

        internal void SetPage(ProductShellPage page, InputPromptDevice inputDevice)
        {
            m_page = page;
            m_inputDevice = inputDevice;
            _refreshUtilitySelection(true, inputDevice);
        }

        internal void ConfirmSetting(int settingIndex)
        {
            if (m_utilityConfirmation == null)
            {
                return;
            }

            m_utilityConfirmation.text = settingIndex switch
            {
                0 => "PREFERENCE SAVED  //  CAMERA RESPONSE UPDATED",
                1 => "PREFERENCE SAVED  //  FLASH RESPONSE UPDATED",
                2 => "PREFERENCE SAVED  //  CONTRAST PROFILE UPDATED",
                3 => "PREFERENCE SAVED  //  AUDIO ROUTING UPDATED",
                _ => "PREFERENCE SAVED"
            };
        }

        public void ApplyPresentationForViewport(Vector2 viewportSize, bool reducedMotion, float phase)
        {
            _refreshBackdrop(viewportSize, phase);
            _refreshSweep(phase, reducedMotion);
            _refreshSelection(reducedMotion);
            _refreshUtilitySelection(reducedMotion, m_inputDevice);
        }

        public static Rect CalculateBackdropUvRect(Vector2 textureSize, Vector2 viewportSize, Vector2 pan)
        {
            if (textureSize.x <= 0f || textureSize.y <= 0f || viewportSize.x <= 0f || viewportSize.y <= 0f)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            var textureAspect = textureSize.x / textureSize.y;
            var viewportAspect = viewportSize.x / viewportSize.y;
            var uv = new Rect(0f, 0f, 1f, 1f);
            if (viewportAspect > textureAspect)
            {
                uv.height = textureAspect / viewportAspect;
                uv.y = (1f - uv.height) * 0.5f;
                uv.y += Mathf.Clamp(pan.y, -1f, 1f) * (1f - uv.height) * 0.5f;
            }
            else
            {
                uv.width = viewportAspect / textureAspect;
                uv.x = (1f - uv.width) * 0.5f;
                uv.x += Mathf.Clamp(pan.x, -1f, 1f) * (1f - uv.width) * 0.5f;
            }

            return uv;
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            var reducedMotion = m_comfortSettings?.ReducedFlashesEnabled ?? false;
            var phase = reducedMotion ? 0f : Time.unscaledTime;
            ApplyPresentationForViewport(new Vector2(Screen.width, Screen.height), reducedMotion, phase);
        }

        private void _refreshBackdrop(Vector2 viewport, float phase)
        {
            if (m_backdrop == null)
            {
                return;
            }

            viewport = new Vector2(Mathf.Max(1f, viewport.x), Mathf.Max(1f, viewport.y));
            var pan = new Vector2(Mathf.Sin(phase * 0.055f) * 0.12f, Mathf.Cos(phase * 0.041f) * 0.08f);
            m_backdrop.uvRect = CalculateBackdropUvRect(m_backdropTextureSize, viewport, pan);
        }

        private void _refreshSweep(float phase, bool reducedMotion)
        {
            if (m_signalSweep == null)
            {
                return;
            }

            var normalized = reducedMotion ? 0.62f : Mathf.Repeat(phase * 0.035f, 1f);
            m_signalSweep.anchorMin = new Vector2(normalized, 0f);
            m_signalSweep.anchorMax = new Vector2(normalized, 1f);
            if (m_stationReadout != null)
            {
                m_stationReadout.text = reducedMotion
                    ? "DS-07  //  PASSIVE SCAN"
                    : $"DS-07  //  ARRAY {Mathf.FloorToInt(normalized * 99f):00}";
            }
        }

        private void _refreshSelection(bool immediate)
        {
            if (m_selectionRail == null || m_mainPanel == null)
            {
                return;
            }

            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected == null || selected.transform.parent != m_mainPanel)
            {
                return;
            }

            var selectedRect = selected.transform as RectTransform;
            var targetY = selectedRect.anchoredPosition.y;
            var railPosition = m_selectionRail.anchoredPosition;
            railPosition.y = immediate
                ? targetY
                : Mathf.SmoothDamp(railPosition.y, targetY, ref m_railVelocity, 0.08f, Mathf.Infinity, Time.unscaledDeltaTime);
            m_selectionRail.anchoredPosition = railPosition;

            if (m_selectionDetail == null)
            {
                return;
            }

            m_selectionDetail.text = selected.name switch
            {
                "Start Run" => "BEGIN STATION RESTORATION",
                "Settings" => "ACCESSIBILITY AND SIGNAL RESPONSE",
                "Controls" => "INPUT MAP AND KEYBOARD BINDINGS",
                "Quit" => "CLOSE DEAD SIGNAL",
                _ => m_selectionDetail.text
            };
        }

        private void _refreshUtilitySelection(bool immediate, InputPromptDevice inputDevice)
        {
            var buttons = m_page == ProductShellPage.Settings ? m_settingButtons : m_controlButtons;
            var rail = m_page == ProductShellPage.Settings ? m_settingsSelectionRail : m_controlsSelectionRail;
            var panel = m_page == ProductShellPage.Settings ? m_settingsPanel : m_controlsPanel;
            if (buttons == null || rail == null || panel == null || !panel.gameObject.activeInHierarchy)
            {
                return;
            }

            var selected = EventSystem.current?.currentSelectedGameObject;
            var selectedButton = System.Array.Find(buttons, button => button != null && button.gameObject == selected);
            if (selectedButton == null)
            {
                return;
            }

            var selectedRect = selectedButton.transform as RectTransform;
            var railPosition = rail.anchoredPosition;
            railPosition.y = immediate
                ? selectedRect.anchoredPosition.y
                : Mathf.SmoothDamp(railPosition.y, selectedRect.anchoredPosition.y, ref m_utilityRailVelocity, 0.08f,
                    Mathf.Infinity, Time.unscaledDeltaTime);
            rail.anchoredPosition = railPosition;

            var detailTitle = m_page == ProductShellPage.Controls ? m_controlsDetailTitle : m_utilityDetailTitle;
            var detail = m_page == ProductShellPage.Controls ? m_controlsDetail : m_utilityDetail;
            var inputHint = m_page == ProductShellPage.Controls ? m_controlsInputHint : m_utilityInputHint;
            if (detailTitle != null)
            {
                detailTitle.text = selectedButton.name.ToUpperInvariant();
            }
            if (detail != null)
            {
                detail.text = _detailFor(selectedButton.name);
            }
            if (inputHint != null)
            {
                inputHint.text = inputDevice == InputPromptDevice.Gamepad
                    ? "LEFT STICK / D-PAD  NAVIGATE     A  APPLY     B  BACK"
                    : "ARROWS  NAVIGATE     ENTER  APPLY     ESC  BACK";
            }
        }

        private static string _detailFor(string selectionName)
        {
            return selectionName switch
            {
                "Steady Camera" => "Removes impact and event camera impulses. Aim and movement remain unchanged.",
                "Reduced Flashes" => "Replaces rapid flashes and pulsing transitions with steady, readable states.",
                "High Contrast" => "Strengthens objective, threat, projectile, and interaction separation.",
                "Signal Audio" => "Routes combat, machinery, warning, and interface audio together.",
                "Move Up" or "Move Down" or "Move Left" or "Move Right" =>
                    "Rebind one keyboard movement direction. Controller movement remains on the left stick.",
                "Fire" => "Rebind continuous basic fire. Controller fire remains on the right trigger or shoulder.",
                "Interact" => "Rebind machinery interaction. Controller interaction remains on the west face button.",
                "Reset Bindings" => "Restore the complete keyboard map to the shipped defaults.",
                "Back" => "Return to mission control without changing saved preferences.",
                _ => string.Empty
            };
        }
    }
}
