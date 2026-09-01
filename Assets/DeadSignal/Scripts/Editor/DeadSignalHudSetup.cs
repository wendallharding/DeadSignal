using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DeadSignal.Presentation;

namespace DeadSignal.Editor
{
    public static class DeadSignalHudSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/UI/SignalReserveConduit.png";
        private const string TUNING_PATH = "Assets/DeadSignal/Resources/Tuning/SignalHudTuning.asset";
        private const string DEBRIEF_TEXTURE_PATH = "Assets/DeadSignal/Resources/UI/RunDebriefInsignia.png";
        private const string HUD_PREFAB_PATH = "Assets/DeadSignal/Resources/UI/DeadSignalHud.prefab";
        private const string EDGE_INDICATOR_TUNING_PATH =
            "Assets/DeadSignal/Resources/Tuning/EdgeIndicatorTuning.asset";

        public static bool HasAssets =>
            AssetDatabase.LoadAssetAtPath<Sprite>(TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Texture2D>(DEBRIEF_TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<SignalHudTuning>(TUNING_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<EdgeIndicatorTuning>(EDGE_INDICATOR_TUNING_PATH) != null &&
            _hasCompositionPrefab() &&
            _hasSignalInstrumentPrefab() &&
            _hasObjectiveCardPrefab() &&
            _hasThreatInstrumentPrefab() &&
            _hasInteractionPromptPrefab() &&
            _hasOutcomePresentationPrefab();

        public static void EnsureAssets()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Signal reserve texture at {TEXTURE_PATH}.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            var debriefImporter = AssetImporter.GetAtPath(DEBRIEF_TEXTURE_PATH) as TextureImporter;
            if (debriefImporter == null)
            {
                throw new InvalidOperationException($"Could not find the run debrief texture at {DEBRIEF_TEXTURE_PATH}.");
            }

            debriefImporter.alphaIsTransparency = true;
            debriefImporter.mipmapEnabled = false;
            debriefImporter.maxTextureSize = 1024;
            debriefImporter.wrapMode = TextureWrapMode.Clamp;
            debriefImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            debriefImporter.SaveAndReimport();

            if (AssetDatabase.LoadAssetAtPath<SignalHudTuning>(TUNING_PATH) == null)
            {
                var tuning = ScriptableObject.CreateInstance<SignalHudTuning>();
                AssetDatabase.CreateAsset(tuning, TUNING_PATH);
            }

            if (AssetDatabase.LoadAssetAtPath<EdgeIndicatorTuning>(EDGE_INDICATOR_TUNING_PATH) == null)
            {
                var tuning = ScriptableObject.CreateInstance<EdgeIndicatorTuning>();
                AssetDatabase.CreateAsset(tuning, EDGE_INDICATOR_TUNING_PATH);
            }

            _ensureCompositionPrefab();
            _ensureSignalInstrumentPrefab();
            _ensureObjectiveCardPrefab();
            _ensureThreatInstrumentPrefab();
            _ensureInteractionPromptPrefab();
            DeadSignalOutcomePresentationSetup.EnsureAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("Signal HUD assets were not imported successfully.");
            }
        }

        private static bool _hasCompositionPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HUD_PREFAB_PATH);
            var layout = prefab == null ? null : prefab.GetComponent<HudCompositionLayout>();
            var scaler = prefab == null ? null : prefab.GetComponent<UnityEngine.UI.CanvasScaler>();
            return layout != null && layout.IsConfigured && scaler != null &&
                   Mathf.Approximately(scaler.matchWidthOrHeight, 1f) &&
                   layout.CompositionFrame.parent == layout.SafeArea;
        }

        private static bool _hasSignalInstrumentPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HUD_PREFAB_PATH);
            var instrument = prefab == null ? null : prefab.GetComponentInChildren<SignalReserveInstrument>(true);
            return instrument != null && instrument.IsConfigured;
        }

        private static bool _hasObjectiveCardPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HUD_PREFAB_PATH);
            var beacon = prefab == null ? null : prefab.GetComponent<ObjectiveBeaconHud>();
            return beacon != null && beacon.IsPresentationConfigured;
        }

        private static bool _hasInteractionPromptPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HUD_PREFAB_PATH);
            var prompt = prefab == null ? null : prefab.GetComponentInChildren<InteractionPromptHud>(true);
            return prompt != null && prompt.IsConfigured;
        }

        private static bool _hasThreatInstrumentPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HUD_PREFAB_PATH);
            var instrument = prefab == null ? null : prefab.GetComponentInChildren<ThreatHudInstrument>(true);
            return instrument != null && instrument.IsConfigured;
        }

        private static bool _hasOutcomePresentationPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HUD_PREFAB_PATH);
            var presentation = prefab == null ? null : prefab.GetComponentInChildren<OutcomePresentation>(true);
            return presentation != null && presentation.IsConfigured;
        }

        private static void _ensureCompositionPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(HUD_PREFAB_PATH);
            try
            {
                var runHud = root.transform.Find("Run HUD") as RectTransform;
                if (runHud == null)
                {
                    throw new InvalidOperationException("The authored HUD prefab is missing its Run HUD root.");
                }

                var frame = runHud.Find("Composition Frame") as RectTransform;
                if (frame == null)
                {
                    var frameObject = new GameObject("Composition Frame", typeof(RectTransform));
                    frame = frameObject.GetComponent<RectTransform>();
                    frame.SetParent(runHud, false);
                }

                frame.anchorMin = new Vector2(0.5f, 0f);
                frame.anchorMax = new Vector2(0.5f, 1f);
                frame.pivot = new Vector2(0.5f, 0.5f);
                frame.anchoredPosition = Vector2.zero;
                frame.sizeDelta = new Vector2(2160f, 0f);

                foreach (var panelName in new[] { "Signal Status", "Objective Status", "Feedback", "Context Prompt" })
                {
                    var panel = runHud.Find(panelName) as RectTransform;
                    if (panel != null)
                    {
                        panel.SetParent(frame, false);
                    }
                }

                var layout = root.GetComponent<HudCompositionLayout>();
                if (layout == null)
                {
                    layout = root.AddComponent<HudCompositionLayout>();
                }
                layout.Configure(runHud, frame);

                var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (scaler == null)
                {
                    throw new InvalidOperationException("The authored HUD prefab is missing its CanvasScaler.");
                }
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;

                PrefabUtility.SaveAsPrefabAsset(root, HUD_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _ensureSignalInstrumentPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(HUD_PREFAB_PATH);
            try
            {
                var status = root.transform.Find("Run HUD/Composition Frame/Signal Status") as RectTransform;
                if (status == null)
                {
                    throw new InvalidOperationException("The authored HUD prefab is missing its Signal Status panel.");
                }

                status.sizeDelta = new Vector2(350f, 178f);
                var bar = status.Find("Signal Bar") as RectTransform;
                var fill = bar == null ? null : bar.Find("Fill")?.GetComponent<Image>();
                var reserveText = status.Find("Signal")?.GetComponent<Text>();
                var flowText = status.Find("Zone")?.GetComponent<Text>();
                if (bar == null || fill == null || reserveText == null || flowText == null)
                {
                    throw new InvalidOperationException("The authored Signal Status panel is missing its reserve controls.");
                }

                bar.sizeDelta = new Vector2(318f, 16f);
                reserveText.rectTransform.sizeDelta = new Vector2(318f, 26f);
                flowText.rectTransform.anchoredPosition = new Vector2(16f, -116f);
                flowText.rectTransform.sizeDelta = new Vector2(318f, 22f);
                flowText.fontSize = 13;
                flowText.fontStyle = FontStyle.Bold;

                var changeBand = _ensureImage("Change Band", bar, new Color(1f, 0.62f, 0.12f, 0.42f));
                changeBand.rectTransform.anchorMin = Vector2.zero;
                changeBand.rectTransform.anchorMax = Vector2.one;
                changeBand.rectTransform.offsetMin = Vector2.zero;
                changeBand.rectTransform.offsetMax = Vector2.zero;

                var marker = _ensureImage("Transaction Marker", bar, new Color(1f, 0.72f, 0.18f, 1f));
                marker.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                marker.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                marker.rectTransform.anchoredPosition = Vector2.zero;
                marker.rectTransform.sizeDelta = new Vector2(3f, 0f);
                marker.gameObject.SetActive(false);

                var transactionText = _ensureText("Transaction Preview", status);
                transactionText.rectTransform.anchorMin = new Vector2(0f, 1f);
                transactionText.rectTransform.anchorMax = new Vector2(0f, 1f);
                transactionText.rectTransform.pivot = new Vector2(0f, 1f);
                transactionText.rectTransform.anchoredPosition = new Vector2(16f, -141f);
                transactionText.rectTransform.sizeDelta = new Vector2(318f, 22f);
                transactionText.text = "PREVIEW";
                transactionText.fontSize = 13;
                transactionText.fontStyle = FontStyle.Bold;
                transactionText.alignment = TextAnchor.UpperLeft;
                transactionText.color = new Color(1f, 0.72f, 0.18f, 1f);
                transactionText.gameObject.SetActive(false);

                var instrument = status.GetComponent<SignalReserveInstrument>();
                if (instrument == null)
                {
                    instrument = status.gameObject.AddComponent<SignalReserveInstrument>();
                }
                instrument.Configure(fill, changeBand, marker, reserveText, flowText, transactionText);

                PrefabUtility.SaveAsPrefabAsset(root, HUD_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _ensureObjectiveCardPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(HUD_PREFAB_PATH);
            try
            {
                var beacon = root.GetComponent<ObjectiveBeaconHud>();
                var panel = root.transform.Find("Run HUD/Objective Beacon") as RectTransform;
                if (beacon == null || panel == null)
                {
                    throw new InvalidOperationException("The authored HUD prefab is missing its objective beacon.");
                }

                panel.sizeDelta = new Vector2(500f, 94f);
                var background = panel.GetComponent<Image>();
                background.color = new Color(0.012f, 0.022f, 0.032f, 0.94f);
                background.raycastTarget = false;
                if (panel.GetComponent<CanvasGroup>() == null)
                {
                    panel.gameObject.AddComponent<CanvasGroup>();
                }

                var direction = panel.Find("Direction")?.GetComponent<RawImage>();
                var title = panel.Find("Objective")?.GetComponent<Text>();
                var hint = panel.Find("Hint")?.GetComponent<Text>();
                var distance = panel.Find("Distance")?.GetComponent<Text>();
                if (direction == null || title == null || hint == null || distance == null)
                {
                    throw new InvalidOperationException("The authored objective beacon is missing its base controls.");
                }

                _setRect(direction.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(36f, 0f), new Vector2(46f, 46f));
                direction.color = new Color(1f, 0.62f, 0.12f, 1f);

                var accent = _ensureImage("Objective Accent", panel, new Color(1f, 0.58f, 0.08f, 0.95f));
                _setRect(accent.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                    new Vector2(0f, 0.5f), Vector2.zero, new Vector2(4f, 0f));

                var room = _ensureText("Room", panel);
                _styleText(room, 11, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.68f, 0.78f, 0.82f, 1f));
                _setRect(room.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(68f, -8f), new Vector2(260f, 16f));

                var phase = _ensureText("Phase", panel);
                _styleText(phase, 11, FontStyle.Bold, TextAnchor.UpperRight, new Color(1f, 0.68f, 0.22f, 1f));
                _setRect(phase.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(1f, 1f), new Vector2(-12f, -8f), new Vector2(104f, 16f));

                _styleText(title, 16, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
                _setRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(68f, -25f), new Vector2(410f, 20f));

                var verb = _ensureText("Verb", panel);
                _styleText(verb, 14, FontStyle.Bold, TextAnchor.UpperLeft, new Color(1f, 0.72f, 0.24f, 1f));
                _setRect(verb.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(68f, -47f), new Vector2(410f, 18f));

                _styleText(hint, 10, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.72f, 0.82f, 0.86f, 1f));
                _setRect(hint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(68f, -68f), new Vector2(350f, 16f));

                _styleText(distance, 13, FontStyle.Bold, TextAnchor.LowerRight, Color.white);
                _setRect(distance.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                    new Vector2(1f, 0f), new Vector2(-12f, 9f), new Vector2(70f, 18f));

                beacon.ConfigurePresentation(accent, room, phase, title, verb, hint, distance);
                PrefabUtility.SaveAsPrefabAsset(root, HUD_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _ensureInteractionPromptPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(HUD_PREFAB_PATH);
            try
            {
                var panel = root.transform.Find("Run HUD/Composition Frame/Context Prompt") as RectTransform;
                if (panel == null)
                {
                    throw new InvalidOperationException("The authored HUD prefab is missing its Context Prompt panel.");
                }

                panel.sizeDelta = new Vector2(560f, 78f);
                panel.anchoredPosition = new Vector2(0f, 42f);
                var background = panel.GetComponent<Image>();
                background.color = new Color(0.012f, 0.022f, 0.032f, 0.96f);
                background.raycastTarget = false;
                var canvasGroup = panel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
                }
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;

                var legacyGlyph = panel.Find("Use Glyph");
                if (legacyGlyph != null)
                {
                    legacyGlyph.gameObject.SetActive(false);
                }
                var legacyPrompt = panel.Find("Prompt");
                if (legacyPrompt != null)
                {
                    legacyPrompt.gameObject.SetActive(false);
                }

                var accent = _ensureImage("Prompt Accent", panel, new Color(0.08f, 0.94f, 1f, 1f));
                _setRect(accent.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                    new Vector2(0f, 0.5f), Vector2.zero, new Vector2(4f, 0f));

                var state = _ensureText("Prompt State", panel);
                _styleText(state, 10, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.08f, 0.94f, 1f, 1f));
                _setRect(state.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(14f, -6f), new Vector2(160f, 16f));

                var primaryGlyphBox = _ensureImage("Primary Glyph Box", panel, new Color(0.045f, 0.11f, 0.13f, 1f));
                _setRect(primaryGlyphBox.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(14f, -27f), new Vector2(48f, 38f));
                var primaryGlyph = _ensureText("Glyph", primaryGlyphBox.transform);
                _styleText(primaryGlyph, 15, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.08f, 0.94f, 1f, 1f));
                _stretch(primaryGlyph.rectTransform, 3f);

                var primaryAction = _ensureText("Primary Action", panel);
                _styleText(primaryAction, 15, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
                _setRect(primaryAction.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(72f, -25f), new Vector2(218f, 21f));

                var detail = _ensureText("Prompt Detail", panel);
                _styleText(detail, 11, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.7f, 0.8f, 0.84f, 1f));
                _setRect(detail.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(72f, -48f), new Vector2(218f, 18f));

                var secondary = _ensureImage("Secondary Action", panel, new Color(0.022f, 0.052f, 0.064f, 0.95f));
                _setRect(secondary.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(300f, -22f), new Vector2(246f, 48f));
                var secondaryGlyphBox = _ensureImage("Glyph Box", secondary.transform, new Color(0.045f, 0.11f, 0.13f, 1f));
                _setRect(secondaryGlyphBox.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(7f, 0f), new Vector2(42f, 36f));
                var secondaryGlyph = _ensureText("Glyph", secondaryGlyphBox.transform);
                _styleText(secondaryGlyph, 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.08f, 0.94f, 1f, 1f));
                _stretch(secondaryGlyph.rectTransform, 3f);
                var secondaryAction = _ensureText("Action", secondary.transform);
                _styleText(secondaryAction, 11, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
                _setRect(secondaryAction.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                    new Vector2(0f, 0.5f), new Vector2(57f, 0f), new Vector2(180f, 0f));
                secondaryAction.horizontalOverflow = HorizontalWrapMode.Wrap;
                secondaryAction.verticalOverflow = VerticalWrapMode.Truncate;

                var prompt = panel.GetComponent<InteractionPromptHud>();
                if (prompt == null)
                {
                    prompt = panel.gameObject.AddComponent<InteractionPromptHud>();
                }
                prompt.Configure(
                    background,
                    accent,
                    state,
                    primaryGlyphBox.gameObject,
                    primaryGlyph,
                    primaryAction,
                    detail,
                    secondary.gameObject,
                    secondaryGlyph,
                    secondaryAction,
                    canvasGroup);

                PrefabUtility.SaveAsPrefabAsset(root, HUD_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _ensureThreatInstrumentPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(HUD_PREFAB_PATH);
            try
            {
                var frame = root.transform.Find("Run HUD/Composition Frame") as RectTransform;
                var panel = frame == null ? null : frame.Find("Threat") as RectTransform;
                panel ??= root.transform.Find("Run HUD/Composition Frame/Objective Status/Threat") as RectTransform;
                if (panel == null)
                {
                    throw new InvalidOperationException("The authored HUD prefab is missing its Threat panel.");
                }

                panel.SetParent(frame, false);
                _setRect(panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(18f, -174f), new Vector2(356f, 88f));
                var legacyText = panel.GetComponent<Text>();
                legacyText.enabled = false;

                var background = _ensureImage("Threat Panel", panel, new Color(0.018f, 0.028f, 0.04f, 0.94f));
                _stretch(background.rectTransform, 0f);
                background.color = new Color(0.018f, 0.028f, 0.04f, 0.94f);
                background.raycastTarget = false;

                var canvasGroup = background.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = background.gameObject.AddComponent<CanvasGroup>();
                }
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;

                var accent = _ensureImage("Threat Accent", background.transform, new Color(1f, 0.25f, 0.18f, 1f));
                _setRect(accent.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                    new Vector2(0f, 0.5f), Vector2.zero, new Vector2(4f, 0f));

                var header = _ensureText("Threat Header", background.transform);
                _styleText(header, 10, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.58f, 0.7f, 0.74f, 1f));
                _setRect(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(12f, -6f), new Vector2(-24f, 15f));

                var role = _ensureText("Threat Role", background.transform);
                _styleText(role, 14, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
                _setRect(role.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(12f, -23f), new Vector2(-154f, 20f));

                var state = _ensureText("Threat State", background.transform);
                _styleText(state, 11, FontStyle.Bold, TextAnchor.UpperRight, new Color(1f, 0.4f, 0.32f, 1f));
                _setRect(state.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(1f, 1f), new Vector2(-10f, -24f), new Vector2(144f, 18f));

                var healthTrack = _ensureImage("Threat Health Track", background.transform, new Color(0.13f, 0.07f, 0.08f, 1f));
                _setRect(healthTrack.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(12f, 29f), new Vector2(-90f, 5f));
                var healthFill = _ensureImage("Fill", healthTrack.transform, new Color(1f, 0.25f, 0.18f, 1f));
                _stretch(healthFill.rectTransform, 0f);
                healthFill.type = Image.Type.Filled;
                healthFill.fillMethod = Image.FillMethod.Horizontal;
                healthFill.fillOrigin = 0;

                var health = _ensureText("Threat Health", background.transform);
                _styleText(health, 11, FontStyle.Bold, TextAnchor.MiddleRight, Color.white);
                _setRect(health.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                    new Vector2(1f, 0f), new Vector2(-10f, 25f), new Vector2(68f, 18f));

                var footer = _ensureText("Threat Footer", background.transform);
                _styleText(footer, 10, FontStyle.Normal, TextAnchor.LowerLeft, new Color(0.66f, 0.76f, 0.79f, 1f));
                _setRect(footer.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(12f, 6f), new Vector2(-24f, 16f));

                var instrument = background.GetComponent<ThreatHudInstrument>();
                if (instrument == null)
                {
                    instrument = background.gameObject.AddComponent<ThreatHudInstrument>();
                }
                instrument.Configure(background, accent, healthFill, header, role, state, health, footer, canvasGroup);

                PrefabUtility.SaveAsPrefabAsset(root, HUD_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _styleText(Text text, int size, FontStyle style, TextAnchor alignment, Color color)
        {
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
        }

        private static void _setRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void _stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one * 0.5f;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = Vector2.one * -inset;
        }

        private static Image _ensureImage(string name, Transform parent, Color color)
        {
            var child = parent.Find(name);
            var image = child == null ? null : child.GetComponent<Image>();
            if (image == null)
            {
                var childObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                childObject.transform.SetParent(parent, false);
                image = childObject.GetComponent<Image>();
            }
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text _ensureText(string name, Transform parent)
        {
            var child = parent.Find(name);
            var text = child == null ? null : child.GetComponent<Text>();
            if (text != null)
            {
                return text;
            }

            var childObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            childObject.transform.SetParent(parent, false);
            text = childObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.raycastTarget = false;
            return text;
        }
    }
}
