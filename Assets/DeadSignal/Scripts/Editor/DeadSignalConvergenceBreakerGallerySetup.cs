using System;
using UnityEditor;
using UnityEngine;
using DeadSignal.World;

namespace DeadSignal.Editor
{
    public static class DeadSignalConvergenceBreakerGallerySetup
    {
        private const string GALLERY_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineInductionGalleryRegion.prefab";
        private const string CHAMBER_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceChamberRegion.prefab";
        private const string REGION_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceBreakerGalleryRegion.prefab";
        private const string BREAKER_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceBusbar.prefab";
        private const string DECAL_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceBreakerGalleryRouteDecal.png";
        private const string MATERIAL_DIRECTORY =
            "Assets/DeadSignal/Resources/Materials/ConvergenceBreakerGallery";
        private const string DECAL_MATERIAL_PATH = MATERIAL_DIRECTORY + "/ConvergenceBreakerGalleryRouteDecal.mat";
        private const string ARMOR_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryArmor.mat";
        private const string DECK_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryDeck.mat";
        private const string CYAN_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SignalCyan.mat";
        private const string CERAMIC_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/SapperCradleCeramic.mat";
        private const string RED_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SecurityRed.mat";

        public static bool HasAssets
        {
            get
            {
                var gallery = AssetDatabase.LoadAssetAtPath<GameObject>(GALLERY_PREFAB_PATH);
                var chamber = AssetDatabase.LoadAssetAtPath<GameObject>(CHAMBER_PREFAB_PATH);
                var region = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                return gallery != null && chamber != null && region != null &&
                       AssetDatabase.LoadAssetAtPath<Texture2D>(DECAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH) != null &&
                       region.GetComponentsInChildren<AuthoredMapObstacle>().Length == 9 &&
                       region.GetComponentsInChildren<AuthoredInterceptorEntrance>().Length == 1 &&
                       region.GetComponent<AuthoredPoweredTerritory>() != null &&
                       chamber.transform.Find("Convergence East Bulkhead") == null &&
                       chamber.transform.Find("Convergence Breaker Gallery Region") != null &&
                       gallery.transform.Find(
                           "Convergence Chamber Region/Convergence Breaker Gallery Region") != null;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup Convergence Breaker Gallery")]
        public static void EnsureAssets()
        {
            _configureDecalImport();
            _ensureMaterialDirectory();
            var materials = _ensureMaterials();
            _ensureRegionPrefab(materials);
            _integrateWithChamber(materials);
            _integrateWithGallery();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The scene-authored Convergence Breaker Gallery is incomplete.");
            }
        }

        private static void _configureDecalImport()
        {
            var importer = AssetImporter.GetAtPath(DECAL_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the breaker-gallery decal at {DECAL_PATH}.");
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
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "ConvergenceBreakerGallery");
            }
        }

        private static Materials _ensureMaterials()
        {
            var decal = AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH);
            if (decal == null)
            {
                decal = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                {
                    name = "ConvergenceBreakerGalleryRouteDecal"
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
                Red = AssetDatabase.LoadAssetAtPath<Material>(RED_MATERIAL_PATH),
                Decal = decal
            };
        }

        private static void _ensureRegionPrefab(Materials materials)
        {
            var root = new GameObject("ConvergenceBreakerGalleryRegion");
            try
            {
                _wall(root.transform, "Breaker Gallery Deck", new Vector3(0f, -0.42f, 0f),
                    new Vector3(7f, 0.55f, 8f), materials.Deck, false);
                _wall(root.transform, "Breaker Gallery East Bulkhead", new Vector3(3.5f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 8f), materials.Armor, true);
                _wall(root.transform, "Breaker Gallery South Bulkhead", new Vector3(0f, 0.5f, -4f),
                    new Vector3(7f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "Breaker Gallery North Bulkhead", new Vector3(0f, 0.5f, 4f),
                    new Vector3(7f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "Breaker Gallery West South", new Vector3(-3.5f, 0.5f, -3.55f),
                    new Vector3(0.35f, 1f, 0.9f), materials.Armor, true);
                _wall(root.transform, "Breaker Gallery West Center", new Vector3(-3.5f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 2.2f), materials.Armor, true);
                _wall(root.transform, "Breaker Gallery West North", new Vector3(-3.5f, 0.5f, 3.55f),
                    new Vector3(0.35f, 1f, 0.9f), materials.Armor, true);

                var breakerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BREAKER_PREFAB_PATH);
                var breaker = (GameObject)PrefabUtility.InstantiatePrefab(breakerPrefab, root.transform);
                breaker.name = "Breaker Bank Assembly";
                breaker.transform.localPosition = new Vector3(0.4f, 0f, 0f);
                breaker.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

                _wall(root.transform, "South Ceramic Breaker Shield", new Vector3(1.8f, 0.5f, -2.35f),
                    new Vector3(2.2f, 1f, 0.42f), materials.Ceramic, true).transform.localRotation =
                    Quaternion.Euler(0f, -28f, 0f);
                _wall(root.transform, "North Ceramic Breaker Shield", new Vector3(1.8f, 0.5f, 2.35f),
                    new Vector3(2.2f, 1f, 0.42f), materials.Ceramic, true).transform.localRotation =
                    Quaternion.Euler(0f, 28f, 0f);

                var routing = new GameObject("Breaker Gallery Signal Lines");
                routing.transform.SetParent(root.transform, false);
                _wall(routing.transform, "South Breaker Feed", new Vector3(-0.4f, -0.1f, -2.25f),
                    new Vector3(5.6f, 0.04f, 0.12f), materials.Cyan, false);
                _wall(routing.transform, "North Breaker Feed", new Vector3(-0.4f, -0.1f, 2.25f),
                    new Vector3(5.6f, 0.04f, 0.12f), materials.Cyan, false);
                _wall(routing.transform, "Outer Breaker Return", new Vector3(2.75f, -0.1f, 0f),
                    new Vector3(0.12f, 0.04f, 4.6f), materials.Cyan, false);

                var decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
                decal.name = "Convergence Breaker Gallery Route Decal";
                decal.transform.SetParent(root.transform, false);
                decal.transform.localPosition = new Vector3(-0.25f, -0.105f, 0f);
                decal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                decal.transform.localScale = Vector3.one * 3.8f;
                decal.GetComponent<Renderer>().sharedMaterial = materials.Decal;
                UnityEngine.Object.DestroyImmediate(decal.GetComponent<Collider>());

                var entrance = new GameObject("Breaker Gallery Reinforcement Gate");
                entrance.transform.SetParent(root.transform, false);
                entrance.transform.localPosition = new Vector3(3.05f, 0f, 0f);
                entrance.AddComponent<AuthoredInterceptorEntrance>().Configure(16);
                _wall(entrance.transform, "Security Gate Header", new Vector3(-0.18f, 0.8f, 0f),
                    new Vector3(0.3f, 0.25f, 2.5f), materials.Red, false);

                root.AddComponent<AuthoredPoweredTerritory>().Configure(
                    PoweredTerritorySource.SpineTower, new Vector2(3.15f, 3.65f), routing);
                PrefabUtility.SaveAsPrefabAsset(root, REGION_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void _integrateWithChamber(Materials materials)
        {
            var chamber = PrefabUtility.LoadPrefabContents(CHAMBER_PREFAB_PATH);
            try
            {
                _remove(chamber.transform, "Convergence East Bulkhead");
                _replaceWall(chamber.transform, "Convergence East South", new Vector3(7f, 0.5f, -3.55f),
                    new Vector3(0.35f, 1f, 0.9f), materials.Armor);
                _replaceWall(chamber.transform, "Convergence East Center", new Vector3(7f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 2.2f), materials.Armor);
                _replaceWall(chamber.transform, "Convergence East North", new Vector3(7f, 0.5f, 3.55f),
                    new Vector3(0.35f, 1f, 0.9f), materials.Armor);

                _remove(chamber.transform, "Convergence Breaker Gallery Region");
                var regionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                var region = (GameObject)PrefabUtility.InstantiatePrefab(regionPrefab, chamber.transform);
                region.name = "Convergence Breaker Gallery Region";
                region.transform.localPosition = new Vector3(10.5f, 0f, 0f);
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

        private static void _replaceWall(Transform parent, string objectName, Vector3 position, Vector3 scale, Material material)
        {
            _remove(parent, objectName);
            _wall(parent, objectName, position, scale, material, true);
        }

        private static void _remove(Transform parent, string objectName)
        {
            var existing = parent.Find(objectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
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
            public Material Red;
            public Material Decal;
        }
    }
}
