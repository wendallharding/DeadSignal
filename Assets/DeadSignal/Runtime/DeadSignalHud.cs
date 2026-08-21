using Reflex.Attributes;
using UnityEngine;

namespace DeadSignal
{
    internal interface IDeadSignalHud
    {
        bool HasPauseInsignia { get; }
        bool HasCameraComfortIcon { get; }
        bool HasReducedFlashesIcon { get; }
        bool HasHighContrastIcon { get; }
        bool HasInputLinkIcon { get; }
        bool HasAudioLinkIcon { get; }

        void Configure(RunModel model, RunMetrics metrics, DeadSignalWorld world, DeadSignalThreatController threats);
        void ShowFeedback(string message);
        void Tick(float dt);
    }

    /// <summary>
    /// Owns all immediate-mode HUD, pause, prompt, and end-of-run presentation.
    /// </summary>
    public sealed class DeadSignalHud : MonoBehaviour, IDeadSignalHud
    {
        private RunModel m_model;
        private RunMetrics m_metrics;
        private DeadSignalWorld m_world;
        private DeadSignalThreatController m_threats;
        private ICombatFeedback m_combatFeedback;
        private IComfortSettings m_comfortSettings;
        private IDeadSignalInput m_input;
        private GUIStyle m_titleStyle;
        private GUIStyle m_labelStyle;
        private GUIStyle m_smallStyle;
        private GUIStyle m_centerStyle;
        private GUIStyle m_giantStyle;
        private GUIStyle m_reportStyle;
        private Texture2D m_pauseInsignia;
        private Texture2D m_cameraComfortIcon;
        private Texture2D m_reducedFlashesIcon;
        private Texture2D m_highContrastIcon;
        private Texture2D m_inputLinkIcon;
        private Texture2D m_audioLinkIcon;
        private float m_feedbackTimer;
        private string m_feedback = string.Empty;

        public bool HasPauseInsignia => m_pauseInsignia != null;
        public bool HasCameraComfortIcon => m_cameraComfortIcon != null;
        public bool HasReducedFlashesIcon => m_reducedFlashesIcon != null;
        public bool HasHighContrastIcon => m_highContrastIcon != null;
        public bool HasInputLinkIcon => m_inputLinkIcon != null;
        public bool HasAudioLinkIcon => m_audioLinkIcon != null;

        [Inject]
        private void _construct(
            ICombatFeedback combatFeedback,
            IComfortSettings comfortSettings,
            IDeadSignalInput input)
        {
            m_combatFeedback = combatFeedback;
            m_comfortSettings = comfortSettings;
            m_input = input;
        }

        void IDeadSignalHud.Configure(
            RunModel model,
            RunMetrics metrics,
            DeadSignalWorld world,
            DeadSignalThreatController threats)
        {
            m_model = model;
            m_metrics = metrics;
            m_world = world;
            m_threats = threats;
            m_pauseInsignia = Resources.Load<Texture2D>("UI/MaintenanceNetworkInsignia");
            m_cameraComfortIcon = Resources.Load<Texture2D>("UI/SteadyCameraIcon");
            m_reducedFlashesIcon = Resources.Load<Texture2D>("UI/ReducedFlashesIcon");
            m_highContrastIcon = Resources.Load<Texture2D>("UI/HighContrastIcon");
            m_inputLinkIcon = Resources.Load<Texture2D>("UI/InputLinkIcon");
            m_audioLinkIcon = Resources.Load<Texture2D>("UI/AudioLinkIcon");
        }

        void IDeadSignalHud.ShowFeedback(string message)
        {
            m_feedback = message;
            m_feedbackTimer = 2.2f;
        }

        void IDeadSignalHud.Tick(float dt)
        {
            m_feedbackTimer = Mathf.Max(0f, m_feedbackTimer - dt);
        }

        private void OnGUI()
        {
            if (m_model == null || m_world == null || m_threats == null)
            {
                return;
            }

            _ensureGuiStyles();
            _applyContrastTheme();
            _drawRunHud();
            _drawContextPrompt();
            _drawFeedback();
            _drawOutcome();
            _drawPauseOverlay();
            GUI.color = Color.white;
        }

        private void _ensureGuiStyles()
        {
            if (m_labelStyle != null)
            {
                return;
            }

            m_titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            m_titleStyle.normal.textColor = new Color(0.15f, 0.95f, 1f);
            m_labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            m_labelStyle.normal.textColor = Color.white;
            m_smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            m_smallStyle.normal.textColor = new Color(0.72f, 0.82f, 0.86f);
            m_centerStyle = new GUIStyle(m_labelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 17 };
            m_giantStyle = new GUIStyle(m_centerStyle) { fontSize = 38, fontStyle = FontStyle.Bold };
            m_reportStyle = new GUIStyle(m_centerStyle) { fontSize = 15, fontStyle = FontStyle.Normal };
        }

        private void _applyContrastTheme()
        {
            m_titleStyle.normal.textColor = m_comfortSettings.HighContrastEnabled
                ? Color.white
                : new Color(0.15f, 0.95f, 1f);
            m_labelStyle.normal.textColor = Color.white;
            m_reportStyle.normal.textColor = m_comfortSettings.HighContrastEnabled
                ? Color.white
                : new Color(0.72f, 0.82f, 0.86f);
        }

        private void _drawRunHud()
        {
            float signalRatio = Mathf.Clamp01(m_model.Signal / RunModel.MaximumSignal);
            var panel = new Rect(18f, 18f, 350f, 154f);
            GUI.color = m_comfortSettings.HighContrastEnabled
                ? new Color(0f, 0f, 0f, 0.98f)
                : new Color(0.015f, 0.025f, 0.035f, 0.94f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(34f, 28f, 300f, 30f), "DEAD SIGNAL", m_titleStyle);
            GUI.Label(new Rect(34f, 61f, 280f, 24f), $"SIGNAL  {Mathf.CeilToInt(m_model.Signal):000}", m_labelStyle);
            GUI.color = new Color(0.05f, 0.09f, 0.11f, 1f);
            GUI.DrawTexture(new Rect(34f, 88f, 300f, 14f), Texture2D.whiteTexture);
            GUI.color = signalRatio > 0.25f
                ? (m_comfortSettings.HighContrastEnabled ? new Color(0.25f, 1f, 1f) : new Color(0.02f, 0.9f, 1f))
                : (m_comfortSettings.HighContrastEnabled ? new Color(1f, 0.72f, 0.05f) : new Color(1f, 0.06f, 0.05f));
            GUI.DrawTexture(new Rect(34f, 88f, 300f * signalRatio, 14f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(34f, 108f, 300f, 23f), $"SALVAGE  {m_model.Salvage}/{RunModel.SalvageRequired}", m_labelStyle);
            bool isPowered = m_world.IsPowered(m_world.Player.position, m_model.TowerOnline);
            string zone = isPowered ? "● POWERED TERRITORY" : "▲ DEAD ZONE — ACTIVE DRAIN";
            m_smallStyle.normal.textColor = isPowered ? new Color(0.05f, 0.95f, 1f) : new Color(1f, 0.22f, 0.18f);
            GUI.Label(new Rect(34f, 134f, 300f, 24f), zone, m_smallStyle);

            GUI.color = new Color(0.015f, 0.025f, 0.035f, 0.86f);
            GUI.Box(new Rect(Screen.width - 374f, 18f, 356f, 190f), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width - 358f, 28f, 330f, 22f), _currentObjective(), m_labelStyle);
            m_smallStyle.normal.textColor = m_model.TowerOnline && m_threats.IsSapperAlive
                ? new Color(1f, 0.18f, 0.72f)
                : new Color(0.5f, 0.68f, 0.7f);
            GUI.Label(new Rect(Screen.width - 358f, 54f, 330f, 22f), _sapperStatus(), m_smallStyle);
            m_smallStyle.normal.textColor = new Color(0.72f, 0.82f, 0.86f);
            if (m_inputLinkIcon != null)
            {
                GUI.DrawTexture(new Rect(Screen.width - 358f, 84f, 72f, 72f), m_inputLinkIcon, ScaleMode.ScaleToFit, true);
            }

            GUI.Label(new Rect(Screen.width - 278f, 78f, 244f, 104f), _activeControlLegend(), m_smallStyle);
        }

        private void _drawContextPrompt()
        {
            string prompt = _contextPrompt();
            if (string.IsNullOrEmpty(prompt))
            {
                return;
            }

            GUI.color = new Color(0.015f, 0.025f, 0.035f, 0.93f);
            GUI.Box(new Rect(Screen.width * 0.5f - 220f, Screen.height - 86f, 440f, 44f), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width * 0.5f - 210f, Screen.height - 80f, 420f, 32f), prompt, m_centerStyle);
        }

        private void _drawFeedback()
        {
            if (m_feedbackTimer <= 0f)
            {
                return;
            }

            GUI.color = m_feedback.Contains("DEAD") || m_feedback.Contains("SECURITY")
                ? new Color(1f, 0.25f, 0.2f)
                : new Color(0.1f, 0.95f, 1f);
            GUI.Label(new Rect(Screen.width * 0.5f - 300f, 28f, 600f, 40f), m_feedback, m_centerStyle);
        }

        private void _drawOutcome()
        {
            if (m_model.Outcome == RunOutcome.Running)
            {
                return;
            }

            GUI.color = new Color(0.002f, 0.005f, 0.008f, 0.93f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = m_model.Outcome == RunOutcome.Victory ? new Color(0.08f, 0.96f, 1f) : new Color(1f, 0.08f, 0.06f);
            string result = m_model.Outcome == RunOutcome.Victory ? "SIGNAL RECOVERED" : "DRONE OFFLINE";
            GUI.Label(new Rect(0f, Screen.height * 0.5f - 80f, Screen.width, 60f), result, m_giantStyle);
            GUI.color = Color.white;
            string detail = m_model.Outcome == RunOutcome.Victory
                ? "Salvage extracted. The station lives a little longer."
                : "Signal depleted in the dark.";
            GUI.Label(new Rect(0f, Screen.height * 0.5f - 10f, Screen.width, 36f), detail, m_centerStyle);
            GUI.color = new Color(0.72f, 0.84f, 0.88f);
            GUI.Label(new Rect(0f, Screen.height * 0.5f + 28f, Screen.width, 54f), _runReport(), m_reportStyle);
            GUI.color = Color.white;
            GUI.Label(new Rect(0f, Screen.height * 0.5f + 88f, Screen.width, 36f),
                $"PRESS {_binding("R / ENTER", "GAMEPAD A")} TO RESTART", m_centerStyle);
        }

        private void _drawPauseOverlay()
        {
            if (!m_combatFeedback.IsPaused)
            {
                return;
            }

            GUI.color = new Color(0.002f, 0.005f, 0.008f, 0.94f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (m_pauseInsignia != null)
            {
                GUI.DrawTexture(new Rect(Screen.width * 0.5f - 76f, Screen.height * 0.5f - 250f, 152f, 152f),
                    m_pauseInsignia, ScaleMode.ScaleToFit, true);
            }

            GUI.color = new Color(0.08f, 0.96f, 1f);
            GUI.Label(new Rect(0f, Screen.height * 0.5f - 94f, Screen.width, 54f), "SIGNAL LINK SUSPENDED", m_giantStyle);
            GUI.color = new Color(0.76f, 0.86f, 0.9f);
            GUI.Label(new Rect(0f, Screen.height * 0.5f - 40f, Screen.width, 32f),
                "Signal drain, threats, projectiles, and run time are frozen.", m_reportStyle);

            const float panelWidth = 340f;
            const float panelHeight = 72f;
            const float panelGap = 12f;
            var left = Screen.width * 0.5f - panelWidth - panelGap * 0.5f;
            var right = Screen.width * 0.5f + panelGap * 0.5f;
            var firstRow = Screen.height * 0.5f + 4f;
            var secondRow = firstRow + panelHeight + panelGap;
            _drawOptionPanel(
                new Rect(left, firstRow, panelWidth, panelHeight),
                m_cameraComfortIcon,
                "STEADY CAMERA",
                m_comfortSettings.CameraImpulseEnabled,
                $"{_binding("C", "Y")}  IMPULSE {(m_comfortSettings.CameraImpulseEnabled ? "ON" : "OFF")}");
            _drawOptionPanel(
                new Rect(right, firstRow, panelWidth, panelHeight),
                m_reducedFlashesIcon,
                "REDUCED FLASHES",
                m_comfortSettings.ReducedFlashesEnabled,
                $"{_binding("F", "D-PAD DOWN")}  REDUCTION {(m_comfortSettings.ReducedFlashesEnabled ? "ON" : "OFF")}");
            _drawOptionPanel(
                new Rect(left, secondRow, panelWidth, panelHeight),
                m_highContrastIcon,
                "HIGH CONTRAST",
                m_comfortSettings.HighContrastEnabled,
                $"{_binding("H", "D-PAD UP")}  CONTRAST {(m_comfortSettings.HighContrastEnabled ? "ON" : "OFF")}");
            _drawOptionPanel(
                new Rect(right, secondRow, panelWidth, panelHeight),
                m_audioLinkIcon,
                "SIGNAL AUDIO",
                m_comfortSettings.AudioEnabled,
                $"{_binding("M", "D-PAD LEFT")}  AUDIO {(m_comfortSettings.AudioEnabled ? "ON" : "MUTED")}");

            GUI.color = Color.white;
            GUI.Label(new Rect(0f, secondRow + panelHeight + 14f, Screen.width, 36f),
                $"PRESS {_binding("ESC", "GAMEPAD MENU")} TO RESUME", m_centerStyle);
        }

        private void _drawOptionPanel(Rect panel, Texture2D icon, string title, bool enabled, string state)
        {
            GUI.color = new Color(0.025f, 0.07f, 0.085f, 0.98f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(panel.x + 10f, panel.y + 9f, 54f, 54f), icon, ScaleMode.ScaleToFit, true);
            }

            GUI.Label(new Rect(panel.x + 74f, panel.y + 9f, panel.width - 84f, 24f), title, m_labelStyle);
            m_smallStyle.normal.textColor = enabled ? new Color(0.08f, 0.96f, 1f) : new Color(1f, 0.68f, 0.12f);
            GUI.Label(new Rect(panel.x + 74f, panel.y + 36f, panel.width - 84f, 25f), state, m_smallStyle);
        }

        private string _currentObjective()
        {
            if (!m_model.TowerOnline)
            {
                return "OBJECTIVE  Bring the tower online";
            }

            if (!m_model.CanExtract)
            {
                return $"OBJECTIVE  Recover salvage ({m_model.Salvage}/{RunModel.SalvageRequired})";
            }

            return "OBJECTIVE  Return to cyan extraction pad";
        }

        private string _runReport()
        {
            int totalSeconds = Mathf.FloorToInt(m_metrics.ElapsedSeconds);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"RUN REPORT   {minutes:00}:{seconds:00}   |   DEAD ZONE {m_metrics.DeadZoneSeconds:0.0}s   |   " +
                   $"SHOTS {m_metrics.ShotsFired}   |   HITS {m_metrics.SecurityHits}   |   " +
                   $"DRAINS {m_metrics.SapperPulses}   |   SIGNAL {Mathf.CeilToInt(m_model.Signal)}";
        }

        private string _sapperStatus()
        {
            if (!m_model.TowerOnline)
            {
                return "THREAT  SIGNAL SAPPER DORMANT";
            }

            if (!m_threats.IsSapperAlive)
            {
                return "THREAT  SIGNAL SAPPER PURGED";
            }

            return m_threats.IsSapperLatched
                ? $"THREAT  SAPPER DRAIN IN {m_threats.SapperPulseCooldown:0.0}s (-{RunModel.SapperPulseCost:0})"
                : "THREAT  SIGNAL SAPPER APPROACHING TOWER";
        }

        private string _contextPrompt()
        {
            if (!m_model.ShortcutOpen && DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.ShortcutPosition) < 1.9f)
            {
                return m_model.TowerOnline
                    ? $"[{_binding("E", "GAMEPAD X")}]  BURN {RunModel.ShortcutCost:0} SIGNAL FOR SHORTCUT"
                    : "SHORTCUT OFFLINE - ACTIVATE TOWER FIRST";
            }

            if (!m_model.TowerOnline && DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.TowerPosition) < 1.8f)
            {
                return $"[{_binding("E", "GAMEPAD X")}]  ACTIVATE SIGNAL TOWER  —  COST 10";
            }

            if (DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.ExtractionPosition) < 1.65f)
            {
                return m_model.CanExtract
                    ? $"[{_binding("E", "GAMEPAD X")}]  EXTRACT SALVAGE"
                    : $"EXTRACTION LOCKED  —  {RunModel.SalvageRequired - m_model.Salvage} SALVAGE MISSING";
            }

            return string.Empty;
        }

        private string _activeControlLegend()
        {
            return m_input.ActivePromptDevice == InputPromptDevice.Gamepad
                ? "GAMEPAD LINK\nLS Move  |  RS Aim\nRT / RB Fire  |  X Use\nMenu Pause  |  A Restart"
                : "KEYBOARD + MOUSE\nWASD Move  |  Mouse Aim\nLMB / Space Fire  |  E Use\nEsc Pause  |  R Restart";
        }

        private string _binding(string keyboardMouse, string gamepad)
        {
            return m_input.ActivePromptDevice == InputPromptDevice.Gamepad ? gamepad : keyboardMouse;
        }
    }
}
