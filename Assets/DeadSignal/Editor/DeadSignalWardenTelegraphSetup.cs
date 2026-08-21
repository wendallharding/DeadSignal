using System;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalWardenTelegraphSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/VFX/WardenStrikeWarning.png";
        private const string TUNING_PATH = "Assets/DeadSignal/Resources/Tuning/WardenThreatTelegraphTuning.asset";

        public static bool HasAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<WardenThreatTelegraphTuning>(TUNING_PATH) != null;

        public static void EnsureAssets()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Warden strike warning at {TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            var tuning = AssetDatabase.LoadAssetAtPath<WardenThreatTelegraphTuning>(TUNING_PATH);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<WardenThreatTelegraphTuning>();
                AssetDatabase.CreateAsset(tuning, TUNING_PATH);
            }

            EditorUtility.SetDirty(tuning);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Warden strike-warning texture or tuning asset is incomplete.");
            }
        }
    }
}
