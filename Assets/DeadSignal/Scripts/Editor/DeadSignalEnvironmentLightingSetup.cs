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
        private const string SPINE_COOKIE_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineHighVoltageLaneCookie.png";
        private const string DEEP_CORE_COOKIE_PATH =
            "Assets/DeadSignal/Resources/Environment/DeepCoreCalibrationApertureCookie.png";
        private const string SECURITY_TRIAL_COOKIE_PATH =
            "Assets/DeadSignal/Resources/Environment/SecurityTrialContainmentCookie.png";
        private const string EXTRACTION_UPLINK_COOKIE_PATH =
            "Assets/DeadSignal/Resources/Environment/ExtractionUplinkLockOnCookie.png";

        [MenuItem("Tools/DEAD SIGNAL/Configure Environment Lighting Tuning")]
        public static void Configure()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<EnvironmentLightingTuning>(ASSET_PATH);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<EnvironmentLightingTuning>();
                AssetDatabase.CreateAsset(tuning, ASSET_PATH);
            }

            _configureCookie(FOUNDRY_COOKIE_PATH);
            _configureCookie(SPINE_COOKIE_PATH);
            _configureCookie(DEEP_CORE_COOKIE_PATH);
            _configureCookie(SECURITY_TRIAL_COOKIE_PATH);
            _configureCookie(EXTRACTION_UPLINK_COOKIE_PATH);
            tuning.ConfigureDeepCoreProfiles();
            tuning.ConfigureSecurityTrialProfiles();
            tuning.ConfigureWithdrawalProfiles();

            EditorUtility.SetDirty(tuning);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"DEAD SIGNAL environment lighting tuning is configured at {ASSET_PATH}.");
        }

        private static void _configureCookie(string path)
        {
            var cookieImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (cookieImporter == null)
            {
                throw new InvalidOperationException($"Could not find the environment light cookie at {path}.");
            }

            cookieImporter.textureType = TextureImporterType.Default;
            cookieImporter.sRGBTexture = false;
            cookieImporter.alphaSource = TextureImporterAlphaSource.None;
            cookieImporter.mipmapEnabled = false;
            cookieImporter.maxTextureSize = 1024;
            cookieImporter.wrapMode = TextureWrapMode.Clamp;
            cookieImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            cookieImporter.SaveAndReimport();
        }
    }
}
