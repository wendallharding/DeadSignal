using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadSignal.World;

namespace DeadSignal.Editor
{
    public static class DeadSignalSalvageAnnexSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Environment/SalvageAnnexAlbedo.png";
        private const string STATUS_TEXTURE_PATH = "Assets/DeadSignal/Resources/Environment/CargoCouplingStatusPanel.png";
        private const string MODEL_PATH = "Assets/DeadSignal/Resources/Environment/SalvageAnnexBarrierModel.fbx";
        private const string COUPLING_BASE_MESH_PATH = "Assets/DeadSignal/Resources/Environment/CargoCouplingBaseReadability.asset";
        private const string COUPLING_ROTOR_MESH_PATH = "Assets/DeadSignal/Resources/Environment/CargoCouplingRotorReadability.asset";
        private const string ARMOR_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SalvageAnnexArmor.mat";
        private const string HAZARD_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SalvageAnnexHazard.mat";
        private const string CONDUIT_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SalvageAnnexConduit.mat";
        private const string STATUS_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/CargoCouplingStatus.mat";
        private const string BARRIER_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/SalvageAnnexBarrier.prefab";
        private const string ANNEX_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/SalvageAnnex.prefab";
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";

        private static readonly Vector3 s_salvagePosition = new(9.7f, 0f, 6.3f);

        public static bool HasAssets
        {
            get
            {
                var barrier = AssetDatabase.LoadAssetAtPath<GameObject>(BARRIER_PREFAB_PATH);
                var annex = AssetDatabase.LoadAssetAtPath<GameObject>(ANNEX_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Texture2D>(STATUS_TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(COUPLING_BASE_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(COUPLING_ROTOR_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(ARMOR_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(HAZARD_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(CONDUIT_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(STATUS_MATERIAL_PATH) != null &&
                       _hasValidBarrier(barrier) &&
                       annex != null &&
                       annex.GetComponentsInChildren<AuthoredMapObstacle>().Length == 3 &&
                       annex.TryGetComponent<AuthoredCargoAnnexObjective>(out var objective) &&
                       objective.IsConfigured &&
                       objective.HasReadabilityAssets;
            }
        }

        public static void EnsureAssets()
        {
            _configureTextureImport();
            _configureStatusTextureImport();
            _configureModelImport();
            _ensureMaterials();
            _ensureReadabilityMeshes();
            _ensureBarrierPrefab();
            _ensureAnnexPrefab();
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The salvage-annex assets are incomplete.");
            }
        }

        private static void _configureTextureImport()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the salvage-annex texture at {TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void _configureModelImport()
        {
            var importer = AssetImporter.GetAtPath(MODEL_PATH) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the salvage-annex model at {MODEL_PATH}.");
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

        private static void _configureStatusTextureImport()
        {
            var importer = AssetImporter.GetAtPath(STATUS_TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Cargo coupling status texture at {STATUS_TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void _ensureMaterials()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            var armor = _loadOrCreateMaterial(ARMOR_MATERIAL_PATH, "SalvageAnnexArmor");
            armor.SetColor("_BaseColor", Color.white);
            armor.SetTexture("_BaseMap", texture);
            armor.SetFloat("_Metallic", 0.38f);
            armor.SetFloat("_Smoothness", 0.32f);
            EditorUtility.SetDirty(armor);

            var hazard = _loadOrCreateMaterial(HAZARD_MATERIAL_PATH, "SalvageAnnexHazard");
            hazard.SetColor("_BaseColor", new Color(0.88f, 0.42f, 0.035f));
            hazard.SetFloat("_Metallic", 0.28f);
            hazard.SetFloat("_Smoothness", 0.36f);
            EditorUtility.SetDirty(hazard);

            var conduit = _loadOrCreateMaterial(CONDUIT_MATERIAL_PATH, "SalvageAnnexConduit");
            var conduitColor = new Color(0.01f, 0.68f, 0.9f);
            conduit.SetColor("_BaseColor", conduitColor);
            conduit.SetColor("_EmissionColor", conduitColor * 1.45f);
            conduit.SetFloat("_Metallic", 0.12f);
            conduit.SetFloat("_Smoothness", 0.68f);
            conduit.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(conduit);

            var status = _loadOrCreateMaterial(STATUS_MATERIAL_PATH, "CargoCouplingStatus");
            var statusTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(STATUS_TEXTURE_PATH);
            status.SetColor("_BaseColor", Color.white);
            status.SetTexture("_BaseMap", statusTexture);
            status.SetColor("_EmissionColor", Color.black);
            status.SetTexture("_EmissionMap", statusTexture);
            status.SetFloat("_Metallic", 0.42f);
            status.SetFloat("_Smoothness", 0.46f);
            status.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(status);
        }

        private static void _ensureReadabilityMeshes()
        {
            var baseBuilder = new MeshBuilder("CargoCouplingBaseReadability");
            baseBuilder.AddPrism(Vector3.up * 0.11f, 12, 0.82f, 0.7f, 0.22f);
            for (var index = 0; index < 4; index++)
            {
                var angle = index * 90f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                baseBuilder.AddBox(direction * 0.72f + Vector3.up * 0.08f, new Vector3(0.34f, 0.16f, 0.42f), angle);
            }
            _saveOrReplaceMesh(COUPLING_BASE_MESH_PATH, baseBuilder.Build());

            var rotorBuilder = new MeshBuilder("CargoCouplingRotorReadability");
            rotorBuilder.AddPrism(Vector3.up * 0.24f, 12, 0.44f, 0.36f, 0.16f);
            for (var index = 0; index < 4; index++)
            {
                var angle = index * 90f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                rotorBuilder.AddBox(direction * 0.43f + Vector3.up * 0.24f, new Vector3(0.24f, 0.18f, 0.38f), angle);
            }
            _saveOrReplaceMesh(COUPLING_ROTOR_MESH_PATH, rotorBuilder.Build());
        }

        private static void _saveOrReplaceMesh(string path, Mesh generated)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, path);
                return;
            }

            EditorUtility.CopySerialized(generated, existing);
            UnityEngine.Object.DestroyImmediate(generated);
            EditorUtility.SetDirty(existing);
        }

        private static Material _loadOrCreateMaterial(string path, string materialName)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Could not find the URP Lit shader for the salvage-annex materials.");
            }

            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void _ensureBarrierPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BARRIER_PREFAB_PATH) == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
                var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException("Could not instantiate the imported salvage-annex barrier model.");
                }

                instance.name = "SalvageAnnexBarrier";
                PrefabUtility.SaveAsPrefabAsset(instance, BARRIER_PREFAB_PATH);
                UnityEngine.Object.DestroyImmediate(instance);
            }

            var root = PrefabUtility.LoadPrefabContents(BARRIER_PREFAB_PATH);
            try
            {
                var obstacle = root.GetComponent<AuthoredMapObstacle>();
                if (obstacle == null)
                {
                    obstacle = root.AddComponent<AuthoredMapObstacle>();
                }

                obstacle.Configure(new Vector2(2.15f, 0.39f));
                _assignMaterial(root.transform, "Salvage Annex Armor", ARMOR_MATERIAL_PATH);
                _assignMaterial(root.transform, "Salvage Annex Hazard Rail", HAZARD_MATERIAL_PATH);
                _assignMaterial(root.transform, "Salvage Annex Conduit", CONDUIT_MATERIAL_PATH);
                PrefabUtility.SaveAsPrefabAsset(root, BARRIER_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _ensureAnnexPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ANNEX_PREFAB_PATH) == null)
            {
                var emptyAnnex = new GameObject("SalvageAnnex");
                try
                {
                    PrefabUtility.SaveAsPrefabAsset(emptyAnnex, ANNEX_PREFAB_PATH);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(emptyAnnex);
                }
            }

            var barrierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BARRIER_PREFAB_PATH);
            var annex = PrefabUtility.LoadPrefabContents(ANNEX_PREFAB_PATH);
            try
            {
                _ensureBarrier(annex.transform, barrierPrefab, "North Cargo Barrier", new Vector3(0.75f, 0f, 1.55f), 0f,
                    new Vector3(0.65f, 1f, 1f));
                _ensureBarrier(annex.transform, barrierPrefab, "East Cargo Barrier", new Vector3(1.72f, 0f, 0f), 90f,
                    new Vector3(0.62f, 1f, 1f));
                _ensureBarrier(annex.transform, barrierPrefab, "South Cargo Barrier", new Vector3(0.75f, 0f, -1.35f), 0f,
                    new Vector3(0.65f, 1f, 1f));
                var commitmentAnchor = _ensureAnchor(annex.transform, "Cargo Commitment Anchor", new Vector3(-0.5f, 0f, 0f));
                var couplingSocket = _ensureAnchor(annex.transform, "Power Coupling Socket", new Vector3(0.15f, 0f, 0f));
                var withdrawalAnchor = _ensureAnchor(annex.transform, "Cargo Withdrawal Anchor", new Vector3(-1.3f, 0f, 0f));
                var commitmentMarker = _ensureMarker(annex.transform, "Cargo Commitment Marker", PrimitiveType.Cube,
                    commitmentAnchor.localPosition + new Vector3(0f, 0.025f, 0f), new Vector3(0.1f, 0.025f, 2.2f),
                    HAZARD_MATERIAL_PATH);
                var withdrawalMarker = _ensureMarker(annex.transform, "Cargo Withdrawal Marker", PrimitiveType.Cube,
                    withdrawalAnchor.localPosition + new Vector3(0f, 0.025f, 0f), new Vector3(0.12f, 0.025f, 2.2f),
                    CONDUIT_MATERIAL_PATH);
                var securedMarker = _ensureMarker(annex.transform, "Power Coupling Secured Marker", PrimitiveType.Cylinder,
                    couplingSocket.localPosition + new Vector3(0f, 0.025f, 0f), new Vector3(0.9f, 0.025f, 0.9f),
                    CONDUIT_MATERIAL_PATH);
                var couplingBase = _ensureReadabilityPart(annex.transform, "Cargo Coupling Base",
                    couplingSocket.localPosition, COUPLING_BASE_MESH_PATH, ARMOR_MATERIAL_PATH);
                var couplingRotor = _ensureReadabilityPart(annex.transform, "Cargo Coupling Rotor",
                    couplingSocket.localPosition, COUPLING_ROTOR_MESH_PATH, STATUS_MATERIAL_PATH);
                var objective = annex.GetComponent<AuthoredCargoAnnexObjective>();
                if (objective == null)
                {
                    objective = annex.AddComponent<AuthoredCargoAnnexObjective>();
                }
                objective.Configure(commitmentAnchor, couplingSocket, withdrawalAnchor,
                    commitmentMarker.gameObject, withdrawalMarker.gameObject, securedMarker.gameObject);
                objective.ConfigureReadability(couplingRotor.GetComponent<Renderer>(), couplingRotor);
                PrefabUtility.SaveAsPrefabAsset(annex, ANNEX_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(annex);
            }
        }

        private static Transform _ensureReadabilityPart(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            string meshPath,
            string materialPath)
        {
            var part = parent.Find(objectName);
            if (part == null)
            {
                part = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer)).transform;
                part.SetParent(parent, false);
            }

            part.localPosition = localPosition;
            part.localRotation = Quaternion.identity;
            part.localScale = Vector3.one;
            part.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            part.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            foreach (var collider in part.GetComponents<Collider>())
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return part;
        }

        private static void _ensureBarrier(
            Transform parent,
            GameObject barrierPrefab,
            string objectName,
            Vector3 localPosition,
            float rotationY,
            Vector3 localScale)
        {
            var barrier = parent.Find(objectName);
            if (barrier == null)
            {
                var instance = PrefabUtility.InstantiatePrefab(barrierPrefab) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException($"Could not instantiate {objectName} for the salvage annex.");
                }

                instance.name = objectName;
                instance.transform.SetParent(parent, false);
                barrier = instance.transform;
            }

            barrier.localPosition = localPosition;
            barrier.localRotation = Quaternion.Euler(0f, rotationY, 0f);
            barrier.localScale = localScale;
        }

        private static Transform _ensureAnchor(Transform parent, string objectName, Vector3 localPosition)
        {
            var anchor = parent.Find(objectName);
            if (anchor == null)
            {
                anchor = new GameObject(objectName).transform;
                anchor.SetParent(parent, false);
            }

            anchor.localPosition = localPosition;
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one;
            return anchor;
        }

        private static Transform _ensureMarker(
            Transform parent,
            string objectName,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            string materialPath)
        {
            var marker = parent.Find(objectName);
            if (marker == null)
            {
                marker = GameObject.CreatePrimitive(primitiveType).transform;
                marker.name = objectName;
                marker.SetParent(parent, false);
            }

            marker.localPosition = localPosition;
            marker.localRotation = Quaternion.identity;
            marker.localScale = localScale;
            if (marker.TryGetComponent<Collider>(out var collider))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!marker.TryGetComponent<Renderer>(out var renderer) || material == null)
            {
                throw new InvalidOperationException($"Could not configure {objectName} with {materialPath}.");
            }

            renderer.sharedMaterial = material;
            return marker;
        }

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Northeast Salvage Annex");
            if (existing == null)
            {
                var annexPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ANNEX_PREFAB_PATH);
                existing = PrefabUtility.InstantiatePrefab(annexPrefab, scene) as GameObject;
                if (existing == null)
                {
                    throw new InvalidOperationException("Could not place the salvage annex in SampleScene.");
                }

                existing.name = "Northeast Salvage Annex";
                existing.transform.position = s_salvagePosition;
                EditorSceneManager.SaveScene(scene);
            }

            if (existing.GetComponentsInChildren<AuthoredMapObstacle>().Length != 3)
            {
                throw new InvalidOperationException("The SampleScene salvage annex does not contain three authored barriers.");
            }
        }

        private static void _assignMaterial(Transform root, string partName, string materialPath)
        {
            var part = root.Find(partName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (part == null || material == null || !part.TryGetComponent<Renderer>(out var renderer))
            {
                throw new InvalidOperationException($"Could not assign {materialPath} to {partName}.");
            }

            renderer.sharedMaterial = material;
        }

        private static bool _hasValidBarrier(GameObject barrier)
        {
            return barrier != null &&
                   barrier.GetComponent<AuthoredMapObstacle>() != null &&
                   barrier.transform.Find("Salvage Annex Armor") != null &&
                   barrier.transform.Find("Salvage Annex Hazard Rail") != null &&
                   barrier.transform.Find("Salvage Annex Conduit") != null;
        }

        private sealed class MeshBuilder
        {
            public MeshBuilder(string name)
            {
                m_name = name;
            }

            public void AddBox(Vector3 center, Vector3 size, float yaw)
            {
                var half = size * 0.5f;
                var rotation = Quaternion.Euler(0f, yaw, 0f);
                var corners = new[]
                {
                    new Vector3(-half.x, -half.y, -half.z), new Vector3(half.x, -half.y, -half.z),
                    new Vector3(half.x, half.y, -half.z), new Vector3(-half.x, half.y, -half.z),
                    new Vector3(-half.x, -half.y, half.z), new Vector3(half.x, -half.y, half.z),
                    new Vector3(half.x, half.y, half.z), new Vector3(-half.x, half.y, half.z)
                };
                for (var index = 0; index < corners.Length; index++)
                {
                    corners[index] = center + rotation * corners[index];
                }

                var start = m_vertices.Count;
                m_vertices.AddRange(corners);
                m_uvs.AddRange(new[]
                {
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up
                });
                m_triangles.AddRange(new[]
                {
                    start, start + 2, start + 1, start, start + 3, start + 2,
                    start + 4, start + 5, start + 6, start + 4, start + 6, start + 7,
                    start, start + 4, start + 7, start, start + 7, start + 3,
                    start + 1, start + 2, start + 6, start + 1, start + 6, start + 5,
                    start + 3, start + 7, start + 6, start + 3, start + 6, start + 2,
                    start, start + 1, start + 5, start, start + 5, start + 4
                });
            }

            public void AddPrism(Vector3 center, int sides, float bottomRadius, float topRadius, float height)
            {
                var start = m_vertices.Count;
                var halfHeight = height * 0.5f;
                for (var index = 0; index < sides; index++)
                {
                    var angle = index * Mathf.PI * 2f / sides;
                    var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                    m_vertices.Add(center + direction * bottomRadius + Vector3.down * halfHeight);
                    m_vertices.Add(center + direction * topRadius + Vector3.up * halfHeight);
                    m_uvs.Add(new Vector2(index / (float)sides, 0f));
                    m_uvs.Add(new Vector2(index / (float)sides, 1f));
                }

                var bottomCenter = m_vertices.Count;
                m_vertices.Add(center + Vector3.down * halfHeight);
                m_uvs.Add(new Vector2(0.5f, 0.5f));
                var topCenter = m_vertices.Count;
                m_vertices.Add(center + Vector3.up * halfHeight);
                m_uvs.Add(new Vector2(0.5f, 0.5f));
                for (var index = 0; index < sides; index++)
                {
                    var next = (index + 1) % sides;
                    m_triangles.AddRange(new[]
                    {
                        start + index * 2, start + next * 2 + 1, start + next * 2,
                        start + index * 2, start + index * 2 + 1, start + next * 2 + 1,
                        bottomCenter, start + next * 2, start + index * 2,
                        topCenter, start + index * 2 + 1, start + next * 2 + 1
                    });
                }
            }

            public Mesh Build()
            {
                var mesh = new Mesh { name = m_name };
                mesh.SetVertices(m_vertices);
                mesh.SetUVs(0, m_uvs);
                mesh.SetTriangles(m_triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                return mesh;
            }

            private readonly string m_name;
            private readonly List<Vector3> m_vertices = new();
            private readonly List<Vector2> m_uvs = new();
            private readonly List<int> m_triangles = new();
        }
    }
}
