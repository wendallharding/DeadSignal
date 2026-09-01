using System;
using System.Collections.Generic;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSignal.Editor
{
    public static class DeadSignalStationFloorFinishSetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/StationFloorFinish.prefab";
        private const string PANEL_MESH_PATH = "Assets/DeadSignal/Resources/Environment/StationFloorPanelSeams.asset";
        private const string THRESHOLD_MESH_PATH = "Assets/DeadSignal/Resources/Environment/StationFloorThresholds.asset";
        private const string WEAR_MESH_PATH = "Assets/DeadSignal/Resources/Environment/StationFloorWear.asset";
        private const string MARKING_MESH_PATH = "Assets/DeadSignal/Resources/Environment/StationFloorMaintenanceMarks.asset";
        private const string PANEL_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/StationFloorPanelSeam.mat";
        private const string THRESHOLD_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/StationFloorThreshold.mat";
        private const string WEAR_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/StationFloorWear.mat";
        private const string MARKING_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/StationFloorMaintenanceMark.mat";
        private const string ENVIRONMENT_PATH = "DEAD SIGNAL — Authored World/Environment";
        private const string FINISH_NAME = "Station Floor Finish";
        private const int FINISHED_ZONE_COUNT = 12;

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                var finish = prefab != null ? prefab.GetComponent<AuthoredStationFloorFinish>() : null;
                return AssetDatabase.LoadAssetAtPath<Mesh>(PANEL_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(THRESHOLD_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(WEAR_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(MARKING_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(PANEL_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(THRESHOLD_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(WEAR_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(MARKING_MATERIAL_PATH) != null &&
                       finish != null && finish.IsConfigured &&
                       prefab.GetComponentsInChildren<Collider>(true).Length == 0;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Station Floor Finish")]
        public static void EnsureAssets()
        {
            _ensureMesh(PANEL_MESH_PATH, _buildPanelSeams());
            _ensureMesh(THRESHOLD_MESH_PATH, _buildThresholds());
            _ensureMesh(WEAR_MESH_PATH, _buildWear());
            _ensureMesh(MARKING_MESH_PATH, _buildMaintenanceMarks());
            _ensureMaterial(PANEL_MATERIAL_PATH, "StationFloorPanelSeam", new Color(0.055f, 0.065f, 0.075f), 0.72f, 0.24f);
            _ensureMaterial(THRESHOLD_MATERIAL_PATH, "StationFloorThreshold", new Color(0.28f, 0.19f, 0.075f), 0.48f, 0.3f);
            _ensureMaterial(WEAR_MATERIAL_PATH, "StationFloorWear", new Color(0.075f, 0.062f, 0.055f), 0.28f, 0.16f);
            _ensureMaterial(MARKING_MATERIAL_PATH, "StationFloorMaintenanceMark", new Color(0.3f, 0.32f, 0.31f), 0.32f, 0.22f);
            _ensurePrefab();
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The authored station floor-finish kit is incomplete.");
            }
        }

        private static Mesh _buildPanelSeams()
        {
            var builder = new MeshBuilder("StationFloorPanelSeams");
            var zones = new[]
            {
                new Zone(new Vector2(-10f, -2f), new Vector2(8f, 7f), 0f),
                new Zone(new Vector2(0f, 4.5f), new Vector2(12f, 10f), 0f),
                new Zone(new Vector2(-6f, 8f), new Vector2(8f, 8f), 0f),
                new Zone(new Vector2(6f, 8f), new Vector2(8f, 8f), 0f),
                new Zone(new Vector2(17f, 1f), new Vector2(14f, 7f), 0f),
                new Zone(new Vector2(29f, 7f), new Vector2(13f, 12f), 0f),
                new Zone(new Vector2(42.5f, 16f), new Vector2(12f, 14f), 0f),
                new Zone(new Vector2(42.5f, 28f), new Vector2(12f, 10f), 0f),
                new Zone(new Vector2(34.5f, 35f), new Vector2(10f, 11f), 0f),
                new Zone(new Vector2(50.5f, 35f), new Vector2(10f, 11f), 0f),
                new Zone(new Vector2(42.5f, 50f), new Vector2(12f, 13f), 0f),
                new Zone(new Vector2(42.5f, 68f), new Vector2(14f, 25f), 0f)
            };

            foreach (var zone in zones)
            {
                _addPanelGrid(builder, zone);
            }

            return builder.Build();
        }

        private static Mesh _buildThresholds()
        {
            var builder = new MeshBuilder("StationFloorThresholds");
            _addThreshold(builder, new Vector2(-5.8f, 4.4f), 4.2f, 90f);
            _addThreshold(builder, new Vector2(5.8f, 4.4f), 4.2f, 90f);
            _addThreshold(builder, new Vector2(11f, 1f), 5.2f, 0f);
            _addThreshold(builder, new Vector2(23f, 4.5f), 5.6f, 0f);
            _addThreshold(builder, new Vector2(36.5f, 11.5f), 5.8f, 0f);
            _addThreshold(builder, new Vector2(42.5f, 22f), 5.8f, 90f);
            _addThreshold(builder, new Vector2(42.5f, 42.5f), 6.2f, 90f);
            _addThreshold(builder, new Vector2(42.5f, 56.5f), 7.4f, 90f);
            _addThreshold(builder, new Vector2(42.5f, 79f), 7.4f, 90f);
            return builder.Build();
        }

        private static Mesh _buildWear()
        {
            var builder = new MeshBuilder("StationFloorWear");
            _addScuffFan(builder, new Vector2(-9f, -1f), 12f);
            _addScuffFan(builder, new Vector2(0f, 3f), -18f);
            _addScuffFan(builder, new Vector2(-6f, 8f), 35f);
            _addScuffFan(builder, new Vector2(6f, 8f), -32f);
            _addScuffFan(builder, new Vector2(28.5f, 7f), 8f);
            _addScuffFan(builder, new Vector2(42.5f, 15f), 28f);
            _addScuffFan(builder, new Vector2(35f, 34f), -22f);
            _addScuffFan(builder, new Vector2(50f, 34f), 18f);
            _addScuffFan(builder, new Vector2(42.5f, 62f), 6f);
            _addScuffFan(builder, new Vector2(42.5f, 74f), -8f);
            return builder.Build();
        }

        private static Mesh _buildMaintenanceMarks()
        {
            var builder = new MeshBuilder("StationFloorMaintenanceMarks");
            _addServiceCorner(builder, new Vector2(-10f, -2f), 0f);
            _addServiceCorner(builder, new Vector2(0f, 4.5f), 90f);
            _addServiceCorner(builder, new Vector2(17f, 1f), 0f);
            _addServiceCorner(builder, new Vector2(29f, 7f), 90f);
            _addServiceCorner(builder, new Vector2(42.5f, 16f), 0f);
            _addServiceCorner(builder, new Vector2(42.5f, 28f), 90f);
            _addServiceCorner(builder, new Vector2(34.5f, 35f), 0f);
            _addServiceCorner(builder, new Vector2(50.5f, 35f), 180f);
            _addServiceCorner(builder, new Vector2(42.5f, 50f), 90f);
            _addServiceCorner(builder, new Vector2(42.5f, 68f), 90f);
            return builder.Build();
        }

        private static void _addPanelGrid(MeshBuilder builder, Zone zone)
        {
            var rotation = Quaternion.Euler(0f, zone.RotationY, 0f);
            for (var column = -1; column <= 1; column++)
            {
                var offset = rotation * new Vector3(column * zone.Size.x / 3f, 0f, 0f);
                builder.AddBox(_position(zone.Center + new Vector2(offset.x, offset.z), 0.008f),
                    new Vector3(0.055f, 0.016f, zone.Size.y), zone.RotationY);
            }

            for (var row = -1; row <= 1; row++)
            {
                var offset = rotation * new Vector3(0f, 0f, row * zone.Size.y / 3f);
                builder.AddBox(_position(zone.Center + new Vector2(offset.x, offset.z), 0.008f),
                    new Vector3(zone.Size.x, 0.016f, 0.055f), zone.RotationY);
            }
        }

        private static void _addThreshold(MeshBuilder builder, Vector2 center, float width, float rotationY)
        {
            for (var index = -2; index <= 2; index++)
            {
                var rotation = Quaternion.Euler(0f, rotationY, 0f);
                var offset = rotation * new Vector3(index * width / 5f, 0f, 0f);
                builder.AddBox(_position(center + new Vector2(offset.x, offset.z), 0.014f),
                    new Vector3(width / 7f, 0.02f, 0.16f), rotationY + 32f);
            }
        }

        private static void _addScuffFan(MeshBuilder builder, Vector2 center, float rotationY)
        {
            for (var index = -1; index <= 1; index++)
            {
                var rotation = Quaternion.Euler(0f, rotationY + index * 11f, 0f);
                var offset = rotation * new Vector3(0.35f, 0f, index * 0.3f);
                builder.AddBox(_position(center + new Vector2(offset.x, offset.z), 0.011f),
                    new Vector3(2.6f - Mathf.Abs(index) * 0.55f, 0.018f, 0.1f), rotationY + index * 11f);
            }
        }

        private static void _addServiceCorner(MeshBuilder builder, Vector2 center, float rotationY)
        {
            var rotation = Quaternion.Euler(0f, rotationY, 0f);
            var offset = rotation * new Vector3(1.3f, 0f, 1.1f);
            var markCenter = center + new Vector2(offset.x, offset.z);
            builder.AddBox(_position(markCenter, 0.013f), new Vector3(1.5f, 0.019f, 0.12f), rotationY);
            builder.AddBox(_position(markCenter, 0.013f), new Vector3(0.12f, 0.019f, 1.5f), rotationY);
        }

        private static void _ensurePrefab()
        {
            var root = new GameObject(FINISH_NAME);
            try
            {
                var layers = new[]
                {
                    _addLayer(root.transform, "Panel Seams", PANEL_MESH_PATH, PANEL_MATERIAL_PATH),
                    _addLayer(root.transform, "Functional Thresholds", THRESHOLD_MESH_PATH, THRESHOLD_MATERIAL_PATH),
                    _addLayer(root.transform, "Wear and Scorch", WEAR_MESH_PATH, WEAR_MATERIAL_PATH),
                    _addLayer(root.transform, "Maintenance Marks", MARKING_MESH_PATH, MARKING_MATERIAL_PATH)
                };
                root.AddComponent<AuthoredStationFloorFinish>().Configure(layers, FINISHED_ZONE_COUNT);
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
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
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

            var existing = environment.transform.Find(FINISH_NAME);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            var instance = PrefabUtility.InstantiatePrefab(prefab, environment.transform) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("Could not place the station floor finish in SampleScene.");
            }

            instance.name = FINISH_NAME;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Vector3 _position(Vector2 position, float y)
        {
            return new Vector3(position.x, y, position.y);
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
                    throw new InvalidOperationException("Could not find the URP Lit shader for station floor finish.");
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

        private readonly struct Zone
        {
            public Zone(Vector2 center, Vector2 size, float rotationY)
            {
                Center = center;
                Size = size;
                RotationY = rotationY;
            }

            public Vector2 Center { get; }
            public Vector2 Size { get; }
            public float RotationY { get; }
        }

        private sealed class MeshBuilder
        {
            public MeshBuilder(string name)
            {
                m_name = name;
            }

            public void AddBox(Vector3 center, Vector3 size, float rotationY = 0f)
            {
                var half = size * 0.5f;
                var rotation = Quaternion.Euler(0f, rotationY, 0f);
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
                    m_vertices.Add(center + rotation * corner);
                }

                m_uvs.AddRange(new[]
                {
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up
                });
                foreach (var index in faces)
                {
                    m_triangles.Add(start + index);
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
