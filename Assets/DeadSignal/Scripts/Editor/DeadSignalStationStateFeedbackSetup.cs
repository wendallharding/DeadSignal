using System;
using UnityEditor;
using UnityEngine;
using DeadSignal.Presentation;

namespace DeadSignal.Editor
{
    public static class DeadSignalStationStateFeedbackSetup
    {
        private const string GLYPH_PATH = "Assets/DeadSignal/Resources/VFX/MachineryStateTransitionGlyph.png";
        private const string TUNING_PATH = "Assets/DeadSignal/Resources/Tuning/StationStateFeedbackTuning.asset";

        public static bool HasAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(GLYPH_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<StationStateFeedbackTuning>(TUNING_PATH) != null;

        [MenuItem("Dead Signal/Setup/Ensure Station State Feedback")]
        public static void EnsureAssets()
        {
            var importer = AssetImporter.GetAtPath(GLYPH_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the station state glyph at {GLYPH_PATH}.");
            }

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            if (AssetDatabase.LoadAssetAtPath<StationStateFeedbackTuning>(TUNING_PATH) == null)
            {
                var tuning = ScriptableObject.CreateInstance<StationStateFeedbackTuning>();
                AssetDatabase.CreateAsset(tuning, TUNING_PATH);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("Station state feedback assets were not imported successfully.");
            }
        }
    }
}
