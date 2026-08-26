using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadSignal.World;

namespace DeadSignal.Editor
{
    public static class DeadSignalSpineDischargeTrenchSetup
    {
        private const string SPINE_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/CapacitorSpineRegion.prefab";
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string REGION_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineDischargeTrenchRegion.prefab";
        private const string DECAL_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineDischargeTrenchRouteDecal.png";
        private const string MATERIAL_DIRECTORY =
            "Assets/DeadSignal/Resources/Materials/SpineDischargeTrench";
        private const string DECAL_MATERIAL_PATH = MATERIAL_DIRECTORY + "/SpineDischargeTrenchRouteDecal.mat";
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
                var spine = AssetDatabase.LoadAssetAtPath<GameObject>(SPINE_PREFAB_PATH);
                var region = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                return spine != null && region != null &&
                       AssetDatabase.LoadAssetAtPath<Texture2D>(DECAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH) != null &&
                       spine.transform.Find("Spine Discharge Trench Region") != null &&
                       spine.transform.Find("Capacitor Spine South Bulkhead") == null &&
                       region.GetComponentsInChildren<AuthoredMapObstacle>().Length == 6 &&
                       region.GetComponentsInChildren<AuthoredInterceptorEntrance>().Length == 1 &&
                       region.GetComponent<AuthoredPoweredTerritory>() != null;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup Spine Discharge Trench")]
        public static void EnsureAssets()
        {
            _configureDecalImport();
            _ensureMaterialDirectory();
            var materials = _ensureMaterials();
            _ensureRegionPrefab(materials);
            _integrateWithSpine(materials);
            _ensureSceneBounds();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The scene-authored Spine Discharge Trench is incomplete.");
            }
        }

        private static void _configureDecalImport()
        {
            var importer = AssetImporter.GetAtPath(DECAL_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the discharge-trench decal at {DECAL_PATH}.");
            }

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void _ensureMaterialDirectory()
        {
            if (!AssetDatabase.IsValidFolder(MATERIAL_DIRECTORY))
            {
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "SpineDischargeTrench");
            }
        }

        private static Materials _ensureMaterials()
        {
            var decal = AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH);
            if (decal == null)
            {
                decal = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                {
                    name = "SpineDischargeTrenchRouteDecal"
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

        private static void _ensureRegionPrefab(Materials materials)
        {
            var root = new GameObject("SpineDischargeTrenchRegion");
            try
            {
                _wall(root.transform, "Discharge Trench Deck", new Vector3(0f, -0.42f, 0f),
                    new Vector3(10f, 0.55f, 6f), materials.Deck, false);
                _wall(root.transform, "Discharge Trench South Bulkhead", new Vector3(0f, 0.5f, -3f),
                    new Vector3(10f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "Discharge Trench West Bulkhead", new Vector3(-5f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 6f), materials.Armor, true);
                _wall(root.transform, "Discharge Trench East Bulkhead", new Vector3(5f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 6f), materials.Armor, true);

                _wall(root.transform, "Central Discharge Coil", new Vector3(0f, 0.5f, 0f),
                    new Vector3(2.2f, 1f, 1.6f), materials.Copper, true);
                _wall(root.transform, "West Ceramic Baffle", new Vector3(-3.1f, 0.5f, -1f),
                    new Vector3(2.2f, 1f, 0.4f), materials.Ceramic, true).transform.localRotation =
                    Quaternion.Euler(0f, 28f, 0f);
                _wall(root.transform, "East Ceramic Baffle", new Vector3(3.1f, 0.5f, -1f),
                    new Vector3(2.2f, 1f, 0.4f), materials.Ceramic, true).transform.localRotation =
                    Quaternion.Euler(0f, -28f, 0f);

                var routing = new GameObject("Discharge Trench Signal Lines");
                routing.transform.SetParent(root.transform, false);
                _wall(routing.transform, "West Feed", new Vector3(-3.8f, -0.1f, 0f),
                    new Vector3(0.1f, 0.04f, 5.4f), materials.Cyan, false);
                _wall(routing.transform, "East Feed", new Vector3(3.8f, -0.1f, 0f),
                    new Vector3(0.1f, 0.04f, 5.4f), materials.Cyan, false);
                _wall(routing.transform, "South Bridge", new Vector3(0f, -0.1f, -2.5f),
                    new Vector3(7.6f, 0.04f, 0.1f), materials.Cyan, false);

                var decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
                decal.name = "Spine Discharge Trench Route Decal";
                decal.transform.SetParent(root.transform, false);
                decal.transform.localPosition = new Vector3(0f, -0.105f, 0f);
                decal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                decal.transform.localScale = Vector3.one * 4.8f;
                decal.GetComponent<Renderer>().sharedMaterial = materials.Decal;
                UnityEngine.Object.DestroyImmediate(decal.GetComponent<Collider>());

                var entrance = new GameObject("Discharge Trench Reinforcement Gate");
                entrance.transform.SetParent(root.transform, false);
                entrance.transform.localPosition = new Vector3(0f, 0f, -2.5f);
                entrance.AddComponent<AuthoredInterceptorEntrance>().Configure(15);

                root.AddComponent<AuthoredPoweredTerritory>().Configure(
                    PoweredTerritorySource.SpineTower, new Vector2(4.7f, 2.7f), routing);
                PrefabUtility.SaveAsPrefabAsset(root, REGION_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void _integrateWithSpine(Materials materials)
        {
            var spine = PrefabUtility.LoadPrefabContents(SPINE_PREFAB_PATH);
            try
            {
                _remove(spine.transform, "Capacitor Spine South Bulkhead");
                _replaceWall(spine.transform, "Capacitor Spine South West", new Vector3(-5f, 0.5f, -5f),
                    new Vector3(4f, 1f, 0.35f), materials.Armor);
                _replaceWall(spine.transform, "Capacitor Spine South Center", new Vector3(0f, 0.5f, -5f),
                    new Vector3(1.6f, 1f, 0.35f), materials.Armor);
                _replaceWall(spine.transform, "Capacitor Spine South East", new Vector3(5f, 0.5f, -5f),
                    new Vector3(4f, 1f, 0.35f), materials.Armor);

                _remove(spine.transform, "Spine Discharge Trench Region");
                var regionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                var region = (GameObject)PrefabUtility.InstantiatePrefab(regionPrefab, spine.transform);
                region.name = "Spine Discharge Trench Region";
                region.transform.localPosition = new Vector3(0f, 0f, -8f);
                PrefabUtility.SaveAsPrefabAsset(spine, SPINE_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(spine);
            }
        }

        private static void _ensureSceneBounds()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var references = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<DeadSignalSceneReferences>(true))
                .FirstOrDefault();
            if (references == null)
            {
                throw new InvalidOperationException("SampleScene is missing DeadSignalSceneReferences.");
            }

            var serialized = new SerializedObject(references);
            var bounds = serialized.FindProperty("m_arenaHalfExtents");
            bounds.vector2Value = new Vector2(Mathf.Max(50.5f, bounds.vector2Value.x), Mathf.Max(12f, bounds.vector2Value.y));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(references);
            EditorSceneManager.SaveScene(scene);
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
