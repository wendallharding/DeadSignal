using System;
using DeadSignal.Presentation;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalEnvironmentLightingSetup
    {
        private const string ASSET_PATH = "Assets/DeadSignal/Resources/Tuning/EnvironmentLightingTuning.asset";
        private const string FOUNDRY_COOKIE_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayFoundryInductionCookie.png";

        [MenuItem("Tools/DEAD SIGNAL/Configure Environment Lighting Tuning")]
        public static void Configure()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<EnvironmentLightingTuning>(ASSET_PATH);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<EnvironmentLightingTuning>();
                AssetDatabase.CreateAsset(tuning, ASSET_PATH);
            }

            var cookieImporter = AssetImporter.GetAtPath(FOUNDRY_COOKIE_PATH) as TextureImporter;
            if (cookieImporter == null)
            {
                throw new InvalidOperationException($"Could not find the Foundry light cookie at {FOUNDRY_COOKIE_PATH}.");
            }

            cookieImporter.textureType = TextureImporterType.Default;
            cookieImporter.sRGBTexture = false;
            cookieImporter.alphaSource = TextureImporterAlphaSource.None;
            cookieImporter.mipmapEnabled = false;
            cookieImporter.maxTextureSize = 1024;
            cookieImporter.wrapMode = TextureWrapMode.Clamp;
            cookieImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            cookieImporter.SaveAndReimport();

            EditorUtility.SetDirty(tuning);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"DEAD SIGNAL environment lighting tuning is configured at {ASSET_PATH}.");
        }
    }
}
