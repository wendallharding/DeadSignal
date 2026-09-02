using System;
using System.Linq;
using DeadSignal.Diagnostics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalSecurityTrialCombatTuningSceneSetup
    {
        private const string SOURCE_SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string TUNING_SCENE_PATH = "Assets/DeadSignal/Scenes/SecurityTrialCombatTuning.unity";
        private const string TUNING_ROOT_NAME = "DEAD SIGNAL — Security Trial Combat Tuning";

        [MenuItem("DEAD SIGNAL/Setup Security Trial Combat Tuning Scene")]
        public static void EnsureScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SOURCE_SCENE_PATH) == null)
            {
                throw new InvalidOperationException($"The source playable scene is missing at {SOURCE_SCENE_PATH}.");
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TUNING_SCENE_PATH) == null &&
                !AssetDatabase.CopyAsset(SOURCE_SCENE_PATH, TUNING_SCENE_PATH))
            {
                throw new InvalidOperationException($"Could not create {TUNING_SCENE_PATH}.");
            }

            var scene = EditorSceneManager.OpenScene(TUNING_SCENE_PATH, OpenSceneMode.Single);
            var root = GameObject.Find(TUNING_ROOT_NAME);
            if (root == null)
            {
                root = new GameObject(TUNING_ROOT_NAME);
            }

            if (root.GetComponent<SecurityTrialCombatTuningScene>() == null)
            {
                root.AddComponent<SecurityTrialCombatTuningScene>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            _ensureBuildSettingsEntry();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TUNING_SCENE_PATH) == null)
            {
                throw new InvalidOperationException("The Security Trial combat-tuning scene was not saved successfully.");
            }
        }

        private static void _ensureBuildSettingsEntry()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            var existingIndex = scenes.FindIndex(scene => scene.path == TUNING_SCENE_PATH);
            if (existingIndex >= 0)
            {
                var existing = scenes[existingIndex];
                if (!existing.enabled)
                {
                    scenes[existingIndex] = new EditorBuildSettingsScene(existing.path, true);
                }
            }
            else
            {
                scenes.Add(new EditorBuildSettingsScene(TUNING_SCENE_PATH, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
