using System;
using System.Collections.Generic;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalCentralReadabilitySetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string TOWER_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/SignalTowerAssembly.prefab";
        private const string VAULT_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/EastSalvageVault.prefab";
        private const string STATUS_TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/CentralMachineryStatusPanel.png";
        private const string BASE_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/CentralTowerBaseReadability.asset";
        private const string COLUMN_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/CentralTowerColumnReadability.asset";
        private const string CORE_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/CentralTowerCoreReadability.asset";
        private const string ASSEMBLER_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/TransferVaultAssemblerReadability.asset";
        private const string MATERIAL_FOLDER = "Assets/DeadSignal/Resources/Materials/CentralReadability";
        private const string STATUS_MATERIAL_PATH = MATERIAL_FOLDER + "/CentralMachineryStatus.mat";
        private const string TOWER_HOUSING_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SignalTowerHousing.mat";
        private const string VAULT_ARMOR_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/EastVaultArmor.mat";

        public static bool HasAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(STATUS_TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Mesh>(BASE_MESH_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Mesh>(COLUMN_MESH_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Mesh>(CORE_MESH_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Mesh>(ASSEMBLER_MESH_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Material>(STATUS_MATERIAL_PATH) != null;

        [MenuItem("DEAD SIGNAL/Setup/Central Machinery Readability")]
        public static void EnsureAssets()
        {
            _configureTexture();
            _ensureFolder();
            var statusMaterial = _ensureStatusMaterial();
            _ensureMesh(BASE_MESH_PATH, _buildTowerBase());
            _ensureMesh(COLUMN_MESH_PATH, _buildTowerColumn());
            _ensureMesh(CORE_MESH_PATH, _buildTowerCore());
            _ensureMesh(ASSEMBLER_MESH_PATH, _buildTransferAssembler());
            _upgradeTowerPrefab(statusMaterial);
            _upgradeTransferVault(statusMaterial);
            _bindSceneReadability();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The Central machinery readability assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(STATUS_TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Central machinery texture at {STATUS_TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void _ensureFolder()
        {
            if (!AssetDatabase.IsValidFolder(MATERIAL_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "CentralReadability");
            }
        }

        private static Material _ensureStatusMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(STATUS_MATERIAL_PATH);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    name = "CentralMachineryStatus"
                };
                AssetDatabase.CreateAsset(material, STATUS_MATERIAL_PATH);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(STATUS_TEXTURE_PATH);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_EmissionColor", new Color(0.02f, 0.75f, 0.9f));
            material.SetFloat("_Metallic", 0.35f);
            material.SetFloat("_Smoothness", 0.42f);
            material.EnableKeyword("_EMISSION");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void _ensureMesh(string path, Mesh generated)
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

        private static void _upgradeTowerPrefab(Material statusMaterial)
        {
            var root = PrefabUtility.LoadPrefabContents(TOWER_PREFAB_PATH);
            try
            {
                var housing = AssetDatabase.LoadAssetAtPath<Material>(TOWER_HOUSING_MATERIAL_PATH);
                _assignMesh(root.transform, "Tower Base", BASE_MESH_PATH, housing);
                _assignMesh(root.transform, "Tower Column", COLUMN_MESH_PATH, housing);
                _assignMesh(root.transform, "Tower Core", CORE_MESH_PATH, statusMaterial);
                PrefabUtility.SaveAsPrefabAsset(root, TOWER_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _upgradeTransferVault(Material statusMaterial)
        {
            var root = PrefabUtility.LoadPrefabContents(VAULT_PREFAB_PATH);
            try
            {
                var rotor = root.transform.Find("Transfer Assembler Rotor")?.gameObject;
                if (rotor == null)
                {
                    rotor = new GameObject("Transfer Assembler Rotor");
                    rotor.transform.SetParent(root.transform, false);
                    rotor.AddComponent<MeshFilter>();
                    rotor.AddComponent<MeshRenderer>();
                }

                rotor.transform.localPosition = new Vector3(0.65f, 0.16f, 0f);
                rotor.transform.localRotation = Quaternion.identity;
                rotor.transform.localScale = Vector3.one;
                rotor.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(ASSEMBLER_MESH_PATH);
                rotor.GetComponent<MeshRenderer>().sharedMaterials = new[]
                {
                    AssetDatabase.LoadAssetAtPath<Material>(VAULT_ARMOR_MATERIAL_PATH),
                    statusMaterial
                };
                root.GetComponent<AuthoredTransferVaultObjective>().ConfigureReadability(
                    rotor.GetComponent<MeshRenderer>(), rotor.transform);
                PrefabUtility.SaveAsPrefabAsset(root, VAULT_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _bindSceneReadability()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var references = UnityEngine.Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var installation = UnityEngine.Object.FindFirstObjectByType<AuthoredCentralInstallationObjective>();
            if (references == null || installation == null)
            {
                throw new InvalidOperationException("Central readability requires scene references and installation markers.");
            }

            var tower = references.SignalTower;
            var readability = tower.GetComponent<AuthoredCentralTowerReadability>() ??
                              tower.AddComponent<AuthoredCentralTowerReadability>();
            var socketRenderers = installation.GetComponentsInChildren<Renderer>(true);
            var statusRenderer = tower.transform.Find("Tower Core").GetComponent<Renderer>();
            statusRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(STATUS_MATERIAL_PATH);
            readability.Configure(statusRenderer, socketRenderers);
            EditorUtility.SetDirty(tower);
            EditorUtility.SetDirty(installation);
            EditorSceneManager.SaveScene(scene);
        }

        private static void _assignMesh(Transform root, string childName, string meshPath, Material material)
        {
            var child = root.Find(childName);
            if (child == null)
            {
                throw new InvalidOperationException($"The Signal Tower prefab is missing {childName}.");
            }

            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
            child.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            child.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Mesh _buildTowerBase()
        {
            var builder = new MeshBuilder("CentralTowerBaseReadability");
            builder.AddPrism(Vector3.zero, 12, 1.18f, 1.02f, 0.32f, 0);
            for (var index = 0; index < 4; index++)
            {
                var angle = index * 90f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                builder.AddBox(direction * 1.16f + Vector3.up * 0.08f, new Vector3(0.42f, 0.16f, 0.62f), angle, 0);
            }

            return builder.Build();
        }

        private static Mesh _buildTowerColumn()
        {
            var builder = new MeshBuilder("CentralTowerColumnReadability");
            builder.AddPrism(new Vector3(0f, 0.78f, 0f), 8, 0.52f, 0.38f, 1.25f, 0);
            for (var index = 0; index < 4; index++)
            {
                var angle = index * 90f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                builder.AddBox(direction * 0.48f + Vector3.up * 0.72f, new Vector3(0.16f, 0.82f, 0.16f), angle, 0);
            }

            return builder.Build();
        }

        private static Mesh _buildTowerCore()
        {
            var builder = new MeshBuilder("CentralTowerCoreReadability");
            builder.AddPrism(new Vector3(0f, 1.55f, 0f), 12, 0.76f, 0.76f, 0.24f, 0);
            for (var index = 0; index < 4; index++)
            {
                var angle = index * 90f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                builder.AddBox(direction * 0.78f + Vector3.up * 1.55f, new Vector3(0.28f, 0.12f, 0.38f), angle, 0);
            }

            return builder.Build();
        }

        private static Mesh _buildTransferAssembler()
        {
            var builder = new MeshBuilder("TransferVaultAssemblerReadability", 2);
            builder.AddBox(Vector3.zero, new Vector3(1.55f, 0.2f, 1.85f), 0f, 0);
            builder.AddBox(new Vector3(-0.58f, 0.2f, 0f), new Vector3(0.18f, 0.28f, 1.55f), 0f, 0);
            builder.AddBox(new Vector3(0.58f, 0.2f, 0f), new Vector3(0.18f, 0.28f, 1.55f), 0f, 0);
            builder.AddPrism(new Vector3(0f, 0.28f, 0f), 8, 0.46f, 0.38f, 0.18f, 1);
            return builder.Build();
        }

        private sealed class MeshBuilder
        {
            public MeshBuilder(string name, int subMeshCount = 1)
            {
                m_name = name;
                for (var index = 0; index < subMeshCount; index++)
                {
                    m_triangles.Add(new List<int>());
                }
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

                var faces = new[]
                {
                    0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7,
                    0, 4, 7, 0, 7, 3, 1, 2, 6, 1, 6, 5,
                    3, 7, 6, 3, 6, 2, 0, 1, 5, 0, 5, 4
                };
                var start = m_vertices.Count;
                m_vertices.AddRange(corners);
                m_uvs.AddRange(new[]
                {
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up
                });
                foreach (var index in faces)
                {
                    m_triangles[subMesh].Add(start + index);
                }
            }

            public void AddPrism(Vector3 center, int sides, float bottomRadius, float topRadius, float height, int subMesh)
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
                    m_triangles[subMesh].AddRange(new[]
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
                mesh.subMeshCount = m_triangles.Count;
                for (var index = 0; index < m_triangles.Count; index++)
                {
                    mesh.SetTriangles(m_triangles[index], index);
                }

                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                return mesh;
            }

            private readonly string m_name;
            private readonly List<Vector3> m_vertices = new();
            private readonly List<Vector2> m_uvs = new();
            private readonly List<List<int>> m_triangles = new();
        }
    }
}
