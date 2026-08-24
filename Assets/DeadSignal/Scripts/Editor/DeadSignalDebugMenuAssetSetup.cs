using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalDebugMenuAssetSetup
    {
        private const string DESTINATION_DIRECTORY = "Assets/DeadSignal/Resources/UI/Debug";

        public static void CopyAutoUIPrefabs()
        {
            if (!AssetDatabase.IsValidFolder(DESTINATION_DIRECTORY))
            {
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/UI", "Debug");
            }

            _copy(
                "Packages/com.otherocean.autoui/Runtime/Prefabs/AutoUI.prefab",
                $"{DESTINATION_DIRECTORY}/AutoUI.prefab");
            _copy(
                "Packages/com.otherocean.autoui/Runtime/Prefabs/Components/Canvas_DebugMenu.prefab",
                $"{DESTINATION_DIRECTORY}/Canvas_DebugMenu.prefab");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void _copy(string source, string destination)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(destination) != null)
            {
                return;
            }

            if (!AssetDatabase.CopyAsset(source, destination))
            {
                throw new UnityException($"Could not copy AutoUI prefab from {source} to {destination}.");
            }
        }
    }
}
