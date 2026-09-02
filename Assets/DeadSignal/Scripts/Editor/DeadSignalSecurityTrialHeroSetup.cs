using System;
using System.Collections.Generic;
using System.Linq;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalSecurityTrialHeroSetup
    {
        private const string WING_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SecurityTrialWingRegion.prefab";
        private const string FURNACE_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ArcFurnaceRegion.prefab";
        private const string CHAMBER_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceChamberRegion.prefab";
        private const string GALLERY_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineInductionGalleryRegion.prefab";
        private const string TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/SecurityTrialHeroAtlas.png";
        private const string COMMITMENT_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/SecurityTrialCommitmentHeroFinish.asset";
        private const string LOCKDOWN_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/SecurityTrialLockdownHeroFinish.asset";
        private const string VAULT_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/SecurityTrialVaultHeroFinish.asset";
        private const string MATERIAL_FOLDER =
            "Assets/DeadSignal/Resources/Materials/SecurityTrialHeroFinish";

        private static readonly string[] s_materialPaths =
        {
            MATERIAL_FOLDER + "/SecurityTrialAlloy.mat",
            MATERIAL_FOLDER + "/SecurityTrialCeramic.mat",
            MATERIAL_FOLDER + "/SecurityTrialLockdown.mat",
            MATERIAL_FOLDER + "/SecurityTrialCapacitor.mat"
        };

        public static bool HasAssets
        {
            get
            {
                var wing = AssetDatabase.LoadAssetAtPath<GameObject>(WING_PREFAB_PATH);
                var finish = wing != null ? wing.GetComponent<AuthoredSecurityTrialHeroFinish>() : null;
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(COMMITMENT_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(LOCKDOWN_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(VAULT_MESH_PATH) != null &&
                       s_materialPaths.All(path => AssetDatabase.LoadAssetAtPath<Material>(path) != null) &&
                       finish != null && finish.IsConfigured &&
                       finish.GetComponentsInChildren<Collider>(true).Length == 0;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Security Trial Hero Finish")]
        public static void EnsureAssets()
        {
            _configureTexture();
            _ensureMaterialFolder();
            var materials = _ensureMaterials();
            _saveOrReplaceMesh(COMMITMENT_MESH_PATH, _buildCommitmentMesh());
            _saveOrReplaceMesh(LOCKDOWN_MESH_PATH, _buildLockdownMesh());
            _saveOrReplaceMesh(VAULT_MESH_PATH, _buildVaultMesh());
            _upgradeWing(materials);
            _refreshHierarchy();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Security Trial hero-finish assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("The Security Trial hero atlas is missing.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void _ensureMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder(MATERIAL_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "SecurityTrialHeroFinish");
            }
        }

        private static Material[] _ensureMaterials()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            return new[]
            {
                _ensureMaterial(s_materialPaths[0], "SecurityTrialAlloy", texture,
                    Vector2.up * 0.5f, 0.82f, 0.24f, new Color(0.48f, 0.46f, 0.44f)),
                _ensureMaterial(s_materialPaths[1], "SecurityTrialCeramic", texture,
                    Vector2.one * 0.5f, 0.04f, 0.32f, new Color(0.88f, 0.84f, 0.79f)),
                _ensureMaterial(s_materialPaths[2], "SecurityTrialLockdown", texture,
                    Vector2.zero, 0.61f, 0.28f, new Color(0.58f, 0.38f, 0.35f)),
                _ensureMaterial(s_materialPaths[3], "SecurityTrialCapacitor", texture,
                    Vector2.right * 0.5f, 0.71f, 0.31f, new Color(0.58f, 0.49f, 0.39f))
            };
        }

        private static Material _ensureMaterial(
            string path,
            string materialName,
            Texture texture,
            Vector2 offset,
            float metallic,
            float smoothness,
            Color baseColor)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for the Security Trial finish.");
                }

                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", texture);
            material.SetTextureOffset("_BaseMap", offset);
            material.SetTextureScale("_BaseMap", Vector2.one * 0.5f);
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_EmissionColor", Color.black);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.DisableKeyword("_EMISSION");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh _buildCommitmentMesh()
        {
            var mesh = new MeshBuilder("SecurityTrialCommitmentHeroFinish", 4);

            // A compact containment collar and converging floor feeds frame commitment without implying another pickup.
            mesh.AddBox(new Vector3(-0.88f, 0.12f, 0f), new Vector3(0.16f, 0.24f, 1.55f), 0f, 0);
            mesh.AddBox(new Vector3(0.88f, 0.12f, 0f), new Vector3(0.16f, 0.24f, 1.55f), 0f, 0);
            mesh.AddBox(new Vector3(0f, 0.13f, -0.72f), new Vector3(1.6f, 0.26f, 0.16f), 0f, 2);
            mesh.AddBox(new Vector3(0f, 0.13f, 0.72f), new Vector3(1.6f, 0.26f, 0.16f), 0f, 2);
            mesh.AddBox(new Vector3(-2.45f, 0.03f, -2.25f), new Vector3(2.1f, 0.08f, 0.14f), 18f, 2);
            mesh.AddBox(new Vector3(2.45f, 0.03f, -2.25f), new Vector3(2.1f, 0.08f, 0.14f), -18f, 2);
            mesh.AddBox(new Vector3(-3.72f, 0.62f, 0f), new Vector3(0.14f, 0.28f, 4.8f), 0f, 1);
            mesh.AddBox(new Vector3(3.72f, 0.62f, 0f), new Vector3(0.14f, 0.28f, 4.8f), 0f, 1);
            return mesh.Build();
        }

        private static Mesh _buildLockdownMesh()
        {
            var mesh = new MeshBuilder("SecurityTrialLockdownHeroFinish", 4);

            // Low circuit frames preserve the central firing volume while making the phase landmark unmistakable.
            for (var index = 0; index < 8; index++)
            {
                var angle = index * 45f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                mesh.AddBox(direction * 3.8f + Vector3.up * 0.035f,
                    new Vector3(2.2f, 0.08f, 0.14f), angle, index % 2 == 0 ? 2 : 0);
            }

            mesh.AddBox(new Vector3(-28.3f, 0.04f, 0f), new Vector3(0.16f, 0.09f, 30f), 0f, 2);
            mesh.AddBox(new Vector3(28.3f, 0.04f, 0f), new Vector3(0.16f, 0.09f, 30f), 0f, 2);
            mesh.AddBox(new Vector3(-15.75f, 0.04f, -16.25f), new Vector3(24.8f, 0.09f, 0.16f), 0f, 0);
            mesh.AddBox(new Vector3(15.75f, 0.04f, -16.25f), new Vector3(24.8f, 0.09f, 0.16f), 0f, 0);
            mesh.AddBox(new Vector3(-15.75f, 0.04f, 16.25f), new Vector3(24.8f, 0.09f, 0.16f), 0f, 0);
            mesh.AddBox(new Vector3(15.75f, 0.04f, 16.25f), new Vector3(24.8f, 0.09f, 0.16f), 0f, 0);
            mesh.AddBox(new Vector3(-12f, 0.12f, 6f), new Vector3(4.1f, 0.22f, 0.12f), 28f, 1);
            mesh.AddBox(new Vector3(12f, 0.12f, -6f), new Vector3(4.1f, 0.22f, 0.12f), -28f, 1);
            return mesh.Build();
        }

        private static Mesh _buildVaultMesh()
        {
            var mesh = new MeshBuilder("SecurityTrialVaultHeroFinish", 4);

            // A four-sided conductor cradle and rear bus make recovery read as secured hardware, not another switch.
            for (var index = 0; index < 4; index++)
            {
                var angle = index * 90f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                mesh.AddBox(direction * 1.25f + Vector3.up * 0.09f,
                    new Vector3(0.78f, 0.18f, 0.18f), angle, 3);
            }

            mesh.AddBox(new Vector3(-2.6f, 0.04f, 1.9f), new Vector3(2f, 0.09f, 0.14f), 0f, 3);
            mesh.AddBox(new Vector3(2.6f, 0.04f, 1.9f), new Vector3(2f, 0.09f, 0.14f), 0f, 3);
            mesh.AddBox(new Vector3(-3.72f, 0.62f, 0f), new Vector3(0.14f, 0.28f, 4.8f), 0f, 1);
            mesh.AddBox(new Vector3(3.72f, 0.62f, 0f), new Vector3(0.14f, 0.28f, 4.8f), 0f, 1);
            mesh.AddBox(new Vector3(0f, 0.55f, 2.72f), new Vector3(6.8f, 0.22f, 0.14f), 0f, 0);
            return mesh.Build();
        }

        private static void _upgradeWing(Material[] materials)
        {
            var root = PrefabUtility.LoadPrefabContents(WING_PREFAB_PATH);
            try
            {
                _assignMaterial(root.transform, "Commitment Room/Commitment Deck", materials[0]);
                _assignMaterial(root.transform, "Commitment Room/Commitment West Bulkhead", materials[0]);
                _assignMaterial(root.transform, "Commitment Room/Commitment East Bulkhead", materials[0]);
                _assignMaterial(root.transform, "Lockdown Arena/Arena Deck", materials[0]);
                _assignMaterial(root.transform, "Lockdown Arena/Arena West Deflector", materials[1]);
                _assignMaterial(root.transform, "Lockdown Arena/Arena East Deflector", materials[1]);
                _assignMaterial(root.transform, "Lockdown Arena/Arena Circuit Spine", materials[2]);
                _assignMaterial(root.transform, "Reward Vault/Vault Deck", materials[3]);
                _assignMaterial(root.transform, "Reward Vault/Vault West Bulkhead", materials[0]);
                _assignMaterial(root.transform, "Reward Vault/Vault East Bulkhead", materials[0]);
                _assignMaterial(root.transform, "Reward Vault/Vault North Bulkhead", materials[0]);

                var commitment = _ensureFinish(root.transform.Find("Commitment Room"),
                    "Commitment Hero Finish", COMMITMENT_MESH_PATH, materials);
                var lockdown = _ensureFinish(root.transform.Find("Lockdown Arena"),
                    "Lockdown Hero Finish", LOCKDOWN_MESH_PATH, materials);
                var vault = _ensureFinish(root.transform.Find("Reward Vault"),
                    "Reward Vault Hero Finish", VAULT_MESH_PATH, materials);
                var finish = root.GetComponent<AuthoredSecurityTrialHeroFinish>() ??
                             root.AddComponent<AuthoredSecurityTrialHeroFinish>();
                finish.Configure(commitment, lockdown, vault);
                PrefabUtility.SaveAsPrefabAsset(root, WING_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static MeshRenderer _ensureFinish(
            Transform parent,
            string objectName,
            string meshPath,
            Material[] materials)
        {
            if (parent == null)
            {
                throw new InvalidOperationException($"Could not find the Security Trial owner for {objectName}.");
            }

            var part = parent.Find(objectName);
            if (part == null)
            {
                part = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer)).transform;
                part.SetParent(parent, false);
            }

            part.localPosition = Vector3.zero;
            part.localRotation = Quaternion.identity;
            part.localScale = Vector3.one;
            part.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            foreach (var collider in part.GetComponents<Collider>())
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return renderer;
        }

        private static void _assignMaterial(Transform root, string childPath, Material material)
        {
            var child = root.Find(childPath);
            if (child == null || !child.TryGetComponent<Renderer>(out var renderer))
            {
                throw new InvalidOperationException($"Could not finish the Security Trial renderer {childPath}.");
            }

            renderer.sharedMaterial = material;
        }

        private static void _refreshHierarchy()
        {
            _replacePrefabChild(FURNACE_PREFAB_PATH, "Security Trial Wing Region", WING_PREFAB_PATH,
                new Vector3(0f, 0f, 7.5f));
            _replacePrefabChild(CHAMBER_PREFAB_PATH, "Arc Furnace Region", FURNACE_PREFAB_PATH,
                new Vector3(0f, 0f, 8.5f));
            _replacePrefabChild(GALLERY_PREFAB_PATH, "Convergence Chamber Region", CHAMBER_PREFAB_PATH,
                new Vector3(0f, 0f, 8.5f));
        }

        private static void _replacePrefabChild(
            string parentPath,
            string childName,
            string childPath,
            Vector3 localPosition)
        {
            var parent = PrefabUtility.LoadPrefabContents(parentPath);
            try
            {
                var existing = parent.transform.Find(childName);
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(childPath);
                var child = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
                child.name = childName;
                child.transform.localPosition = localPosition;
                PrefabUtility.SaveAsPrefabAsset(parent, parentPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(parent);
            }
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

        private sealed class MeshBuilder
        {
            public MeshBuilder(string name, int subMeshCount)
            {
                m_name = name;
                m_triangles = Enumerable.Range(0, subMeshCount).Select(_ => new List<int>()).ToList();
            }

            public void AddBox(Vector3 center, Vector3 size, float yaw, int subMesh)
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
                m_triangles[subMesh].AddRange(new[]
                {
                    start, start + 2, start + 1, start, start + 3, start + 2,
                    start + 4, start + 5, start + 6, start + 4, start + 6, start + 7,
                    start, start + 4, start + 7, start, start + 7, start + 3,
                    start + 1, start + 2, start + 6, start + 1, start + 6, start + 5,
                    start + 3, start + 7, start + 6, start + 3, start + 6, start + 2,
                    start, start + 1, start + 5, start, start + 5, start + 4
                });
            }

            public Mesh Build()
            {
                var mesh = new Mesh { name = m_name };
                mesh.SetVertices(m_vertices);
                mesh.SetUVs(0, m_uvs);
                mesh.subMeshCount = m_triangles.Count;
                for (var index = 0; index < m_triangles.Count; index++)
                {
                    mesh.SetTriangles(m_triangles[index], index);
                }

                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }

            private readonly string m_name;
            private readonly List<Vector3> m_vertices = new();
            private readonly List<Vector2> m_uvs = new();
            private readonly List<List<int>> m_triangles;
        }
    }
}
