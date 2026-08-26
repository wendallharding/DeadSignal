using System;
using UnityEditor;
using UnityEngine;
using DeadSignal.Missions;
using DeadSignal.World;

namespace DeadSignal.Editor
{
    public static class DeadSignalRelayCoolingGantrySetup
    {
        private const string FOUNDRY_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayFoundryRegion.prefab";
        private const string REGION_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayCoolingGantryRegion.prefab";
        private const string MODEL_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayHeatExchangerModel.fbx";
        private const string LANDMARK_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayHeatExchanger.prefab";
        private const string DECAL_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayCoolingGantryRouteDecal.png";
        private const string MATERIAL_DIRECTORY =
            "Assets/DeadSignal/Resources/Materials/RelayCoolingGantry";
        private const string DECAL_MATERIAL_PATH = MATERIAL_DIRECTORY + "/RelayCoolingGantryRouteDecal.mat";
        private const string ARMOR_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryArmor.mat";
        private const string DECK_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryDeck.mat";
        private const string CYAN_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryCyan.mat";
        private const string CERAMIC_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/SapperCradleCeramic.mat";
        private const string COPPER_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/EastVaultCopper.mat";

        public static bool HasAssets
        {
            get
            {
                var foundry = AssetDatabase.LoadAssetAtPath<GameObject>(FOUNDRY_PREFAB_PATH);
                var region = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                var landmark = AssetDatabase.LoadAssetAtPath<GameObject>(LANDMARK_PREFAB_PATH);
                return foundry != null && region != null && landmark != null &&
                       AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Texture2D>(DECAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH) != null &&
                       foundry.transform.Find("Relay Cooling Gantry Region") != null &&
                       foundry.transform.Find("Protected Relay Payload Socket") != null &&
                       foundry.transform.Find("Foundry South Bulkhead") == null &&
                       foundry.GetComponentsInChildren<AuthoredSalvageSocket>().Length == 2 &&
                       region.GetComponentsInChildren<AuthoredMapObstacle>().Length == 6 &&
                       region.GetComponentsInChildren<AuthoredInterceptorEntrance>().Length == 1 &&
                       region.GetComponentsInChildren<AuthoredSalvageSocket>().Length == 1 &&
                       region.GetComponent<AuthoredPoweredTerritory>() != null &&
                       landmark.GetComponent<AuthoredMapObstacle>() != null;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup Relay Cooling Gantry")]
        public static void EnsureAssets()
        {
            _configureImports();
            _ensureMaterialDirectory();
            var materials = _ensureMaterials();
            _ensureLandmarkPrefab(materials);
            _ensureRegionPrefab(materials);
            _integrateWithFoundry(materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The scene-authored Relay Cooling Gantry is incomplete.");
            }
        }

        private static void _configureImports()
        {
            var modelImporter = AssetImporter.GetAtPath(MODEL_PATH) as ModelImporter;
            if (modelImporter == null)
            {
                throw new InvalidOperationException($"Could not find the Relay exchanger model at {MODEL_PATH}.");
            }

            modelImporter.addCollider = false;
            modelImporter.importAnimation = false;
            modelImporter.importCameras = false;
            modelImporter.importLights = false;
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
            modelImporter.meshCompression = ModelImporterMeshCompression.Low;
            modelImporter.optimizeMeshPolygons = true;
            modelImporter.optimizeMeshVertices = true;
            modelImporter.SaveAndReimport();

            var textureImporter = AssetImporter.GetAtPath(DECAL_PATH) as TextureImporter;
            if (textureImporter == null)
            {
                throw new InvalidOperationException($"Could not find the cooling-gantry decal at {DECAL_PATH}.");
            }

            textureImporter.alphaIsTransparency = true;
            textureImporter.mipmapEnabled = true;
            textureImporter.maxTextureSize = 2048;
            textureImporter.wrapMode = TextureWrapMode.Clamp;
            textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            textureImporter.SaveAndReimport();
        }

        private static void _ensureMaterialDirectory()
        {
            if (!AssetDatabase.IsValidFolder(MATERIAL_DIRECTORY))
            {
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "RelayCoolingGantry");
            }
        }

        private static Materials _ensureMaterials()
        {
            var decal = AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH);
            if (decal == null)
            {
                decal = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                {
                    name = "RelayCoolingGantryRouteDecal"
                };
                AssetDatabase.CreateAsset(decal, DECAL_MATERIAL_PATH);
            }

            decal.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(DECAL_PATH));
            decal.SetColor("_BaseColor", Color.white);
            decal.SetFloat("_Surface", 1f);
            decal.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            decal.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            decal.SetFloat("_ZWrite", 0f);
            decal.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            decal.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(decal);

            return new Materials
            {
                Armor = AssetDatabase.LoadAssetAtPath<Material>(ARMOR_MATERIAL_PATH),
                Deck = AssetDatabase.LoadAssetAtPath<Material>(DECK_MATERIAL_PATH),
                Cyan = AssetDatabase.LoadAssetAtPath<Material>(CYAN_MATERIAL_PATH),
                Ceramic = AssetDatabase.LoadAssetAtPath<Material>(CERAMIC_MATERIAL_PATH),
                Copper = AssetDatabase.LoadAssetAtPath<Material>(COPPER_MATERIAL_PATH),
                Decal = decal
            };
        }

        private static void _ensureLandmarkPrefab(Materials materials)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
            var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("Could not instantiate the Relay heat-exchanger model.");
            }

            try
            {
                instance.name = "RelayHeatExchanger";
                instance.AddComponent<AuthoredMapObstacle>().Configure(new Vector2(1.3f, 2f));
                _assignMaterial(instance.transform, "Exchanger armored plinth", materials.Armor);
                _assignMaterial(instance.transform, "Exchanger ceramic spine", materials.Ceramic);
                _assignMaterial(instance.transform, "West coolant coil", materials.Cyan);
                _assignMaterial(instance.transform, "East coolant coil", materials.Cyan);
                _assignMaterial(instance.transform, "South copper manifold", materials.Copper);
                _assignMaterial(instance.transform, "North ceramic manifold", materials.Ceramic);
                PrefabUtility.SaveAsPrefabAsset(instance, LANDMARK_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void _ensureRegionPrefab(Materials materials)
        {
            var root = new GameObject("RelayCoolingGantryRegion");
            try
            {
                _wall(root.transform, "Cooling Gantry Deck", new Vector3(0f, -0.42f, 0f),
                    new Vector3(12f, 0.55f, 8.5f), materials.Deck, false);
                _wall(root.transform, "Cooling Gantry South Bulkhead", new Vector3(0f, 0.5f, -4.25f),
                    new Vector3(12f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "Cooling Gantry West Bulkhead", new Vector3(-6f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 8.5f), materials.Armor, true);
                _wall(root.transform, "Cooling Gantry East Bulkhead", new Vector3(6f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 8.5f), materials.Armor, true);

                var landmarkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LANDMARK_PREFAB_PATH);
                var landmark = (GameObject)PrefabUtility.InstantiatePrefab(landmarkPrefab, root.transform);
                landmark.name = "Relay Heat Exchanger";
                landmark.transform.localPosition = new Vector3(0f, 0f, 0.2f);

                _wall(root.transform, "West Ceramic Deflector", new Vector3(-3.55f, 0.5f, -1.15f),
                    new Vector3(2.7f, 1f, 0.45f), materials.Ceramic, true).transform.localRotation =
                    Quaternion.Euler(0f, 31f, 0f);
                _wall(root.transform, "East Copper Deflector", new Vector3(3.55f, 0.5f, 1.45f),
                    new Vector3(2.7f, 1f, 0.45f), materials.Copper, true).transform.localRotation =
                    Quaternion.Euler(0f, -31f, 0f);

                var routing = new GameObject("Cooling Gantry Signal Lines");
                routing.transform.SetParent(root.transform, false);
                _wall(routing.transform, "West Return Feed", new Vector3(-4.65f, -0.1f, 0f),
                    new Vector3(0.12f, 0.04f, 7.6f), materials.Cyan, false);
                _wall(routing.transform, "East Return Feed", new Vector3(4.65f, -0.1f, 0f),
                    new Vector3(0.12f, 0.04f, 7.6f), materials.Cyan, false);
                _wall(routing.transform, "South Return Bridge", new Vector3(0f, -0.1f, -3.7f),
                    new Vector3(9.4f, 0.04f, 0.12f), materials.Cyan, false);

                var decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
                decal.name = "Relay Cooling Gantry Route Decal";
                decal.transform.SetParent(root.transform, false);
                decal.transform.localPosition = new Vector3(0f, -0.105f, 0f);
                decal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                decal.transform.localScale = Vector3.one * 5.2f;
                decal.GetComponent<Renderer>().sharedMaterial = materials.Decal;
                UnityEngine.Object.DestroyImmediate(decal.GetComponent<Collider>());

                var entrance = new GameObject("Cooling Gantry Reinforcement Gate");
                entrance.transform.SetParent(root.transform, false);
                entrance.transform.localPosition = new Vector3(0f, 0f, -3.7f);
                entrance.AddComponent<AuthoredInterceptorEntrance>().Configure(14);

                _salvageSocket(root.transform, "Cooling Gantry Relay Payload Socket",
                    new Vector3(3.75f, 0f, -2.55f));

                root.AddComponent<AuthoredPoweredTerritory>().Configure(
                    PoweredTerritorySource.RelayTower, new Vector2(5.7f, 4f), routing);
                PrefabUtility.SaveAsPrefabAsset(root, REGION_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void _integrateWithFoundry(Materials materials)
        {
            var foundry = PrefabUtility.LoadPrefabContents(FOUNDRY_PREFAB_PATH);
            try
            {
                _remove(foundry.transform, "Foundry North West");
                _remove(foundry.transform, "Foundry North Center");
                _remove(foundry.transform, "Foundry North East");
                _replaceWall(foundry.transform, "Foundry North Bulkhead", new Vector3(0f, 0.5f, 7f),
                    new Vector3(16f, 1f, 0.35f), materials.Armor);
                _remove(foundry.transform, "Foundry South Bulkhead");
                _replaceWall(foundry.transform, "Foundry South West", new Vector3(-7.1f, 0.5f, -7f),
                    new Vector3(1.8f, 1f, 0.35f), materials.Armor);
                _replaceWall(foundry.transform, "Foundry South Center", new Vector3(0f, 0.5f, -7f),
                    new Vector3(5.6f, 1f, 0.35f), materials.Armor);
                _replaceWall(foundry.transform, "Foundry South East", new Vector3(7.1f, 0.5f, -7f),
                    new Vector3(1.8f, 1f, 0.35f), materials.Armor);

                _remove(foundry.transform, "Protected Relay Payload Socket");
                _salvageSocket(foundry.transform, "Protected Relay Payload Socket",
                    new Vector3(7.25f, 0f, 4.8f));

                _remove(foundry.transform, "Relay Cooling Gantry Region");
                var regionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                var region = (GameObject)PrefabUtility.InstantiatePrefab(regionPrefab, foundry.transform);
                region.name = "Relay Cooling Gantry Region";
                region.transform.localPosition = new Vector3(0f, 0f, -11.25f);
                PrefabUtility.SaveAsPrefabAsset(foundry, FOUNDRY_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(foundry);
            }
        }

        private static void _assignMaterial(Transform root, string childName, Material material)
        {
            var child = root.Find(childName);
            if (child == null || material == null || !child.TryGetComponent<Renderer>(out var renderer))
            {
                throw new InvalidOperationException($"Could not assign the Relay Cooling Gantry material for {childName}.");
            }

            renderer.sharedMaterial = material;
        }

        private static void _remove(Transform parent, string objectName)
        {
            var existing = parent.Find(objectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static void _replaceWall(Transform parent, string objectName, Vector3 position, Vector3 scale, Material material)
        {
            _remove(parent, objectName);
            _wall(parent, objectName, position, scale, material, true);
        }

        private static void _salvageSocket(Transform parent, string objectName, Vector3 position)
        {
            var socket = new GameObject(objectName);
            socket.transform.SetParent(parent, false);
            socket.transform.localPosition = position;
            socket.AddComponent<AuthoredSalvageSocket>().Configure(SignalRegion.Relay, false);
        }

        private static GameObject _wall(
            Transform parent,
            string objectName,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool obstacle)
        {
            var result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.name = objectName;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.transform.localScale = scale;
            result.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(result.GetComponent<Collider>());
            if (obstacle)
            {
                result.AddComponent<AuthoredMapObstacle>().Configure(Vector2.one * 0.5f);
            }

            return result;
        }

        private sealed class Materials
        {
            public Material Armor;
            public Material Deck;
            public Material Cyan;
            public Material Ceramic;
            public Material Copper;
            public Material Decal;
        }
    }
}
