using System;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalCameraSetup
    {
        private const string TUNING_PATH = "Assets/DeadSignal/Resources/Tuning/PlayerCameraTuning.asset";

        public static bool HasAssets => AssetDatabase.LoadAssetAtPath<PlayerCameraTuning>(TUNING_PATH) != null;

        [MenuItem("DEAD SIGNAL/Setup Player Follow Camera")]
        public static void EnsureAssets()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<PlayerCameraTuning>(TUNING_PATH);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<PlayerCameraTuning>();
                AssetDatabase.CreateAsset(tuning, TUNING_PATH);
            }

            EditorUtility.SetDirty(tuning);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The player follow-camera tuning asset is missing.");
            }
        }
    }
}
