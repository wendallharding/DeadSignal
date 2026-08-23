using System;
using UnityEditor;
using UnityEngine;
using DeadSignal.Combat;

namespace DeadSignal.Editor
{
    public static class DeadSignalProjectileSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Projectiles/SignalBoltAlbedo.png";
        private const string TRAIL_TEXTURE_PATH = "Assets/DeadSignal/Resources/Projectiles/SignalBoltTrail.png";
        private const string BULKHEAD_IMPACT_TEXTURE_PATH = "Assets/DeadSignal/Resources/Projectiles/SignalBoltBulkheadImpact.png";
        private const string MODEL_PATH = "Assets/DeadSignal/Resources/Projectiles/SignalBoltModel.fbx";
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/Projectiles/SignalBoltAssembly.prefab";
        private const string SHELL_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SignalBoltShell.mat";
        private const string ENERGY_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SignalBoltEnergy.mat";
        private const string TRAIL_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SignalBoltTrail.mat";
        private const string BULKHEAD_IMPACT_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SignalBoltBulkheadImpact.mat";
        private const string TUNING_PATH = "Assets/DeadSignal/Resources/Tuning/SignalBoltPresentationTuning.asset";

        public static bool HasAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Texture2D>(TRAIL_TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Texture2D>(BULKHEAD_IMPACT_TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Material>(BULKHEAD_IMPACT_MATERIAL_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<SignalBoltPresentationTuning>(TUNING_PATH) != null &&
            _hasModelPrefab() &&
            _hasMaterialAssignments() &&
            _hasAuthoredTrail();

        public static void EnsureAssets()
        {
            _configureTexture(TEXTURE_PATH, false, TextureWrapMode.Repeat);
            _configureTexture(TRAIL_TEXTURE_PATH, true, TextureWrapMode.Clamp);
            _configureTexture(BULKHEAD_IMPACT_TEXTURE_PATH, false, TextureWrapMode.Clamp);
            _configureModel();
            _ensureTuning();
            _ensureMaterials();
            _ensureTrailMaterial();
            _ensureBulkheadImpactMaterial();
            _ensurePrefab();
            _assignMaterials();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The authored Signal bolt texture, model, materials, or prefab are incomplete.");
            }
        }

        private static void _configureTexture(string texturePath, bool alphaIsTransparency, TextureWrapMode wrapMode)
        {
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Signal bolt texture at {texturePath}.");
            }

            importer.alphaIsTransparency = alphaIsTransparency;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = wrapMode;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void _ensureTuning()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<SignalBoltPresentationTuning>(TUNING_PATH);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<SignalBoltPresentationTuning>();
                AssetDatabase.CreateAsset(tuning, TUNING_PATH);
            }

            EditorUtility.SetDirty(tuning);
        }

        private static void _configureModel()
        {
            var importer = AssetImporter.GetAtPath(MODEL_PATH) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Signal bolt model at {MODEL_PATH}.");
            }

            importer.addCollider = false;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.meshCompression = ModelImporterMeshCompression.Low;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.SaveAndReimport();
        }

        private static void _ensureMaterials()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            _configureMaterial(
                SHELL_MATERIAL_PATH,
                "SignalBoltShell",
                new Color(0.78f, 0.82f, 0.82f),
                Color.black,
                0.36f,
                texture);
            _configureMaterial(
                ENERGY_MATERIAL_PATH,
                "SignalBoltEnergy",
                new Color(0.01f, 0.72f, 0.9f),
                new Color(0f, 2.2f, 3.1f),
                0.2f,
                null);
        }

        private static void _configureMaterial(
            string assetPath,
            string materialName,
            Color baseColor,
            Color emissionColor,
            float smoothness,
            Texture2D texture)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            var isNewMaterial = material == null;
            if (isNewMaterial)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for Signal bolt materials.");
                }

                material = new Material(shader);
                material.name = materialName;
                material.color = baseColor;
                material.SetColor("_BaseColor", baseColor);
                material.SetFloat("_Smoothness", smoothness);
                material.SetColor("_EmissionColor", emissionColor);
                if (emissionColor.maxColorComponent > 0f)
                {
                    material.EnableKeyword("_EMISSION");
                }

                AssetDatabase.CreateAsset(material, assetPath);
            }

            if (texture != null)
            {
                material.mainTexture = texture;
                material.SetTexture("_BaseMap", texture);
            }

            if (isNewMaterial || texture != null)
            {
                EditorUtility.SetDirty(material);
            }
        }

        private static void _ensureTrailMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(TRAIL_MATERIAL_PATH);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Particles Unlit shader for the Signal bolt trail.");
                }

                material = new Material(shader)
                {
                    name = "SignalBoltTrail",
                    renderQueue = 3000
                };
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                AssetDatabase.CreateAsset(material, TRAIL_MATERIAL_PATH);
            }

            var trailTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TRAIL_TEXTURE_PATH);
            material.mainTexture = trailTexture;
            material.SetTexture("_BaseMap", trailTexture);
            EditorUtility.SetDirty(material);
        }

        private static void _ensureBulkheadImpactMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(BULKHEAD_IMPACT_MATERIAL_PATH);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP particle shader for bulkhead impacts.");
                }

                material = new Material(shader)
                {
                    name = "SignalBoltBulkheadImpact",
                    renderQueue = 3000
                };
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 1f);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                AssetDatabase.CreateAsset(material, BULKHEAD_IMPACT_MATERIAL_PATH);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BULKHEAD_IMPACT_TEXTURE_PATH);
            material.mainTexture = texture;
            material.SetTexture("_BaseMap", texture);
            EditorUtility.SetDirty(material);
        }

        private static void _ensurePrefab()
        {
            if (_hasModelPrefab())
            {
                return;
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
            var bolt = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (bolt == null)
            {
                throw new InvalidOperationException("Could not instantiate the imported Signal bolt model.");
            }

            bolt.name = "Signal Bolt Assembly";
            PrefabUtility.SaveAsPrefabAsset(bolt, PREFAB_PATH);
            UnityEngine.Object.DestroyImmediate(bolt);
        }

        private static void _assignMaterials()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            try
            {
                _assignMaterial(prefabRoot.transform, "Bolt Shell", SHELL_MATERIAL_PATH);
                _assignMaterial(prefabRoot.transform, "Bolt Energy", ENERGY_MATERIAL_PATH);
                _configureTrail(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void _assignMaterial(Transform root, string partName, string materialPath)
        {
            var part = root.Find(partName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (part == null || material == null || !part.TryGetComponent<Renderer>(out var renderer))
            {
                throw new InvalidOperationException($"Could not assign {materialPath} to Signal bolt part {partName}.");
            }

            renderer.sharedMaterial = material;
        }

        private static void _configureTrail(GameObject prefabRoot)
        {
            var tuning = AssetDatabase.LoadAssetAtPath<SignalBoltPresentationTuning>(TUNING_PATH);
            var trailMaterial = AssetDatabase.LoadAssetAtPath<Material>(TRAIL_MATERIAL_PATH);
            if (tuning == null || trailMaterial == null)
            {
                throw new InvalidOperationException("Signal bolt trail tuning or material is missing.");
            }

            var trail = prefabRoot.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = prefabRoot.AddComponent<TrailRenderer>();
            }

            trail.sharedMaterial = trailMaterial;
            trail.time = tuning.TrailDuration;
            trail.startWidth = tuning.StartingWidth;
            trail.endWidth = tuning.EndingWidth;
            trail.minVertexDistance = tuning.MinimumVertexDistance;
            trail.startColor = new Color(1f, 1f, 1f, tuning.MaximumAlpha);
            trail.endColor = new Color(1f, 1f, 1f, 0f);
            trail.textureMode = LineTextureMode.Stretch;
            trail.alignment = LineAlignment.View;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = true;
        }

        private static bool _hasModelPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            return prefab != null &&
                   _isImportedPart(prefab.transform.Find("Bolt Shell")) &&
                   _isImportedPart(prefab.transform.Find("Bolt Energy"));
        }

        private static bool _isImportedPart(Transform part)
        {
            return part != null &&
                   part.TryGetComponent<MeshFilter>(out var meshFilter) &&
                   meshFilter.sharedMesh != null &&
                   AssetDatabase.GetAssetPath(meshFilter.sharedMesh) == MODEL_PATH;
        }

        private static bool _hasMaterialAssignments()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            return prefab != null &&
                   _hasMaterial(prefab.transform.Find("Bolt Shell"), SHELL_MATERIAL_PATH) &&
                   _hasMaterial(prefab.transform.Find("Bolt Energy"), ENERGY_MATERIAL_PATH);
        }

        private static bool _hasMaterial(Transform part, string materialPath)
        {
            return part != null &&
                   part.TryGetComponent<Renderer>(out var renderer) &&
                   renderer.sharedMaterial == AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        }

        private static bool _hasAuthoredTrail()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            var trailMaterial = AssetDatabase.LoadAssetAtPath<Material>(TRAIL_MATERIAL_PATH);
            return prefab != null &&
                   trailMaterial != null &&
                   prefab.TryGetComponent<TrailRenderer>(out var trail) &&
                   trail.sharedMaterial == trailMaterial &&
                   trailMaterial.mainTexture == AssetDatabase.LoadAssetAtPath<Texture2D>(TRAIL_TEXTURE_PATH);
        }
    }
}
