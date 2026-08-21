using System;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalProjectSetup
    {
        private const string RESOURCES_FOLDER = "Assets/DeadSignal/Resources";
        private const string REFLEX_SETTINGS_PATH = RESOURCES_FOLDER + "/ReflexSettings.asset";
        private const string CREATE_REFLEX_SETTINGS_MENU = "Assets/Create/Reflex/Settings";

        public static void EnsureReflexSettings()
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(REFLEX_SETTINGS_PATH) != null)
            {
                return;
            }

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(RESOURCES_FOLDER);
            if (!EditorApplication.ExecuteMenuItem(CREATE_REFLEX_SETTINGS_MENU))
            {
                throw new InvalidOperationException($"Could not execute the Reflex settings menu at {CREATE_REFLEX_SETTINGS_MENU}.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(REFLEX_SETTINGS_PATH) == null)
            {
                throw new InvalidOperationException($"Reflex settings were not created at {REFLEX_SETTINGS_PATH}.");
            }
        }
    }
}
