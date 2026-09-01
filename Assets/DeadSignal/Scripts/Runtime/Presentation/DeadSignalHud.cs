using Reflex.Attributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DeadSignal.Combat;
using DeadSignal.Missions;
using DeadSignal.Player;
using DeadSignal.Salvage;
using DeadSignal.World;

namespace DeadSignal.Presentation
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
        int CurrentMissionPhase { get; }
        string CurrentMissionObjective { get; }

        void Configure(RunModel model, RunMetrics metrics, DeadSignalWorld world, DeadSignalThreatController threats,
            DeadSignalSalvageController salvage, ExtractionUplink extractionUplink, SignalOverclockChoice overclockChoice);
        void ShowFeedback(string message);
        void SetDebugObjective(string objective);
        void SetDebugMenuVisible(bool visible);
        void SetMainMenuVisible(bool visible);
        void ConfigureShellActions(System.Action resumeRun, System.Action restartRun, System.Action returnToMenu);
        void SetTraversalSignalState(float drainPerSecond, bool powered);
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
        [SerializeField] private CanvasGroup m_outcomeCanvasGroup;
        [SerializeField] private ProductShellTransitionTuning m_transitionTuning;
        [SerializeField] private OutcomePresentation m_outcomePresentation;

        [Header("Pause")]
        [SerializeField] private GameObject m_pauseOverlay;
        [SerializeField] private Text m_resumeText;
        [SerializeField] private Text[] m_optionStateTexts;
        [SerializeField] private Text m_routingStatusText;
        [SerializeField] private Button[] m_rebindButtons;
        [SerializeField] private Text[] m_rebindButtonTexts;
        [SerializeField] private PauseMenuPresentation m_pauseMenuPresentation;

        [Header("Shell Navigation")]
        [SerializeField] private Button m_pauseResumeButton;
        [SerializeField] private Button m_pauseMainMenuButton;
        [SerializeField] private Button m_outcomeRestartButton;
        [SerializeField] private Button m_outcomeMainMenuButton;

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
        private DeadSignalSalvageController m_salvage;
        private ExtractionUplink m_extractionUplink;
        private SignalOverclockChoice m_overclockChoice;
        private ICombatFeedback m_combatFeedback;
        private IComfortSettings m_comfortSettings;
        private IDeadSignalInput m_input;
        private SignalHudTuning m_signalHudTuning;
        private SignalReserveInstrument m_signalInstrument;
        private ThreatHudInstrument m_threatInstrument;
        private ObjectiveBeaconHud m_objectiveBeacon;
        private InteractionPromptHud m_interactionPrompt;
        private RectTransform m_contextPromptRect;
        private RectTransform m_contextPromptParent;
        private Vector2 m_contextPromptAnchorMin;
        private Vector2 m_contextPromptAnchorMax;
        private Vector2 m_contextPromptPivot;
        private Vector2 m_contextPromptPosition;
        private Texture2D m_runDebriefTexture;
        private float m_feedbackTimer;
        private float m_signalPulseTime;
        private float m_signalDrainPerSecond;
        private float m_outcomeTransitionElapsed;
        private int m_feedbackPriority;
        private bool m_resultRecorded;
        private bool m_debugMenuVisible;
        private bool m_mainMenuVisible;
        private bool m_pauseNavigationVisible;
        private bool m_outcomeNavigationVisible;
        private bool m_isPowered;
        private string m_personalBestText = string.Empty;
        private string m_feedback = string.Empty;
        private string m_debugObjective = string.Empty;

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
        public int CurrentMissionPhase { get; private set; }
        public string CurrentMissionObjective => m_objectiveText == null ? string.Empty : m_objectiveText.text;
        public bool IsDebugMenuVisible => m_debugMenuVisible;
        public bool IsPauseOverlayVisible => m_pauseOverlay != null && m_pauseOverlay.activeSelf;
        public bool IsOutcomeTransitioning { get; private set; }
        public float OutcomeTransitionOpacity => m_outcomeCanvasGroup == null ? 0f : m_outcomeCanvasGroup.alpha;

        [Inject]
        private void _construct(ICombatFeedback combatFeedback, IComfortSettings comfortSettings, IDeadSignalInput input)
        {
            m_combatFeedback = combatFeedback;
            m_comfortSettings = comfortSettings;
            m_input = input;
        }

        void IDeadSignalHud.Configure(RunModel model, RunMetrics metrics, DeadSignalWorld world, DeadSignalThreatController threats,
            DeadSignalSalvageController salvage, ExtractionUplink extractionUplink, SignalOverclockChoice overclockChoice)
        {
            m_model = model;
            m_metrics = metrics;
            m_world = world;
            m_threats = threats;
            m_salvage = salvage;
            m_extractionUplink = extractionUplink;
            m_overclockChoice = overclockChoice;
            m_signalHudTuning = Resources.Load<SignalHudTuning>("Tuning/SignalHudTuning");
            m_signalInstrument = m_signalFill.GetComponentInParent<SignalReserveInstrument>();
            m_threatInstrument = m_threatText.GetComponentInChildren<ThreatHudInstrument>(true);
            m_objectiveBeacon = GetComponent<ObjectiveBeaconHud>();
            m_interactionPrompt = m_contextPrompt.GetComponent<InteractionPromptHud>();
            m_pauseMenuPresentation.Configure(m_input);
            m_contextPromptRect = m_contextPrompt.transform as RectTransform;
            m_contextPromptParent = m_contextPromptRect.parent as RectTransform;
            m_contextPromptAnchorMin = m_contextPromptRect.anchorMin;
            m_contextPromptAnchorMax = m_contextPromptRect.anchorMax;
            m_contextPromptPivot = m_contextPromptRect.pivot;
            m_contextPromptPosition = m_contextPromptRect.anchoredPosition;
            var signalSprite = Resources.Load<Sprite>("UI/SignalReserveConduit");
            m_runDebriefTexture = Resources.Load<Texture2D>("UI/RouteLedgerInsignia");
            if (m_runReportText != null)
            {
                m_runReportText.resizeTextForBestFit = true;
                m_runReportText.resizeTextMinSize = 8;
                m_runReportText.resizeTextMaxSize = 13;
            }
            if (m_signalHudTuning == null || signalSprite == null || m_runDebriefTexture == null ||
                m_signalInstrument == null || !m_signalInstrument.IsConfigured)
            {
                Debug.LogError("The authored Signal HUD tuning or reserve conduit art is missing.");
                return;
            }

            m_signalFill.sprite = signalSprite;
            m_runDebriefInsignia.texture = m_runDebriefTexture;
            _configureRunHudReadability();
            _wireButtons();
            _refresh();
        }

        void IDeadSignalHud.ShowFeedback(string message)
        {
            var priority = _feedbackPriority(message);
            if (m_feedbackTimer > 0.35f && priority < m_feedbackPriority)
            {
                return;
            }

            m_feedback = message;
            m_feedbackTimer = 2.2f;
            m_feedbackPriority = priority;
        }

        void IDeadSignalHud.SetDebugObjective(string objective)
        {
            m_debugObjective = objective ?? string.Empty;
            _refresh();
        }

        void IDeadSignalHud.SetDebugMenuVisible(bool visible)
        {
            m_debugMenuVisible = visible;
            _refresh();
        }

        void IDeadSignalHud.SetMainMenuVisible(bool visible)
        {
            m_mainMenuVisible = visible;
            _refresh();
        }

        void IDeadSignalHud.ConfigureShellActions(System.Action resumeRun, System.Action restartRun, System.Action returnToMenu)
        {
            m_pauseResumeButton.onClick.AddListener(() => resumeRun());
            m_pauseMainMenuButton.onClick.AddListener(() => returnToMenu());
            m_outcomeRestartButton.onClick.AddListener(() => restartRun());
            m_outcomeMainMenuButton.onClick.AddListener(() => returnToMenu());
        }

        void IDeadSignalHud.SetTraversalSignalState(float drainPerSecond, bool powered)
        {
            m_signalDrainPerSecond = Mathf.Max(0f, drainPerSecond);
            m_isPowered = powered;
        }

        void IDeadSignalHud.Tick(float dt)
        {
            m_feedbackTimer = Mathf.Max(0f, m_feedbackTimer - dt);
            if (m_feedbackTimer <= 0f)
            {
                m_feedbackPriority = 0;
            }
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
            m_runHud.SetActive(running && !paused && !m_debugMenuVisible && !m_mainMenuVisible);
            m_pauseOverlay.SetActive(paused && !m_debugMenuVisible && !m_mainMenuVisible);
            m_outcomeOverlay.SetActive(!running && !paused && !m_debugMenuVisible && !m_mainMenuVisible);
            _refreshOutcomeTransition(!running && !paused && !m_debugMenuVisible && !m_mainMenuVisible);
            _refreshShellNavigation(paused, running);
            _refreshRunHud();
            _refreshOutcome();
            _refreshPause();
        }

        private void _refreshRunHud()
        {
            var presentation = SignalHudPresentation.Evaluate(m_model.Signal, RunModel.MaximumSignal,
                m_comfortSettings.ReducedFlashesEnabled, m_signalPulseTime, m_signalHudTuning);
            CurrentSignalReserveState = presentation.State;
            var transaction = _signalTransactionPreview();
            m_signalInstrument.Apply(
                m_model.Signal,
                RunModel.MaximumSignal,
                presentation,
                m_signalDrainPerSecond,
                m_isPowered,
                transaction.Cost,
                transaction.Name,
                Time.unscaledDeltaTime,
                m_signalHudTuning);
            var chainText = m_salvage.ChainCount > 0 && m_model.Salvage < RunModel.SalvageRequired
                ? $"  //  CHAIN x{m_salvage.ChainCount}  {m_salvage.ChainSecondsRemaining:0.0}s"
                : string.Empty;
            var overclockText = m_overclockChoice.Selected == SignalOverclock.None
                ? string.Empty
                : $"  //  {_overclockName(m_overclockChoice.Selected)}";
            var auxiliaryText = m_overclockChoice.SelectedAuxiliary == SignalAuxiliaryOverclock.None
                ? string.Empty
                : $"  //  {_auxiliaryOverclockName()}";
            var synergyText = m_overclockChoice.Synergy == SignalOverclockSynergy.None
                ? string.Empty
                : $"  //  {_overclockSynergyName()}";
            var weaponText = m_overclockChoice.SelectedWeapon == SignalWeaponOverclock.None
                ? string.Empty
                : $"  //  {_weaponOverclockName(m_overclockChoice.SelectedWeapon)}" +
                  (m_overclockChoice.IsWeaponEvolved ? " EVOLVED" : string.Empty);
            var optionalText = m_model.OptionalSalvageSecured ? "  //  OPTIONAL CACHE SECURED" : string.Empty;
            m_salvageText.text =
                $"SALVAGE  {m_model.Salvage}/{RunModel.SalvageRequired}{chainText}{overclockText}{auxiliaryText}" +
                $"{synergyText}{weaponText}{optionalText}";
            var guidance = MissionGuidance.Evaluate(m_model, m_threats.IsSapperAlive, m_threats.IsSapperLatched,
                m_threats.SapperPulseCooldown);
            CurrentMissionPhase = guidance.Phase;
            m_objectiveText.text = !string.IsNullOrEmpty(m_debugObjective)
                ? m_debugObjective
                : m_overclockChoice.IsWeaponPending
                ? $"RELAY WEAPON CALIBRATION\nFIRE [{m_input.FireKeyboardBinding}]  PIERCING PULSE — STRIKE TWO THREATS\n" +
                  $"USE [{m_input.InteractKeyboardBinding}]  CONTROLLED RICOCHET — REDIRECT FROM COVER"
                : m_overclockChoice.IsPrimaryPending
                ? $"SIGNAL OVERCLOCK AVAILABLE\nFIRE [{m_input.FireKeyboardBinding}]  CHAIN ARC — BOLTS JUMP\n" +
                  $"USE [{m_input.InteractKeyboardBinding}]  OVERDRIVE — MOVE FASTER"
                : m_overclockChoice.IsAuxiliaryPending
                ? $"AUXILIARY OVERCLOCK AVAILABLE\nFIRE [{m_input.FireKeyboardBinding}]  CAPACITOR — LOW-SIGNAL REFILL\n" +
                  $"USE [{m_input.InteractKeyboardBinding}]  SHIELD — NEGATE ONE THREAT"
                : _isExtractionUplinkChoiceAvailable()
                ? $"CHOOSE EXTRACTION LINK{_extractionCountertraceHeader()}\nFIRE [{m_input.FireKeyboardBinding}]  OVERDRIVE — " +
                  $"{m_extractionUplink.OverdriveDuration:0.##}s / −{m_extractionUplink.OverdriveSignalCost:0} / " +
                  $"PURGE +{m_extractionUplink.OverdrivePurgeAcceleration:0.##}s / PREDICTIVE SWEEP\n" +
                  $"USE [{m_input.InteractKeyboardBinding}]  STABLE — {m_extractionUplink.StableDuration:0.##}s / FREE / " +
                  $"PURGE +{m_extractionUplink.StablePurgeAcceleration:0.##}s / CENTERED SWEEP"
                : m_extractionUplink.IsActive
                ? $"PHASE 3/3  //  {m_extractionUplink.Mode.ToString().ToUpperInvariant()} UPLINK\n" +
                  $"SURVIVE PURSUIT  {m_extractionUplink.SecondsRemaining:0.0}s\n" +
                  _extractionPursuitAdvisory()
                : m_salvage.IsOptionalCacheAvailable
                ? $"PHASE 3/3  //  {(m_model.PoweredWithdrawalComplete ? "EXTRACTION READY" : "WITHDRAWAL ACTIVE")}\n" +
                  $"{(m_model.PoweredWithdrawalComplete ? "RETURN TO DOCK" : "FOLLOW CYAN RETURN")} OR RAID OPTIONAL CACHE  " +
                  $"{m_salvage.OptionalCacheDistance:0}m\n" +
                  $"GREED +{m_salvage.OptionalCacheSignalReward:0} SIGNAL — {_optionalGreedCountertrace()}"
                : $"PHASE {guidance.Phase}/3  //  {guidance.Title}\n{guidance.Action}\n{guidance.Advisory}";
            if (m_threatInstrument != null)
            {
                m_threatInstrument.Apply(
                    m_model,
                    m_threats,
                    m_extractionUplink.IsActive,
                    m_comfortSettings.ReducedFlashesEnabled,
                    Time.unscaledDeltaTime);
            }
            else
            {
                m_threatText.text = _threatStatus();
            }
            m_controlLegendText.text = _activeControlLegend();
            var prompt = _contextPrompt();
            if (m_interactionPrompt != null)
            {
                m_interactionPrompt.Apply(prompt, Time.unscaledDeltaTime);
            }
            else
            {
                m_contextPrompt.SetActive(prompt.IsVisible);
                m_contextPromptText.text = prompt.PrimaryAction;
            }
            _positionContextPrompt(prompt.IsVisible);
            m_feedbackText.gameObject.SetActive(m_feedbackTimer > 0f);
            m_feedbackText.text = m_feedback;
            m_feedbackText.color = m_feedback.Contains("DEAD") || m_feedback.Contains("SECURITY")
                ? new Color(1f, 0.25f, 0.2f) : new Color(0.1f, 0.95f, 1f);
        }

        private string _optionalGreedCountertrace()
        {
            return m_overclockChoice.SelectedWeapon switch
            {
                SignalWeaponOverclock.PiercingPulse => "COUNTERTRACE: CROSS-LANE SWEEP AT EXTRACTION",
                SignalWeaponOverclock.ControlledRicochet => "COUNTERTRACE: COVER FLUSH AT EXTRACTION",
                _ => "SECURITY REMAINS ACTIVE"
            };
        }

        private string _extractionCountertraceHeader()
        {
            if (!m_model.OptionalSalvageSecured)
            {
                return string.Empty;
            }

            return m_overclockChoice.SelectedWeapon switch
            {
                SignalWeaponOverclock.PiercingPulse => "  //  QUENCH CROSS-LANE COUNTERTRACE",
                SignalWeaponOverclock.ControlledRicochet => "  //  QUENCH COVER-FLUSH COUNTERTRACE",
                _ => string.Empty
            };
        }

        private string _extractionPursuitAdvisory()
        {
            return m_threats.CurrentExtractionSuppressionProfile switch
            {
                ExtractionSuppressionProfile.PiercingCrossLane => "CROSS-LANE SWEEP — TAKE THE OPPOSITE EXIT",
                ExtractionSuppressionProfile.RicochetCoverFlush => "COVER FLUSH — LEAVE YOUR CURRENT ANCHOR",
                _ => m_extractionUplink.Mode == ExtractionUplinkMode.Overdrive
                    ? "BREAK YOUR RETREAT LINE — PREDICTIVE SWEEP INBOUND"
                    : "LEAVE THE LOCKED RING — FIGHTING ADVANCES THE LINK"
            };
        }

        private void _refreshOutcome()
        {
            if (m_model.Outcome == RunOutcome.Running)
            {
                return;
            }

            var victory = m_model.Outcome == RunOutcome.Victory;
            _recordPersonalBest();
            m_outcomePresentation.Present(m_model.Outcome, m_comfortSettings.ReducedFlashesEnabled);
            if (victory)
            {
                var debrief = RunDebrief.Evaluate(m_model, m_metrics);
                m_outcomeTitle.text = "MISSION COMPLETE";
                m_outcomeTitle.color = new Color(0.08f, 0.96f, 1f);
                m_outcomeDetail.text = "STATION RESTARTED  //  NETWORK EXTENDED  //  SIGNAL CORE REBUILT  //  EXTRACTION SECURED";
                m_runReportText.text = $"{debrief.Mission}  //  GRADE {debrief.Grade}  //  {m_personalBestText}\n" +
                                       $"{debrief.Station}  //  {debrief.Route}\n" +
                                       $"{debrief.CombatHighlight}  //  {debrief.SignalHighlight}\n" +
                                       _victoryBuildSummary();
                m_restartText.text = $"RESTART RUN  {_binding("R / ENTER", "GAMEPAD A")}   |   MAIN MENU AVAILABLE";
                return;
            }

            var failure = RunFailureDebrief.Evaluate(m_model, m_metrics);
            m_outcomeTitle.text = "MISSION LOST";
            m_outcomeTitle.color = new Color(1f, 0.08f, 0.06f);
            m_outcomeDetail.text = failure.Cause;
            m_runReportText.text = $"{failure.Progress}\n{failure.Summary}\n{failure.Coaching}";
            m_restartText.text = $"RESTART RUN  {_binding("R / ENTER", "GAMEPAD A")}   |   MAIN MENU AVAILABLE";
        }

        private void _refreshOutcomeTransition(bool outcomeVisible)
        {
            if (!outcomeVisible)
            {
                IsOutcomeTransitioning = false;
                m_outcomeTransitionElapsed = 0f;
                m_outcomeCanvasGroup.alpha = 0f;
                m_outcomeCanvasGroup.interactable = false;
                m_outcomeCanvasGroup.blocksRaycasts = false;
                return;
            }

            if (!m_outcomeNavigationVisible && !IsOutcomeTransitioning)
            {
                IsOutcomeTransitioning = true;
                m_outcomeTransitionElapsed = 0f;
                m_outcomeCanvasGroup.alpha = 0f;
                m_outcomeCanvasGroup.interactable = false;
                m_outcomeCanvasGroup.blocksRaycasts = false;
            }

            if (!IsOutcomeTransitioning)
            {
                return;
            }

            var duration = m_transitionTuning.Duration(m_comfortSettings.ReducedFlashesEnabled);
            m_outcomeTransitionElapsed += Time.unscaledDeltaTime;
            m_outcomeCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m_outcomeTransitionElapsed / duration));
            if (m_outcomeTransitionElapsed < duration)
            {
                return;
            }

            m_outcomeCanvasGroup.alpha = 1f;
            m_outcomeCanvasGroup.interactable = true;
            m_outcomeCanvasGroup.blocksRaycasts = true;
            IsOutcomeTransitioning = false;
        }

        private void _refreshShellNavigation(bool paused, bool running)
        {
            var pauseVisible = paused && running && !m_debugMenuVisible && !m_mainMenuVisible;
            var outcomeVisible = !running && !paused && !m_debugMenuVisible && !m_mainMenuVisible;
            if (pauseVisible && !m_pauseNavigationVisible)
            {
                EventSystem.current?.SetSelectedGameObject(m_pauseResumeButton.gameObject);
            }
            else if (outcomeVisible && !m_outcomeNavigationVisible)
            {
                EventSystem.current?.SetSelectedGameObject(m_outcomeRestartButton.gameObject);
            }

            m_pauseNavigationVisible = pauseVisible;
            m_outcomeNavigationVisible = outcomeVisible;
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
                m_input.IsRebinding ? "PRESS A KEY  |  ESC CANCELS" :
                "PERSISTED BINDINGS  |  G GUIDANCE  |  V DIFFICULTY";
            var labels = new[] { $"UP  {m_input.MoveUpKeyboardBinding}", $"DOWN  {m_input.MoveDownKeyboardBinding}",
                $"LEFT  {m_input.MoveLeftKeyboardBinding}", $"RIGHT  {m_input.MoveRightKeyboardBinding}",
                $"FIRE  {m_input.FireKeyboardBinding}", $"USE  {m_input.InteractKeyboardBinding}", "RESET" };
            for (var i = 0; i < m_rebindButtons.Length; i++)
            {
                m_rebindButtons[i].interactable = !m_input.IsRebinding;
                m_rebindButtonTexts[i].text = labels[i];
            }

            m_resumeText.text = $"PRESS {_binding("ESC", "GAMEPAD MENU")} TO RESUME";
            m_pauseMenuPresentation.Apply(!m_pauseNavigationVisible);
        }

        private string _runReport()
        {
            var totalSeconds = Mathf.FloorToInt(m_metrics.ElapsedSeconds);
            return $"RUN REPORT   {totalSeconds / 60:00}:{totalSeconds % 60:00}   |   DEAD ZONE {m_metrics.DeadZoneSeconds:0.0}s   |   " +
                   $"SHOTS {m_metrics.ShotsFired}   |   HITS {m_metrics.SecurityHits}   |   DRAINS {m_metrics.SapperPulses}   |   " +
                   $"PURGES {m_metrics.ThreatsPurged}   |   RECLAIMED {m_metrics.SignalRecovered:0}   |   " +
                   $"BEST CHAIN x{m_metrics.BestSalvageChain}   |   CHAIN SIGNAL {m_metrics.SalvageSignalRecovered:0}   |   " +
                   $"SIGNAL {Mathf.CeilToInt(m_model.Signal)}";
        }

        private string _victoryBuildSummary()
        {
            var primary = m_overclockChoice.Selected == SignalOverclock.None
                ? "OVERCLOCK: STANDARD"
                : _overclockName(m_overclockChoice.Selected);
            var auxiliary = m_overclockChoice.SelectedAuxiliary switch
            {
                SignalAuxiliaryOverclock.EmergencyCapacitor => "AUXILIARY: EMERGENCY CAPACITOR",
                SignalAuxiliaryOverclock.FeedbackShield => "AUXILIARY: FEEDBACK SHIELD",
                _ => "AUXILIARY: NONE"
            };
            var weapon = m_overclockChoice.SelectedWeapon == SignalWeaponOverclock.None
                ? "WEAPON: SIGNAL BOLT"
                : _weaponOverclockName(m_overclockChoice.SelectedWeapon) +
                  (m_overclockChoice.IsWeaponEvolved ? " EVOLVED" : string.Empty);
            return $"BUILD  {primary}  //  {auxiliary}  //  {weapon}";
        }

        private void _recordPersonalBest()
        {
            if (m_resultRecorded)
            {
                return;
            }

            m_resultRecorded = true;
            const string bestTimeKey = "DeadSignal.PersonalBest.VictorySeconds";
            const string bestGradeKey = "DeadSignal.PersonalBest.Grade";
            var previousBest = PlayerPrefs.GetFloat(bestTimeKey, float.PositiveInfinity);
            if (m_model.Outcome == RunOutcome.Victory && m_metrics.ElapsedSeconds < previousBest)
            {
                PlayerPrefs.SetFloat(bestTimeKey, m_metrics.ElapsedSeconds);
                PlayerPrefs.SetString(bestGradeKey, RunDebrief.Evaluate(m_model, m_metrics).Grade);
                PlayerPrefs.Save();
                m_personalBestText = "NEW PERSONAL BEST — FASTEST RECOVERY";
                return;
            }

            m_personalBestText = float.IsPositiveInfinity(previousBest)
                ? "PERSONAL BEST — COMPLETE A RECOVERY TO ESTABLISH"
                : $"PERSONAL BEST  {Mathf.FloorToInt(previousBest) / 60:00}:{Mathf.FloorToInt(previousBest) % 60:00}  //  " +
                  $"GRADE {PlayerPrefs.GetString(bestGradeKey, "—")}";
        }

        private static int _feedbackPriority(string message)
        {
            if (message.Contains("CRITICAL") || message.Contains("DEAD ZONE") || message.Contains("AWAKENED")) return 3;
            if (message.Contains("CHOOSE") || message.Contains("EXTRACTION") || message.Contains("TOWER")) return 2;
            return 1;
        }

        private string _threatStatus()
        {
            if (!m_model.TowerOnline)
            {
                return $"SECURITY DORMANT  //  BOUNTIES W+{m_threats.WardenSignalReward:0} I+{m_threats.InterceptorSignalReward:0} " +
                       $"S+{m_threats.SapperSignalReward:0} X+{m_threats.SuppressorSignalReward:0}";
            }

            var warden = m_threats.IsWardenAlive
                ? m_threats.IsWardenScreeningSapper
                    ? $"WARDEN {m_threats.WardenHealth:0}/{m_threats.WardenMaximumHealth:0} SCREENING SAPPER (+{m_threats.WardenSignalReward:0})"
                    : $"WARDEN {m_threats.WardenHealth:0}/{m_threats.WardenMaximumHealth:0} PURSUING (+{m_threats.WardenSignalReward:0})"
                : "WARDEN PURGED";
            var sapper = !m_threats.IsSapperAlive
                ? "SAPPER PURGED"
                : m_threats.IsSapperLatched
                    ? $"SAPPER {m_threats.SapperHealth:0}/{m_threats.SapperMaximumHealth:0} DRAIN {m_threats.SapperPulseCooldown:0.0}s" +
                      $"{(m_threats.IsInterceptorCuttingSapperFlank ? " / FLANK CUT" : string.Empty)} (+{m_threats.SapperSignalReward:0})"
                    : $"SAPPER {m_threats.SapperHealth:0}/{m_threats.SapperMaximumHealth:0} APPROACHING (+{m_threats.SapperSignalReward:0})";
            var interceptor = !m_threats.IsInterceptorAlive
                ? "INTERCEPTOR CLEAR"
                : m_threats.IsInterceptorRecovering
                    ? $"INTERCEPTOR {m_threats.InterceptorHealth:0}/{m_threats.InterceptorMaximumHealth:0} EXPOSED " +
                      $"{m_threats.InterceptorRecoverySecondsRemaining:0.0}s (+{m_threats.InterceptorSignalReward:0})"
                    : m_threats.IsInterceptorCharging
                    ? $"INTERCEPTOR {m_threats.InterceptorHealth:0}/{m_threats.InterceptorMaximumHealth:0} LOCKING (+{m_threats.InterceptorSignalReward:0})"
                    : m_threats.IsInterceptorCuttingSapperFlank
                        ? $"INTERCEPTOR {m_threats.InterceptorHealth:0}/{m_threats.InterceptorMaximumHealth:0} SAPPER FLANK " +
                          $"(+{m_threats.InterceptorSignalReward:0})"
                        : $"INTERCEPTOR {m_threats.InterceptorHealth:0}/{m_threats.InterceptorMaximumHealth:0} FLANKING " +
                          $"(+{m_threats.InterceptorSignalReward:0})";
            var suppressor = !m_threats.IsSuppressorAlive
                ? "SUPPRESSOR CLEAR"
                : m_threats.IsSuppressorFieldActive
                    ? $"SUPPRESSOR {m_threats.SuppressorHealth:0}/{m_threats.SuppressorMaximumHealth:0} FIELD ACTIVE (+{m_threats.SuppressorSignalReward:0})"
                    : $"SUPPRESSOR {m_threats.SuppressorHealth:0}/{m_threats.SuppressorMaximumHealth:0} POSITIONING (+{m_threats.SuppressorSignalReward:0})";
            var entry = m_threats.PendingReinforcement == SecurityReinforcement.None
                ? string.Empty
                : m_threats.IsReinforcementEntryBlocked
                    ? $"  {m_threats.PendingReinforcement.ToString().ToUpperInvariant()} ENTRY BLOCKED — CLEAR GATE"
                    : $"  {m_threats.PendingReinforcement.ToString().ToUpperInvariant()} ENTRY {m_threats.ReinforcementEntryCountdown:0.0}s";
            var trace = m_threats.IsDeadZoneTraceActive
                ? m_threats.IsDeadZoneTraceCooling
                    ? "  TRACE COOLING"
                    : $"  TRACE {m_threats.DeadZoneTraceSecondsRemaining:0.0}s"
                : string.Empty;
            var alert = m_extractionUplink.IsActive ? "PURSUIT" : $"ALERT {m_threats.EscalationTier}/{RunModel.SalvageRequired}";
            var priorityThreat = m_threats.IsSapperAlive ? sapper : m_threats.IsInterceptorAlive ? interceptor :
                m_threats.IsSuppressorAlive ? suppressor : warden;
            return $"{alert}  //  {priorityThreat}  //  RESERVE {m_threats.ReinforcementsRemaining}{trace}{entry}";
        }

        private void _configureRunHudReadability()
        {
            m_signalText.fontSize = Mathf.Max(m_signalText.fontSize, 20);
            m_salvageText.fontSize = Mathf.Max(m_salvageText.fontSize, 17);
            m_zoneText.fontSize = Mathf.Max(m_zoneText.fontSize, 17);
            m_objectiveText.fontSize = Mathf.Max(m_objectiveText.fontSize, 20);
            m_threatText.fontSize = Mathf.Max(m_threatText.fontSize, 16);
            m_controlLegendText.fontSize = Mathf.Max(m_controlLegendText.fontSize, 15);
            m_feedbackText.fontSize = Mathf.Max(m_feedbackText.fontSize, 20);
        }

        private void _positionContextPrompt(bool visible)
        {
            var hasModalChoice = m_overclockChoice.IsPrimaryPending || m_overclockChoice.IsAuxiliaryPending ||
                                 m_overclockChoice.IsWeaponPending || _isExtractionUplinkChoiceAvailable();
            var attachToObjective = visible && !hasModalChoice && m_objectiveBeacon != null &&
                                    m_objectiveBeacon.IsObjectiveIndicatorCompact &&
                                    m_objectiveBeacon.ObjectiveIndicatorIconRect != null &&
                                    m_contextPromptParent != null;
            if (!attachToObjective)
            {
                m_contextPromptRect.anchorMin = m_contextPromptAnchorMin;
                m_contextPromptRect.anchorMax = m_contextPromptAnchorMax;
                m_contextPromptRect.pivot = m_contextPromptPivot;
                m_contextPromptRect.anchoredPosition = m_contextPromptPosition;
                return;
            }

            var eventCamera = m_canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : m_canvas.worldCamera;
            var iconScreenPosition = RectTransformUtility.WorldToScreenPoint(
                eventCamera, m_objectiveBeacon.ObjectiveIndicatorIconRect.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_contextPromptParent, iconScreenPosition, eventCamera, out var iconPosition);

            m_contextPromptRect.anchorMin = Vector2.one * 0.5f;
            m_contextPromptRect.anchorMax = Vector2.one * 0.5f;
            m_contextPromptRect.pivot = Vector2.one * 0.5f;
            var halfSize = m_contextPromptRect.rect.size * 0.5f;
            var iconHalfWidth = m_objectiveBeacon.ObjectiveIndicatorIconRect.rect.width * 0.5f;
            var horizontalDirection = iconPosition.x <= 0f ? 1f : -1f;
            var desiredPosition = iconPosition + Vector2.right * horizontalDirection * (iconHalfWidth + halfSize.x + 10f);
            var parentRect = m_contextPromptParent.rect;
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, parentRect.xMin + halfSize.x + 8f,
                parentRect.xMax - halfSize.x - 8f);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, parentRect.yMin + halfSize.y + 8f,
                parentRect.yMax - halfSize.y - 8f);
            var anchorReference = new Vector2(
                Mathf.Lerp(parentRect.xMin, parentRect.xMax, m_contextPromptRect.anchorMin.x),
                Mathf.Lerp(parentRect.yMin, parentRect.yMax, m_contextPromptRect.anchorMin.y));
            m_contextPromptRect.anchoredPosition = desiredPosition - anchorReference;
        }

        private InteractionPromptPresentation _contextPrompt()
        {
            if (!string.IsNullOrEmpty(m_debugObjective))
                return InteractionPromptPresentation.Hidden;
            if (m_overclockChoice.IsPrimaryPending)
                return _choicePrompt("CHAIN ARC", "OVERRIDE: BOLTS JUMP", "OVERDRIVE", "OVERRIDE: MOVE FASTER");
            if (m_overclockChoice.IsAuxiliaryPending)
                return _choicePrompt("CAPACITOR", "LOW-SIGNAL REFILL", "SHIELD", "NEGATE ONE THREAT");
            if (m_overclockChoice.IsWeaponPending)
                return _choicePrompt("PIERCING PULSE", "STRIKE TWO THREATS", "CONTROLLED RICOCHET", "REDIRECT FROM COVER");
            if (_isExtractionUplinkChoiceAvailable())
                return _choicePrompt(
                    "OVERDRIVE UPLINK",
                    $"−{m_extractionUplink.OverdriveSignalCost:0} SIGNAL  •  {m_extractionUplink.OverdriveDuration:0.##}s",
                    "STABLE UPLINK",
                    $"FREE  •  {m_extractionUplink.StableDuration:0.##}s");
            if (m_model.CurrentObjective.Id == MissionObjectiveId.RelayFork && m_world.RelayForkObjective != null &&
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.RelayForkObjective.Position) < 1.8f)
                return _availablePrompt("ROUTE BOTH CENTRAL FEEDS", "TWO COMPONENTS READY");
            if (m_model.CurrentObjective.Id == MissionObjectiveId.CentralAssembly && m_world.TransferVaultObjective != null &&
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.TransferVaultObjective.Position) < 1.8f)
                return _availablePrompt("ASSEMBLE CENTRAL PAYLOAD", "TRANSFER VAULT ONLINE");
            if (m_model.CurrentObjective.Id == MissionObjectiveId.CentralInstallation &&
                m_world.CentralInstallationObjective != null &&
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.CentralInstallationObjective.Position) < 1.8f)
                return _availablePrompt("INSTALL CENTRAL PAYLOAD", "CENTRAL SOCKET READY");
            if (m_model.CurrentObjective.Id == MissionObjectiveId.RelayInstallation &&
                m_world.RelayPayloadObjective != null &&
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.RelayPayloadObjective.Position) < 1.8f)
                return _availablePrompt("INSTALL RELAY PAYLOAD", "WEAPON CALIBRATION FOLLOWS");
            if (m_model.CurrentObjective.Id == MissionObjectiveId.SpineVenting &&
                m_world.SpineVentingObjective != null &&
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.SpineVentingObjective.Position) < 1.8f)
                return _availablePrompt("VENT SPINE BERTH", "RELEASE PRESSURE INTERLOCK");
            if (m_model.CurrentObjective.Id == MissionObjectiveId.InductionLattice &&
                m_world.InductionLatticeObjective != null &&
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.InductionLatticeObjective.Position) < 1.8f)
                return _availablePrompt("CHARGE EMPTY LATTICE", "INDUCTION COIL ONLINE");
            if (m_model.CurrentObjective.Id == MissionObjectiveId.FluxShunt &&
                m_world.FluxShuntObjective != null &&
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.FluxShuntObjective.Position) < 1.8f)
                return _availablePrompt("THROW FLUX SHUNT", "OPENS RETURN FLANK");
            if (m_model.CurrentObjective.Id == MissionObjectiveId.ConvergenceCalibration &&
                m_world.ConvergenceCalibrationObjective != null)
            {
                if (m_model.ConvergenceCalibrationActive)
                {
                    var remaining = Mathf.Max(
                        0f,
                        m_model.ConvergenceCalibrationDuration - m_model.ConvergenceCalibrationProgress);
                    return m_world.ConvergenceCalibrationObjective.Contains(m_world.Player.position)
                        ? new InteractionPromptPresentation(true, InteractionPromptState.Progress, "HOLD",
                            "MAINTAIN CHAMBER CONTROL", $"CALIBRATING  •  {remaining:0.0}s")
                        : new InteractionPromptPresentation(true, InteractionPromptState.Blocked, "",
                            "RETURN TO CHAMBER", $"CALIBRATION PAUSED  •  {remaining:0.0}s");
                }

                if (DeadSignalWorld.FlatDistance(
                        m_world.Player.position,
                        m_world.ConvergenceCalibrationObjective.Position) < 1.8f)
                {
                    return _availablePrompt("BEGIN CONVERGENCE CALIBRATION", "BOUNDED HOLDOUT");
                }
            }
            if (m_model.CurrentObjective.Id == MissionObjectiveId.BreakerReset &&
                m_world.BreakerResetObjective != null &&
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.BreakerResetObjective.Position) < 1.8f)
                return _availablePrompt("RESET DISTRIBUTION", "UNLOCKS FURNACE PROCESS");
            if (m_model.CurrentObjective.Id == MissionObjectiveId.FurnaceForge &&
                m_world.FurnaceForgeObjective != null &&
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.FurnaceForgeObjective.Position) < 1.8f)
                return _availablePrompt("FORGE CHARGED LATTICE", "ARC FURNACE READY");
            if (m_model.CurrentObjective.Id == MissionObjectiveId.QuenchStabilization &&
                m_world.QuenchStabilizationObjective != null &&
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.QuenchStabilizationObjective.Position) < 1.8f)
                return _availablePrompt("STABILIZE SIGNAL CORE", "OPENS QUENCH RETURN");
            if (m_model.CurrentObjective.Id == MissionObjectiveId.TrialCommitment &&
                m_world.CombatChamber != null && m_world.CombatChamber.CanInteract(m_world.Player.position))
                return m_threats.CanBeginCombatChamber
                    ? _availablePrompt("ARM FINAL TRIAL", "CROSSING THRESHOLD SEALS ROOM B")
                    : _blockedPrompt("TRIAL INTERLOCKED", "CLEAR ACTIVE THREATS");
            if (m_model.CurrentObjective.Id == MissionObjectiveId.StationCapacitor &&
                m_world.CombatChamber != null && m_world.CombatChamber.RewardAvailable &&
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.CombatChamber.RewardPosition) < 1.8f)
                return _availablePrompt("RECOVER STATION CAPACITOR", "COMPLETES SIGNAL CORE");
            if (m_model.CurrentObjective.Id == MissionObjectiveId.SpineCoreInstallation &&
                m_world.SpineCoreInstallationObjective != null &&
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.SpineCoreInstallationObjective.Position) < 1.8f)
                return _availablePrompt("INSTALL FINAL SIGNAL CORE", "ENABLES POWERED WITHDRAWAL");
            if (!m_model.SpineTowerOnline &&
                m_world.IsSpineTowerInteractionInRange(m_world.Player.position))
                return m_model.RelayTowerOnline
                    ? _availablePrompt("INSTALL RELAY RESULT", "SPINE BERTH VENTED")
                    : _blockedPrompt("SPINE LOCKED", "ACTIVATE RELAY FOUNDRY FIRST");
            if (!m_model.RelayTowerOnline &&
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.RelayTowerPosition) < 1.8f)
                return m_model.TowerOnline
                    ? _availablePrompt("ACTIVATE RELAY FOUNDRY", "FREE ACTIVATION  •  PAYLOAD PROCESSING")
                    : _blockedPrompt("RELAY LOCKED", "ACTIVATE CENTRAL TOWER FIRST");
            if (!m_model.ShortcutOpen && DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.ShortcutPosition) < 1.9f)
                return !m_model.TowerOnline
                    ? _blockedPrompt("SHORTCUT OFFLINE", "ACTIVATE CENTRAL TOWER FIRST")
                    : m_model.Signal < RunModel.ShortcutCost
                        ? _blockedPrompt("INSUFFICIENT SIGNAL",
                            $"SHORTCUT COST {RunModel.ShortcutCost:0}  •  RESERVE {m_model.Signal:0}")
                        : _availablePrompt("OPEN CENTRAL SHORTCUT",
                            $"COST {RunModel.ShortcutCost:0} SIGNAL  •  RESERVE {m_model.Signal:0}");
            if (!m_model.TowerOnline && DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.TowerPosition) < 1.8f)
                return _availablePrompt("ACTIVATE SIGNAL TOWER", $"STARTUP COST {RunModel.TowerCost:0}  •  EMERGENCY LINK AVAILABLE");
            if (DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.ExtractionPosition) < 1.65f)
                return m_extractionUplink.IsActive
                    ? new InteractionPromptPresentation(true, InteractionPromptState.Progress, "LINK",
                        "UPLINK IN PROGRESS", $"LOCKED  •  {m_extractionUplink.SecondsRemaining:0.0}s")
                    : m_model.CanExtract
                        ? _blockedPrompt("UPLINK READY", "SELECT STABLE OR OVERDRIVE")
                        : _blockedPrompt("EXTRACTION LOCKED",
                            $"{RunModel.SalvageRequired - m_model.Salvage} SALVAGE MISSING");
            return InteractionPromptPresentation.Hidden;
        }

        private InteractionPromptPresentation _availablePrompt(string action, string detail) =>
            new InteractionPromptPresentation(true, InteractionPromptState.Available, _interactGlyph(), action, detail);

        private static InteractionPromptPresentation _blockedPrompt(string action, string detail) =>
            new InteractionPromptPresentation(true, InteractionPromptState.Blocked, "", action, detail);

        private InteractionPromptPresentation _choicePrompt(
            string fireAction,
            string fireDetail,
            string interactAction,
            string interactDetail) =>
            new InteractionPromptPresentation(
                true,
                InteractionPromptState.Choice,
                _fireGlyph(),
                fireAction,
                fireDetail,
                _interactGlyph(),
                $"{interactAction}  •  {interactDetail}");

        private string _fireGlyph() =>
            m_input.ActivePromptDevice == InputPromptDevice.Gamepad ? "RB" : m_input.FireKeyboardBinding;

        private string _interactGlyph() =>
            m_input.ActivePromptDevice == InputPromptDevice.Gamepad ? "X" : m_input.InteractKeyboardBinding;

        private SignalTransactionPreview _signalTransactionPreview()
        {
            if (!m_model.ShortcutOpen && m_model.TowerOnline &&
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.ShortcutPosition) < 1.9f)
            {
                return new SignalTransactionPreview(RunModel.ShortcutCost, "SHORTCUT");
            }

            if (_isExtractionUplinkChoiceAvailable())
            {
                return new SignalTransactionPreview(m_extractionUplink.OverdriveSignalCost, "OVERDRIVE");
            }

            return default;
        }

        private string _activeControlLegend()
        {
            return m_input.ActivePromptDevice == InputPromptDevice.Gamepad ? "GAMEPAD  LS / RS  |  RT-RB / X  |  MENU-A"
                : $"KEYBOARD  {m_input.MoveUpKeyboardBinding}{m_input.MoveLeftKeyboardBinding}{m_input.MoveDownKeyboardBinding}" +
                  $"{m_input.MoveRightKeyboardBinding} / MOUSE  |  LMB-{m_input.FireKeyboardBinding} / {m_input.InteractKeyboardBinding}  |  ESC-R";
        }

        private bool _isExtractionUplinkChoiceAvailable()
        {
            return m_model.CanExtract &&
                   !m_extractionUplink.IsActive &&
                   !m_extractionUplink.IsComplete &&
                   DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.ExtractionPosition) < 1.65f;
        }

        private string _binding(string keyboardMouse, string gamepad) =>
            m_input.ActivePromptDevice == InputPromptDevice.Gamepad ? gamepad : keyboardMouse;

        private static bool _hasTexture(RawImage image) => image != null && image.texture != null;

        private static string _overclockName(SignalOverclock overclock) => overclock switch
        {
            SignalOverclock.ChainArc => "OVERCLOCK: CHAIN ARC",
            SignalOverclock.OverdriveThrusters => "OVERCLOCK: OVERDRIVE",
            _ => string.Empty
        };

        private static string _weaponOverclockName(SignalWeaponOverclock overclock) => overclock switch
        {
            SignalWeaponOverclock.PiercingPulse => "WEAPON: PIERCING PULSE",
            SignalWeaponOverclock.ControlledRicochet => "WEAPON: CONTROLLED RICOCHET",
            _ => string.Empty
        };

        private string _auxiliaryOverclockName() => m_overclockChoice.SelectedAuxiliary switch
        {
            SignalAuxiliaryOverclock.EmergencyCapacitor =>
                m_overclockChoice.IsEmergencyCapacitorAvailable ? "CAPACITOR: ARMED" : "CAPACITOR: SPENT",
            SignalAuxiliaryOverclock.FeedbackShield =>
                m_overclockChoice.IsFeedbackShieldCharged ? "SHIELD: CHARGED" : "SHIELD: EMPTY",
            _ => string.Empty
        };

        private string _overclockSynergyName() => m_overclockChoice.Synergy switch
        {
            SignalOverclockSynergy.ArcOverload =>
                m_overclockChoice.IsChainArcOverloadReady ? "PAIR: ARC OVERLOAD READY" : "PAIR: ARC OVERLOAD",
            SignalOverclockSynergy.ReactiveArc =>
                m_overclockChoice.IsChainArcOverloadReady ? "PAIR: REACTIVE ARC READY" : "PAIR: REACTIVE ARC",
            SignalOverclockSynergy.CapacitorSurge or SignalOverclockSynergy.ShieldSurge
                when m_overclockChoice.IsOverdriveSurgeActive =>
                $"PAIR: THRUSTER SURGE {m_overclockChoice.OverdriveSurgeSecondsRemaining:0.0}s",
            SignalOverclockSynergy.CapacitorSurge => "PAIR: CAPACITOR SURGE",
            SignalOverclockSynergy.ShieldSurge => "PAIR: SHIELD SURGE",
            _ => string.Empty
        };

        private readonly struct SignalTransactionPreview
        {
            public SignalTransactionPreview(float cost, string name)
            {
                Cost = cost;
                Name = name;
            }

            public float Cost { get; }
            public string Name { get; }
        }
    }
}
