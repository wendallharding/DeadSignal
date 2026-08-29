using System.Collections;
using DeadSignal.Application;
using DeadSignal.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeadSignal.Presentation
{
    public enum ProductShellPage
    {
        Main,
        Settings,
        Controls
    }

    /// <summary>Owns the authored pre-run menu without extending deterministic mission state.</summary>
    public sealed class DeadSignalShellController : MonoBehaviour
    {
        [Header("Shell")]
        [SerializeField] private GameObject m_menuOverlay;
        [SerializeField] private GameObject m_mainPanel;
        [SerializeField] private GameObject m_settingsPanel;
        [SerializeField] private GameObject m_controlsPanel;
        [SerializeField] private CanvasGroup m_menuCanvasGroup;
        [SerializeField] private ProductShellTransitionTuning m_transitionTuning;

        [Header("Main Menu")]
        [SerializeField] private Button m_startButton;
        [SerializeField] private Button m_settingsButton;
        [SerializeField] private Button m_controlsButton;
        [SerializeField] private Button m_quitButton;

        [Header("Settings")]
        [SerializeField] private Button[] m_settingButtons;
        [SerializeField] private Text[] m_settingTexts;
        [SerializeField] private Button m_settingsBackButton;

        [Header("Controls")]
        [SerializeField] private Button[] m_rebindButtons;
        [SerializeField] private Text[] m_rebindTexts;
        [SerializeField] private Text m_rebindStatusText;
        [SerializeField] private Button m_controlsBackButton;

        private DeadSignalGame m_game;
        private IDeadSignalHud m_hud;
        private IComfortSettings m_comfortSettings;
        private IDeadSignalInput m_input;
        private bool m_configured;
        private bool m_keyboardNavigationHeld;
        private bool m_keyboardSubmitHeld;
        private bool m_keyboardBackHeld;
        private Coroutine m_startTransition;
        private static bool s_showMenuAfterReload;

        public bool IsMenuVisible => m_menuOverlay != null && m_menuOverlay.activeSelf;
        public bool IsTransitioning => m_startTransition != null;
        public float TransitionOpacity => m_menuCanvasGroup == null ? 0f : m_menuCanvasGroup.alpha;
        public ProductShellPage CurrentPage { get; private set; } = ProductShellPage.Main;

        internal void Configure(DeadSignalGame game, IDeadSignalHud hud, IComfortSettings comfortSettings, IDeadSignalInput input)
        {
            m_game = game;
            m_hud = hud;
            m_comfortSettings = comfortSettings;
            m_input = input;
            _wireButtons();
            m_hud.ConfigureShellActions(m_game.ResumeRun, m_game.RestartRun, _returnToMenu);
            m_configured = true;
            if (_consumeForcedMenu() || _shouldShowMenu())
            {
                _openMenu();
            }
            else
            {
                m_menuOverlay.SetActive(false);
                m_hud.SetMainMenuVisible(false);
                m_game.SetMainMenuOpen(false);
            }
        }

        public void DebugShowMenu()
        {
            if (Debug.isDebugBuild)
            {
                _openMenu();
            }
        }

        private void Update()
        {
            if (!m_configured || !IsMenuVisible)
            {
                return;
            }

            if (!m_game.IsMainMenuOpen || !m_game.IsPaused || m_game.enabled)
            {
                m_game.SetMainMenuOpen(true);
            }
            _refreshLabels();
            _handleKeyboardNavigation();
            if (CurrentPage != ProductShellPage.Main && Gamepad.current?.buttonEast.wasPressedThisFrame == true)
            {
                _showPage(ProductShellPage.Main);
            }
        }

        private void _handleKeyboardNavigation()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var direction = keyboard.downArrowKey.isPressed ? 1 : keyboard.upArrowKey.isPressed ? -1 : 0;
            if (direction != 0 && !m_keyboardNavigationHeld)
            {
                _moveSelection(direction);
            }
            m_keyboardNavigationHeld = direction != 0;

            var submit = keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed;
            if (submit && !m_keyboardSubmitHeld)
            {
                var selectedButton = EventSystem.current?.currentSelectedGameObject?.GetComponent<Button>();
                if (selectedButton != null && selectedButton.interactable)
                {
                    selectedButton.onClick.Invoke();
                }
            }
            m_keyboardSubmitHeld = submit;

            var back = keyboard.escapeKey.isPressed;
            if (back && !m_keyboardBackHeld && CurrentPage != ProductShellPage.Main)
            {
                _showPage(ProductShellPage.Main);
            }
            m_keyboardBackHeld = back;
        }

        private void _moveSelection(int direction)
        {
            var buttons = _activePageButtons();
            var selected = EventSystem.current?.currentSelectedGameObject;
            var currentIndex = System.Array.FindIndex(buttons, button => button.gameObject == selected);
            var nextIndex = currentIndex < 0 ? 0 : (currentIndex + direction + buttons.Length) % buttons.Length;
            EventSystem.current?.SetSelectedGameObject(buttons[nextIndex].gameObject);
        }

        private Button[] _activePageButtons()
        {
            return CurrentPage switch
            {
                ProductShellPage.Settings => new[]
                {
                    m_settingButtons[0], m_settingButtons[1], m_settingButtons[2], m_settingButtons[3], m_settingsBackButton
                },
                ProductShellPage.Controls => new[]
                {
                    m_rebindButtons[0], m_rebindButtons[1], m_rebindButtons[2], m_rebindButtons[3], m_rebindButtons[4],
                    m_rebindButtons[5], m_rebindButtons[6], m_controlsBackButton
                },
                _ => new[] { m_startButton, m_settingsButton, m_controlsButton, m_quitButton }
            };
        }

        private void _wireButtons()
        {
            m_startButton.onClick.AddListener(_startRun);
            m_settingsButton.onClick.AddListener(() => _showPage(ProductShellPage.Settings));
            m_controlsButton.onClick.AddListener(() => _showPage(ProductShellPage.Controls));
            m_quitButton.onClick.AddListener(UnityEngine.Application.Quit);
            m_settingsBackButton.onClick.AddListener(() => _showPage(ProductShellPage.Main));
            m_controlsBackButton.onClick.AddListener(() => _showPage(ProductShellPage.Main));

            m_settingButtons[0].onClick.AddListener(m_comfortSettings.ToggleCameraImpulse);
            m_settingButtons[1].onClick.AddListener(m_comfortSettings.ToggleReducedFlashes);
            m_settingButtons[2].onClick.AddListener(m_comfortSettings.ToggleHighContrast);
            m_settingButtons[3].onClick.AddListener(m_comfortSettings.ToggleAudio);

            m_rebindButtons[0].onClick.AddListener(m_input.BeginMoveUpKeyboardRebind);
            m_rebindButtons[1].onClick.AddListener(m_input.BeginMoveDownKeyboardRebind);
            m_rebindButtons[2].onClick.AddListener(m_input.BeginMoveLeftKeyboardRebind);
            m_rebindButtons[3].onClick.AddListener(m_input.BeginMoveRightKeyboardRebind);
            m_rebindButtons[4].onClick.AddListener(m_input.BeginFireKeyboardRebind);
            m_rebindButtons[5].onClick.AddListener(m_input.BeginInteractKeyboardRebind);
            m_rebindButtons[6].onClick.AddListener(m_input.ResetKeyboardBindings);
        }

        private void _startRun()
        {
            if (m_startTransition == null)
            {
                m_startTransition = StartCoroutine(_transitionToRun());
            }
        }

        private IEnumerator _transitionToRun()
        {
            m_menuCanvasGroup.interactable = false;
            m_menuCanvasGroup.blocksRaycasts = false;
            var elapsed = 0f;
            var duration = m_transitionTuning.Duration(m_comfortSettings.ReducedFlashesEnabled);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                m_menuCanvasGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            m_menuCanvasGroup.alpha = 0f;
            m_menuOverlay.SetActive(false);
            m_game.SetMainMenuOpen(false);
            m_hud.SetMainMenuVisible(false);
            m_startTransition = null;
        }

        private void _returnToMenu()
        {
            s_showMenuAfterReload = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private static bool _consumeForcedMenu()
        {
            if (!s_showMenuAfterReload)
            {
                return false;
            }

            s_showMenuAfterReload = false;
            return true;
        }

        private void _openMenu()
        {
            if (m_startTransition != null)
            {
                StopCoroutine(m_startTransition);
                m_startTransition = null;
            }
            m_menuCanvasGroup.alpha = 1f;
            m_menuCanvasGroup.interactable = true;
            m_menuCanvasGroup.blocksRaycasts = true;
            _showPage(ProductShellPage.Main);
            m_hud.SetMainMenuVisible(true);
            m_game.SetMainMenuOpen(true);
        }

        private static bool _shouldShowMenu()
        {
            if (UnityEngine.Application.isBatchMode)
            {
                return false;
            }

            foreach (var argument in System.Environment.GetCommandLineArgs())
            {
                if (argument.StartsWith("-deadSignal", System.StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private void _showPage(ProductShellPage page)
        {
            CurrentPage = page;
            m_menuOverlay.SetActive(true);
            m_mainPanel.SetActive(page == ProductShellPage.Main);
            m_settingsPanel.SetActive(page == ProductShellPage.Settings);
            m_controlsPanel.SetActive(page == ProductShellPage.Controls);
            _refreshLabels();

            var selection = page switch
            {
                ProductShellPage.Settings => m_settingButtons[0].gameObject,
                ProductShellPage.Controls => m_rebindButtons[0].gameObject,
                _ => m_startButton.gameObject
            };
            EventSystem.current?.SetSelectedGameObject(selection);
        }

        private void _refreshLabels()
        {
            if (m_comfortSettings == null || m_input == null)
            {
                return;
            }

            m_settingTexts[0].text = $"STEADY CAMERA  {(m_comfortSettings.CameraImpulseEnabled ? "OFF" : "ON")}";
            m_settingTexts[1].text = $"REDUCED FLASHES  {(m_comfortSettings.ReducedFlashesEnabled ? "ON" : "OFF")}";
            m_settingTexts[2].text = $"HIGH CONTRAST  {(m_comfortSettings.HighContrastEnabled ? "ON" : "OFF")}";
            m_settingTexts[3].text = $"SIGNAL AUDIO  {(m_comfortSettings.AudioEnabled ? "ON" : "OFF")}";

            var bindings = new[]
            {
                m_input.MoveUpKeyboardBinding,
                m_input.MoveDownKeyboardBinding,
                m_input.MoveLeftKeyboardBinding,
                m_input.MoveRightKeyboardBinding,
                m_input.FireKeyboardBinding,
                m_input.InteractKeyboardBinding,
                "DEFAULTS"
            };
            var labels = new[] { "MOVE UP", "MOVE DOWN", "MOVE LEFT", "MOVE RIGHT", "FIRE", "INTERACT", "RESET BINDINGS" };
            for (var i = 0; i < m_rebindTexts.Length; i++)
            {
                m_rebindTexts[i].text = $"{labels[i]}  //  {bindings[i]}";
            }

            m_rebindStatusText.text = m_input.IsRebinding
                ? "PRESS A KEY  //  ESC CANCELS"
                : string.IsNullOrEmpty(m_input.RebindStatusMessage)
                    ? "GAMEPAD: MOVE / AIM / FIRE / INTERACT / DASH / PAUSE"
                    : m_input.RebindStatusMessage;
        }
    }
}
