using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadSignal.World;

namespace DeadSignal.Editor
{
    public static class DeadSignalSpineInductionGallerySetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineInductionGalleryRegion.prefab";
        private const string CAPACITOR_PATH = "Assets/DeadSignal/Resources/Environment/DepartureCapacitor.prefab";
        private const string DECAL_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineInductionGalleryRouteDecal.png";
        private const string MATERIAL_DIRECTORY = "Assets/DeadSignal/Resources/Materials/SpineInductionGallery";
        private const string DECAL_MATERIAL_PATH = MATERIAL_DIRECTORY + "/SpineInductionGalleryRouteDecal.mat";
        private const string ARMOR_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryArmor.mat";
        private const string DECK_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryDeck.mat";
        private const string CYAN_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SignalCyan.mat";

        private static readonly Vector3 s_regionPosition = new(42.5f, 0f, 8.5f);

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                return prefab != null &&
                       AssetDatabase.LoadAssetAtPath<Texture2D>(DECAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH) != null &&
                       prefab.GetComponentsInChildren<AuthoredMapObstacle>().Length >= 17 &&
                       prefab.GetComponent<AuthoredPoweredTerritory>() != null &&
                       prefab.transform.Find("Induction Gallery Signal Lines") != null &&
                       prefab.transform.Find("Induction Gallery Route Decal") != null &&
                       prefab.transform.Find("Induction Coil") != null;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup Spine Induction Gallery")]
        public static void EnsureAssets()
        {
            _configureDecalImport();
            _ensureMaterialDirectory();
            var materials = _ensureMaterials();
            _ensurePrefab(materials);
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The scene-authored Spine Induction Gallery is incomplete.");
            }
        }

        private static void _configureDecalImport()
        {
            var importer = AssetImporter.GetAtPath(DECAL_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the gallery decal at {DECAL_PATH}.");
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
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "SpineInductionGallery");
            }
        }

        private static Materials _ensureMaterials()
        {
            var decal = AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH);
            if (decal == null)
            {
                decal = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                {
                    name = "SpineInductionGalleryRouteDecal"
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
                Decal = decal
            };
        }

        private static void _ensurePrefab(Materials materials)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null)
            {
                return;
            }

            if (materials.Armor == null || materials.Deck == null || materials.Cyan == null)
            {
                throw new InvalidOperationException("The Induction Gallery requires the established world materials.");
            }

            var root = new GameObject("SpineInductionGalleryRegion");
            try
            {
                _wall(root.transform, "Induction Gallery Deck", new Vector3(0f, -0.42f, 0f),
                    new Vector3(14f, 0.55f, 7f), materials.Deck, false);
                _wall(root.transform, "Induction Gallery North Bulkhead", new Vector3(0f, 0.5f, 3.5f),
                    new Vector3(14f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "Induction Gallery West Bulkhead", new Vector3(-7f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 7f), materials.Armor, true);
                _wall(root.transform, "Induction Gallery East Bulkhead", new Vector3(7f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 7f), materials.Armor, true);

                _wall(root.transform, "West Deflection Baffle", new Vector3(-2.65f, 0.5f, 0.65f),
                    new Vector3(2.4f, 1f, 0.45f), materials.Armor, true).transform.localRotation =
                    Quaternion.Euler(0f, -24f, 0f);
                _wall(root.transform, "East Deflection Baffle", new Vector3(2.65f, 0.5f, 0.65f),
                    new Vector3(2.4f, 1f, 0.45f), materials.Armor, true).transform.localRotation =
                    Quaternion.Euler(0f, 24f, 0f);

                var capacitorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CAPACITOR_PATH);
                var coil = (GameObject)PrefabUtility.InstantiatePrefab(capacitorPrefab, root.transform);
                coil.name = "Induction Coil";
                coil.transform.localPosition = new Vector3(0f, 0f, 2.05f);
                coil.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

                var routing = new GameObject("Induction Gallery Signal Lines");
                routing.transform.SetParent(root.transform, false);
                _wall(routing.transform, "West Return Feed", new Vector3(-4.55f, -0.1f, -1.8f),
                    new Vector3(0.12f, 0.04f, 3f), materials.Cyan, false);
                _wall(routing.transform, "East Return Feed", new Vector3(4.55f, -0.1f, -1.8f),
                    new Vector3(0.12f, 0.04f, 3f), materials.Cyan, false);
                _wall(routing.transform, "Outer Return Bus", new Vector3(0f, -0.1f, 2.85f),
                    new Vector3(9.1f, 0.04f, 0.12f), materials.Cyan, false);

                var decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
                decal.name = "Induction Gallery Route Decal";
                decal.transform.SetParent(root.transform, false);
                decal.transform.localPosition = new Vector3(0f, -0.105f, -0.8f);
                decal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                decal.transform.localScale = Vector3.one * 4.4f;
                decal.GetComponent<Renderer>().sharedMaterial = materials.Decal;
                UnityEngine.Object.DestroyImmediate(decal.GetComponent<Collider>());

                root.AddComponent<AuthoredPoweredTerritory>().Configure(
                    PoweredTerritorySource.SpineTower, new Vector2(6.65f, 3.15f), routing);
                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(item => item.name == "Spine Induction Gallery Region");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (existing == null || PrefabUtility.GetCorrespondingObjectFromSource(existing) != prefab)
            {
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing);
                }

                existing = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                existing.name = "Spine Induction Gallery Region";
            }

            existing.transform.position = s_regionPosition;
            existing.transform.rotation = Quaternion.identity;
            existing.transform.localScale = Vector3.one;

            var references = UnityEngine.Object.FindFirstObjectByType<DeadSignalSceneReferences>(FindObjectsInactive.Include);
            var serialized = new SerializedObject(references);
            serialized.FindProperty("m_arenaHalfExtents").vector2Value = new Vector2(50.5f, 12.4f);
            serialized.FindProperty("m_spineInductionGallery").objectReferenceValue = existing;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(references);
            EditorSceneManager.SaveScene(scene);
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
            public Material Cyan;
            public Material Decal;
        }
    }
}
