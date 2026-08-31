using System;
using System.Collections.Generic;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSignal.Editor
{
    public static class DeadSignalStatefulDoorFrameSetup
    {
        private const string PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/StatefulDoorFrameKit.prefab";
        private const string HOUSING_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/StatefulDoorFrameHousing.asset";
        private const string MECHANISM_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/StatefulDoorFrameMechanisms.asset";
        private const string STATUS_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/StatefulDoorFrameStatus.asset";
        private const string OPEN_GLYPH_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/StatefulDoorOpenGlyph.asset";
        private const string BULKHEAD_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/MaintenanceBulkhead.mat";
        private const string STEEL_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/StationSteel.mat";
        private const string STATUS_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RouteDoorThresholdStatus.mat";
        private const string KIT_NAME = "Stateful Door Frame Kit";
        private const int EXPECTED_DOOR_COUNT = 7;

        private static readonly string[] s_doorPrefabPaths =
        {
            "Assets/DeadSignal/Resources/Environment/ShortcutGateAssembly.prefab",
            "Assets/DeadSignal/Resources/Environment/EastSalvageVault.prefab",
            "Assets/DeadSignal/Resources/Environment/RelayFoundryRegion.prefab",
            "Assets/DeadSignal/Resources/Environment/CapacitorSpineRegion.prefab",
            "Assets/DeadSignal/Resources/Environment/QuenchLoopRegion.prefab",
            "Assets/DeadSignal/Resources/Environment/SecurityTrialWingRegion.prefab"
        };

        public static bool HasAssets
        {
            get
            {
                var framePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                var frame = framePrefab != null ? framePrefab.GetComponent<AuthoredStatefulDoorFrame>() : null;
                if (AssetDatabase.LoadAssetAtPath<Mesh>(HOUSING_MESH_PATH) == null ||
                    AssetDatabase.LoadAssetAtPath<Mesh>(MECHANISM_MESH_PATH) == null ||
                    AssetDatabase.LoadAssetAtPath<Mesh>(STATUS_MESH_PATH) == null ||
                    AssetDatabase.LoadAssetAtPath<Mesh>(OPEN_GLYPH_MESH_PATH) == null ||
                    frame == null || !frame.IsConfigured ||
                    framePrefab.GetComponentsInChildren<Collider>(true).Length != 0)
                {
                    return false;
                }

                var configuredDoors = 0;
                foreach (var path in s_doorPrefabPaths)
                {
                    var doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (doorPrefab == null)
                    {
                        return false;
                    }

                    foreach (var readability in doorPrefab.GetComponentsInChildren<AuthoredRouteDoorReadability>(true))
                    {
                        if (readability.FrameKit == null || !readability.FrameKit.IsConfigured)
                        {
                            return false;
                        }

                        configuredDoors++;
                    }
                }

                return configuredDoors == EXPECTED_DOOR_COUNT;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Stateful Door Frame Kit")]
        public static void EnsureAssets()
        {
            _ensureMesh(HOUSING_MESH_PATH, _buildHousing());
            _ensureMesh(MECHANISM_MESH_PATH, _buildMechanisms());
            _ensureMesh(STATUS_MESH_PATH, _buildStatus());
            _ensureMesh(OPEN_GLYPH_MESH_PATH, _buildOpenGlyph());
            _ensurePrefab();

            foreach (var path in s_doorPrefabPaths)
            {
                _installFrames(path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The reusable stateful progression-door frame kit is incomplete.");
            }
        }

        private static Mesh _buildHousing()
        {
            var builder = new MeshBuilder("StatefulDoorFrameHousing");
            builder.AddBox(new Vector3(0f, 1.42f, -1.5f), new Vector3(0.68f, 2.85f, 0.38f));
            builder.AddBox(new Vector3(0f, 1.42f, 1.5f), new Vector3(0.68f, 2.85f, 0.38f));
            builder.AddBox(new Vector3(0f, 2.78f, 0f), new Vector3(0.68f, 0.34f, 3.38f));
            builder.AddBox(new Vector3(0f, 0.13f, -1.5f), new Vector3(0.78f, 0.26f, 0.52f));
            builder.AddBox(new Vector3(0f, 0.13f, 1.5f), new Vector3(0.78f, 0.26f, 0.52f));
            return builder.Build();
        }

        private static Mesh _buildMechanisms()
        {
            var builder = new MeshBuilder("StatefulDoorFrameMechanisms");
            builder.AddBox(new Vector3(-0.37f, 1.42f, -1.57f), new Vector3(0.12f, 2.25f, 0.58f));
            builder.AddBox(new Vector3(-0.37f, 1.42f, 1.57f), new Vector3(0.12f, 2.25f, 0.58f));
            builder.AddBox(new Vector3(0.37f, 1.42f, -1.57f), new Vector3(0.12f, 2.25f, 0.58f));
            builder.AddBox(new Vector3(0.37f, 1.42f, 1.57f), new Vector3(0.12f, 2.25f, 0.58f));
            builder.AddBox(new Vector3(-0.38f, 2.39f, -0.88f), new Vector3(0.14f, 0.16f, 0.78f));
            builder.AddBox(new Vector3(-0.38f, 2.39f, 0.88f), new Vector3(0.14f, 0.16f, 0.78f));
            builder.AddBox(new Vector3(0.38f, 2.39f, -0.88f), new Vector3(0.14f, 0.16f, 0.78f));
            builder.AddBox(new Vector3(0.38f, 2.39f, 0.88f), new Vector3(0.14f, 0.16f, 0.78f));
            return builder.Build();
        }

        private static Mesh _buildStatus()
        {
            var builder = new MeshBuilder("StatefulDoorFrameStatus");
            builder.AddBox(new Vector3(-0.355f, 0.08f, 0f), new Vector3(0.08f, 0.12f, 2.35f));
            builder.AddBox(new Vector3(0.355f, 0.08f, 0f), new Vector3(0.08f, 0.12f, 2.35f));
            for (var index = -1; index <= 1; index++)
            {
                builder.AddBox(new Vector3(-0.38f, 2.82f, index * 0.62f), new Vector3(0.1f, 0.13f, 0.32f));
                builder.AddBox(new Vector3(0.38f, 2.82f, index * 0.62f), new Vector3(0.1f, 0.13f, 0.32f));
            }

            return builder.Build();
        }

        private static Mesh _buildOpenGlyph()
        {
            var builder = new MeshBuilder("StatefulDoorOpenGlyph");
            builder.AddBox(new Vector3(-0.39f, 0.12f, -0.48f), new Vector3(0.07f, 0.08f, 0.72f), -38f);
            builder.AddBox(new Vector3(-0.39f, 0.12f, 0.48f), new Vector3(0.07f, 0.08f, 0.72f), 38f);
            builder.AddBox(new Vector3(0.39f, 0.12f, -0.48f), new Vector3(0.07f, 0.08f, 0.72f), -38f);
            builder.AddBox(new Vector3(0.39f, 0.12f, 0.48f), new Vector3(0.07f, 0.08f, 0.72f), 38f);
            return builder.Build();
        }

        private static void _ensurePrefab()
        {
            var root = new GameObject(KIT_NAME);
            try
            {
                var housing = _addPart(root.transform, "Frame Housing", HOUSING_MESH_PATH, BULKHEAD_MATERIAL_PATH);
                var mechanisms = _addPart(root.transform, "Tracks Pistons and Pockets", MECHANISM_MESH_PATH,
                    STEEL_MATERIAL_PATH);
                var status = _addPart(root.transform, "Threshold Seals and Warning Lamps", STATUS_MESH_PATH,
                    STATUS_MATERIAL_PATH);
                var openGlyph = _addPart(root.transform, "Open Route Glyph", OPEN_GLYPH_MESH_PATH,
                    STATUS_MATERIAL_PATH);
                root.AddComponent<AuthoredStatefulDoorFrame>().Configure(housing, mechanisms, status, openGlyph);
                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Renderer _addPart(Transform parent, string objectName, string meshPath, string materialPath)
        {
            var part = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(parent, false);
            part.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            return renderer;
        }

        private static void _installFrames(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var framePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                foreach (var readability in root.GetComponentsInChildren<AuthoredRouteDoorReadability>(true))
                {
                    var threshold = readability.transform.GetComponentInChildren<Renderer>(true);
                    var serialized = new SerializedObject(readability);
                    var thresholdProperty = serialized.FindProperty("m_thresholdRenderer");
                    threshold = thresholdProperty.objectReferenceValue as Renderer;
                    if (threshold == null)
                    {
                        throw new InvalidOperationException($"{readability.name} has no threshold renderer.");
                    }

                    var existing = threshold.transform.Find(KIT_NAME);
                    if (existing != null)
                    {
                        UnityEngine.Object.DestroyImmediate(existing.gameObject);
                    }

                    var instance = PrefabUtility.InstantiatePrefab(framePrefab, threshold.transform) as GameObject;
                    if (instance == null)
                    {
                        throw new InvalidOperationException($"Could not add the door-frame kit to {readability.name}.");
                    }

                    instance.name = KIT_NAME;
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    instance.transform.localScale = Vector3.one;
                    readability.ConfigureFrameKit(instance.GetComponent<AuthoredStatefulDoorFrame>());
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
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
