using DeadSignal.Presentation;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalEnvironmentLightingSetup
    {
        private const string ASSET_PATH = "Assets/DeadSignal/Resources/Tuning/EnvironmentLightingTuning.asset";

        [MenuItem("Tools/DEAD SIGNAL/Configure Environment Lighting Tuning")]
        public static void Configure()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<EnvironmentLightingTuning>(ASSET_PATH);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<EnvironmentLightingTuning>();
                AssetDatabase.CreateAsset(tuning, ASSET_PATH);
            }

            EditorUtility.SetDirty(tuning);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"DEAD SIGNAL environment lighting tuning is configured at {ASSET_PATH}.");
        }
    }
}
