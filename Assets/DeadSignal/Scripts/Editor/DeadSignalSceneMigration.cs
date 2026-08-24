using System;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSignal.Editor
{
    public static class DeadSignalSceneMigration
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string ROOT_NAME = "DEAD SIGNAL — Authored World";

        [MenuItem("Tools/DEAD SIGNAL/Migrate Runtime World To Scene")]
        public static void Migrate()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var existing = GameObject.Find(ROOT_NAME);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            var root = new GameObject(ROOT_NAME);
            var references = root.AddComponent<DeadSignalSceneReferences>();
            var anchors = _createChild(root.transform, "Spatial Anchors");
            var extractionAnchor = _createAnchor(anchors, "Extraction Anchor", new Vector3(-9.2f, 0f, -5.6f));
            var towerAnchor = _createAnchor(anchors, "Tower Anchor", new Vector3(-0.6f, 0f, 0.4f));
            var shortcutAnchor = _createAnchor(anchors, "Shortcut Anchor", new Vector3(4f, 0f, 0.4f));

            var environment = _createChild(root.transform, "Environment");
            var deck = _createChild(environment, "Maintenance Deck Modules").gameObject;
            for (var gridX = -3; gridX <= 3; gridX++)
            {
                for (var gridZ = -2; gridZ <= 2; gridZ++)
                {
                    var module = _instantiate("Assets/DeadSignal/Resources/Environment/MaintenanceDeckModule.prefab", deck.transform);
                    module.name = $"Maintenance Deck Module {gridX},{gridZ}";
                    module.transform.localPosition = new Vector3(gridX * 3.9f, -0.45f, gridZ * 3.6f);
                    module.transform.localScale = new Vector3(3.9f, 0.6f, 3.6f);
                }
            }

            var shell = _instantiate("Assets/DeadSignal/Resources/Environment/MaintenanceRoomShell.prefab", environment);
            shell.name = "Maintenance Room Shell";
            var extraction = _instantiate("Assets/DeadSignal/Resources/Environment/ExtractionPadAssembly.prefab", environment);
            extraction.name = "Extraction Pad Assembly";
            extraction.transform.position = extractionAnchor.position;
            var tower = _instantiate("Assets/DeadSignal/Resources/Environment/SignalTowerAssembly.prefab", environment);
            tower.name = "Signal Tower Assembly";
            tower.transform.position = towerAnchor.position;
            var routing = _instantiate("Assets/DeadSignal/Resources/Environment/SignalRoutingAssembly.prefab", environment);
            routing.name = "Tower Signal Lines";
            routing.SetActive(false);
            var shortcut = _instantiate("Assets/DeadSignal/Resources/Environment/ShortcutGateAssembly.prefab", environment);
            shortcut.name = "Shortcut Gate Assembly";
            shortcut.transform.position = shortcutAnchor.position;

            var machines = _createChild(environment, "Station Machines").gameObject;
            var sockets = shell.transform.Find("Machine Sockets");
            if (sockets == null || sockets.childCount != 6)
            {
                throw new InvalidOperationException("MaintenanceRoomShell must expose exactly six Machine Sockets.");
            }

            for (var index = 0; index < sockets.childCount; index++)
            {
                var machine = _instantiate("Assets/DeadSignal/Resources/Environment/StationMachineAssembly.prefab", machines.transform);
                machine.name = $"Station Machine {index + 1:00}";
                machine.transform.position = sockets.GetChild(index).position;
            }

            var presentation = _createChild(root.transform, "Presentation");
            var cameraRig = _createChild(presentation, "Player Camera Rig");
            var cameraObject = _createChild(cameraRig, "Dead Signal Camera").gameObject;
            cameraObject.transform.localPosition = new Vector3(0f, 12f, -7.4f);
            cameraObject.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 38f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.002f, 0.004f, 0.008f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            cameraObject.AddComponent<AudioListener>();
            var lightObject = _createChild(presentation, "Cold Overhead Light").gameObject;
            lightObject.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            var keyLight = lightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(0.38f, 0.52f, 0.65f);
            keyLight.intensity = 1.35f;
            keyLight.shadows = LightShadows.Soft;

            var actors = _createChild(root.transform, "Actors");
            var player = _actor("MaintenanceDroneAssembly", "Maintenance Drone", extractionAnchor.position, actors);
            var warden = _actor("SecurityWardenAssembly", "Security Warden", new Vector3(6.8f, 0f, 4.7f), actors);
            var sapper = _actor("SignalSapperAssembly", "Signal Sapper", new Vector3(-10.8f, 0f, 5.7f), actors);
            var interceptor = _actor("SecurityInterceptorAssembly", "Security Interceptor", new Vector3(-16.4f, 0f, 7.1f), actors);
            var suppressor = _actor("SecuritySuppressorAssembly", "Security Suppressor", new Vector3(-16.4f, 0f, 7.1f), actors);

            references.Configure(
                extractionAnchor, towerAnchor, shortcutAnchor, deck, shell, extraction, tower, routing, shortcut, machines,
                camera, cameraRig, keyLight, player, warden, sapper, interceptor, suppressor);

            foreach (var sceneCamera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (sceneCamera != camera)
                {
                    sceneCamera.gameObject.SetActive(false);
                }
            }

            foreach (var sceneLight in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (sceneLight != keyLight && !sceneLight.transform.IsChildOf(root.transform))
                {
                    sceneLight.gameObject.SetActive(false);
                }
            }

            EditorUtility.SetDirty(references);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Migrated the fixed DEAD SIGNAL world and persistent actors into SampleScene.");
        }

        private static Transform _actor(string prefabName, string objectName, Vector3 position, Transform parent)
        {
            var actor = _instantiate($"Assets/DeadSignal/Resources/Actors/{prefabName}.prefab", parent);
            actor.name = objectName;
            actor.transform.position = position;
            return actor.transform;
        }

        private static GameObject _instantiate(string path, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Required authored prefab is missing: {path}");
            }

            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        }

        private static Transform _createAnchor(Transform parent, string name, Vector3 position)
        {
            var anchor = _createChild(parent, name);
            anchor.position = position;
            return anchor;
        }

        private static Transform _createChild(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }
    }
}
