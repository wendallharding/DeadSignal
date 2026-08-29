using System;
using DeadSignal.Presentation;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalExtractionOutcomeFeedbackSetup
    {
        private const string GLYPH_PATH = "Assets/DeadSignal/Resources/VFX/MachineryStateTransitionGlyph.png";
        private const string TUNING_PATH = "Assets/DeadSignal/Resources/Tuning/ExtractionOutcomeFeedbackTuning.asset";

        public static bool HasAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(GLYPH_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<ExtractionOutcomeFeedbackTuning>(TUNING_PATH) != null;

        [MenuItem("Dead Signal/Setup/Ensure Extraction Outcome Feedback")]
        public static void EnsureAssets()
        {
            var importer = AssetImporter.GetAtPath(GLYPH_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the extraction outcome glyph at {GLYPH_PATH}.");
            }

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            if (AssetDatabase.LoadAssetAtPath<ExtractionOutcomeFeedbackTuning>(TUNING_PATH) == null)
            {
                var tuning = ScriptableObject.CreateInstance<ExtractionOutcomeFeedbackTuning>();
                AssetDatabase.CreateAsset(tuning, TUNING_PATH);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("Extraction outcome feedback assets were not imported successfully.");
            }
        }
    }
}
