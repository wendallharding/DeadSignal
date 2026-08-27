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
        private const string EDGE_INDICATOR_TUNING_PATH =
            "Assets/DeadSignal/Resources/Tuning/EdgeIndicatorTuning.asset";

        public static bool HasAssets =>
            AssetDatabase.LoadAssetAtPath<Sprite>(TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Texture2D>(DEBRIEF_TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<SignalHudTuning>(TUNING_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<EdgeIndicatorTuning>(EDGE_INDICATOR_TUNING_PATH) != null;

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

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("Signal HUD assets were not imported successfully.");
            }
        }
    }
}
