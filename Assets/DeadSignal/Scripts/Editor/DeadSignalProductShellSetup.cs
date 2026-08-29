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

            var root = new GameObject("Main Menu Overlay", typeof(RectTransform), typeof(DeadSignalShellController));
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
                backdropImage.color = new Color(0.72f, 0.76f, 0.8f, 1f);
                var shade = _createRect("Readability Shade", overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                shade.gameObject.AddComponent<Image>().color = new Color(0.01f, 0.015f, 0.025f, 0.58f);

                var mainPanel = _createPanel("Main Panel", overlay, new Vector2(0.08f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(560f, 650f), new Vector2(0f, 0f));
                _createText("Title", mainPanel, "DEAD SIGNAL", 54, TextAnchor.MiddleLeft, new Color(0.92f, 0.98f, 1f),
                    new Vector2(0f, 238f), new Vector2(490f, 72f));
                _createText("Subtitle", mainPanel, "STATION RESTORATION PROTOCOL", 17, TextAnchor.MiddleLeft,
                    new Color(0.15f, 0.9f, 1f), new Vector2(0f, 193f), new Vector2(490f, 34f));
                _createText("Route", mainPanel, "RESTART  >  EXTEND  >  REBUILD  >  WITHDRAW", 13, TextAnchor.MiddleLeft,
                    new Color(0.8f, 0.67f, 0.27f), new Vector2(0f, 155f), new Vector2(510f, 28f));

                var start = _createButton("Start Run", mainPanel, "START RUN", 80f);
                var settings = _createButton("Settings", mainPanel, "SETTINGS", 12f);
                var controls = _createButton("Controls", mainPanel, "CONTROLS", -56f);
                var quit = _createButton("Quit", mainPanel, "QUIT", -124f);
                _createText("Input Hint", mainPanel, "ARROWS / STICK  NAVIGATE     ENTER / A  SELECT", 12, TextAnchor.MiddleLeft,
                    new Color(0.65f, 0.72f, 0.76f), new Vector2(0f, -222f), new Vector2(500f, 30f));

                var settingsPanel = _createPanel("Settings Panel", overlay, new Vector2(0.08f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(620f, 650f), Vector2.zero);
                _createText("Title", settingsPanel, "SETTINGS", 40, TextAnchor.MiddleLeft, new Color(0.92f, 0.98f, 1f),
                    new Vector2(0f, 245f), new Vector2(520f, 60f));
                _createText("Hint", settingsPanel, "PRESENTATION COMFORT  //  SAVED LOCALLY", 13, TextAnchor.MiddleLeft,
                    new Color(0.15f, 0.9f, 1f), new Vector2(0f, 205f), new Vector2(520f, 28f));
                var settingButtons = new Button[4];
                var settingTexts = new Text[4];
                var settingNames = new[] { "Steady Camera", "Reduced Flashes", "High Contrast", "Signal Audio" };
                for (var i = 0; i < settingButtons.Length; i++)
                {
                    settingButtons[i] = _createButton(settingNames[i], settingsPanel, settingNames[i].ToUpperInvariant(), 125f - i * 68f);
                    settingTexts[i] = settingButtons[i].GetComponentInChildren<Text>();
                }
                var settingsBack = _createButton("Back", settingsPanel, "BACK", -190f);

                var controlsPanel = _createPanel("Controls Panel", overlay, new Vector2(0.08f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(650f, 720f), Vector2.zero);
                _createText("Title", controlsPanel, "CONTROLS", 40, TextAnchor.MiddleLeft, new Color(0.92f, 0.98f, 1f),
                    new Vector2(0f, 282f), new Vector2(540f, 60f));
                _createText("Hint", controlsPanel, "SELECT A KEYBOARD ACTION TO REBIND", 13, TextAnchor.MiddleLeft,
                    new Color(0.15f, 0.9f, 1f), new Vector2(0f, 242f), new Vector2(540f, 28f));
                var rebindButtons = new Button[7];
                var rebindTexts = new Text[7];
                var rebindNames = new[] { "Move Up", "Move Down", "Move Left", "Move Right", "Fire", "Interact", "Reset Bindings" };
                for (var i = 0; i < rebindButtons.Length; i++)
                {
                    rebindButtons[i] = _createButton(rebindNames[i], controlsPanel, rebindNames[i].ToUpperInvariant(), 170f - i * 52f, 44f);
                    rebindTexts[i] = rebindButtons[i].GetComponentInChildren<Text>();
                }
                var status = _createText("Status", controlsPanel, string.Empty, 12, TextAnchor.MiddleLeft,
                    new Color(0.72f, 0.78f, 0.82f), new Vector2(0f, -216f), new Vector2(560f, 26f));
                var controlsBack = _createButton("Back", controlsPanel, "BACK", -270f, 44f);

                settingsPanel.gameObject.SetActive(false);
                controlsPanel.gameObject.SetActive(false);
                var serialized = new SerializedObject(controller);
                _set(serialized, "m_menuOverlay", root);
                _set(serialized, "m_mainPanel", mainPanel.gameObject);
                _set(serialized, "m_settingsPanel", settingsPanel.gameObject);
                _set(serialized, "m_controlsPanel", controlsPanel.gameObject);
                _set(serialized, "m_menuCanvasGroup", menuCanvasGroup);
                _set(serialized, "m_transitionTuning", transitionTuning);
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
                (pause.Find("Resume") as RectTransform).anchoredPosition = new Vector2(0f, -376f);
                (outcome.Find("Restart") as RectTransform).anchoredPosition = new Vector2(0f, -154f);

                var serialized = new SerializedObject(hud);
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
            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            var button = _createButton(name, parent, label, position.y, 54f);
            var rect = button.transform as RectTransform;
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
            Vector2 position, Vector2 dimensions)
        {
            var rect = _createRect(name, parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = position + new Vector2(32f, 0f);
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

        private static Button _createButton(string name, Transform parent, string label, float y, float height = 54f)
        {
            var rect = _createRect(name, parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(32f, y);
            rect.sizeDelta = new Vector2(470f, height);
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
                new Vector2(420f, height));
            text.raycastTarget = false;
            return button;
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
