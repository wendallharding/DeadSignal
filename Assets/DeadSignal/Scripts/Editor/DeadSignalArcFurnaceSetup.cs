using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadSignal.World;

namespace DeadSignal.Editor
{
    public static class DeadSignalArcFurnaceSetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string GALLERY_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineInductionGalleryRegion.prefab";
        private const string CHAMBER_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceChamberRegion.prefab";
        private const string SPINE_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/CapacitorSpineRegion.prefab";
        private const string REGION_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ArcFurnaceRegion.prefab";
        private const string MODEL_PATH =
            "Assets/DeadSignal/Resources/Environment/ArcFurnaceModel.fbx";
        private const string MODEL_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ArcFurnace.prefab";
        private const string DECAL_PATH =
            "Assets/DeadSignal/Resources/Environment/ArcFurnaceRouteDecal.png";
        private const string MATERIAL_DIRECTORY = "Assets/DeadSignal/Resources/Materials/ArcFurnace";
        private const string DECAL_MATERIAL_PATH = MATERIAL_DIRECTORY + "/ArcFurnaceRouteDecal.mat";
        private const string ARMOR_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryArmor.mat";
        private const string DECK_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryDeck.mat";
        private const string WHITE_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/DroneWhite.mat";
        private const string AMBER_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SalvageAmber.mat";
        private const string RED_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SecurityRed.mat";
        private const string CYAN_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SignalCyan.mat";

        public static bool HasAssets
        {
            get
            {
                var region = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                var chamber = AssetDatabase.LoadAssetAtPath<GameObject>(CHAMBER_PREFAB_PATH);
                var gallery = AssetDatabase.LoadAssetAtPath<GameObject>(GALLERY_PREFAB_PATH);
                var spine = AssetDatabase.LoadAssetAtPath<GameObject>(SPINE_PREFAB_PATH);
                return region != null && chamber != null && gallery != null && spine != null &&
                       AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PREFAB_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Texture2D>(DECAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH) != null &&
                       region.GetComponentsInChildren<AuthoredMapObstacle>().Length >= 9 &&
                       region.GetComponentsInChildren<AuthoredSalvageSocket>().Length == 1 &&
                       region.GetComponent<AuthoredFurnaceForgeObjective>()?.IsConfigured == true &&
                       region.GetComponent<AuthoredPoweredTerritory>() != null &&
                       region.GetComponentInChildren<AuthoredInterceptorEntrance>() != null &&
                       chamber.transform.Find("Convergence North Bulkhead") == null &&
                       chamber.transform.Find("Arc Furnace Region") != null &&
                       gallery.transform.Find("Convergence Chamber Region/Arc Furnace Region") != null &&
                       spine.GetComponentsInChildren<AuthoredSalvageSocket>().Length == 0;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup Arc Furnace")]
        public static void EnsureAssets()
        {
            _configureDecalImport();
            _ensureMaterialDirectory();
            var materials = _ensureMaterials();
            _ensureModelPrefab(materials);
            _ensureRegionPrefab(materials);
            _relocateGreedCache();
            _integrateWithChamber(materials);
            _integrateWithGallery();
            _ensureSceneBounds();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The scene-authored Arc Furnace is incomplete.");
            }
        }

        private static void _configureDecalImport()
        {
            var importer = AssetImporter.GetAtPath(DECAL_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Arc Furnace decal at {DECAL_PATH}.");
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
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "ArcFurnace");
            }
        }

        private static Materials _ensureMaterials()
        {
            var decal = AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH);
            if (decal == null)
            {
                decal = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                {
                    name = "ArcFurnaceRouteDecal"
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
                Red = AssetDatabase.LoadAssetAtPath<Material>(RED_MATERIAL_PATH),
                Cyan = AssetDatabase.LoadAssetAtPath<Material>(CYAN_MATERIAL_PATH),
                Decal = decal
            };
        }

        private static void _ensureModelPrefab(Materials materials)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
            if (model == null)
            {
                throw new InvalidOperationException($"Could not find the Arc Furnace model at {MODEL_PATH}.");
            }

            var root = new GameObject("ArcFurnace");
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                instance.name = "Arc Furnace Model";
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterial = _materialFor(renderer.name, materials);
                }

                root.AddComponent<AuthoredMapObstacle>().Configure(new Vector2(1.72f, 1.72f));
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

            if (partName.Contains("Return", StringComparison.OrdinalIgnoreCase))
            {
                return materials.Cyan;
            }

            if (partName.Contains("Core", StringComparison.OrdinalIgnoreCase))
            {
                return materials.Red;
            }

            if (partName.Contains("ring", StringComparison.OrdinalIgnoreCase))
            {
                return materials.Amber;
            }

            return materials.Armor;
        }

        private static void _ensureRegionPrefab(Materials materials)
        {
            var root = new GameObject("ArcFurnaceRegion");
            try
            {
                _wall(root.transform, "Arc Furnace Deck", new Vector3(0f, -0.42f, 0f),
                    new Vector3(14f, 0.55f, 9f), materials.Deck, false);
                _wall(root.transform, "Arc Furnace North Bulkhead", new Vector3(0f, 0.5f, 4.5f),
                    new Vector3(14f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "Arc Furnace West Bulkhead", new Vector3(-7f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 9f), materials.Armor, true);
                _wall(root.transform, "Arc Furnace East Bulkhead", new Vector3(7f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 9f), materials.Armor, true);
                _wall(root.transform, "Arc Furnace South West", new Vector3(-6.425f, 0.5f, -4.5f),
                    new Vector3(1.15f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "Arc Furnace South Center", new Vector3(0f, 0.5f, -4.5f),
                    new Vector3(6.7f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "Arc Furnace South East", new Vector3(6.425f, 0.5f, -4.5f),
                    new Vector3(1.15f, 1f, 0.35f), materials.Armor, true);

                _wall(root.transform, "West Furnace Shield South", new Vector3(-4.2f, 0.5f, -1.25f),
                    new Vector3(2.3f, 1f, 0.45f), materials.White, true).transform.localRotation =
                    Quaternion.Euler(0f, 27f, 0f);
                _wall(root.transform, "West Furnace Shield North", new Vector3(-3.8f, 0.5f, 1.9f),
                    new Vector3(2.3f, 1f, 0.45f), materials.White, true).transform.localRotation =
                    Quaternion.Euler(0f, -27f, 0f);

                var furnacePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PREFAB_PATH);
                var furnace = (GameObject)PrefabUtility.InstantiatePrefab(furnacePrefab, root.transform);
                furnace.name = "Arc Furnace Assembly";
                furnace.transform.localPosition = new Vector3(0f, 0f, 0.35f);

                var availableMarker = _wall(root.transform, "Furnace Forge Available", new Vector3(0f, 0.16f, -1.55f),
                    new Vector3(1.5f, 0.08f, 0.2f), materials.Amber, false);
                var completeMarker = _wall(root.transform, "Furnace Forge Complete", new Vector3(0f, 0.16f, -1.55f),
                    new Vector3(1.5f, 0.08f, 0.2f), materials.Cyan, false);
                var forgeAnchor = new GameObject("Arc Furnace Forge Control");
                forgeAnchor.transform.SetParent(root.transform, false);
                forgeAnchor.transform.localPosition = new Vector3(0f, 0f, -2.25f);
                root.AddComponent<AuthoredFurnaceForgeObjective>().Configure(
                    forgeAnchor.transform,
                    availableMarker,
                    completeMarker);

                var routing = new GameObject("Arc Furnace Signal Lines");
                routing.transform.SetParent(root.transform, false);
                _wall(routing.transform, "Furnace Return Trunk", new Vector3(0f, -0.1f, 2.85f),
                    new Vector3(0.12f, 0.04f, 2.2f), materials.Cyan, false);
                _wall(routing.transform, "West Return Feed", new Vector3(-4.6f, -0.1f, -2.8f),
                    new Vector3(0.12f, 0.04f, 3f), materials.Cyan, false);
                _wall(routing.transform, "East Return Feed", new Vector3(4.6f, -0.1f, -2.8f),
                    new Vector3(0.12f, 0.04f, 3f), materials.Cyan, false);

                var decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
                decal.name = "Arc Furnace Route Decal";
                decal.transform.SetParent(root.transform, false);
                decal.transform.localPosition = new Vector3(0f, -0.105f, -2.55f);
                decal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                decal.transform.localScale = Vector3.one * 3.6f;
                decal.GetComponent<Renderer>().sharedMaterial = materials.Decal;
                UnityEngine.Object.DestroyImmediate(decal.GetComponent<Collider>());

                var salvageSocket = new GameObject("Arc Furnace Salvage Socket");
                salvageSocket.transform.SetParent(root.transform, false);
                salvageSocket.transform.localPosition = new Vector3(0f, 0f, 3.55f);
                salvageSocket.AddComponent<AuthoredSalvageSocket>();

                var entrance = new GameObject("Arc Furnace Reinforcement Gate");
                entrance.transform.SetParent(root.transform, false);
                entrance.transform.localPosition = new Vector3(5.3f, 0f, 4.05f);
                entrance.AddComponent<AuthoredInterceptorEntrance>().Configure(6);
                _wall(entrance.transform, "Security Gate Header", new Vector3(0f, 0.8f, 0.28f),
                    new Vector3(2.8f, 0.25f, 0.3f), materials.Red, false);

                root.AddComponent<AuthoredPoweredTerritory>().Configure(
                    PoweredTerritorySource.SpineTower, new Vector2(6.65f, 4.15f), routing);
                PrefabUtility.SaveAsPrefabAsset(root, REGION_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void _relocateGreedCache()
        {
            var spine = PrefabUtility.LoadPrefabContents(SPINE_PREFAB_PATH);
            try
            {
                var socket = spine.transform.Find("Capacitor Spine Salvage Socket");
                if (socket != null)
                {
                    UnityEngine.Object.DestroyImmediate(socket.gameObject);
                }

                PrefabUtility.SaveAsPrefabAsset(spine, SPINE_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(spine);
            }
        }

        private static void _integrateWithChamber(Materials materials)
        {
            var chamber = PrefabUtility.LoadPrefabContents(CHAMBER_PREFAB_PATH);
            try
            {
                _remove(chamber.transform, "Convergence North Bulkhead");
                _replaceWall(chamber.transform, "Convergence North West", new Vector3(-6.425f, 0.5f, 4f),
                    new Vector3(1.15f, 1f, 0.35f), materials.Armor);
                _replaceWall(chamber.transform, "Convergence North Center", new Vector3(0f, 0.5f, 4f),
                    new Vector3(6.7f, 1f, 0.35f), materials.Armor);
                _replaceWall(chamber.transform, "Convergence North East", new Vector3(6.425f, 0.5f, 4f),
                    new Vector3(1.15f, 1f, 0.35f), materials.Armor);

                _remove(chamber.transform, "Arc Furnace Region");
                var regionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                var region = (GameObject)PrefabUtility.InstantiatePrefab(regionPrefab, chamber.transform);
                region.name = "Arc Furnace Region";
                region.transform.localPosition = new Vector3(0f, 0f, 8.5f);
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
            serialized.FindProperty("m_arenaHalfExtents").vector2Value = new Vector2(50.5f, 30.4f);
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
            public Material Red;
            public Material Cyan;
            public Material Decal;
        }
    }
}
