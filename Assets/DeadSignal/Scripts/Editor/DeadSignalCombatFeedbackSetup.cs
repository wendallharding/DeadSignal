using DeadSignal.Combat;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalCombatFeedbackSetup
    {
        private const string TUNING_PATH = "Assets/DeadSignal/Resources/Tuning/CombatFeedbackTuning.asset";

        [MenuItem("DEAD SIGNAL/Ensure Combat Feedback Tuning")]
        public static void EnsureAssets()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<CombatFeedbackTuning>(TUNING_PATH);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<CombatFeedbackTuning>();
                AssetDatabase.CreateAsset(tuning, TUNING_PATH);
            }

            EditorUtility.SetDirty(tuning);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
