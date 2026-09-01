using System;
using System.Collections.Generic;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSignal.Editor
{
    public static class DeadSignalStationFunctionalPropSetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string ENVIRONMENT_PATH = "DEAD SIGNAL — Authored World/Environment";
        private const string KIT_PATH = "Assets/DeadSignal/Resources/Environment/StationFunctionalProps.prefab";
        private const string KIT_NAME = "Station Functional Props";
        private const string MESH_DIRECTORY = "Assets/DeadSignal/Resources/Environment/FunctionalProps/";
        private const string PREFAB_DIRECTORY = "Assets/DeadSignal/Resources/Environment/FunctionalProps/";
        private const string MATERIAL_DIRECTORY = "Assets/DeadSignal/Resources/Materials/FunctionalProps/";
        private const int PROP_TYPE_COUNT = 6;
        private const int PLACEMENT_COUNT = 18;

        private static readonly PropDefinition[] s_definitions =
        {
            new("CargoCrate", new Color(0.13f, 0.15f, 0.16f), 0.72f, 0.24f, _buildCargoCrate),
            new("ToolCart", new Color(0.18f, 0.20f, 0.20f), 0.68f, 0.28f, _buildToolCart),
            new("ServiceCanister", new Color(0.52f, 0.49f, 0.42f), 0.58f, 0.30f, _buildServiceCanister),
            new("CableReel", new Color(0.10f, 0.12f, 0.13f), 0.62f, 0.22f, _buildCableReel),
            new("GuardRail", new Color(0.36f, 0.25f, 0.08f), 0.56f, 0.26f, _buildGuardRail),
            new("MaintenanceFixture", new Color(0.17f, 0.19f, 0.19f), 0.70f, 0.25f, _buildMaintenanceFixture)
        };

        private static readonly Placement[] s_placements =
        {
            new(0, new Vector3(-13.2f, 0f, -4.5f), 8f), new(0, new Vector3(-11.8f, 0f, -4.6f), -4f),
            new(0, new Vector3(3.9f, 0f, 8.1f), 90f), new(1, new Vector3(-7.9f, 0f, 10.5f), 0f),
            new(1, new Vector3(25.2f, 0f, 10.6f), 90f), new(1, new Vector3(46.8f, 0f, 29.8f), 180f),
            new(2, new Vector3(8.5f, 0f, 10.4f), 0f), new(2, new Vector3(32.8f, 0f, 38.8f), 0f),
            new(2, new Vector3(53.1f, 0f, 38.7f), 0f), new(3, new Vector3(20.5f, 0f, -1.6f), 90f),
            new(3, new Vector3(46.8f, 0f, 18.8f), 0f), new(3, new Vector3(38.1f, 0f, 51.8f), 20f),
            new(4, new Vector3(-4.2f, 0f, 11.6f), 0f), new(4, new Vector3(29f, 0f, 12.1f), 90f),
            new(4, new Vector3(48.5f, 0f, 54.7f), 0f), new(5, new Vector3(13.4f, 0f, -1.7f), 180f),
            new(5, new Vector3(39f, 0f, 30.7f), 90f), new(5, new Vector3(47.5f, 0f, 75.2f), 180f)
        };

        public static bool HasAssets
        {
            get
            {
                var kit = AssetDatabase.LoadAssetAtPath<GameObject>(KIT_PATH);
                var props = kit != null ? kit.GetComponent<AuthoredStationFunctionalProps>() : null;
                if (props == null || !props.IsConfigured || kit.GetComponentsInChildren<Collider>(true).Length != 0)
                {
                    return false;
                }

                foreach (var definition in s_definitions)
                {
                    if (AssetDatabase.LoadAssetAtPath<Mesh>(_meshPath(definition.Name)) == null ||
                        AssetDatabase.LoadAssetAtPath<Material>(_materialPath(definition.Name)) == null ||
                        AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath(definition.Name)) == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Station Functional Props")]
        public static void EnsureAssets()
        {
            _ensureDirectories();
            foreach (var definition in s_definitions)
            {
                _ensureMesh(_meshPath(definition.Name), definition.Build());
                _ensureMaterial(_materialPath(definition.Name), definition.Name, definition.Color,
                    definition.Metallic, definition.Smoothness);
                _ensurePropPrefab(definition.Name);
            }

            _ensureKitPrefab();
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The station functional-prop kit is incomplete.");
            }
        }

        private static Mesh _buildCargoCrate()
        {
            var builder = new MeshBuilder("StationCargoCrate");
            builder.AddBox(new Vector3(0f, 0.48f, 0f), new Vector3(1.25f, 0.9f, 0.9f));
            builder.AddBox(new Vector3(0f, 0.96f, 0f), new Vector3(1.32f, 0.08f, 0.97f));
            for (var x = -1; x <= 1; x += 2)
            for (var z = -1; z <= 1; z += 2)
            {
                builder.AddBox(new Vector3(x * 0.59f, 0.48f, z * 0.41f), new Vector3(0.08f, 0.96f, 0.08f));
            }
            builder.AddBox(new Vector3(0f, 0.49f, -0.46f), new Vector3(0.72f, 0.34f, 0.035f));
            return builder.Build();
        }

        private static Mesh _buildToolCart()
        {
            var builder = new MeshBuilder("StationToolCart");
            builder.AddBox(new Vector3(0f, 0.43f, 0f), new Vector3(1.35f, 0.12f, 0.72f));
            builder.AddBox(new Vector3(0f, 0.79f, 0f), new Vector3(1.35f, 0.09f, 0.72f));
            builder.AddBox(new Vector3(0f, 0.61f, 0.31f), new Vector3(1.28f, 0.06f, 0.06f));
            for (var x = -1; x <= 1; x += 2)
            {
                builder.AddBox(new Vector3(x * 0.61f, 0.58f, 0f), new Vector3(0.07f, 0.76f, 0.07f));
                builder.AddCylinder(new Vector3(x * 0.61f, 0.14f, -0.25f), 0.14f, 0.10f, 12, Quaternion.Euler(90f, 0f, 0f));
                builder.AddCylinder(new Vector3(x * 0.61f, 0.14f, 0.25f), 0.14f, 0.10f, 12, Quaternion.Euler(90f, 0f, 0f));
            }
            builder.AddBox(new Vector3(-0.78f, 1.02f, 0f), new Vector3(0.07f, 0.58f, 0.07f));
            builder.AddBox(new Vector3(-0.98f, 1.28f, 0f), new Vector3(0.45f, 0.07f, 0.07f));
            return builder.Build();
        }

        private static Mesh _buildServiceCanister()
        {
            var builder = new MeshBuilder("StationServiceCanister");
            builder.AddCylinder(new Vector3(0f, 0.56f, 0f), 0.36f, 1.02f, 16, Quaternion.identity);
            builder.AddCylinder(new Vector3(0f, 0.14f, 0f), 0.41f, 0.10f, 16, Quaternion.identity);
            builder.AddCylinder(new Vector3(0f, 0.98f, 0f), 0.41f, 0.10f, 16, Quaternion.identity);
            builder.AddBox(new Vector3(0f, 1.15f, 0f), new Vector3(0.32f, 0.24f, 0.18f));
            builder.AddBox(new Vector3(0f, 0.58f, -0.365f), new Vector3(0.28f, 0.34f, 0.025f));
            return builder.Build();
        }

        private static Mesh _buildCableReel()
        {
            var builder = new MeshBuilder("StationCableReel");
            var axis = Quaternion.Euler(0f, 0f, 90f);
            builder.AddCylinder(new Vector3(-0.38f, 0.58f, 0f), 0.62f, 0.10f, 16, axis);
            builder.AddCylinder(new Vector3(0.38f, 0.58f, 0f), 0.62f, 0.10f, 16, axis);
            builder.AddCylinder(new Vector3(0f, 0.58f, 0f), 0.34f, 0.78f, 16, axis);
            builder.AddCylinder(new Vector3(0f, 0.58f, 0f), 0.14f, 0.94f, 12, axis);
            builder.AddBox(new Vector3(0f, 0.10f, 0f), new Vector3(1.05f, 0.12f, 0.82f));
            return builder.Build();
        }

        private static Mesh _buildGuardRail()
        {
            var builder = new MeshBuilder("StationGuardRail");
            for (var x = -1; x <= 1; x++)
            {
                builder.AddBox(new Vector3(x * 1.05f, 0.58f, 0f), new Vector3(0.11f, 1.16f, 0.11f));
            }
            builder.AddCylinder(new Vector3(0f, 1.08f, 0f), 0.065f, 2.2f, 10, Quaternion.Euler(0f, 0f, 90f));
            builder.AddCylinder(new Vector3(0f, 0.58f, 0f), 0.045f, 2.2f, 10, Quaternion.Euler(0f, 0f, 90f));
            builder.AddBox(new Vector3(0f, 0.05f, 0f), new Vector3(2.28f, 0.10f, 0.28f));
            return builder.Build();
        }

        private static Mesh _buildMaintenanceFixture()
        {
            var builder = new MeshBuilder("StationMaintenanceFixture");
            builder.AddBox(new Vector3(0f, 0.48f, 0f), new Vector3(1.55f, 0.9f, 0.62f));
            builder.AddBox(new Vector3(0f, 0.98f, 0f), new Vector3(1.68f, 0.10f, 0.72f));
            builder.AddBox(new Vector3(0f, 1.47f, 0.27f), new Vector3(1.5f, 0.92f, 0.08f));
            builder.AddBox(new Vector3(-0.48f, 1.48f, 0.21f), new Vector3(0.28f, 0.28f, 0.06f));
            builder.AddBox(new Vector3(0.48f, 1.48f, 0.21f), new Vector3(0.28f, 0.28f, 0.06f));
            builder.AddBox(new Vector3(0f, 0.49f, -0.325f), new Vector3(0.06f, 0.72f, 0.03f));
            return builder.Build();
        }

        private static void _ensureDirectories()
        {
            _ensureDirectory("Assets/DeadSignal/Resources/Environment", "FunctionalProps");
            _ensureDirectory("Assets/DeadSignal/Resources/Materials", "FunctionalProps");
        }

        private static void _ensureDirectory(string parent, string name)
        {
            var path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void _ensurePropPrefab(string name)
        {
            var root = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            try
            {
                root.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(_meshPath(name));
                _configureRenderer(root.GetComponent<MeshRenderer>(), AssetDatabase.LoadAssetAtPath<Material>(_materialPath(name)));
                PrefabUtility.SaveAsPrefabAsset(root, _prefabPath(name));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void _ensureKitPrefab()
        {
            var root = new GameObject(KIT_NAME);
            try
            {
                var renderers = new List<Renderer>(PLACEMENT_COUNT);
                for (var index = 0; index < s_placements.Length; index++)
                {
                    var placement = s_placements[index];
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath(s_definitions[placement.DefinitionIndex].Name));
                    var instance = PrefabUtility.InstantiatePrefab(source, root.transform) as GameObject;
                    if (instance == null)
                    {
                        throw new InvalidOperationException($"Could not instantiate functional prop {source.name}.");
                    }

                    instance.name = $"{source.name} {index + 1:00}";
                    instance.transform.localPosition = placement.Position;
                    instance.transform.localRotation = Quaternion.Euler(0f, placement.RotationY, 0f);
                    renderers.Add(instance.GetComponent<Renderer>());
                }

                root.AddComponent<AuthoredStationFunctionalProps>().Configure(renderers.ToArray(), PROP_TYPE_COUNT, PLACEMENT_COUNT);
                PrefabUtility.SaveAsPrefabAsset(root, KIT_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var environment = GameObject.Find(ENVIRONMENT_PATH);
            if (environment == null)
            {
                throw new InvalidOperationException($"The authored scene is missing {ENVIRONMENT_PATH}.");
            }

            var existing = environment.transform.Find(KIT_NAME);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KIT_PATH);
            var instance = PrefabUtility.InstantiatePrefab(prefab, environment.transform) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("Could not place the station functional props in SampleScene.");
            }

            instance.name = KIT_NAME;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void _configureRenderer(Renderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
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

        private static void _ensureMaterial(string path, string name, Color color, float metallic, float smoothness)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for station functional props.");
                }

                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }

        private static string _meshPath(string name) => $"{MESH_DIRECTORY}{name}.asset";
        private static string _materialPath(string name) => $"{MATERIAL_DIRECTORY}{name}.mat";
        private static string _prefabPath(string name) => $"{PREFAB_DIRECTORY}{name}.prefab";

        private readonly struct PropDefinition
        {
            public PropDefinition(string name, Color color, float metallic, float smoothness, Func<Mesh> build)
            {
                Name = name;
                Color = color;
                Metallic = metallic;
                Smoothness = smoothness;
                Build = build;
            }

            public string Name { get; }
            public Color Color { get; }
            public float Metallic { get; }
            public float Smoothness { get; }
            public Func<Mesh> Build { get; }
        }

        private readonly struct Placement
        {
            public Placement(int definitionIndex, Vector3 position, float rotationY)
            {
                DefinitionIndex = definitionIndex;
                Position = position;
                RotationY = rotationY;
            }

            public int DefinitionIndex { get; }
            public Vector3 Position { get; }
            public float RotationY { get; }
        }

        private sealed class MeshBuilder
        {
            public MeshBuilder(string name) => m_name = name;

            public void AddBox(Vector3 center, Vector3 size)
            {
                var half = size * 0.5f;
                var corners = new[]
                {
                    new Vector3(-half.x, -half.y, -half.z), new Vector3(half.x, -half.y, -half.z),
                    new Vector3(half.x, half.y, -half.z), new Vector3(-half.x, half.y, -half.z),
                    new Vector3(-half.x, -half.y, half.z), new Vector3(half.x, -half.y, half.z),
                    new Vector3(half.x, half.y, half.z), new Vector3(-half.x, half.y, half.z)
                };
                var faces = new[]
                {
                    0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7,
                    0, 4, 7, 0, 7, 3, 1, 2, 6, 1, 6, 5,
                    3, 7, 6, 3, 6, 2, 0, 1, 5, 0, 5, 4
                };
                var start = m_vertices.Count;
                foreach (var corner in corners)
                {
                    m_vertices.Add(center + corner);
                    m_uvs.Add(new Vector2(corner.x / Mathf.Max(size.x, 0.01f) + 0.5f,
                        corner.y / Mathf.Max(size.y, 0.01f) + 0.5f));
                }
                foreach (var index in faces)
                {
                    m_triangles.Add(start + index);
                }
            }

            public void AddCylinder(Vector3 center, float radius, float height, int segments, Quaternion rotation)
            {
                var start = m_vertices.Count;
                for (var index = 0; index < segments; index++)
                {
                    var angle = index * Mathf.PI * 2f / segments;
                    var radial = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    m_vertices.Add(center + rotation * (radial + Vector3.down * height * 0.5f));
                    m_vertices.Add(center + rotation * (radial + Vector3.up * height * 0.5f));
                    m_uvs.Add(new Vector2(index / (float)segments, 0f));
                    m_uvs.Add(new Vector2(index / (float)segments, 1f));
                }
                var bottomCenter = m_vertices.Count;
                m_vertices.Add(center + rotation * Vector3.down * height * 0.5f);
                m_uvs.Add(new Vector2(0.5f, 0.5f));
                var topCenter = m_vertices.Count;
                m_vertices.Add(center + rotation * Vector3.up * height * 0.5f);
                m_uvs.Add(new Vector2(0.5f, 0.5f));
                for (var index = 0; index < segments; index++)
                {
                    var next = (index + 1) % segments;
                    m_triangles.Add(start + index * 2);
                    m_triangles.Add(start + next * 2 + 1);
                    m_triangles.Add(start + index * 2 + 1);
                    m_triangles.Add(start + index * 2);
                    m_triangles.Add(start + next * 2);
                    m_triangles.Add(start + next * 2 + 1);
                    m_triangles.Add(bottomCenter);
                    m_triangles.Add(start + next * 2);
                    m_triangles.Add(start + index * 2);
                    m_triangles.Add(topCenter);
                    m_triangles.Add(start + index * 2 + 1);
                    m_triangles.Add(start + next * 2 + 1);
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
