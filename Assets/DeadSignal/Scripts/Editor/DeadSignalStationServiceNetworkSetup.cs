using System;
using System.Collections.Generic;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSignal.Editor
{
    public static class DeadSignalStationServiceNetworkSetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/StationServiceNetwork.prefab";
        private const string TRAY_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/StationCableTrays.asset";
        private const string BUS_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/StationPowerBuses.asset";
        private const string PIPE_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/StationCoolantPipes.asset";
        private const string TERMINATION_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/StationServiceTerminations.asset";
        private const string TRAY_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/StationCableTray.mat";
        private const string BUS_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/StationPowerBus.mat";
        private const string PIPE_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/StationCoolantPipe.mat";
        private const string TERMINATION_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/StationServiceTermination.mat";
        private const string ENVIRONMENT_PATH = "DEAD SIGNAL — Authored World/Environment";
        private const string NETWORK_NAME = "Station Service Network";
        private const int CONNECTION_CLUSTER_COUNT = 8;
        private const float OVERHEAD_SERVICE_HEIGHT = 1.6f;

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                var network = prefab != null ? prefab.GetComponent<AuthoredStationServiceNetwork>() : null;
                return AssetDatabase.LoadAssetAtPath<Mesh>(TRAY_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(BUS_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(PIPE_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(TERMINATION_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(TRAY_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(BUS_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(PIPE_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(TERMINATION_MATERIAL_PATH) != null &&
                       network != null && network.IsConfigured &&
                       prefab.GetComponentsInChildren<Collider>(true).Length == 0;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Station Service Network")]
        public static void EnsureAssets()
        {
            _ensureMesh(TRAY_MESH_PATH, _buildCableTrays());
            _ensureMesh(BUS_MESH_PATH, _buildPowerBuses());
            _ensureMesh(PIPE_MESH_PATH, _buildCoolantPipes());
            _ensureMesh(TERMINATION_MESH_PATH, _buildTerminations());
            _ensureMaterial(TRAY_MATERIAL_PATH, "StationCableTray", new Color(0.055f, 0.07f, 0.08f), 0.78f, 0.28f);
            _ensureMaterial(BUS_MATERIAL_PATH, "StationPowerBus", new Color(0.04f, 0.34f, 0.39f), 0.62f, 0.38f);
            _ensureMaterial(PIPE_MATERIAL_PATH, "StationCoolantPipe", new Color(0.48f, 0.62f, 0.64f), 0.34f, 0.48f);
            _ensureMaterial(TERMINATION_MATERIAL_PATH, "StationServiceTermination", new Color(0.42f, 0.16f, 0.055f), 0.72f, 0.34f);
            _ensurePrefab();
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The authored station service network is incomplete.");
            }
        }

        private static Mesh _buildCableTrays()
        {
            var builder = new MeshBuilder("StationCableTrays");
            _addTrayRun(builder, new[]
            {
                new Vector3(-10.4f, OVERHEAD_SERVICE_HEIGHT, -0.3f), new Vector3(-7.2f, OVERHEAD_SERVICE_HEIGHT, -0.3f),
                new Vector3(-7.2f, OVERHEAD_SERVICE_HEIGHT, 2.1f), new Vector3(-2.4f, OVERHEAD_SERVICE_HEIGHT, 2.1f)
            });
            _addTrayRun(builder, new[]
            {
                new Vector3(-5.8f, OVERHEAD_SERVICE_HEIGHT, 8.5f), new Vector3(-2.2f, OVERHEAD_SERVICE_HEIGHT, 8.5f),
                new Vector3(-2.2f, OVERHEAD_SERVICE_HEIGHT, 6.1f)
            });
            _addTrayRun(builder, new[]
            {
                new Vector3(10.7f, OVERHEAD_SERVICE_HEIGHT, 1.1f), new Vector3(14.5f, OVERHEAD_SERVICE_HEIGHT, 1.1f),
                new Vector3(14.5f, OVERHEAD_SERVICE_HEIGHT, 4.7f), new Vector3(20.8f, OVERHEAD_SERVICE_HEIGHT, 4.7f)
            });
            _addTrayRun(builder, new[]
            {
                new Vector3(27.2f, OVERHEAD_SERVICE_HEIGHT, 5.3f), new Vector3(32.4f, OVERHEAD_SERVICE_HEIGHT, 5.3f),
                new Vector3(32.4f, OVERHEAD_SERVICE_HEIGHT, 9.4f), new Vector3(38.8f, OVERHEAD_SERVICE_HEIGHT, 9.4f)
            });
            _addTrayRun(builder, new[]
            {
                new Vector3(45.8f, OVERHEAD_SERVICE_HEIGHT, 14.2f), new Vector3(47.5f, OVERHEAD_SERVICE_HEIGHT, 14.2f),
                new Vector3(47.5f, OVERHEAD_SERVICE_HEIGHT, 27.8f)
            });
            _addTrayRun(builder, new[]
            {
                new Vector3(39.2f, OVERHEAD_SERVICE_HEIGHT, 30.1f), new Vector3(36.4f, OVERHEAD_SERVICE_HEIGHT, 30.1f),
                new Vector3(36.4f, OVERHEAD_SERVICE_HEIGHT, 36.7f), new Vector3(39.3f, OVERHEAD_SERVICE_HEIGHT, 36.7f)
            });
            _addTrayRun(builder, new[]
            {
                new Vector3(46f, OVERHEAD_SERVICE_HEIGHT, 39.1f), new Vector3(48.1f, OVERHEAD_SERVICE_HEIGHT, 39.1f),
                new Vector3(48.1f, OVERHEAD_SERVICE_HEIGHT, 47.2f), new Vector3(45.7f, OVERHEAD_SERVICE_HEIGHT, 47.2f)
            });
            _addTrayRun(builder, new[]
            {
                new Vector3(39.2f, OVERHEAD_SERVICE_HEIGHT, 51.4f), new Vector3(37.1f, OVERHEAD_SERVICE_HEIGHT, 51.4f),
                new Vector3(37.1f, OVERHEAD_SERVICE_HEIGHT, 68.1f), new Vector3(39.2f, OVERHEAD_SERVICE_HEIGHT, 68.1f)
            });
            return builder.Build();
        }

        private static Mesh _buildPowerBuses()
        {
            var builder = new MeshBuilder("StationPowerBuses");
            _addBusPair(builder, new Vector3(-8.5f, 0.14f, 0.4f), new Vector3(-2.1f, 0.14f, 3.4f));
            _addBusPair(builder, new Vector3(-1.7f, 0.14f, 5.3f), new Vector3(-4.7f, 0.14f, 7.2f));
            _addBusPair(builder, new Vector3(1.7f, 0.14f, 5.3f), new Vector3(8.4f, 0.14f, 2.5f));
            _addBusPair(builder, new Vector3(12.2f, 0.14f, 2.4f), new Vector3(21.1f, 0.14f, 4.4f));
            _addBusPair(builder, new Vector3(26.7f, 0.14f, 5.2f), new Vector3(40.2f, 0.14f, 12.2f));
            _addBusPair(builder, new Vector3(42.5f, 0.14f, 17.1f), new Vector3(42.5f, 0.14f, 28.1f));
            _addBusPair(builder, new Vector3(42.5f, 0.14f, 31.8f), new Vector3(42.5f, 0.14f, 45.7f));
            _addBusPair(builder, new Vector3(42.5f, 0.14f, 48.5f), new Vector3(42.5f, 0.14f, 67.7f));
            return builder.Build();
        }

        private static Mesh _buildCoolantPipes()
        {
            var builder = new MeshBuilder("StationCoolantPipes");
            _addPipeRun(builder, new[]
            {
                new Vector3(5.7f, OVERHEAD_SERVICE_HEIGHT, 8.7f), new Vector3(8.8f, OVERHEAD_SERVICE_HEIGHT, 8.7f),
                new Vector3(8.8f, OVERHEAD_SERVICE_HEIGHT, 3.2f), new Vector3(11.1f, OVERHEAD_SERVICE_HEIGHT, 3.2f)
            });
            _addPipeRun(builder, new[]
            {
                new Vector3(23.6f, OVERHEAD_SERVICE_HEIGHT, 2.1f), new Vector3(23.6f, OVERHEAD_SERVICE_HEIGHT, -1.2f),
                new Vector3(28.7f, OVERHEAD_SERVICE_HEIGHT, -1.2f), new Vector3(28.7f, OVERHEAD_SERVICE_HEIGHT, 2.2f)
            });
            _addPipeRun(builder, new[]
            {
                new Vector3(40.5f, OVERHEAD_SERVICE_HEIGHT, 17.2f), new Vector3(38.6f, OVERHEAD_SERVICE_HEIGHT, 17.2f),
                new Vector3(38.6f, OVERHEAD_SERVICE_HEIGHT, 24.7f), new Vector3(40.2f, OVERHEAD_SERVICE_HEIGHT, 24.7f)
            });
            _addPipeRun(builder, new[]
            {
                new Vector3(45.1f, OVERHEAD_SERVICE_HEIGHT, 38.6f), new Vector3(49.2f, OVERHEAD_SERVICE_HEIGHT, 38.6f),
                new Vector3(49.2f, OVERHEAD_SERVICE_HEIGHT, 43.7f), new Vector3(45.4f, OVERHEAD_SERVICE_HEIGHT, 43.7f)
            });
            return builder.Build();
        }

        private static Mesh _buildTerminations()
        {
            var builder = new MeshBuilder("StationServiceTerminations");
            var centers = new[]
            {
                new Vector3(-2.1f, 0.34f, 3.4f), new Vector3(-4.7f, 0.34f, 7.2f),
                new Vector3(8.4f, 0.34f, 2.5f), new Vector3(21.1f, 0.34f, 4.4f),
                new Vector3(40.2f, 0.34f, 12.2f), new Vector3(42.5f, 0.34f, 28.1f),
                new Vector3(42.5f, 0.34f, 45.7f), new Vector3(42.5f, 0.34f, 67.7f)
            };
            foreach (var center in centers)
            {
                builder.AddBox(center, new Vector3(0.72f, 0.44f, 0.72f));
                builder.AddPipe(center + Vector3.up * 0.28f, center + Vector3.up * 0.72f, 0.16f, 8);
            }

            return builder.Build();
        }

        private static void _addTrayRun(MeshBuilder builder, IReadOnlyList<Vector3> points)
        {
            for (var index = 0; index < points.Count - 1; index++)
            {
                builder.AddBeam(points[index], points[index + 1], 0.32f, 0.12f);
            }

            foreach (var point in points)
            {
                builder.AddBox(point + Vector3.up * 0.08f, new Vector3(0.48f, 0.22f, 0.48f));
            }
        }

        private static void _addBusPair(MeshBuilder builder, Vector3 start, Vector3 end)
        {
            var direction = end - start;
            var side = Vector3.Cross(Vector3.up, direction.normalized) * 0.16f;
            builder.AddBeam(start - side, end - side, 0.12f, 0.07f);
            builder.AddBeam(start + side, end + side, 0.12f, 0.07f);
        }

        private static void _addPipeRun(MeshBuilder builder, IReadOnlyList<Vector3> points)
        {
            for (var index = 0; index < points.Count - 1; index++)
            {
                builder.AddPipe(points[index], points[index + 1], 0.16f, 8);
            }

            foreach (var point in points)
            {
                builder.AddPipe(point - Vector3.up * 0.1f, point + Vector3.up * 0.1f, 0.23f, 8);
            }
        }

        private static void _ensurePrefab()
        {
            var root = new GameObject(NETWORK_NAME);
            try
            {
                var layers = new[]
                {
                    _addLayer(root.transform, "Cable Trays", TRAY_MESH_PATH, TRAY_MATERIAL_PATH),
                    _addLayer(root.transform, "Power Buses", BUS_MESH_PATH, BUS_MATERIAL_PATH),
                    _addLayer(root.transform, "Coolant Pipes", PIPE_MESH_PATH, PIPE_MATERIAL_PATH),
                    _addLayer(root.transform, "Junctions and Terminations", TERMINATION_MESH_PATH, TERMINATION_MATERIAL_PATH)
                };
                root.AddComponent<AuthoredStationServiceNetwork>().Configure(layers, CONNECTION_CLUSTER_COUNT);
                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Renderer _addLayer(Transform parent, string objectName, string meshPath, string materialPath)
        {
            var layer = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
            layer.transform.SetParent(parent, false);
            layer.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            var renderer = layer.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            return renderer;
        }

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var environment = GameObject.Find(ENVIRONMENT_PATH);
            if (environment == null)
            {
                throw new InvalidOperationException($"The authored scene is missing {ENVIRONMENT_PATH}.");
            }

            var existing = environment.transform.Find(NETWORK_NAME);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            var instance = PrefabUtility.InstantiatePrefab(prefab, environment.transform) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("Could not place the station service network in SampleScene.");
            }

            instance.name = NETWORK_NAME;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
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

        private static void _ensureMaterial(string path, string materialName, Color color, float metallic, float smoothness)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for station service materials.");
                }

                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }

        private sealed class MeshBuilder
        {
            public MeshBuilder(string name)
            {
                m_name = name;
            }

            public void AddBeam(Vector3 start, Vector3 end, float width, float height)
            {
                var delta = end - start;
                var center = (start + end) * 0.5f;
                var rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
                AddBox(center, new Vector3(width, height, delta.magnitude), rotation);
            }

            public void AddBox(Vector3 center, Vector3 size, Quaternion rotation = default)
            {
                if (rotation == default)
                {
                    rotation = Quaternion.identity;
                }

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
                var startIndex = m_vertices.Count;
                foreach (var corner in corners)
                {
                    m_vertices.Add(center + rotation * corner);
                }

                m_uvs.AddRange(new[]
                {
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up
                });
                foreach (var index in faces)
                {
                    m_triangles.Add(startIndex + index);
                }
            }

            public void AddPipe(Vector3 start, Vector3 end, float radius, int sides)
            {
                var axis = (end - start).normalized;
                var tangent = Vector3.Cross(axis, Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.9f ? Vector3.right : Vector3.up).normalized;
                var bitangent = Vector3.Cross(axis, tangent).normalized;
                var startIndex = m_vertices.Count;
                for (var ring = 0; ring < 2; ring++)
                {
                    var center = ring == 0 ? start : end;
                    for (var side = 0; side < sides; side++)
                    {
                        var angle = side * Mathf.PI * 2f / sides;
                        var normal = tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle);
                        m_vertices.Add(center + normal * radius);
                        m_uvs.Add(new Vector2((float)side / sides, ring));
                    }
                }

                for (var side = 0; side < sides; side++)
                {
                    var next = (side + 1) % sides;
                    m_triangles.Add(startIndex + side);
                    m_triangles.Add(startIndex + sides + side);
                    m_triangles.Add(startIndex + sides + next);
                    m_triangles.Add(startIndex + side);
                    m_triangles.Add(startIndex + sides + next);
                    m_triangles.Add(startIndex + next);
                }

                var startCapIndex = m_vertices.Count;
                m_vertices.Add(start);
                m_uvs.Add(new Vector2(0.5f, 0.5f));
                var endCapIndex = m_vertices.Count;
                m_vertices.Add(end);
                m_uvs.Add(new Vector2(0.5f, 0.5f));
                for (var side = 0; side < sides; side++)
                {
                    var next = (side + 1) % sides;
                    m_triangles.Add(startCapIndex);
                    m_triangles.Add(startIndex + next);
                    m_triangles.Add(startIndex + side);
                    m_triangles.Add(endCapIndex);
                    m_triangles.Add(startIndex + sides + side);
                    m_triangles.Add(startIndex + sides + next);
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
