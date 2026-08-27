using DeadSignal.Missions;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalMissionObjectiveSetup
    {
        private const string ASSET_PATH = "Assets/DeadSignal/Resources/Tuning/CompatibilityMissionObjectives.asset";

        [MenuItem("DEAD SIGNAL/Setup/Create Compatibility Mission Objectives")]
        public static void CreateCompatibilityMissionObjectives()
        {
            var configuration = AssetDatabase.LoadAssetAtPath<MissionObjectiveGraphConfiguration>(ASSET_PATH);
            if (configuration == null)
            {
                configuration = ScriptableObject.CreateInstance<MissionObjectiveGraphConfiguration>();
                AssetDatabase.CreateAsset(configuration, ASSET_PATH);
            }

            configuration.ReplaceDefinitions(CompatibilityMissionObjectiveGraph.Instance.Definitions);
            EditorUtility.SetDirty(configuration);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
