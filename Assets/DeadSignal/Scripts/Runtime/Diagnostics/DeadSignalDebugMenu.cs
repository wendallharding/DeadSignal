using OOI.AutoUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DeadSignal.Application;

namespace DeadSignal.Diagnostics
{
    public enum DebugLocation
    {
        Extraction, CentralTower, Shortcut, RelayTower, SpineTower, CacheOne, CacheTwo, CacheThree, CacheFour,
        FarEast, NorthBoundary, SouthBoundary, CurrentObjective
    }

    public enum DebugScenario
    {
        FreshRun, TowerActivation, FirstOverclock, ActiveSalvageChain, SapperPulse, InterceptorCharge,
        SuppressorExtraction, CriticalRecovery, OptionalCache, StableExtraction, OverdriveExtraction, Victory,
        Failure, EasternRoomCombat, AllEffects
    }

    public enum DebugTimeScale
    {
        Paused, Quarter, Half, Normal, Double
    }

    /// <summary>Development-only AutoUI playtest laboratory and input owner.</summary>
    public sealed class DeadSignalDebugMenu : MonoBehaviour
    {
        private DeadSignalGame m_game;
        private AutoUIProvider m_provider;
        private bool m_isOpen;
        private bool m_runWhileOpen;
        private string m_lastCommand = "Ready — choose a page and run a command.";

        public bool IsOpen => m_isOpen;
        public Canvas DebugCanvas => m_provider?.CanvasRef;
        public static bool IsAvailable => IsAllowed(UnityEngine.Application.isEditor, Debug.isDebugBuild);

        [BeginLayoutGroup("Live Run", width: 570, backgroundColor: 0x071820FA)]
        [Label, HideLabel]
        public string LiveRunState => m_game != null ? m_game.DebugOverview : "Waiting for DEAD SIGNAL runtime...";
        [Label, HideLabel]
        public string LastCommand => $"LAST COMMAND\n{m_lastCommand}";
        [EndLayoutGroup]

        [BeginLayoutGroup("Live Diagnostics", width: 570, backgroundColor: 0x101B22FA)]
        [Label, HideLabel]
        public string TelemetryMovement => _telemetryLine(0);
        [Label, HideLabel]
        public string TelemetryInput => _telemetryLine(1);
        [Label, HideLabel]
        public string TelemetryCamera => _telemetryLine(2);
        [Label, HideLabel]
        public string LatestEvent => m_game == null
            ? "LATEST EVENT  Runtime unavailable"
            : $"LATEST EVENT  {m_game.DebugEventLog.Split('\n').LastOrDefault()}";
        [Button]
        public void CopyDiagnostics()
        {
            GUIUtility.systemCopyBuffer = $"{LiveRunState}\n{m_game?.DebugTelemetry}\n{m_game?.DebugEventLog}";
            Confirm("Diagnostics copied to clipboard");
        }
        [EndLayoutGroup]

        public static bool IsAllowed(bool isEditor, bool isDebugBuild) => isEditor || isDebugBuild;

        public void Configure(DeadSignalGame game)
        {
            m_game = game;
            if (!IsAvailable)
            {
                enabled = false;
                return;
            }

            var generatorPrefab = Resources.Load<AutoUIGenerator>("UI/Debug/AutoUI");
            var canvasPrefab = Resources.Load<Canvas>("UI/Debug/Canvas_DebugMenu");
            if (generatorPrefab == null || canvasPrefab == null)
            {
                Debug.LogError("DEAD SIGNAL debug menu could not load the AutoUI runtime prefabs.", this);
                enabled = false;
                return;
            }

            _addPage<DebugScenariosPage>();
            _addPage<DebugPlayerPage>();
            _addPage<DebugThreatsPage>();
            _addPage<DebugAutomationPage>();
            _addPage<DebugCapturePage>();
            _addPage<DebugSettingsPage>();

            m_provider = gameObject.AddComponent<AutoUIProvider>();
            _configureProvider(generatorPrefab, canvasPrefab);
            m_provider.Generate();
            _configurePresentation();
            m_provider.CanvasRef.gameObject.SetActive(false);
        }

        public void Confirm(string message)
        {
            m_lastCommand = $"{DateTime.Now:HH:mm:ss}  {message}";
            m_game?.DebugConfirm(message);
        }

        public void Execute(Action action, string confirmation)
        {
            action?.Invoke();
            Confirm(confirmation);
        }

        public void SetRunWhileOpen(bool runWhileOpen)
        {
            m_runWhileOpen = runWhileOpen;
            if (m_isOpen)
            {
                m_game.SetDebugMenuState(true, m_runWhileOpen);
            }
            Confirm(runWhileOpen ? "Live simulation enabled while menu is open" : "Simulation paused while menu is open");
        }

        private void Update()
        {
            if (!IsAvailable)
            {
                return;
            }

            var keyboardToggle = Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame;
            var controllerToggle = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame &&
                                   Gamepad.current.leftShoulder.isPressed;
            var closeWithEscape = m_isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            if (keyboardToggle || controllerToggle || closeWithEscape)
            {
                _setOpen(!m_isOpen);
            }
        }

        private void OnDisable()
        {
            m_isOpen = false;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnGUI()
        {
            if (!IsAvailable)
            {
                return;
            }

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperRight,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = new Color(1f, 0.68f, 0.12f, 0.9f);
            GUI.Label(new Rect(Screen.width - 410f, m_isOpen ? Screen.height - 28f : 4f, 390f, 24f),
                "PLAYTEST TOOLS ACTIVE  //  F5  //  LB+MENU", style);
        }

        private void _setOpen(bool open)
        {
            m_isOpen = open;
            if (m_provider != null && m_provider.CanvasRef != null)
            {
                m_provider.CanvasRef.gameObject.SetActive(open);
            }
            if (m_game != null)
            {
                m_game.SetDebugMenuState(open, m_runWhileOpen);
            }
            Cursor.visible = open;
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void _addPage<T>() where T : DebugMenuPage
        {
            var page = gameObject.AddComponent<T>();
            page.Configure(this, m_game);
        }

        private void _configureProvider(AutoUIGenerator generatorPrefab, Canvas canvasPrefab)
        {
            const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic;
            var providerType = typeof(AutoUIProvider);
            providerType.GetField("m_autoUIPrefab", FLAGS)?.SetValue(m_provider, generatorPrefab);
            providerType.GetField("m_canvasTemplate", FLAGS)?.SetValue(m_provider, canvasPrefab);
            providerType.GetField("m_autoUIObjects", FLAGS)?.SetValue(m_provider, new List<GameObject> { gameObject });
            providerType.GetField("m_additionalPagePrefabs", FLAGS)?.SetValue(m_provider, new List<GameObject>());
            providerType.GetField("m_pageProviders", FLAGS)?.SetValue(m_provider, new List<MenuScreenProviderBase>());
        }

        private void _configurePresentation()
        {
            var canvas = m_provider.CanvasRef;
            canvas.gameObject.name = "DEAD SIGNAL — Debug Menu";
            canvas.sortingOrder = 200;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1600f, 900f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            var backdrop = new GameObject("DEAD SIGNAL — Debug Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backdrop.transform.SetParent(canvas.transform, false);
            backdrop.transform.SetAsFirstSibling();
            var rect = (RectTransform)backdrop.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            backdrop.GetComponent<Image>().color = new Color(0.015f, 0.045f, 0.065f, 0.985f);

            var tabs = canvas.GetComponentsInChildren<Transform>(true).FirstOrDefault(candidate => candidate.name == "TabsContent");
            if (tabs == null)
            {
                return;
            }

            var layout = tabs.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
            }
            foreach (Transform child in tabs)
            {
                var element = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
                element.minWidth = 120f;
                element.preferredWidth = 150f;
                element.flexibleWidth = 0f;
            }

            var firstTabText = tabs.GetChild(0).GetComponentsInChildren<Graphic>(true)
                .FirstOrDefault(graphic => graphic.GetType().GetProperty("text") != null);
            firstTabText?.GetType().GetProperty("text")?.SetValue(firstTabText, "Overview");
        }

        private string _telemetryLine(int line)
        {
            if (m_game == null)
            {
                return "Runtime unavailable";
            }

            var lines = m_game.DebugTelemetry.Split('\n');
            return line < lines.Length ? lines[line] : string.Empty;
        }
    }
}
