using System;
using System.Collections.Generic;
using System.Linq;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalConvergenceBreakerHeroSetup
    {
        private const string CONVERGENCE_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceChamberRegion.prefab";
        private const string BREAKER_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceBreakerGalleryRegion.prefab";
        private const string TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceBreakerHeroAtlas.png";
        private const string CONVERGENCE_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceChamberHeroFinish.asset";
        private const string BREAKER_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/BreakerGalleryHeroFinish.asset";
        private const string MATERIAL_FOLDER =
            "Assets/DeadSignal/Resources/Materials/ConvergenceBreakerHeroFinish";

        private static readonly string[] s_materialPaths =
        {
            MATERIAL_FOLDER + "/ConvergenceBreakerAlloy.mat",
            MATERIAL_FOLDER + "/ConvergenceBreakerCeramic.mat",
            MATERIAL_FOLDER + "/ConvergenceBreakerConductor.mat",
            MATERIAL_FOLDER + "/ConvergenceBreakerDeck.mat"
        };

        public static bool HasAssets
        {
            get
            {
                var convergence = AssetDatabase.LoadAssetAtPath<GameObject>(CONVERGENCE_PREFAB_PATH);
                var breaker = AssetDatabase.LoadAssetAtPath<GameObject>(BREAKER_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(CONVERGENCE_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(BREAKER_MESH_PATH) != null &&
                       s_materialPaths.All(path => AssetDatabase.LoadAssetAtPath<Material>(path) != null) &&
                       _hasFinish(convergence, "Convergence Chamber Hero Finish") &&
                       _hasFinish(breaker, "Breaker Gallery Hero Finish");
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Convergence and Breaker Hero Finish")]
        public static void EnsureAssets()
        {
            _configureTexture();
            _ensureMaterialFolder();
            var materials = _ensureMaterials();
            _saveOrReplaceMesh(CONVERGENCE_MESH_PATH, _buildConvergenceMesh());
            _saveOrReplaceMesh(BREAKER_MESH_PATH, _buildBreakerMesh());
            _upgradeConvergence(materials);
            _upgradeBreaker(materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Convergence and Breaker hero-finish assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("The Convergence/Breaker hero atlas is missing.");
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
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "ConvergenceBreakerHeroFinish");
            }
        }

        private static Material[] _ensureMaterials()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            return new[]
            {
                _ensureMaterial(s_materialPaths[0], "ConvergenceBreakerAlloy", texture,
                    Vector2.up * 0.5f, 0.76f, 0.31f, new Color(0.46f, 0.48f, 0.5f)),
                _ensureMaterial(s_materialPaths[1], "ConvergenceBreakerCeramic", texture,
                    new Vector2(0.5f, 0.5f), 0.04f, 0.39f, new Color(0.91f, 0.88f, 0.8f)),
                _ensureMaterial(s_materialPaths[2], "ConvergenceBreakerConductor", texture,
                    Vector2.zero, 0.82f, 0.35f, new Color(0.69f, 0.4f, 0.22f)),
                _ensureMaterial(s_materialPaths[3], "ConvergenceBreakerDeck", texture,
                    Vector2.right * 0.5f, 0.48f, 0.24f, new Color(0.4f, 0.43f, 0.46f))
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
                    throw new InvalidOperationException("Could not find the URP Lit shader for the Convergence/Breaker finish.");
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

        private static Mesh _buildConvergenceMesh()
        {
            var mesh = new MeshBuilder("ConvergenceChamberHeroFinish", 4);

            // A broken calibration aperture frames the holdout without narrowing its playable volume.
            for (var index = 0; index < 12; index++)
            {
                var angle = index * 30f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                mesh.AddBox(direction * 2.15f + Vector3.up * 0.045f,
                    new Vector3(0.78f, 0.1f, 0.15f), angle, index % 3 == 0 ? 2 : 0);
            }

            mesh.AddBox(new Vector3(-5.05f, 0.06f, -2.8f), new Vector3(3.1f, 0.14f, 0.18f), 0f, 1);
            mesh.AddBox(new Vector3(5.05f, 0.06f, -2.8f), new Vector3(3.1f, 0.14f, 0.18f), 0f, 1);
            mesh.AddBox(new Vector3(-5.05f, 0.035f, 2.9f), new Vector3(0.14f, 0.1f, 1.7f), 0f, 2);
            mesh.AddBox(new Vector3(5.05f, 0.035f, 2.9f), new Vector3(0.14f, 0.1f, 1.7f), 0f, 2);
            mesh.AddBox(new Vector3(0f, -0.105f, 3.55f), new Vector3(11.8f, 0.08f, 0.18f), 0f, 3);
            mesh.AddBox(new Vector3(-6.55f, -0.105f, 0f), new Vector3(0.18f, 0.08f, 6.8f), 0f, 0);
            return mesh.Build();
        }

        private static Mesh _buildBreakerMesh()
        {
            var mesh = new MeshBuilder("BreakerGalleryHeroFinish", 4);

            // Parallel branches make distribution legible while leaving both west thresholds and the outer loop open.
            mesh.AddBox(new Vector3(-1.25f, 0.035f, 0f), new Vector3(0.14f, 0.1f, 6.1f), 0f, 2);
            mesh.AddBox(new Vector3(-0.75f, 0.035f, 0f), new Vector3(0.14f, 0.1f, 5.2f), 0f, 2);
            mesh.AddBox(new Vector3(0.2f, 0.055f, -2.65f), new Vector3(1.9f, 0.13f, 0.16f), -20f, 1);
            mesh.AddBox(new Vector3(0.2f, 0.055f, 2.65f), new Vector3(1.9f, 0.13f, 0.16f), 20f, 1);
            mesh.AddBox(new Vector3(2.65f, 0.035f, 0f), new Vector3(0.14f, 0.1f, 5.6f), 0f, 2);
            mesh.AddBox(new Vector3(1.7f, 0.035f, -2.35f), new Vector3(1.9f, 0.1f, 0.14f), 0f, 2);
            mesh.AddBox(new Vector3(1.7f, 0.035f, 2.35f), new Vector3(1.9f, 0.1f, 0.14f), 0f, 2);
            mesh.AddBox(new Vector3(3.15f, -0.105f, 0f), new Vector3(0.18f, 0.08f, 7.1f), 0f, 0);
            mesh.AddBox(new Vector3(0f, -0.105f, -3.55f), new Vector3(5.8f, 0.08f, 0.18f), 0f, 3);
            mesh.AddBox(new Vector3(0f, -0.105f, 3.55f), new Vector3(5.8f, 0.08f, 0.18f), 0f, 3);
            return mesh.Build();
        }

        private static void _upgradeConvergence(Material[] materials)
        {
            var root = PrefabUtility.LoadPrefabContents(CONVERGENCE_PREFAB_PATH);
            try
            {
                _assignMaterial(root.transform, "Convergence Chamber Deck", materials[3]);
                _assignMaterial(root.transform, "West Convergence Baffle", materials[0]);
                _assignMaterial(root.transform, "East Convergence Baffle", materials[0]);
                _assignMaterial(root.transform, "Convergence Calibration Console/Calibration Console Base", materials[1]);
                _assignLandmarkMaterials(root.transform, "Convergence Busbar Assembly", materials);
                _ensureFinish(root, "Convergence Chamber Hero Finish", CONVERGENCE_MESH_PATH, materials);
                PrefabUtility.SaveAsPrefabAsset(root, CONVERGENCE_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _upgradeBreaker(Material[] materials)
        {
            var root = PrefabUtility.LoadPrefabContents(BREAKER_PREFAB_PATH);
            try
            {
                _assignMaterial(root.transform, "Breaker Gallery Deck", materials[3]);
                _assignMaterial(root.transform, "South Ceramic Breaker Shield", materials[1]);
                _assignMaterial(root.transform, "North Ceramic Breaker Shield", materials[1]);
                _assignLandmarkMaterials(root.transform, "Breaker Bank Assembly", materials);
                _ensureFinish(root, "Breaker Gallery Hero Finish", BREAKER_MESH_PATH, materials);
                PrefabUtility.SaveAsPrefabAsset(root, BREAKER_PREFAB_PATH);
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

            var finish = root.GetComponent<AuthoredConvergenceBreakerHeroFinish>() ??
                         root.AddComponent<AuthoredConvergenceBreakerHeroFinish>();
            finish.Configure(renderer);
        }

        private static void _assignMaterial(Transform root, string childPath, Material material)
        {
            var child = root.Find(childPath);
            if (child == null || material == null || !child.TryGetComponent<Renderer>(out var renderer))
            {
                throw new InvalidOperationException($"Could not finish the Convergence/Breaker renderer {childPath}.");
            }

            renderer.sharedMaterial = material;
        }

        private static void _assignLandmarkMaterials(Transform root, string childPath, Material[] materials)
        {
            var child = root.Find(childPath);
            var renderers = child != null ? child.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException($"Could not finish the Convergence/Breaker landmark {childPath}.");
            }

            foreach (var renderer in renderers)
            {
                var name = renderer.gameObject.name;
                renderer.sharedMaterial = name.Contains("Ceramic", StringComparison.OrdinalIgnoreCase) ||
                                          name.Contains("Insulator", StringComparison.OrdinalIgnoreCase)
                    ? materials[1]
                    : name.Contains("Bus", StringComparison.OrdinalIgnoreCase) ||
                      name.Contains("Coil", StringComparison.OrdinalIgnoreCase)
                        ? materials[2]
                        : materials[0];
            }
        }

        private static bool _hasFinish(GameObject prefab, string objectName)
        {
            var finish = prefab != null ? prefab.GetComponent<AuthoredConvergenceBreakerHeroFinish>() : null;
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
