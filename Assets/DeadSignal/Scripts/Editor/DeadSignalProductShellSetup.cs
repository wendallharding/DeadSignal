using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DeadSignal.Presentation;

namespace DeadSignal.Editor
{
    public static class DeadSignalProductShellSetup
    {
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/UI/DeadSignalMainMenu.prefab";
        private const string HUD_PREFAB_PATH = "Assets/DeadSignal/Resources/UI/DeadSignalHud.prefab";
        private const string BACKDROP_PATH = "Assets/DeadSignal/Resources/UI/MainMenuStationBackdrop.png";
        private const string MOVEMENT_GLYPH_PATH = "Assets/DeadSignal/Resources/UI/MovementControlGlyph.png";
        private const string AIM_GLYPH_PATH = "Assets/DeadSignal/Resources/UI/AimControlGlyph.png";
        private const string TRANSITION_TUNING_PATH =
            "Assets/DeadSignal/Resources/Tuning/ProductShellTransitionTuning.asset";

        [MenuItem("DEAD SIGNAL/Ensure Product Shell")]
        public static void EnsureAssets()
        {
            var transitionTuning = _ensureTransitionTuning();

            var importer = AssetImporter.GetAtPath(BACKDROP_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new System.InvalidOperationException($"Main-menu backdrop importer is missing at {BACKDROP_PATH}.");
            }

            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            var backdrop = AssetDatabase.LoadAssetAtPath<Texture2D>(BACKDROP_PATH);
            if (backdrop == null)
            {
                throw new System.InvalidOperationException($"Main-menu backdrop is missing at {BACKDROP_PATH}.");
            }
            var movementGlyph = AssetDatabase.LoadAssetAtPath<Texture2D>(MOVEMENT_GLYPH_PATH);
            var aimGlyph = AssetDatabase.LoadAssetAtPath<Texture2D>(AIM_GLYPH_PATH);
            if (movementGlyph == null || aimGlyph == null)
            {
                throw new System.InvalidOperationException("The authored control-diagram textures are missing.");
            }

            var root = new GameObject(
                "Main Menu Overlay",
                typeof(RectTransform),
                typeof(DeadSignalShellController),
                typeof(MainMenuPresentation));
            try
            {
                var controller = root.GetComponent<DeadSignalShellController>();
                var overlay = root.GetComponent<RectTransform>();
                overlay.anchorMin = Vector2.zero;
                overlay.anchorMax = Vector2.one;
                overlay.offsetMin = Vector2.zero;
                overlay.offsetMax = Vector2.zero;
                var backdropImage = overlay.gameObject.AddComponent<RawImage>();
                var menuCanvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();
                backdropImage.texture = backdrop;
                backdropImage.color = new Color(0.82f, 0.86f, 0.9f, 1f);
                var shade = _createRect("Readability Shade", overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                shade.gameObject.AddComponent<Image>().color = new Color(0.008f, 0.014f, 0.024f, 0.5f);
                var signalSweep = _createRect("Signal Sweep", overlay, new Vector2(0.62f, 0f), new Vector2(0.62f, 1f),
                    new Vector2(-1f, 0f), new Vector2(1f, 0f));
                signalSweep.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.78f, 0.9f, 0f);

                _createText("Protocol Mark", overlay, "DEAD SIGNAL  //  RESTORATION AUTHORITY", 12, TextAnchor.MiddleRight,
                    new Color(0.58f, 0.68f, 0.72f), new Vector2(-48f, 486f), new Vector2(360f, 24f),
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
                var stationReadout = _createText("Station Readout", overlay, "DS-07  //  ARRAY 00", 12, TextAnchor.MiddleRight,
                    new Color(0.15f, 0.9f, 1f), new Vector2(-48f, -486f), new Vector2(360f, 24f),
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));

                var mainPanel = _createPanel("Main Panel", overlay, new Vector2(0.07f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(560f, 670f), new Vector2(0f, 0f));
                var panelRule = _createRect("Cyan Rule", mainPanel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    Vector2.zero, Vector2.zero);
                panelRule.pivot = new Vector2(0f, 0.5f);
                panelRule.anchoredPosition = new Vector2(24f, 0f);
                panelRule.sizeDelta = new Vector2(3f, 606f);
                panelRule.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.82f, 0.92f, 0.82f);
                _createText("Eyebrow", mainPanel, "MISSION CONTROL  //  DS-07", 12, TextAnchor.MiddleLeft,
                    new Color(0.8f, 0.67f, 0.27f), new Vector2(0f, 276f), new Vector2(490f, 24f));
                _createText("Title", mainPanel, "DEAD SIGNAL", 58, TextAnchor.MiddleLeft, new Color(0.94f, 0.98f, 1f),
                    new Vector2(0f, 195f), new Vector2(490f, 92f));
                _createText("Subtitle", mainPanel, "STATION RESTORATION PROTOCOL", 15, TextAnchor.MiddleLeft,
                    new Color(0.15f, 0.9f, 1f), new Vector2(0f, 120f), new Vector2(490f, 28f));
                _createText("Route", mainPanel, "RESTART  /  EXTEND  /  REBUILD  /  WITHDRAW", 12, TextAnchor.MiddleLeft,
                    new Color(0.72f, 0.78f, 0.8f), new Vector2(0f, 88f), new Vector2(510f, 24f));

                var start = _createButton("Start Run", mainPanel, "01  START RUN", 34f, 62f);
                var settings = _createButton("Settings", mainPanel, "02  SETTINGS", -38f, 52f);
                var controls = _createButton("Controls", mainPanel, "03  CONTROLS", -100f, 52f);
                var quit = _createButton("Quit", mainPanel, "04  QUIT", -162f, 52f);
                var selectionRail = _createRect("Selection Rail", mainPanel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    Vector2.zero, Vector2.zero);
                selectionRail.pivot = new Vector2(0f, 0.5f);
                selectionRail.anchoredPosition = new Vector2(32f, 34f);
                selectionRail.sizeDelta = new Vector2(5f, 62f);
                selectionRail.gameObject.AddComponent<Image>().color = new Color(0.98f, 0.72f, 0.2f, 1f);
                selectionRail.SetAsLastSibling();
                var selectionDetail = _createText("Selection Detail", mainPanel, "BEGIN STATION RESTORATION", 11,
                    TextAnchor.MiddleLeft, new Color(0.8f, 0.67f, 0.27f), new Vector2(0f, -214f), new Vector2(500f, 24f));
                _createText("Input Hint", mainPanel, "ARROWS / STICK  NAVIGATE     ENTER / A  SELECT", 11, TextAnchor.MiddleLeft,
                    new Color(0.58f, 0.66f, 0.7f), new Vector2(0f, -258f), new Vector2(500f, 24f));

                var settingsPanel = _createPanel("Settings Panel", overlay, new Vector2(0.06f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(900f, 650f), Vector2.zero);
                _createText("Section", settingsPanel, "SYSTEM  /  ACCESSIBILITY", 12, TextAnchor.MiddleLeft,
                    new Color(0.8f, 0.67f, 0.27f), new Vector2(0f, 280f), new Vector2(820f, 24f));
                _createText("Title", settingsPanel, "SETTINGS", 40, TextAnchor.MiddleLeft, new Color(0.92f, 0.98f, 1f),
                    new Vector2(0f, 230f), new Vector2(520f, 56f));
                _createText("Hint", settingsPanel, "PRESENTATION COMFORT  //  CHANGES SAVE IMMEDIATELY", 13, TextAnchor.MiddleLeft,
                    new Color(0.15f, 0.9f, 1f), new Vector2(0f, 190f), new Vector2(820f, 28f));
                var settingButtons = new Button[4];
                var settingTexts = new Text[4];
                var settingNames = new[] { "Steady Camera", "Reduced Flashes", "High Contrast", "Signal Audio" };
                for (var i = 0; i < settingButtons.Length; i++)
                {
                    settingButtons[i] = _createButton(settingNames[i], settingsPanel, settingNames[i].ToUpperInvariant(),
                        115f - i * 68f, 54f, 480f);
                    settingTexts[i] = settingButtons[i].GetComponentInChildren<Text>();
                }
                var settingsBack = _createButton("Back", settingsPanel, "BACK TO MISSION CONTROL", -190f, 48f, 480f);
                var settingsRail = _createSelectionRail("Settings Selection Rail", settingsPanel, new Vector2(32f, 115f), 54f);
                _createDetailCard(settingsPanel, new Vector2(548f, 38f), new Vector2(316f, 312f));
                var utilityDetailTitle = _createText("Utility Detail Title", settingsPanel, "STEADY CAMERA", 14,
                    TextAnchor.UpperLeft, new Color(0.98f, 0.72f, 0.2f), new Vector2(520f, 126f), new Vector2(280f, 28f));
                var utilityDetail = _createText("Utility Detail", settingsPanel,
                    "Removes impact and event camera impulses. Aim and movement remain unchanged.", 14, TextAnchor.UpperLeft,
                    new Color(0.82f, 0.88f, 0.9f), new Vector2(520f, 42f), new Vector2(280f, 80f));
                var utilityConfirmation = _createText("Utility Confirmation", settingsPanel,
                    "PREFERENCES STORED LOCALLY", 11, TextAnchor.MiddleLeft, new Color(0.15f, 0.9f, 1f),
                    new Vector2(520f, -58f), new Vector2(280f, 28f));
                var settingsInputHint = _createText("Utility Input Hint", settingsPanel,
                    "ARROWS  NAVIGATE     ENTER  APPLY     ESC  BACK", 11, TextAnchor.MiddleLeft,
                    new Color(0.58f, 0.66f, 0.7f), new Vector2(0f, -270f), new Vector2(820f, 24f));

                var controlsPanel = _createPanel("Controls Panel", overlay, new Vector2(0.04f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(1000f, 720f), Vector2.zero);
                _createText("Section", controlsPanel, "INPUT  /  KEYBOARD ROUTING", 12, TextAnchor.MiddleLeft,
                    new Color(0.8f, 0.67f, 0.27f), new Vector2(0f, 318f), new Vector2(920f, 24f));
                _createText("Title", controlsPanel, "CONTROLS", 40, TextAnchor.MiddleLeft, new Color(0.92f, 0.98f, 1f),
                    new Vector2(0f, 268f), new Vector2(540f, 56f));
                _createText("Hint", controlsPanel, "SELECT A KEYBOARD ACTION TO REBIND  //  GAMEPAD MAP IS FIXED", 13,
                    TextAnchor.MiddleLeft, new Color(0.15f, 0.9f, 1f), new Vector2(0f, 228f), new Vector2(920f, 28f));
                var rebindButtons = new Button[7];
                var rebindTexts = new Text[7];
                var rebindNames = new[] { "Move Up", "Move Down", "Move Left", "Move Right", "Fire", "Interact", "Reset Bindings" };
                for (var i = 0; i < rebindButtons.Length; i++)
                {
                    rebindButtons[i] = _createButton(rebindNames[i], controlsPanel, rebindNames[i].ToUpperInvariant(),
                        156f - i * 50f, 42f, 430f);
                    rebindTexts[i] = rebindButtons[i].GetComponentInChildren<Text>();
                }
                var status = _createText("Status", controlsPanel, string.Empty, 12, TextAnchor.MiddleLeft,
                    new Color(0.15f, 0.9f, 1f), new Vector2(460f, -278f), new Vector2(450f, 28f));
                var controlsBack = _createButton("Back", controlsPanel, "BACK TO MISSION CONTROL", -250f, 42f, 430f);
                var controlsRail = _createSelectionRail("Controls Selection Rail", controlsPanel, new Vector2(32f, 156f), 42f);
                _createDetailCard(controlsPanel, new Vector2(488f, 38f), new Vector2(476f, 390f));
                _createText("Control Diagram Header", controlsPanel, "GAMEPAD  //  FIXED COMBAT MAP", 12,
                    TextAnchor.MiddleLeft, new Color(0.98f, 0.72f, 0.2f), new Vector2(460f, 168f), new Vector2(450f, 24f));
                _createRawImage("Movement Diagram", controlsPanel, movementGlyph, new Vector2(492f, 68f), new Vector2(184f, 142f));
                _createRawImage("Aim Diagram", controlsPanel, aimGlyph, new Vector2(706f, 68f), new Vector2(184f, 142f));
                _createText("Control Diagram Labels", controlsPanel,
                    "LEFT STICK  MOVE       RIGHT STICK  AIM\nRT / RB  FIRE          X / WEST  INTERACT\nA / SOUTH  DASH        MENU  PAUSE",
                    12, TextAnchor.UpperLeft, new Color(0.82f, 0.88f, 0.9f), new Vector2(460f, -54f),
                    new Vector2(450f, 92f));
                var controlsDetailTitle = _createText("Control Detail Title", controlsPanel, "MOVE UP", 12,
                    TextAnchor.MiddleLeft, new Color(0.98f, 0.72f, 0.2f), new Vector2(460f, -172f),
                    new Vector2(450f, 24f));
                var controlsDetail = _createText("Control Detail", controlsPanel,
                    "Rebind one keyboard movement direction. Controller movement remains on the left stick.", 12,
                    TextAnchor.UpperLeft, new Color(0.76f, 0.82f, 0.85f), new Vector2(460f, -220f),
                    new Vector2(450f, 64f));
                var controlsInputHint = _createText("Utility Input Hint", controlsPanel,
                    "ARROWS  NAVIGATE     ENTER  REBIND     ESC  BACK", 11, TextAnchor.MiddleLeft,
                    new Color(0.58f, 0.66f, 0.7f), new Vector2(0f, -318f), new Vector2(920f, 24f));

                settingsPanel.gameObject.SetActive(false);
                controlsPanel.gameObject.SetActive(false);
                var serialized = new SerializedObject(controller);
                _set(serialized, "m_menuOverlay", root);
                _set(serialized, "m_mainPanel", mainPanel.gameObject);
                _set(serialized, "m_settingsPanel", settingsPanel.gameObject);
                _set(serialized, "m_controlsPanel", controlsPanel.gameObject);
                _set(serialized, "m_menuCanvasGroup", menuCanvasGroup);
                _set(serialized, "m_transitionTuning", transitionTuning);
                _set(serialized, "m_mainMenuPresentation", root.GetComponent<MainMenuPresentation>());
                _set(serialized, "m_startButton", start);
                _set(serialized, "m_settingsButton", settings);
                _set(serialized, "m_controlsButton", controls);
                _set(serialized, "m_quitButton", quit);
                _setArray(serialized, "m_settingButtons", settingButtons);
                _setArray(serialized, "m_settingTexts", settingTexts);
                _set(serialized, "m_settingsBackButton", settingsBack);
                _setArray(serialized, "m_rebindButtons", rebindButtons);
                _setArray(serialized, "m_rebindTexts", rebindTexts);
                _set(serialized, "m_rebindStatusText", status);
                _set(serialized, "m_controlsBackButton", controlsBack);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var presentation = new SerializedObject(root.GetComponent<MainMenuPresentation>());
                _set(presentation, "m_backdrop", backdropImage);
                _set(presentation, "m_mainPanel", mainPanel);
                _set(presentation, "m_selectionRail", selectionRail);
                _set(presentation, "m_selectionDetail", selectionDetail);
                _set(presentation, "m_signalSweep", signalSweep);
                _set(presentation, "m_stationReadout", stationReadout);
                _set(presentation, "m_settingsPanel", settingsPanel);
                _set(presentation, "m_controlsPanel", controlsPanel);
                _setArray(presentation, "m_settingButtons", _append(settingButtons, settingsBack));
                _setArray(presentation, "m_controlButtons", _append(rebindButtons, controlsBack));
                _set(presentation, "m_settingsSelectionRail", settingsRail);
                _set(presentation, "m_controlsSelectionRail", controlsRail);
                _set(presentation, "m_utilityDetailTitle", utilityDetailTitle);
                _set(presentation, "m_utilityDetail", utilityDetail);
                _set(presentation, "m_utilityConfirmation", utilityConfirmation);
                _set(presentation, "m_utilityInputHint", settingsInputHint);
                _set(presentation, "m_controlsDetailTitle", controlsDetailTitle);
                _set(presentation, "m_controlsDetail", controlsDetail);
                _set(presentation, "m_controlsInputHint", controlsInputHint);
                presentation.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _ensureHudNavigation(transitionTuning);
        }

        public static void EnsureTransitionAssets()
        {
            var transitionTuning = _ensureTransitionTuning();
            _ensureMainMenuTransition(transitionTuning);
            _ensureHudNavigation(transitionTuning);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static ProductShellTransitionTuning _ensureTransitionTuning()
        {
            var transitionTuning = AssetDatabase.LoadAssetAtPath<ProductShellTransitionTuning>(TRANSITION_TUNING_PATH);
            if (transitionTuning != null)
            {
                return transitionTuning;
            }

            transitionTuning = ScriptableObject.CreateInstance<ProductShellTransitionTuning>();
            AssetDatabase.CreateAsset(transitionTuning, TRANSITION_TUNING_PATH);
            return transitionTuning;
        }

        private static void _ensureMainMenuTransition(ProductShellTransitionTuning transitionTuning)
        {
            var root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            try
            {
                var controller = root.GetComponent<DeadSignalShellController>();
                var canvasGroup = root.GetComponent<CanvasGroup>();
                if (controller == null)
                {
                    throw new System.InvalidOperationException("The authored main-menu controller is missing.");
                }
                if (canvasGroup == null)
                {
                    canvasGroup = root.AddComponent<CanvasGroup>();
                }

                var serialized = new SerializedObject(controller);
                _set(serialized, "m_menuCanvasGroup", canvasGroup);
                _set(serialized, "m_transitionTuning", transitionTuning);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _ensureHudNavigation(ProductShellTransitionTuning transitionTuning)
        {
            var root = PrefabUtility.LoadPrefabContents(HUD_PREFAB_PATH);
            try
            {
                var hud = root.GetComponent<DeadSignalHud>();
                var pause = root.transform.Find("Pause Overlay");
                var outcome = root.transform.Find("Outcome Overlay");
                if (hud == null || pause == null || outcome == null)
                {
                    throw new System.InvalidOperationException("The authored HUD shell-navigation anchors are missing.");
                }

                var pauseResume = _ensureHudButton("Resume Run", pause, "RESUME RUN", new Vector2(-150f, -316f));
                var pauseMenu = _ensureHudButton("Main Menu", pause, "MAIN MENU", new Vector2(150f, -316f));
                var outcomeRestart = _ensureHudButton("Restart Run", outcome, "RESTART RUN", new Vector2(-150f, -94f));
                var outcomeMenu = _ensureHudButton("Main Menu", outcome, "MAIN MENU", new Vector2(150f, -94f));
                var outcomeCanvasGroup = outcome.GetComponent<CanvasGroup>();
                if (outcomeCanvasGroup == null)
                {
                    outcomeCanvasGroup = outcome.gameObject.AddComponent<CanvasGroup>();
                }
                pause.GetComponent<Image>().color = new Color(0.002f, 0.005f, 0.008f, 0.9f);
                _styleHudButton(pauseResume, true);
                _styleHudButton(pauseMenu, false);
                var pauseHeader = _ensureHudText("Pause Header", pause, "RUN PAUSED", 38, new Vector2(0f, 430f),
                    new Vector2(800f, 54f), new Color(0.94f, 0.98f, 1f));
                pauseHeader.alignment = TextAnchor.MiddleCenter;
                var pauseSubhead = _ensureHudText("Pause Subhead", pause,
                    "STATION STATE HELD  //  SIGNAL DRAIN SUSPENDED", 13, new Vector2(0f, 388f),
                    new Vector2(800f, 28f), new Color(0.15f, 0.9f, 1f));
                pauseSubhead.alignment = TextAnchor.MiddleCenter;
                _ensureHudText("Comfort Section", pause, "PRESENTATION COMFORT", 11, new Vector2(-200f, 258f),
                    new Vector2(320f, 24f), new Color(0.8f, 0.67f, 0.27f));
                _ensureHudText("Routing Section", pause, "KEYBOARD ROUTING", 11, new Vector2(200f, 258f),
                    new Vector2(320f, 24f), new Color(0.8f, 0.67f, 0.27f));
                var actions = _ensureHudText("Actions Section", pause, "RUN ACTIONS", 11, new Vector2(0f, -252f),
                    new Vector2(640f, 24f), new Color(0.8f, 0.67f, 0.27f));
                actions.alignment = TextAnchor.MiddleCenter;
                var pauseRail = _ensureHudImage("Pause Selection Rail", pause, new Vector2(-150f, -286f),
                    new Vector2(270f, 4f), new Color(0.98f, 0.72f, 0.2f));
                var pauseDetail = _ensureHudText("Pause Selection Detail", pause, "RETURN TO THE HELD STATION STATE", 11,
                    new Vector2(0f, -370f), new Vector2(800f, 24f), new Color(0.8f, 0.67f, 0.27f));
                pauseDetail.alignment = TextAnchor.MiddleCenter;
                var pauseInputHint = _ensureHudText("Pause Input Hint", pause,
                    "ARROWS  NAVIGATE     ENTER  SELECT     ESC  RESUME", 11, new Vector2(0f, -444f),
                    new Vector2(800f, 24f), new Color(0.58f, 0.66f, 0.7f));
                pauseInputHint.alignment = TextAnchor.MiddleCenter;
                (pause.Find("Resume") as RectTransform).anchoredPosition = new Vector2(0f, -408f);
                (outcome.Find("Restart") as RectTransform).anchoredPosition = new Vector2(0f, -154f);

                var pausePresentation = root.GetComponent<PauseMenuPresentation>();
                if (pausePresentation == null)
                {
                    pausePresentation = root.AddComponent<PauseMenuPresentation>();
                }
                var pausePresentationSerialized = new SerializedObject(pausePresentation);
                _set(pausePresentationSerialized, "m_pausePanel", pause as RectTransform);
                _setArray(pausePresentationSerialized, "m_actionButtons", new[] { pauseResume, pauseMenu });
                _set(pausePresentationSerialized, "m_selectionRail", pauseRail);
                _set(pausePresentationSerialized, "m_selectionDetail", pauseDetail);
                _set(pausePresentationSerialized, "m_inputHint", pauseInputHint);
                pausePresentationSerialized.ApplyModifiedPropertiesWithoutUndo();

                var serialized = new SerializedObject(hud);
                _set(serialized, "m_pauseMenuPresentation", pausePresentation);
                _set(serialized, "m_pauseResumeButton", pauseResume);
                _set(serialized, "m_pauseMainMenuButton", pauseMenu);
                _set(serialized, "m_outcomeRestartButton", outcomeRestart);
                _set(serialized, "m_outcomeMainMenuButton", outcomeMenu);
                _set(serialized, "m_outcomeCanvasGroup", outcomeCanvasGroup);
                _set(serialized, "m_transitionTuning", transitionTuning);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, HUD_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Button _ensureHudButton(string name, Transform parent, string label, Vector2 position)
        {
            var existing = parent.Find(name);
            Button button;
            if (existing != null)
            {
                button = existing.GetComponent<Button>();
            }
            else
            {
                button = _createButton(name, parent, label, position.y, 54f);
            }
            var rect = button.transform as RectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(270f, 54f);
            var text = button.GetComponentInChildren<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.rectTransform.anchoredPosition = Vector2.zero;
            text.rectTransform.sizeDelta = Vector2.zero;
            return button;
        }

        private static void _styleHudButton(Button button, bool primary)
        {
            var image = button.GetComponent<Image>();
            image.color = primary
                ? new Color(0.08f, 0.19f, 0.22f, 0.98f)
                : new Color(0.07f, 0.11f, 0.14f, 0.98f);
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.2f, 0.9f, 1f);
            colors.selectedColor = new Color(0.2f, 0.9f, 1f);
            colors.pressedColor = new Color(0.98f, 0.72f, 0.2f);
            button.colors = colors;
            var label = button.GetComponentInChildren<Text>();
            label.fontSize = primary ? 20 : 18;
            label.fontStyle = FontStyle.Bold;
        }

        private static Text _ensureHudText(string name, Transform parent, string value, int fontSize, Vector2 position,
            Vector2 size, Color color)
        {
            var existing = parent.Find(name);
            Text text;
            RectTransform rect;
            if (existing == null)
            {
                rect = _createRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                    Vector2.zero);
                text = rect.gameObject.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.raycastTarget = false;
            }
            else
            {
                rect = existing as RectTransform;
                text = existing.GetComponent<Text>();
            }

            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = color;
            return text;
        }

        private static RectTransform _ensureHudImage(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var existing = parent.Find(name);
            RectTransform rect;
            Image image;
            if (existing == null)
            {
                rect = _createRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                    Vector2.zero);
                image = rect.gameObject.AddComponent<Image>();
                image.raycastTarget = false;
            }
            else
            {
                rect = existing as RectTransform;
                image = existing.GetComponent<Image>();
            }

            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            image.color = color;
            return rect;
        }

        private static RectTransform _createRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private static RectTransform _createPanel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 position)
        {
            var rect = _createRect(name, parent, anchor, anchor, Vector2.zero, Vector2.zero);
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.04f, 0.055f, 0.92f);
            return rect;
        }

        private static Text _createText(string name, Transform parent, string value, int size, TextAnchor alignment, Color color,
            Vector2 position, Vector2 dimensions, Vector2? anchor = null, Vector2? pivot = null)
        {
            var resolvedAnchor = anchor ?? new Vector2(0f, 0.5f);
            var resolvedPivot = pivot ?? new Vector2(0f, 0.5f);
            var rect = _createRect(name, parent, resolvedAnchor, resolvedAnchor, Vector2.zero, Vector2.zero);
            rect.pivot = resolvedPivot;
            rect.anchoredPosition = position + (resolvedPivot.x < 0.5f ? new Vector2(32f, 0f) : Vector2.zero);
            rect.sizeDelta = dimensions;
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = color;
            text.text = value;
            return text;
        }

        private static Button _createButton(string name, Transform parent, string label, float y, float height = 54f,
            float width = 470f)
        {
            var rect = _createRect(name, parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(32f, y);
            rect.sizeDelta = new Vector2(width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.13f, 0.16f, 0.96f);
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.2f, 0.9f, 1f);
            colors.selectedColor = new Color(0.2f, 0.9f, 1f);
            colors.pressedColor = new Color(0.95f, 0.7f, 0.2f);
            button.colors = colors;
            var text = _createText("Label", rect, label, 18, TextAnchor.MiddleLeft, Color.white, new Vector2(-14f, 0f),
                new Vector2(width - 50f, height));
            text.raycastTarget = false;
            return button;
        }

        private static RectTransform _createSelectionRail(string name, Transform parent, Vector2 position, float height)
        {
            var rail = _createRect(name, parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            rail.pivot = new Vector2(0f, 0.5f);
            rail.anchoredPosition = position;
            rail.sizeDelta = new Vector2(5f, height);
            rail.gameObject.AddComponent<Image>().color = new Color(0.98f, 0.72f, 0.2f, 1f);
            rail.SetAsLastSibling();
            return rail;
        }

        private static void _createDetailCard(Transform parent, Vector2 position, Vector2 size)
        {
            var card = _createRect("Detail Card", parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                Vector2.zero);
            card.pivot = new Vector2(0f, 0.5f);
            card.anchoredPosition = position;
            card.sizeDelta = size;
            card.gameObject.AddComponent<Image>().color = new Color(0.045f, 0.07f, 0.085f, 0.96f);
            card.SetAsFirstSibling();
        }

        private static RawImage _createRawImage(string name, Transform parent, Texture texture, Vector2 position, Vector2 size)
        {
            var rect = _createRect(name, parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static T[] _append<T>(T[] values, T finalValue)
        {
            var result = new T[values.Length + 1];
            System.Array.Copy(values, result, values.Length);
            result[values.Length] = finalValue;
            return result;
        }

        private static void _set(SerializedObject serialized, string propertyName, Object value)
        {
            serialized.FindProperty(propertyName).objectReferenceValue = value;
        }

        private static void _setArray<T>(SerializedObject serialized, string propertyName, T[] values) where T : Object
        {
            var property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
