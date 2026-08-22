using Reflex.Attributes;
using UnityEngine;
using UnityEngine.UI;

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
        bool HasBindingMatrixIcon { get; }
        bool HasBindingConflictIcon { get; }
        bool HasMovementRoutingIcon { get; }
        bool HasControlGlyphSet { get; }
        bool HasSignalReserveArt { get; }
        bool HasRunDebriefArt { get; }
        SignalReserveState CurrentSignalReserveState { get; }

        void Configure(RunModel model, RunMetrics metrics, DeadSignalWorld world, DeadSignalThreatController threats);
        void ShowFeedback(string message);
        void Tick(float dt);
    }

    /// <summary>Updates the serialized uGUI hierarchy authored in the DEAD SIGNAL HUD prefab.</summary>
    public sealed class DeadSignalHud : MonoBehaviour, IDeadSignalHud
    {
        [Header("Run HUD")]
        [SerializeField] private Canvas m_canvas;
        [SerializeField] private GameObject m_runHud;
        [SerializeField] private Image m_signalFill;
        [SerializeField] private Text m_signalText;
        [SerializeField] private Text m_salvageText;
        [SerializeField] private Text m_zoneText;
        [SerializeField] private Text m_objectiveText;
        [SerializeField] private Text m_threatText;
        [SerializeField] private Text m_controlLegendText;
        [SerializeField] private GameObject m_contextPrompt;
        [SerializeField] private Text m_contextPromptText;
        [SerializeField] private Text m_feedbackText;

        [Header("Outcome")]
        [SerializeField] private GameObject m_outcomeOverlay;
        [SerializeField] private Text m_outcomeTitle;
        [SerializeField] private Text m_outcomeDetail;
        [SerializeField] private Text m_runReportText;
        [SerializeField] private Text m_restartText;

        [Header("Pause")]
        [SerializeField] private GameObject m_pauseOverlay;
        [SerializeField] private Text m_resumeText;
        [SerializeField] private Text[] m_optionStateTexts;
        [SerializeField] private Text m_routingStatusText;
        [SerializeField] private Button[] m_rebindButtons;
        [SerializeField] private Text[] m_rebindButtonTexts;

        [Header("Authored Images")]
        [SerializeField] private RawImage m_pauseInsignia;
        [SerializeField] private RawImage m_cameraComfortIcon;
        [SerializeField] private RawImage m_reducedFlashesIcon;
        [SerializeField] private RawImage m_highContrastIcon;
        [SerializeField] private RawImage m_inputLinkIcon;
        [SerializeField] private RawImage m_audioLinkIcon;
        [SerializeField] private RawImage m_bindingMatrixIcon;
        [SerializeField] private RawImage m_bindingConflictIcon;
        [SerializeField] private RawImage m_movementRoutingIcon;
        [SerializeField] private RawImage[] m_controlGlyphs;
        [SerializeField] private RawImage m_runDebriefInsignia;

        private RunModel m_model;
        private RunMetrics m_metrics;
        private DeadSignalWorld m_world;
        private DeadSignalThreatController m_threats;
        private ICombatFeedback m_combatFeedback;
        private IComfortSettings m_comfortSettings;
        private IDeadSignalInput m_input;
        private SignalHudTuning m_signalHudTuning;
        private Texture2D m_runDebriefTexture;
        private float m_feedbackTimer;
        private float m_signalPulseTime;
        private string m_feedback = string.Empty;

        public bool HasPauseInsignia => _hasTexture(m_pauseInsignia);
        public bool HasCameraComfortIcon => _hasTexture(m_cameraComfortIcon);
        public bool HasReducedFlashesIcon => _hasTexture(m_reducedFlashesIcon);
        public bool HasHighContrastIcon => _hasTexture(m_highContrastIcon);
        public bool HasInputLinkIcon => _hasTexture(m_inputLinkIcon);
        public bool HasAudioLinkIcon => _hasTexture(m_audioLinkIcon);
        public bool HasBindingMatrixIcon => _hasTexture(m_bindingMatrixIcon);
        public bool HasBindingConflictIcon => _hasTexture(m_bindingConflictIcon);
        public bool HasMovementRoutingIcon => _hasTexture(m_movementRoutingIcon);
        public bool HasControlGlyphSet => m_controlGlyphs != null && m_controlGlyphs.Length == 5 &&
                                          System.Array.TrueForAll(m_controlGlyphs, _hasTexture);
        public bool HasSignalReserveArt => m_signalFill != null && m_signalFill.sprite != null;
        public bool HasRunDebriefArt => m_runDebriefTexture != null && _hasTexture(m_runDebriefInsignia);
        public SignalReserveState CurrentSignalReserveState { get; private set; }

        [Inject]
        private void _construct(ICombatFeedback combatFeedback, IComfortSettings comfortSettings, IDeadSignalInput input)
        {
            m_combatFeedback = combatFeedback;
            m_comfortSettings = comfortSettings;
            m_input = input;
        }

        void IDeadSignalHud.Configure(RunModel model, RunMetrics metrics, DeadSignalWorld world, DeadSignalThreatController threats)
        {
            m_model = model;
            m_metrics = metrics;
            m_world = world;
            m_threats = threats;
            m_signalHudTuning = Resources.Load<SignalHudTuning>("Tuning/SignalHudTuning");
            var signalSprite = Resources.Load<Sprite>("UI/SignalReserveConduit");
            m_runDebriefTexture = Resources.Load<Texture2D>("UI/RunDebriefInsignia");
            if (m_signalHudTuning == null || signalSprite == null || m_runDebriefTexture == null)
            {
                Debug.LogError("The authored Signal HUD tuning or reserve conduit art is missing.");
                return;
            }

            m_signalFill.sprite = signalSprite;
            m_runDebriefInsignia.texture = m_runDebriefTexture;
            _wireButtons();
            _refresh();
        }

        void IDeadSignalHud.ShowFeedback(string message)
        {
            m_feedback = message;
            m_feedbackTimer = 2.2f;
        }

        void IDeadSignalHud.Tick(float dt)
        {
            m_feedbackTimer = Mathf.Max(0f, m_feedbackTimer - dt);
            if (!m_combatFeedback.IsPaused)
            {
                m_signalPulseTime += Mathf.Max(0f, dt);
            }
            _refresh();
        }

        private void _wireButtons()
        {
            if (m_rebindButtons == null || m_rebindButtons.Length < 7)
            {
                return;
            }

            m_rebindButtons[0].onClick.AddListener(m_input.BeginMoveUpKeyboardRebind);
            m_rebindButtons[1].onClick.AddListener(m_input.BeginMoveDownKeyboardRebind);
            m_rebindButtons[2].onClick.AddListener(m_input.BeginMoveLeftKeyboardRebind);
            m_rebindButtons[3].onClick.AddListener(m_input.BeginMoveRightKeyboardRebind);
            m_rebindButtons[4].onClick.AddListener(m_input.BeginFireKeyboardRebind);
            m_rebindButtons[5].onClick.AddListener(m_input.BeginInteractKeyboardRebind);
            m_rebindButtons[6].onClick.AddListener(m_input.ResetKeyboardBindings);
        }

        private void _refresh()
        {
            if (m_model == null || m_world == null || m_threats == null)
            {
                return;
            }

            var paused = m_combatFeedback.IsPaused;
            var running = m_model.Outcome == RunOutcome.Running;
            m_runHud.SetActive(running && !paused);
            m_pauseOverlay.SetActive(paused);
            m_outcomeOverlay.SetActive(!running && !paused);
            _refreshRunHud();
            _refreshOutcome();
            _refreshPause();
        }

        private void _refreshRunHud()
        {
            var ratio = Mathf.Clamp01(m_model.Signal / RunModel.MaximumSignal);
            var presentation = SignalHudPresentation.Evaluate(m_model.Signal, RunModel.MaximumSignal,
                m_comfortSettings.ReducedFlashesEnabled, m_signalPulseTime, m_signalHudTuning);
            CurrentSignalReserveState = presentation.State;
            m_signalFill.fillAmount = ratio;
            var signalColor = presentation.Color;
            signalColor.a = presentation.Alpha;
            m_signalFill.color = signalColor;
            m_signalText.text = $"SIGNAL  {Mathf.CeilToInt(m_model.Signal):000}  //  {presentation.State.ToString().ToUpperInvariant()}";
            m_salvageText.text = $"SALVAGE  {m_model.Salvage}/{RunModel.SalvageRequired}";
            var powered = m_world.IsPowered(m_world.Player.position, m_model.TowerOnline);
            m_zoneText.text = powered ? "● POWERED TERRITORY" : "▲ DEAD ZONE — ACTIVE DRAIN";
            m_zoneText.color = powered ? new Color(0.05f, 0.95f, 1f) : new Color(1f, 0.22f, 0.18f);
            m_objectiveText.text = _currentObjective();
            m_threatText.text = _sapperStatus();
            m_controlLegendText.text = _activeControlLegend();
            var prompt = _contextPrompt();
            m_contextPrompt.SetActive(!string.IsNullOrEmpty(prompt));
            m_contextPromptText.text = prompt;
            m_feedbackText.gameObject.SetActive(m_feedbackTimer > 0f);
            m_feedbackText.text = m_feedback;
            m_feedbackText.color = m_feedback.Contains("DEAD") || m_feedback.Contains("SECURITY")
                ? new Color(1f, 0.25f, 0.2f) : new Color(0.1f, 0.95f, 1f);
        }

        private void _refreshOutcome()
        {
            if (m_model.Outcome == RunOutcome.Running)
            {
                return;
            }

            var victory = m_model.Outcome == RunOutcome.Victory;
            var debrief = RunDebrief.Evaluate(m_model, m_metrics);
            m_outcomeTitle.text = victory ? "SIGNAL RECOVERED" : "DRONE OFFLINE";
            m_outcomeTitle.color = victory ? new Color(0.08f, 0.96f, 1f) : new Color(1f, 0.08f, 0.06f);
            m_outcomeDetail.text = victory ? "Salvage extracted. The station lives a little longer." : "Signal depleted in the dark.";
            m_runReportText.text = $"DEBRIEF GRADE  {debrief.Grade}\n{debrief.Signal}   |   {debrief.Combat}\n" +
                                   $"{debrief.Exposure}   |   {debrief.Route}\n{_runReport()}";
            m_restartText.text = $"PRESS {_binding("R / ENTER", "GAMEPAD A")} TO RESTART";
        }

        private void _refreshPause()
        {
            if (!m_combatFeedback.IsPaused)
            {
                return;
            }

            m_optionStateTexts[0].text = $"{_binding("C", "Y")}  IMPULSE {(m_comfortSettings.CameraImpulseEnabled ? "ON" : "OFF")}";
            m_optionStateTexts[1].text = $"{_binding("F", "D-PAD DOWN")}  REDUCTION {(m_comfortSettings.ReducedFlashesEnabled ? "ON" : "OFF")}";
            m_optionStateTexts[2].text = $"{_binding("H", "D-PAD UP")}  CONTRAST {(m_comfortSettings.HighContrastEnabled ? "ON" : "OFF")}";
            m_optionStateTexts[3].text = $"{_binding("M", "D-PAD LEFT")}  AUDIO {(m_comfortSettings.AudioEnabled ? "ON" : "MUTED")}";
            var conflict = !string.IsNullOrEmpty(m_input.RebindStatusMessage);
            m_routingStatusText.text = conflict ? m_input.RebindStatusMessage :
                m_input.IsRebinding ? "PRESS A KEY  |  ESC CANCELS" : "PERSISTED KEYBOARD BINDINGS";
            var labels = new[] { $"UP  {m_input.MoveUpKeyboardBinding}", $"DOWN  {m_input.MoveDownKeyboardBinding}",
                $"LEFT  {m_input.MoveLeftKeyboardBinding}", $"RIGHT  {m_input.MoveRightKeyboardBinding}",
                $"FIRE  {m_input.FireKeyboardBinding}", $"USE  {m_input.InteractKeyboardBinding}", "RESET" };
            for (var i = 0; i < m_rebindButtons.Length; i++)
            {
                m_rebindButtons[i].interactable = !m_input.IsRebinding;
                m_rebindButtonTexts[i].text = labels[i];
            }

            m_resumeText.text = $"PRESS {_binding("ESC", "GAMEPAD MENU")} TO RESUME";
        }

        private string _currentObjective()
        {
            if (!m_model.TowerOnline) return "OBJECTIVE  Bring the tower online";
            return !m_model.CanExtract
                ? $"OBJECTIVE  Recover salvage ({m_model.Salvage}/{RunModel.SalvageRequired})"
                : "OBJECTIVE  Return to cyan extraction pad";
        }

        private string _runReport()
        {
            var totalSeconds = Mathf.FloorToInt(m_metrics.ElapsedSeconds);
            return $"RUN REPORT   {totalSeconds / 60:00}:{totalSeconds % 60:00}   |   DEAD ZONE {m_metrics.DeadZoneSeconds:0.0}s   |   " +
                   $"SHOTS {m_metrics.ShotsFired}   |   HITS {m_metrics.SecurityHits}   |   DRAINS {m_metrics.SapperPulses}   |   " +
                   $"SIGNAL {Mathf.CeilToInt(m_model.Signal)}";
        }

        private string _sapperStatus()
        {
            if (!m_model.TowerOnline) return "THREAT  SIGNAL SAPPER DORMANT";
            if (!m_threats.IsSapperAlive) return "THREAT  SIGNAL SAPPER PURGED";
            return m_threats.IsSapperLatched
                ? $"THREAT  SAPPER DRAIN IN {m_threats.SapperPulseCooldown:0.0}s (-{RunModel.SapperPulseCost:0})"
                : "THREAT  SIGNAL SAPPER APPROACHING TOWER";
        }

        private string _contextPrompt()
        {
            if (!m_model.ShortcutOpen && DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.ShortcutPosition) < 1.9f)
                return m_model.TowerOnline ? $"[{_binding("E", "GAMEPAD X")}]  BURN {RunModel.ShortcutCost:0} SIGNAL FOR SHORTCUT"
                    : "SHORTCUT OFFLINE - ACTIVATE TOWER FIRST";
            if (!m_model.TowerOnline && DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.TowerPosition) < 1.8f)
                return $"[{_binding("E", "GAMEPAD X")}]  ACTIVATE SIGNAL TOWER  —  COST 10";
            if (DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.ExtractionPosition) < 1.65f)
                return m_model.CanExtract ? $"[{_binding("E", "GAMEPAD X")}]  EXTRACT SALVAGE"
                    : $"EXTRACTION LOCKED  —  {RunModel.SalvageRequired - m_model.Salvage} SALVAGE MISSING";
            return string.Empty;
        }

        private string _activeControlLegend()
        {
            return m_input.ActivePromptDevice == InputPromptDevice.Gamepad ? "GAMEPAD  LS / RS  |  RT-RB / X  |  MENU-A"
                : $"KEYBOARD  {m_input.MoveUpKeyboardBinding}{m_input.MoveLeftKeyboardBinding}{m_input.MoveDownKeyboardBinding}" +
                  $"{m_input.MoveRightKeyboardBinding} / MOUSE  |  LMB-{m_input.FireKeyboardBinding} / {m_input.InteractKeyboardBinding}  |  ESC-R";
        }

        private string _binding(string keyboardMouse, string gamepad) =>
            m_input.ActivePromptDevice == InputPromptDevice.Gamepad ? gamepad : keyboardMouse;

        private static bool _hasTexture(RawImage image) => image != null && image.texture != null;
    }
}
