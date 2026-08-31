using System;
using System.Collections.Generic;
using System.Linq;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalFurnaceQuenchHeroSetup
    {
        private const string FURNACE_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ArcFurnaceRegion.prefab";
        private const string QUENCH_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/QuenchLoopRegion.prefab";
        private const string TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/FurnaceQuenchHeroAtlas.png";
        private const string FURNACE_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/ArcFurnaceHeroFinish.asset";
        private const string QUENCH_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/QuenchLoopHeroFinish.asset";
        private const string MATERIAL_FOLDER =
            "Assets/DeadSignal/Resources/Materials/FurnaceQuenchHeroFinish";

        private static readonly string[] s_materialPaths =
        {
            MATERIAL_FOLDER + "/FurnaceQuenchAlloy.mat",
            MATERIAL_FOLDER + "/FurnaceQuenchCeramic.mat",
            MATERIAL_FOLDER + "/FurnaceQuenchCoolant.mat",
            MATERIAL_FOLDER + "/FurnaceQuenchDeck.mat"
        };

        public static bool HasAssets
        {
            get
            {
                var furnace = AssetDatabase.LoadAssetAtPath<GameObject>(FURNACE_PREFAB_PATH);
                var quench = AssetDatabase.LoadAssetAtPath<GameObject>(QUENCH_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(FURNACE_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(QUENCH_MESH_PATH) != null &&
                       s_materialPaths.All(path => AssetDatabase.LoadAssetAtPath<Material>(path) != null) &&
                       _hasFinish(furnace, "Arc Furnace Hero Finish") &&
                       _hasFinish(quench, "Quench Loop Hero Finish");
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Arc Furnace and Quench Hero Finish")]
        public static void EnsureAssets()
        {
            _configureTexture();
            _ensureMaterialFolder();
            var materials = _ensureMaterials();
            _saveOrReplaceMesh(FURNACE_MESH_PATH, _buildFurnaceMesh());
            _saveOrReplaceMesh(QUENCH_MESH_PATH, _buildQuenchMesh());
            _upgradeFurnace(materials);
            _upgradeQuench(materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Arc Furnace and Quench hero-finish assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("The Furnace/Quench hero atlas is missing.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
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
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "FurnaceQuenchHeroFinish");
            }
        }

        private static Material[] _ensureMaterials()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            return new[]
            {
                _ensureMaterial(s_materialPaths[0], "FurnaceQuenchAlloy", texture,
                    Vector2.up * 0.5f, 0.79f, 0.28f, new Color(0.43f, 0.4f, 0.37f)),
                _ensureMaterial(s_materialPaths[1], "FurnaceQuenchCeramic", texture,
                    new Vector2(0.5f, 0.5f), 0.03f, 0.35f, new Color(0.9f, 0.86f, 0.78f)),
                _ensureMaterial(s_materialPaths[2], "FurnaceQuenchCoolant", texture,
                    Vector2.zero, 0.72f, 0.32f, new Color(0.53f, 0.43f, 0.35f)),
                _ensureMaterial(s_materialPaths[3], "FurnaceQuenchDeck", texture,
                    Vector2.right * 0.5f, 0.45f, 0.22f, new Color(0.34f, 0.38f, 0.42f))
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
                    throw new InvalidOperationException("Could not find the URP Lit shader for the Furnace/Quench finish.");
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

        private static Mesh _buildFurnaceMesh()
        {
            var mesh = new MeshBuilder("ArcFurnaceHeroFinish", 4);

            // Broken radial heat shields frame forging without altering the central projectile-authoritative landmark.
            for (var index = 0; index < 8; index++)
            {
                var angle = index * 45f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                mesh.AddBox(direction * 2.25f + Vector3.up * 0.045f,
                    new Vector3(0.75f, 0.1f, 0.16f), angle, index % 2 == 0 ? 1 : 0);
            }

            mesh.AddBox(new Vector3(-4.8f, 0.04f, -3.25f), new Vector3(3.1f, 0.1f, 0.14f), 0f, 0);
            mesh.AddBox(new Vector3(-4.8f, 0.04f, 3.25f), new Vector3(3.1f, 0.1f, 0.14f), 0f, 0);
            mesh.AddBox(new Vector3(4.8f, 0.035f, -3.25f), new Vector3(3.1f, 0.09f, 0.13f), 0f, 2);
            mesh.AddBox(new Vector3(4.8f, 0.035f, 3.25f), new Vector3(3.1f, 0.09f, 0.13f), 0f, 2);
            mesh.AddBox(new Vector3(0f, -0.105f, -4.28f), new Vector3(10.6f, 0.08f, 0.18f), 0f, 3);
            mesh.AddBox(new Vector3(0f, -0.105f, 4.28f), new Vector3(10.6f, 0.08f, 0.18f), 0f, 3);
            return mesh.Build();
        }

        private static Mesh _buildQuenchMesh()
        {
            var mesh = new MeshBuilder("QuenchLoopHeroFinish", 4);

            // Parallel coolant loops and drains distinguish stabilization while leaving both flanks and shutter clear.
            mesh.AddBox(new Vector3(2.65f, 0.035f, 0f), new Vector3(0.14f, 0.1f, 6.8f), 0f, 2);
            mesh.AddBox(new Vector3(2.25f, 0.035f, 0f), new Vector3(0.14f, 0.1f, 5.9f), 0f, 2);
            mesh.AddBox(new Vector3(-2.75f, 0.04f, -2.95f), new Vector3(0.15f, 0.1f, 1.9f), 0f, 1);
            mesh.AddBox(new Vector3(-2.75f, 0.04f, 2.95f), new Vector3(0.15f, 0.1f, 1.9f), 0f, 1);
            mesh.AddBox(new Vector3(0.5f, 0.035f, -3.55f), new Vector3(4.3f, 0.09f, 0.14f), 0f, 2);
            mesh.AddBox(new Vector3(0.5f, 0.035f, 3.55f), new Vector3(4.3f, 0.09f, 0.14f), 0f, 2);
            mesh.AddBox(new Vector3(0f, -0.105f, -4.28f), new Vector3(5.7f, 0.08f, 0.18f), 0f, 3);
            mesh.AddBox(new Vector3(0f, -0.105f, 4.28f), new Vector3(5.7f, 0.08f, 0.18f), 0f, 3);
            mesh.AddBox(new Vector3(3.25f, -0.105f, 0f), new Vector3(0.18f, 0.08f, 7.2f), 0f, 0);
            return mesh.Build();
        }

        private static void _upgradeFurnace(Material[] materials)
        {
            var root = PrefabUtility.LoadPrefabContents(FURNACE_PREFAB_PATH);
            try
            {
                _assignMaterial(root.transform, "Arc Furnace Deck", materials[3]);
                _assignMaterial(root.transform, "West Furnace Shield South", materials[1]);
                _assignMaterial(root.transform, "West Furnace Shield North", materials[1]);
                _assignLandmarkMaterials(root.transform, "Arc Furnace Assembly", materials);
                _ensureFinish(root, "Arc Furnace Hero Finish", FURNACE_MESH_PATH, materials);
                PrefabUtility.SaveAsPrefabAsset(root, FURNACE_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _upgradeQuench(Material[] materials)
        {
            var root = PrefabUtility.LoadPrefabContents(QUENCH_PREFAB_PATH);
            try
            {
                _assignMaterial(root.transform, "Quench Loop Deck", materials[3]);
                _assignMaterial(root.transform, "South Quench Deflector", materials[1]);
                _assignMaterial(root.transform, "North Quench Deflector", materials[1]);
                _assignLandmarkMaterials(root.transform, "Quench Condenser Assembly", materials);
                _ensureFinish(root, "Quench Loop Hero Finish", QUENCH_MESH_PATH, materials);
                PrefabUtility.SaveAsPrefabAsset(root, QUENCH_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _ensureFinish(GameObject root, string objectName, string meshPath, Material[] materials)
        {
            var part = root.transform.Find(objectName);
            if (part == null)
            {
                part = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer)).transform;
                part.SetParent(root.transform, false);
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

            var finish = root.GetComponent<AuthoredFurnaceQuenchHeroFinish>() ??
                         root.AddComponent<AuthoredFurnaceQuenchHeroFinish>();
            finish.Configure(renderer);
        }

        private static void _assignMaterial(Transform root, string childPath, Material material)
        {
            var child = root.Find(childPath);
            if (child == null || material == null || !child.TryGetComponent<Renderer>(out var renderer))
            {
                throw new InvalidOperationException($"Could not finish the Furnace/Quench renderer {childPath}.");
            }

            renderer.sharedMaterial = material;
        }

        private static void _assignLandmarkMaterials(Transform root, string childPath, Material[] materials)
        {
            var child = root.Find(childPath);
            var renderers = child != null ? child.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException($"Could not finish the Furnace/Quench landmark {childPath}.");
            }

            foreach (var renderer in renderers)
            {
                var name = renderer.gameObject.name;
                renderer.sharedMaterial = name.Contains("Ceramic", StringComparison.OrdinalIgnoreCase) ||
                                          name.Contains("Shield", StringComparison.OrdinalIgnoreCase)
                    ? materials[1]
                    : name.Contains("Coolant", StringComparison.OrdinalIgnoreCase) ||
                      name.Contains("Condenser", StringComparison.OrdinalIgnoreCase) ||
                      name.Contains("Coil", StringComparison.OrdinalIgnoreCase)
                        ? materials[2]
                        : materials[0];
            }
        }

        private static bool _hasFinish(GameObject prefab, string objectName)
        {
            var finish = prefab != null ? prefab.GetComponent<AuthoredFurnaceQuenchHeroFinish>() : null;
            return finish != null && finish.IsConfigured && finish.transform.Find(objectName) != null &&
                   finish.FinishRenderer.sharedMaterials.Length == 4 &&
                   finish.FinishRenderer.GetComponents<Collider>().Length == 0;
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
