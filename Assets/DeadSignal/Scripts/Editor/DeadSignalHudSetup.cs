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
            _hasSignalInstrumentPrefab();

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
