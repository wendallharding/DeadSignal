using System;
using System.Collections.Generic;
using System.Linq;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSignal.Editor
{
    public static class DeadSignalDepartureDockHeroSetup
    {
        private const string CHANNEL_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ExtractionDepartureChannel.prefab";
        private const string DOCK_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ExtractionPadAssembly.prefab";
        private const string TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/DepartureDockHeroAtlas.png";
        private const string CHANNEL_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/DepartureChannelHeroFinish.asset";
        private const string DOCK_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/ExtractionDockHeroFinish.asset";
        private const string MATERIAL_FOLDER =
            "Assets/DeadSignal/Resources/Materials/DepartureDockHeroFinish";

        private static readonly string[] s_materialPaths =
        {
            MATERIAL_FOLDER + "/DepartureDockAlloy.mat",
            MATERIAL_FOLDER + "/DepartureDockCeramic.mat",
            MATERIAL_FOLDER + "/DepartureDockUplink.mat",
            MATERIAL_FOLDER + "/DepartureDockSurge.mat"
        };

        public static bool HasAssets
        {
            get
            {
                var channel = AssetDatabase.LoadAssetAtPath<GameObject>(CHANNEL_PREFAB_PATH);
                var dock = AssetDatabase.LoadAssetAtPath<GameObject>(DOCK_PREFAB_PATH);
                var channelFinish = channel?.GetComponent<AuthoredDepartureDockHeroFinish>();
                var dockFinish = dock?.GetComponent<AuthoredDepartureDockHeroFinish>();
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(CHANNEL_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(DOCK_MESH_PATH) != null &&
                       s_materialPaths.All(path => AssetDatabase.LoadAssetAtPath<Material>(path) != null) &&
                       channelFinish is { IsConfigured: true, Owner: DepartureDockHeroOwner.DepartureChannel } &&
                       dockFinish is { IsConfigured: true, Owner: DepartureDockHeroOwner.ExtractionDock } &&
                       channelFinish.Renderer.GetComponents<Collider>().Length == 0 &&
                       dockFinish.Renderer.GetComponents<Collider>().Length == 0;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Departure Channel and Extraction Dock Hero Finish")]
        public static void EnsureAssets()
        {
            _configureTexture();
            _ensureMaterialFolder();
            var materials = _ensureMaterials();
            _saveOrReplaceMesh(CHANNEL_MESH_PATH, _buildChannelMesh());
            _saveOrReplaceMesh(DOCK_MESH_PATH, _buildDockMesh());
            _upgradePrefab(CHANNEL_PREFAB_PATH, "Departure Channel Hero Finish", CHANNEL_MESH_PATH,
                DepartureDockHeroOwner.DepartureChannel, materials);
            _upgradePrefab(DOCK_PREFAB_PATH, "Extraction Dock Hero Finish", DOCK_MESH_PATH,
                DepartureDockHeroOwner.ExtractionDock, materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Departure Channel and Extraction Dock hero finish is incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("The Departure and Dock hero atlas is missing.");
            }

            importer.alphaIsTransparency = true;
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
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "DepartureDockHeroFinish");
            }
        }

        private static Material[] _ensureMaterials()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            return new[]
            {
                _ensureMaterial(s_materialPaths[0], "DepartureDockAlloy", texture,
                    Vector2.up * 0.5f, 0.78f, 0.24f, new Color(0.64f, 0.66f, 0.68f)),
                _ensureMaterial(s_materialPaths[1], "DepartureDockCeramic", texture,
                    Vector2.one * 0.5f, 0.08f, 0.35f, new Color(0.92f, 0.9f, 0.86f)),
                _ensureMaterial(s_materialPaths[2], "DepartureDockUplink", texture,
                    Vector2.zero, 0.58f, 0.32f, new Color(0.58f, 0.72f, 0.74f)),
                _ensureMaterial(s_materialPaths[3], "DepartureDockSurge", texture,
                    Vector2.right * 0.5f, 0.7f, 0.3f, new Color(0.75f, 0.64f, 0.46f))
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
                    throw new InvalidOperationException("Could not find the URP Lit shader for the Departure/Dock finish.");
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

        private static Mesh _buildChannelMesh()
        {
            var mesh = new MeshBuilder("DepartureChannelHeroFinish", 4);

            // Low rails reveal the direct surge lane while side housings frame both established flanks.
            mesh.AddBox(new Vector3(-1.8f, 0.04f, -0.68f), new Vector3(2.2f, 0.08f, 0.12f), 0f, 3);
            mesh.AddBox(new Vector3(-1.8f, 0.04f, 0.68f), new Vector3(2.2f, 0.08f, 0.12f), 0f, 3);
            mesh.AddBox(new Vector3(1.8f, 0.04f, -0.68f), new Vector3(2.2f, 0.08f, 0.12f), 0f, 2);
            mesh.AddBox(new Vector3(1.8f, 0.04f, 0.68f), new Vector3(2.2f, 0.08f, 0.12f), 0f, 2);
            mesh.AddBox(new Vector3(0f, 0.18f, -1.76f), new Vector3(4.7f, 0.14f, 0.14f), 0f, 1);
            mesh.AddBox(new Vector3(0f, 0.18f, 1.76f), new Vector3(4.7f, 0.14f, 0.14f), 0f, 1);
            mesh.AddBox(new Vector3(-0.3f, 0.18f, -0.96f), new Vector3(0.18f, 0.35f, 0.16f), 0f, 0);
            mesh.AddBox(new Vector3(-0.3f, 0.18f, 0.96f), new Vector3(0.18f, 0.35f, 0.16f), 0f, 0);
            mesh.AddBox(new Vector3(0.3f, 0.18f, -0.96f), new Vector3(0.18f, 0.35f, 0.16f), 0f, 0);
            mesh.AddBox(new Vector3(0.3f, 0.18f, 0.96f), new Vector3(0.18f, 0.35f, 0.16f), 0f, 0);
            return mesh.Build();
        }

        private static Mesh _buildDockMesh()
        {
            var mesh = new MeshBuilder("ExtractionDockHeroFinish", 4);

            // Radial service ribs and an uplink crown reinforce extraction without occupying the escape floor.
            for (var index = 0; index < 8; index++)
            {
                var angle = index * 45f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                mesh.AddBox(direction * 1.72f + Vector3.up * 0.035f,
                    new Vector3(1.05f, 0.07f, 0.13f), angle, index % 2 == 0 ? 2 : 0);
            }

            for (var index = 0; index < 4; index++)
            {
                var angle = index * 90f + 45f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                mesh.AddBox(direction * 1.08f + Vector3.up * 0.18f,
                    new Vector3(0.18f, 0.35f, 0.28f), angle, 1);
            }

            mesh.AddBox(new Vector3(0f, 0.16f, -2.35f), new Vector3(2.2f, 0.12f, 0.14f), 0f, 3);
            mesh.AddBox(new Vector3(0f, 0.16f, 2.35f), new Vector3(2.2f, 0.12f, 0.14f), 0f, 3);
            return mesh.Build();
        }

        private static void _upgradePrefab(
            string prefabPath,
            string objectName,
            string meshPath,
            DepartureDockHeroOwner owner,
            Material[] materials)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
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
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                foreach (var collider in part.GetComponents<Collider>())
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }

                var finish = root.GetComponent<AuthoredDepartureDockHeroFinish>() ??
                             root.AddComponent<AuthoredDepartureDockHeroFinish>();
                finish.Configure(owner, renderer);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
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
                mesh.RecalculateTangents();
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
