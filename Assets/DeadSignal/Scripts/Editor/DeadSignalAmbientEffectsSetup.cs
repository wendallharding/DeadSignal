using DeadSignal.Presentation;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalAmbientEffectsSetup
    {
        private const string TUNING_PATH =
            "Assets/DeadSignal/Resources/Tuning/StationAmbientEffectsTuning.asset";

        [MenuItem("DEAD SIGNAL/Setup/Station Ambient Effects")]
        public static void EnsureAssets()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<StationAmbientEffectsTuning>(TUNING_PATH);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<StationAmbientEffectsTuning>();
                AssetDatabase.CreateAsset(tuning, TUNING_PATH);
            }

            EditorUtility.SetDirty(tuning);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
