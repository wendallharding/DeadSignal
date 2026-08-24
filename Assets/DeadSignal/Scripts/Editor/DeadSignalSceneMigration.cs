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
        private const string PALETTE_PATH = "Assets/DeadSignal/Resources/Materials/WorldPalette";

        [MenuItem("Tools/DEAD SIGNAL/Migrate Runtime World To Scene")]
        public static void Migrate()
        {
            var palette = _createPalette();
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
                    module.GetComponent<Renderer>().sharedMaterial = palette.Deck;
                }
            }

            var shell = _instantiate("Assets/DeadSignal/Resources/Environment/MaintenanceRoomShell.prefab", environment);
            shell.name = "Maintenance Room Shell";
            _setAllMaterials(shell, palette.Bulkhead);
            var extraction = _instantiate("Assets/DeadSignal/Resources/Environment/ExtractionPadAssembly.prefab", environment);
            extraction.name = "Extraction Pad Assembly";
            extraction.transform.position = extractionAnchor.position;
            extraction.transform.Find("Extraction Plinth").GetComponent<Renderer>().sharedMaterial = palette.ExtractionHousing;
            extraction.transform.Find("Extraction Ring").GetComponent<Renderer>().sharedMaterial = palette.Cyan;
            extraction.transform.Find("Extraction Center").GetComponent<Renderer>().sharedMaterial = palette.ExtractionHousing;
            extraction.transform.Find("Extraction Beacon").GetComponent<Renderer>().sharedMaterial = palette.Cyan;
            var tower = _instantiate("Assets/DeadSignal/Resources/Environment/SignalTowerAssembly.prefab", environment);
            tower.name = "Signal Tower Assembly";
            tower.transform.position = towerAnchor.position;
            tower.transform.Find("Tower Base").GetComponent<Renderer>().sharedMaterial = palette.TowerHousing;
            tower.transform.Find("Tower Column").GetComponent<Renderer>().sharedMaterial = palette.TowerHousing;
            tower.transform.Find("Tower Core").GetComponent<Renderer>().sharedMaterial = palette.RedDim;
            var routing = _instantiate("Assets/DeadSignal/Resources/Environment/SignalRoutingAssembly.prefab", environment);
            routing.name = "Tower Signal Lines";
            _setAllMaterials(routing, palette.SignalRouting);
            routing.SetActive(false);
            var shortcut = _instantiate("Assets/DeadSignal/Resources/Environment/ShortcutGateAssembly.prefab", environment);
            shortcut.name = "Shortcut Gate Assembly";
            shortcut.transform.position = shortcutAnchor.position;
            _setAllMaterials(shortcut, palette.ShortcutHousing);
            shortcut.transform.Find("Shortcut Gate Signal").GetComponent<Renderer>().sharedMaterial = palette.CyanDim;
            shortcut.transform.Find("Signal Shortcut Gate").GetComponent<Renderer>().sharedMaterial = palette.ShortcutLocked;

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
                machine.transform.Find("Machine Housing").GetComponent<Renderer>().sharedMaterial = palette.StationMachineHousing;
                machine.transform.Find("Machine Status").GetComponent<Renderer>().sharedMaterial =
                    index % 2 == 0 ? palette.RedDim : palette.CyanDim;
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
            _applyActorMaterials(player, warden, sapper, interceptor, suppressor, palette);

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

        private static PaletteMaterials _createPalette()
        {
            if (!AssetDatabase.IsValidFolder(PALETTE_PATH))
            {
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "WorldPalette");
            }

            var palette = new PaletteMaterials
            {
                Cyan = _material("SignalCyan"), CyanDim = _material("PoweredDeck"), Amber = _material("SalvageAmber"),
                Red = _material("SecurityRed"), RedDim = _material("DeadZoneRed"), Magenta = _material("SapperMagenta"),
                Deck = _material("MaintenanceDeck", "Environment/MaintenanceDeckPanel", 0.22f),
                Bulkhead = _material("MaintenanceBulkhead", "Environment/MaintenanceBulkheadPanel", 0.3f),
                TowerHousing = _material("SignalTowerHousing", "Environment/SignalTowerHousingPanel", 0.38f),
                ExtractionHousing = _material("ExtractionDockHousing", "Environment/ExtractionDockPanel", 0.32f),
                ShortcutHousing = _material("ShortcutGateHousing", "Environment/ShortcutGatePanel", 0.34f),
                ShortcutLocked = _material("ShortcutGateLocked", "Environment/ShortcutGatePanel", 0.34f),
                SignalRouting = _material("SignalRouting", "Environment/SignalRoutingPanel", 0.4f),
                StationMachineHousing = _material("StationMachineHousing", "Environment/StationMachinePanel", 0.28f),
                SalvageCacheHousing = _material("SalvageCacheHousing", "Environment/SalvageCachePanel", 0.36f),
                PlayerHousing = _material("MaintenanceDroneHousing", "Actors/MaintenanceDroneHullAlbedo", 0.42f),
                WardenHousing = _material("SecurityWardenHousing", "Actors/SecurityWardenArmorAlbedo", 0.3f),
                SapperHousing = _material("SignalSapperHousing", "Actors/SignalSapperArmorAlbedo", 0.38f),
                Dark = _material("StationBlack"), Steel = _material("StationSteel"), White = _material("DroneWhite"),
                PoweredTerritory = _poweredTerritoryMaterial()
            };
            _setMaterial(palette.Cyan, new Color(0.02f, 0.92f, 1f), new Color(0f, 1.8f, 2.2f));
            _setMaterial(palette.CyanDim, new Color(0.012f, 0.095f, 0.12f), new Color(0f, 0.045f, 0.065f));
            _setMaterial(palette.Amber, new Color(1f, 0.48f, 0.06f), new Color(2.4f, 0.65f, 0.02f));
            _setMaterial(palette.Red, new Color(1f, 0.035f, 0.045f), new Color(2.2f, 0.01f, 0.01f));
            _setMaterial(palette.RedDim, new Color(0.2f, 0.018f, 0.025f), new Color(0.14f, 0.005f, 0.005f));
            _setMaterial(palette.Magenta, new Color(0.92f, 0.025f, 0.62f), new Color(2.2f, 0.01f, 1.15f));
            _setMaterial(palette.Deck, new Color(0.58f, 0.66f, 0.72f), Color.black);
            _setMaterial(palette.Bulkhead, new Color(0.48f, 0.56f, 0.62f), Color.black);
            _setMaterial(palette.TowerHousing, new Color(0.72f, 0.78f, 0.82f), Color.black);
            _setMaterial(palette.ExtractionHousing, new Color(0.7f, 0.78f, 0.82f), Color.black);
            _setMaterial(palette.ShortcutHousing, new Color(0.66f, 0.72f, 0.76f), Color.black);
            _setMaterial(palette.ShortcutLocked, new Color(0.52f, 0.15f, 0.16f), new Color(0.12f, 0.005f, 0.005f));
            _setMaterial(palette.SignalRouting, new Color(0.72f, 0.82f, 0.86f), new Color(0.015f, 0.32f, 0.38f));
            _setMaterial(palette.StationMachineHousing, new Color(0.5f, 0.56f, 0.6f), Color.black);
            _setMaterial(palette.SalvageCacheHousing, new Color(0.82f, 0.58f, 0.28f), new Color(0.18f, 0.05f, 0.005f));
            _setMaterial(palette.PlayerHousing, new Color(0.82f, 0.86f, 0.88f), Color.black);
            _setMaterial(palette.WardenHousing, new Color(0.16f, 0.18f, 0.21f), new Color(0.04f, 0.002f, 0.002f));
            _setMaterial(palette.SapperHousing, new Color(0.21f, 0.17f, 0.25f), new Color(0.035f, 0.001f, 0.02f));
            _setMaterial(palette.Dark, new Color(0.022f, 0.03f, 0.042f), Color.black);
            _setMaterial(palette.Steel, new Color(0.085f, 0.11f, 0.14f), new Color(0.01f, 0.018f, 0.02f));
            _setMaterial(palette.White, new Color(0.62f, 0.72f, 0.75f), new Color(0.03f, 0.06f, 0.07f));
            AssetDatabase.SaveAssets();
            return palette;
        }

        private static void _setMaterial(Material material, Color baseColor, Color emission)
        {
            material.color = baseColor;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            EditorUtility.SetDirty(material);
        }

        private static Material _material(string name, string textureResource = null, float smoothness = 0f)
        {
            var path = $"{PALETTE_PATH}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var template = AssetDatabase.LoadAssetAtPath<Material>("Assets/DeadSignal/Resources/Materials/RuntimeLitTemplate.mat");
                material = new Material(template) { name = name, enableInstancing = true };
                AssetDatabase.CreateAsset(material, path);
            }

            if (!string.IsNullOrEmpty(textureResource))
            {
                var texture = Resources.Load<Texture2D>(textureResource);
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", texture);
                }
                if (material.HasProperty("_Smoothness"))
                {
                    material.SetFloat("_Smoothness", smoothness);
                }
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material _poweredTerritoryMaterial()
        {
            var path = $"{PALETTE_PATH}/PoweredTerritory.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Dead Signal/Powered Territory");
            if (shader == null)
            {
                throw new InvalidOperationException("The authored Powered Territory shader is missing.");
            }
            if (material == null)
            {
                material = new Material(shader) { name = "PoweredTerritory" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.015f, 0.42f, 0.5f, 0.32f));
            material.SetColor("_EdgeColor", new Color(0.05f, 0.95f, 1f, 0.92f));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void _applyActorMaterials(Transform player, Transform warden, Transform sapper,
            Transform interceptor, Transform suppressor, PaletteMaterials palette)
        {
            player.Find("Drone Chassis").GetComponent<Renderer>().sharedMaterial = palette.PlayerHousing;
            player.Find("Drone Signal Ring").GetComponent<Renderer>().sharedMaterial = palette.Cyan;
            player.Find("Drone Core").GetComponent<Renderer>().sharedMaterial = palette.Dark;
            player.Find("Drone Tool").GetComponent<Renderer>().sharedMaterial = palette.Cyan;
            warden.Find("Warden Chassis").GetComponent<Renderer>().sharedMaterial = palette.WardenHousing;
            warden.Find("Warden Eye").GetComponent<Renderer>().sharedMaterial = palette.Red;
            warden.Find("Warden Crown").GetComponent<Renderer>().sharedMaterial = palette.RedDim;
            sapper.Find("Sapper Chassis").GetComponent<Renderer>().sharedMaterial = palette.SapperHousing;
            sapper.Find("Sapper Fork Left").GetComponent<Renderer>().sharedMaterial = palette.Magenta;
            sapper.Find("Sapper Fork Right").GetComponent<Renderer>().sharedMaterial = palette.Magenta;
            sapper.Find("Sapper Drain Core").GetComponent<Renderer>().sharedMaterial = palette.Magenta;
            interceptor.Find("Interceptor Chassis").GetComponent<Renderer>().sharedMaterial = palette.WardenHousing;
            interceptor.Find("Interceptor Blade Left").GetComponent<Renderer>().sharedMaterial = palette.RedDim;
            interceptor.Find("Interceptor Blade Right").GetComponent<Renderer>().sharedMaterial = palette.RedDim;
            interceptor.Find("Interceptor Core").GetComponent<Renderer>().sharedMaterial = palette.Amber;
            suppressor.Find("Suppressor Chassis").GetComponent<Renderer>().sharedMaterial = palette.WardenHousing;
            suppressor.Find("Suppressor Emitter Left").GetComponent<Renderer>().sharedMaterial = palette.Magenta;
            suppressor.Find("Suppressor Emitter Right").GetComponent<Renderer>().sharedMaterial = palette.Magenta;
            suppressor.Find("Suppressor Core").GetComponent<Renderer>().sharedMaterial = palette.Amber;
        }

        private static void _setAllMaterials(GameObject root, Material material)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
            }
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

        private sealed class PaletteMaterials
        {
            public Material Cyan, CyanDim, Amber, Red, RedDim, Magenta, Deck, Bulkhead, TowerHousing,
                ExtractionHousing, ShortcutHousing, ShortcutLocked, SignalRouting, StationMachineHousing,
                SalvageCacheHousing, PlayerHousing, WardenHousing, SapperHousing, Dark, Steel, White, PoweredTerritory;
        }
    }
}
