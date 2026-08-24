using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadSignal.World;

namespace DeadSignal.Editor
{
    public static class DeadSignalQuenchLoopSetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string GALLERY_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineInductionGalleryRegion.prefab";
        private const string CHAMBER_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceChamberRegion.prefab";
        private const string FURNACE_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ArcFurnaceRegion.prefab";
        private const string REGION_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/QuenchLoopRegion.prefab";
        private const string MODEL_PATH =
            "Assets/DeadSignal/Resources/Environment/QuenchCondenserModel.fbx";
        private const string MODEL_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/QuenchCondenser.prefab";
        private const string DECAL_PATH =
            "Assets/DeadSignal/Resources/Environment/QuenchLoopRouteDecal.png";
        private const string MATERIAL_DIRECTORY = "Assets/DeadSignal/Resources/Materials/QuenchLoop";
        private const string DECAL_MATERIAL_PATH = MATERIAL_DIRECTORY + "/QuenchLoopRouteDecal.mat";
        private const string ARMOR_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryArmor.mat";
        private const string DECK_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryDeck.mat";
        private const string WHITE_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/DroneWhite.mat";
        private const string AMBER_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SalvageAmber.mat";
        private const string CYAN_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SignalCyan.mat";

        public static bool HasAssets
        {
            get
            {
                var region = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                var furnace = AssetDatabase.LoadAssetAtPath<GameObject>(FURNACE_PREFAB_PATH);
                var chamber = AssetDatabase.LoadAssetAtPath<GameObject>(CHAMBER_PREFAB_PATH);
                var gallery = AssetDatabase.LoadAssetAtPath<GameObject>(GALLERY_PREFAB_PATH);
                return region != null && furnace != null && chamber != null && gallery != null &&
                       AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PREFAB_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Texture2D>(DECAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH) != null &&
                       region.GetComponentsInChildren<AuthoredMapObstacle>().Length == 9 &&
                       region.GetComponent<AuthoredPoweredTerritory>() != null &&
                       furnace.transform.Find("Arc Furnace East Bulkhead") == null &&
                       furnace.transform.Find("Quench Loop Region") != null &&
                       chamber.transform.Find("Arc Furnace Region/Quench Loop Region") != null &&
                       gallery.transform.Find(
                           "Convergence Chamber Region/Arc Furnace Region/Quench Loop Region") != null;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup Quench Loop")]
        public static void EnsureAssets()
        {
            _configureDecalImport();
            _ensureMaterialDirectory();
            var materials = _ensureMaterials();
            _ensureModelPrefab(materials);
            _ensureRegionPrefab(materials);
            _integrateWithFurnace(materials);
            _integrateWithChamber();
            _integrateWithGallery();
            _ensureSceneBounds();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The scene-authored Quench Loop is incomplete.");
            }
        }

        private static void _configureDecalImport()
        {
            var importer = AssetImporter.GetAtPath(DECAL_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Quench Loop decal at {DECAL_PATH}.");
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
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "QuenchLoop");
            }
        }

        private static Materials _ensureMaterials()
        {
            var decal = AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH);
            if (decal == null)
            {
                decal = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                {
                    name = "QuenchLoopRouteDecal"
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
                White = AssetDatabase.LoadAssetAtPath<Material>(WHITE_MATERIAL_PATH),
                Amber = AssetDatabase.LoadAssetAtPath<Material>(AMBER_MATERIAL_PATH),
                Cyan = AssetDatabase.LoadAssetAtPath<Material>(CYAN_MATERIAL_PATH),
                Decal = decal
            };
        }

        private static void _ensureModelPrefab(Materials materials)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
            if (model == null)
            {
                throw new InvalidOperationException($"Could not find the Quench condenser model at {MODEL_PATH}.");
            }

            var root = new GameObject("QuenchCondenser");
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                instance.name = "Quench Condenser Model";
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterial = _materialFor(renderer.name, materials);
                }

                root.AddComponent<AuthoredMapObstacle>().Configure(new Vector2(1.1f, 0.75f));
                PrefabUtility.SaveAsPrefabAsset(root, MODEL_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Material _materialFor(string partName, Materials materials)
        {
            if (partName.Contains("Ceramic", StringComparison.OrdinalIgnoreCase))
            {
                return materials.White;
            }

            if (partName.Contains("Warning", StringComparison.OrdinalIgnoreCase))
            {
                return materials.Amber;
            }

            if (partName.Contains("Coolant", StringComparison.OrdinalIgnoreCase) ||
                partName.Contains("Return", StringComparison.OrdinalIgnoreCase))
            {
                return materials.Cyan;
            }

            return materials.Armor;
        }

        private static void _ensureRegionPrefab(Materials materials)
        {
            var root = new GameObject("QuenchLoopRegion");
            try
            {
                _wall(root.transform, "Quench Loop Deck", new Vector3(0f, -0.42f, 0f),
                    new Vector3(7f, 0.55f, 9f), materials.Deck, false);
                _wall(root.transform, "Quench East Bulkhead", new Vector3(3.5f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 9f), materials.Armor, true);
                _wall(root.transform, "Quench South Bulkhead", new Vector3(0f, 0.5f, -4.5f),
                    new Vector3(7f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "Quench North Bulkhead", new Vector3(0f, 0.5f, 4.5f),
                    new Vector3(7f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "Quench West South", new Vector3(-3.5f, 0.5f, -4.125f),
                    new Vector3(0.35f, 1f, 0.75f), materials.Armor, true);
                _wall(root.transform, "Quench West Center", new Vector3(-3.5f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 2.5f), materials.Armor, true);
                _wall(root.transform, "Quench West North", new Vector3(-3.5f, 0.5f, 4.125f),
                    new Vector3(0.35f, 1f, 0.75f), materials.Armor, true);

                _wall(root.transform, "South Quench Deflector", new Vector3(0.85f, 0.5f, -2.3f),
                    new Vector3(2.6f, 1f, 0.45f), materials.White, true).transform.localRotation =
                    Quaternion.Euler(0f, -31f, 0f);
                _wall(root.transform, "North Quench Deflector", new Vector3(-0.85f, 0.5f, 2.3f),
                    new Vector3(2.6f, 1f, 0.45f), materials.White, true).transform.localRotation =
                    Quaternion.Euler(0f, 31f, 0f);

                var condenserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PREFAB_PATH);
                var condenser = (GameObject)PrefabUtility.InstantiatePrefab(condenserPrefab, root.transform);
                condenser.name = "Quench Condenser Assembly";

                var routing = new GameObject("Quench Loop Signal Lines");
                routing.transform.SetParent(root.transform, false);
                _wall(routing.transform, "Quench Return Trunk", new Vector3(2.55f, -0.1f, 0f),
                    new Vector3(0.12f, 0.04f, 7.3f), materials.Cyan, false);
                _wall(routing.transform, "Quench South Feed", new Vector3(-0.2f, -0.1f, -2.5f),
                    new Vector3(5.3f, 0.04f, 0.12f), materials.Cyan, false);
                _wall(routing.transform, "Quench North Feed", new Vector3(-0.2f, -0.1f, 2.5f),
                    new Vector3(5.3f, 0.04f, 0.12f), materials.Cyan, false);

                var decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
                decal.name = "Quench Loop Route Decal";
                decal.transform.SetParent(root.transform, false);
                decal.transform.localPosition = new Vector3(0f, -0.105f, 0f);
                decal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                decal.transform.localScale = Vector3.one * 3.8f;
                decal.GetComponent<Renderer>().sharedMaterial = materials.Decal;
                UnityEngine.Object.DestroyImmediate(decal.GetComponent<Collider>());

                root.AddComponent<AuthoredPoweredTerritory>().Configure(
                    PoweredTerritorySource.SpineTower, new Vector2(3.15f, 4.15f), routing);
                PrefabUtility.SaveAsPrefabAsset(root, REGION_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void _integrateWithFurnace(Materials materials)
        {
            var furnace = PrefabUtility.LoadPrefabContents(FURNACE_PREFAB_PATH);
            try
            {
                _remove(furnace.transform, "Arc Furnace East Bulkhead");
                _replaceWall(furnace.transform, "Arc Furnace East South", new Vector3(7f, 0.5f, -4.125f),
                    new Vector3(0.35f, 1f, 0.75f), materials.Armor);
                _replaceWall(furnace.transform, "Arc Furnace East Center", new Vector3(7f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 2.5f), materials.Armor);
                _replaceWall(furnace.transform, "Arc Furnace East North", new Vector3(7f, 0.5f, 4.125f),
                    new Vector3(0.35f, 1f, 0.75f), materials.Armor);

                _remove(furnace.transform, "Quench Loop Region");
                var regionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                var region = (GameObject)PrefabUtility.InstantiatePrefab(regionPrefab, furnace.transform);
                region.name = "Quench Loop Region";
                region.transform.localPosition = new Vector3(10.5f, 0f, 0f);
                PrefabUtility.SaveAsPrefabAsset(furnace, FURNACE_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(furnace);
            }
        }

        private static void _integrateWithChamber()
        {
            var chamber = PrefabUtility.LoadPrefabContents(CHAMBER_PREFAB_PATH);
            try
            {
                _remove(chamber.transform, "Arc Furnace Region");
                var furnacePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FURNACE_PREFAB_PATH);
                var furnace = (GameObject)PrefabUtility.InstantiatePrefab(furnacePrefab, chamber.transform);
                furnace.name = "Arc Furnace Region";
                furnace.transform.localPosition = new Vector3(0f, 0f, 8.5f);
                PrefabUtility.SaveAsPrefabAsset(chamber, CHAMBER_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(chamber);
            }
        }

        private static void _integrateWithGallery()
        {
            var gallery = PrefabUtility.LoadPrefabContents(GALLERY_PREFAB_PATH);
            try
            {
                _remove(gallery.transform, "Convergence Chamber Region");
                var chamberPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CHAMBER_PREFAB_PATH);
                var chamber = (GameObject)PrefabUtility.InstantiatePrefab(chamberPrefab, gallery.transform);
                chamber.name = "Convergence Chamber Region";
                chamber.transform.localPosition = new Vector3(0f, 0f, 8.5f);
                PrefabUtility.SaveAsPrefabAsset(gallery, GALLERY_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(gallery);
            }
        }

        private static void _ensureSceneBounds()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var references = UnityEngine.Object.FindFirstObjectByType<DeadSignalSceneReferences>(FindObjectsInactive.Include);
            var serialized = new SerializedObject(references);
            serialized.FindProperty("m_arenaHalfExtents").vector2Value = new Vector2(57.5f, 30.4f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(references);
            EditorSceneManager.SaveScene(scene);
        }

        private static void _remove(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static void _replaceWall(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            _remove(parent, name);
            _wall(parent, name, position, scale, material, true);
        }

        private static GameObject _wall(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool obstacle)
        {
            var result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.name = name;
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
            public Material White;
            public Material Amber;
            public Material Cyan;
            public Material Decal;
        }
    }
}
