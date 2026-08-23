using System;
using UnityEditor;
using UnityEngine;
using DeadSignal.Combat;
using DeadSignal.Missions;

namespace DeadSignal.Editor
{
    public static class DeadSignalThreatSetup
    {
        private const string TUNING_PATH = "Assets/DeadSignal/Resources/Tuning/ThreatBalanceTuning.asset";
        private const string OVERCLOCK_TUNING_PATH = "Assets/DeadSignal/Resources/Tuning/SignalOverclockTuning.asset";
        private const string RECOVERY_TEXTURE_PATH = "Assets/DeadSignal/Resources/VFX/SignalRecoveryBurst.png";

        public static bool HasAssets =>
            AssetDatabase.LoadAssetAtPath<ThreatBalanceTuning>(TUNING_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<SignalOverclockTuning>(OVERCLOCK_TUNING_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Texture2D>(RECOVERY_TEXTURE_PATH) != null;

        public static void EnsureAssets()
        {
            var importer = AssetImporter.GetAtPath(RECOVERY_TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find Signal recovery art at {RECOVERY_TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            if (AssetDatabase.LoadAssetAtPath<ThreatBalanceTuning>(TUNING_PATH) == null)
            {
                var tuning = ScriptableObject.CreateInstance<ThreatBalanceTuning>();
                AssetDatabase.CreateAsset(tuning, TUNING_PATH);
            }

            if (AssetDatabase.LoadAssetAtPath<SignalOverclockTuning>(OVERCLOCK_TUNING_PATH) == null)
            {
                var overclockTuning = ScriptableObject.CreateInstance<SignalOverclockTuning>();
                AssetDatabase.CreateAsset(overclockTuning, OVERCLOCK_TUNING_PATH);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("Threat balance assets were not imported successfully.");
            }
        }
    }
}
