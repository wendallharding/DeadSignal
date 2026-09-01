using System;
using DeadSignal.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadSignal.Editor
{
    public static class DeadSignalOutcomePresentationSetup
    {
        private const string HUD_PREFAB_PATH = "Assets/DeadSignal/Resources/UI/DeadSignalHud.prefab";

        [MenuItem("DEAD SIGNAL/Ensure Outcome Presentation")]
        public static void EnsureAssets()
        {
            var root = PrefabUtility.LoadPrefabContents(HUD_PREFAB_PATH);
            try
            {
                var outcome = root.transform.Find("Outcome Overlay") as RectTransform;
                if (outcome == null)
                {
                    throw new InvalidOperationException("The authored HUD prefab is missing its Outcome Overlay.");
                }

                var result = _required<Text>(outcome, "Result");
                var detail = _required<Text>(outcome, "Detail");
                var report = _required<Text>(outcome, "Run Report");
                var restartHint = _required<Text>(outcome, "Restart");
                var insignia = _required<RawImage>(outcome, "System Glyph");
                var restart = _required<Button>(outcome, "Restart Run");
                var mainMenu = _required<Button>(outcome, "Main Menu");

                var frame = _ensureImage(outcome, "Outcome Frame", new Color(0.055f, 0.012f, 0.016f, 0.97f));
                _setRect(frame.rectTransform, Vector2.one * 0.5f, Vector2.zero, new Vector2(1040f, 640f));
                frame.transform.SetAsFirstSibling();

                var accentRail = _ensureImage(outcome, "Outcome Accent Rail", new Color(0.92f, 0.12f, 0.08f, 0.95f));
                _setRect(accentRail.rectTransform, Vector2.one * 0.5f, new Vector2(-498f, 0f), new Vector2(4f, 580f));
                accentRail.transform.SetSiblingIndex(1);

                var protocol = _ensureText(outcome, "Outcome Protocol", "STATION RECOVERY  /  TERMINAL STATE", 12,
                    TextAnchor.MiddleLeft, new Color(0.72f, 0.78f, 0.8f));
                _setRect(protocol.rectTransform, Vector2.one * 0.5f, new Vector2(0f, 266f), new Vector2(880f, 24f));

                var causeLabel = _ensureText(outcome, "Failure Cause Label", "FAILURE CAUSE", 11,
                    TextAnchor.MiddleLeft, new Color(0.98f, 0.72f, 0.2f));
                _setRect(causeLabel.rectTransform, Vector2.one * 0.5f, new Vector2(2f, 126f), new Vector2(640f, 20f));

                var evidenceLabel = _ensureText(outcome, "Run Evidence Label", "RUN EVIDENCE  /  LAST STABLE TELEMETRY", 11,
                    TextAnchor.MiddleLeft, new Color(0.98f, 0.72f, 0.2f));
                _setRect(evidenceLabel.rectTransform, Vector2.one * 0.5f, new Vector2(0f, 40f), new Vector2(880f, 20f));

                var optionsLabel = _ensureText(outcome, "Recovery Options Label", "RECOVERY OPTIONS", 11,
                    TextAnchor.MiddleLeft, new Color(0.98f, 0.72f, 0.2f));
                _setRect(optionsLabel.rectTransform, Vector2.one * 0.5f, new Vector2(0f, -132f), new Vector2(880f, 20f));

                var selectionRail = _ensureImage(outcome, "Outcome Selection Rail", new Color(0.98f, 0.72f, 0.2f, 1f));
                _setRect(selectionRail.rectTransform, Vector2.one * 0.5f, new Vector2(-296f, -195f), new Vector2(5f, 54f));

                var selectionDetail = _ensureText(outcome, "Outcome Selection Detail", "RESTART FROM STATION ENTRY", 11,
                    TextAnchor.MiddleCenter, new Color(0.98f, 0.72f, 0.2f));
                _setRect(selectionDetail.rectTransform, Vector2.one * 0.5f, new Vector2(0f, -244f), new Vector2(880f, 22f));

                var inputHint = _ensureText(outcome, "Outcome Input Hint",
                    "ARROWS / STICK  NAVIGATE     ENTER / A  SELECT     R  QUICK RESTART", 11,
                    TextAnchor.MiddleCenter, new Color(0.58f, 0.66f, 0.7f));
                _setRect(inputHint.rectTransform, Vector2.one * 0.5f, new Vector2(0f, -276f), new Vector2(880f, 22f));

                _setRect(insignia.rectTransform, Vector2.one * 0.5f, new Vector2(-410f, 185f), new Vector2(104f, 104f));
                insignia.color = new Color(0.8f, 0.14f, 0.12f, 0.92f);

                _configureText(result, new Vector2(2f, 194f), new Vector2(640f, 58f), 42, TextAnchor.MiddleLeft,
                    new Color(1f, 0.12f, 0.08f));
                _configureText(detail, new Vector2(2f, 92f), new Vector2(640f, 44f), 17, TextAnchor.UpperLeft,
                    new Color(0.96f, 0.9f, 0.88f));
                _configureText(report, new Vector2(0f, -42f), new Vector2(880f, 128f), 15, TextAnchor.UpperLeft,
                    new Color(0.78f, 0.84f, 0.86f));
                report.resizeTextForBestFit = true;
                report.resizeTextMinSize = 11;
                report.resizeTextMaxSize = 15;
                _configureText(restartHint, new Vector2(0f, -112f), new Vector2(880f, 26f), 12, TextAnchor.MiddleCenter,
                    new Color(0.58f, 0.66f, 0.7f));

                _configureButton(restart, new Vector2(-145f, -195f));
                _configureButton(mainMenu, new Vector2(145f, -195f));
                restart.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = mainMenu, selectOnRight = mainMenu };
                mainMenu.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = restart, selectOnRight = restart };

                var presentation = outcome.GetComponent<OutcomePresentation>();
                if (presentation == null)
                {
                    presentation = outcome.gameObject.AddComponent<OutcomePresentation>();
                }

                var titleGroup = _ensureCanvasGroup(result.gameObject);
                var causeGroup = _ensureCanvasGroup(detail.gameObject);
                var evidenceGroup = _ensureCanvasGroup(report.gameObject);
                var actionGroups = new[]
                {
                    _ensureCanvasGroup(restartHint.gameObject),
                    _ensureCanvasGroup(restart.gameObject),
                    _ensureCanvasGroup(mainMenu.gameObject)
                };

                var serialized = new SerializedObject(presentation);
                _set(serialized, "m_backdrop", outcome.GetComponent<Image>());
                _set(serialized, "m_frame", frame);
                _set(serialized, "m_accentRail", accentRail);
                _set(serialized, "m_selectionRail", selectionRail.rectTransform);
                _set(serialized, "m_insignia", insignia);
                _set(serialized, "m_protocol", protocol);
                _set(serialized, "m_causeLabel", causeLabel);
                _set(serialized, "m_evidenceLabel", evidenceLabel);
                _set(serialized, "m_optionsLabel", optionsLabel);
                _set(serialized, "m_selectionDetail", selectionDetail);
                _set(serialized, "m_restartButton", restart);
                _set(serialized, "m_mainMenuButton", mainMenu);
                _set(serialized, "m_titleGroup", titleGroup);
                _set(serialized, "m_causeGroup", causeGroup);
                _set(serialized, "m_evidenceGroup", evidenceGroup);
                _setArray(serialized, "m_actionGroups", actionGroups);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var hud = new SerializedObject(root.GetComponent<DeadSignalHud>());
                _set(hud, "m_outcomePresentation", presentation);
                hud.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, HUD_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static T _required<T>(Transform parent, string name) where T : Component
        {
            var child = parent.Find(name);
            var component = child == null ? null : child.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException($"Outcome presentation is missing '{name}' ({typeof(T).Name}).");
            }
            return component;
        }

        private static Image _ensureImage(RectTransform parent, string name, Color color)
        {
            var child = parent.Find(name) as RectTransform;
            if (child == null)
            {
                child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                    .GetComponent<RectTransform>();
                child.SetParent(parent, false);
            }
            var image = child.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text _ensureText(RectTransform parent, string name, string value, int size, TextAnchor anchor, Color color)
        {
            var child = parent.Find(name) as RectTransform;
            if (child == null)
            {
                child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text))
                    .GetComponent<RectTransform>();
                child.SetParent(parent, false);
            }
            var text = child.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void _configureText(Text text, Vector2 position, Vector2 size, int fontSize,
            TextAnchor alignment, Color color)
        {
            _setRect(text.rectTransform, Vector2.one * 0.5f, position, size);
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
        }

        private static void _configureButton(Button button, Vector2 position)
        {
            _setRect((RectTransform)button.transform, Vector2.one * 0.5f, position, new Vector2(270f, 54f));
            var colors = button.colors;
            colors.normalColor = new Color(0.07f, 0.09f, 0.11f, 0.98f);
            colors.highlightedColor = new Color(0.16f, 0.28f, 0.32f, 1f);
            colors.selectedColor = new Color(0.16f, 0.28f, 0.32f, 1f);
            colors.pressedColor = new Color(0.98f, 0.72f, 0.2f, 1f);
            button.colors = colors;
        }

        private static void _setRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static CanvasGroup _ensureCanvasGroup(GameObject target)
        {
            var group = target.GetComponent<CanvasGroup>();
            return group == null ? target.AddComponent<CanvasGroup>() : group;
        }

        private static void _set(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            serialized.FindProperty(propertyName).objectReferenceValue = value;
        }

        private static void _setArray<T>(SerializedObject serialized, string propertyName, T[] values) where T : UnityEngine.Object
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
