using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DeadSignal.Presentation;

namespace DeadSignal.Editor
{
    public static class DeadSignalScreenFeedbackSetup
    {
        private const string HUD_PREFAB_PATH = "Assets/DeadSignal/Resources/UI/DeadSignalHud.prefab";
        private const string TUNING_PATH = "Assets/DeadSignal/Resources/Tuning/ScreenFeedbackTuning.asset";

        public static bool HasAssets =>
            AssetDatabase.LoadAssetAtPath<ScreenFeedbackTuning>(TUNING_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(HUD_PREFAB_PATH)?.GetComponent<DirectionalDamageFeedbackController>() != null;

        [MenuItem("Dead Signal/Setup/Ensure Screen Feedback")]
        public static void EnsureAssets()
        {
            if (AssetDatabase.LoadAssetAtPath<ScreenFeedbackTuning>(TUNING_PATH) == null)
            {
                var tuning = ScriptableObject.CreateInstance<ScreenFeedbackTuning>();
                AssetDatabase.CreateAsset(tuning, TUNING_PATH);
            }

            var root = PrefabUtility.LoadPrefabContents(HUD_PREFAB_PATH);
            try
            {
                var controller = root.GetComponent<DirectionalDamageFeedbackController>();
                if (controller == null)
                {
                    controller = root.AddComponent<DirectionalDamageFeedbackController>();
                }

                var indicator = root.transform.Find("Directional Damage Indicator") as RectTransform;
                if (indicator == null)
                {
                    var indicatorObject = new GameObject(
                        "Directional Damage Indicator", typeof(RectTransform));
                    indicator = indicatorObject.GetComponent<RectTransform>();
                    indicator.SetParent(root.transform, false);
                }

                indicator.anchorMin = new Vector2(0.5f, 0.9f);
                indicator.anchorMax = indicator.anchorMin;
                indicator.anchoredPosition = Vector2.zero;
                indicator.sizeDelta = new Vector2(92f, 34f);
                indicator.pivot = new Vector2(0.5f, 0.5f);
                var left = _ensureSegment(indicator, "Damage Direction Left", new Vector2(-16f, 0f), 24f);
                var right = _ensureSegment(indicator, "Damage Direction Right", new Vector2(16f, 0f), -24f);
                indicator.gameObject.SetActive(false);

                var serializedController = new SerializedObject(controller);
                serializedController.FindProperty("m_indicator").objectReferenceValue = indicator;
                var segments = serializedController.FindProperty("m_segments");
                segments.arraySize = 2;
                segments.GetArrayElementAtIndex(0).objectReferenceValue = left;
                segments.GetArrayElementAtIndex(1).objectReferenceValue = right;
                serializedController.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, HUD_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("Screen feedback assets were not imported successfully.");
            }
        }

        private static Image _ensureSegment(RectTransform parent, string name, Vector2 position, float rotation)
        {
            var segment = parent.Find(name)?.GetComponent<Image>();
            if (segment == null)
            {
                var segmentObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                segmentObject.transform.SetParent(parent, false);
                segment = segmentObject.GetComponent<Image>();
            }

            var rect = segment.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(42f, 5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            segment.color = new Color(1f, 0.12f, 0.08f, 0f);
            segment.raycastTarget = false;
            segment.maskable = true;
            return segment;
        }
    }
}
