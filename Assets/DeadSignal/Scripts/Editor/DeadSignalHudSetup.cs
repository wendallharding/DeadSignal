using System;
using UnityEditor;
using UnityEngine;
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
            _hasCompositionPrefab();

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
    }
}
