using System;
using System.Collections.Generic;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSignal.Editor
{
    public static class DeadSignalStationNavigationSignageSetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/StationNavigationSignageKit.prefab";
        private const string SECTOR_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/StationSectorSymbols.asset";
        private const string HAZARD_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/StationHazardBands.asset";
        private const string CHEVRON_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/StationDirectionalChevrons.asset";
        private const string IDENTIFIER_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/StationRoomIdentifiers.asset";
        private const string RETURN_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/StationPoweredReturnDecals.asset";
        private const string WHITE_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/NavigationSignageWhite.mat";
        private const string AMBER_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/NavigationSignageAmber.mat";
        private const string CYAN_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/NavigationSignageCyan.mat";
        private const string ENVIRONMENT_PATH = "DEAD SIGNAL — Authored World/Environment";
        private const string KIT_NAME = "Station Navigation Signage Kit";

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                var signage = prefab != null ? prefab.GetComponent<AuthoredStationNavigationSignage>() : null;
                return AssetDatabase.LoadAssetAtPath<Mesh>(SECTOR_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(HAZARD_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(CHEVRON_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(IDENTIFIER_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(RETURN_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(WHITE_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(AMBER_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(CYAN_MATERIAL_PATH) != null &&
                       signage != null && signage.IsConfigured &&
                       prefab.GetComponentsInChildren<Collider>(true).Length == 0;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Station Navigation Signage Kit")]
        public static void EnsureAssets()
        {
            _ensureMesh(SECTOR_MESH_PATH, _buildSectorSymbols());
            _ensureMesh(HAZARD_MESH_PATH, _buildHazardBands());
            _ensureMesh(CHEVRON_MESH_PATH, _buildDirectionalChevrons());
            _ensureMesh(IDENTIFIER_MESH_PATH, _buildRoomIdentifiers());
            _ensureMesh(RETURN_MESH_PATH, _buildPoweredReturnDecals());
            _ensureMaterial(WHITE_MATERIAL_PATH, "NavigationSignageWhite", new Color(0.3f, 0.38f, 0.42f));
            _ensureMaterial(AMBER_MATERIAL_PATH, "NavigationSignageAmber", new Color(0.56f, 0.19f, 0.025f));
            _ensureMaterial(CYAN_MATERIAL_PATH, "NavigationSignageCyan", new Color(0.015f, 0.31f, 0.38f));
            _ensurePrefab();
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The authored station navigation-signage kit is incomplete.");
            }
        }

        private static Mesh _buildSectorSymbols()
        {
            var builder = new MeshBuilder("StationSectorSymbols");
            _addCentralSymbol(builder, new Vector2(0f, 5.2f));
            _addRelaySymbol(builder, new Vector2(24f, 4.8f));
            _addSpineSymbol(builder, new Vector2(42.5f, 14.2f));
            _addCoreSymbol(builder, new Vector2(42.5f, 34.2f));
            _addTrialSymbol(builder, new Vector2(42.5f, 50.2f));
            _addDockSymbol(builder, new Vector2(-10.2f, -2.2f));
            return builder.Build();
        }

        private static Mesh _buildHazardBands()
        {
            var builder = new MeshBuilder("StationHazardBands");
            _addBand(builder, new Vector2(42.5f, 46.7f), 7.2f, 0f);
            _addBand(builder, new Vector2(42.5f, 60.2f), 8.4f, 0f);
            _addBand(builder, new Vector2(42.5f, 72.4f), 6.8f, 0f);
            _addBand(builder, new Vector2(50.8f, 39.4f), 5.4f, 90f);
            return builder.Build();
        }

        private static Mesh _buildDirectionalChevrons()
        {
            var builder = new MeshBuilder("StationDirectionalChevrons");
            _addChevron(builder, new Vector2(-8.4f, -1.2f), 0f);
            _addChevron(builder, new Vector2(6.2f, 0.4f), 0f);
            _addChevron(builder, new Vector2(15.2f, 0.4f), 0f);
            _addChevron(builder, new Vector2(30.5f, 8.2f), 0f);
            _addChevron(builder, new Vector2(42.5f, 15.4f), -90f);
            _addChevron(builder, new Vector2(42.5f, 25.5f), -90f);
            _addChevron(builder, new Vector2(34.8f, 30.2f), 180f);
            _addChevron(builder, new Vector2(49.8f, 30.2f), 0f);
            _addChevron(builder, new Vector2(42.5f, 41.8f), -90f);
            _addChevron(builder, new Vector2(42.5f, 49.1f), -90f);
            return builder.Build();
        }

        private static Mesh _buildRoomIdentifiers()
        {
            var builder = new MeshBuilder("StationRoomIdentifiers");
            _addIdentifier(builder, new Vector2(-5.4f, 7.4f), 1, 0f);
            _addIdentifier(builder, new Vector2(5.6f, 7.4f), 2, 0f);
            _addIdentifier(builder, new Vector2(10.8f, 2.2f), 3, 0f);
            _addIdentifier(builder, new Vector2(29.2f, 2.2f), 1, 0f);
            _addIdentifier(builder, new Vector2(42.5f, 10.8f), 2, 0f);
            _addIdentifier(builder, new Vector2(42.5f, 30.2f), 3, 90f);
            _addIdentifier(builder, new Vector2(42.5f, 44.2f), 4, 90f);
            _addIdentifier(builder, new Vector2(42.5f, 55.8f), 1, 90f);
            _addIdentifier(builder, new Vector2(42.5f, 67.2f), 2, 90f);
            _addIdentifier(builder, new Vector2(42.5f, 76.3f), 3, 90f);
            return builder.Build();
        }

        private static Mesh _buildPoweredReturnDecals()
        {
            var builder = new MeshBuilder("StationPoweredReturnDecals");
            _addReturnGlyph(builder, new Vector2(42.5f, 43.2f), 90f);
            _addReturnGlyph(builder, new Vector2(42.5f, 34.2f), 90f);
            _addReturnGlyph(builder, new Vector2(42.5f, 22.5f), 90f);
            _addReturnGlyph(builder, new Vector2(31.4f, 8.9f), 180f);
            _addReturnGlyph(builder, new Vector2(17f, 1.3f), 180f);
            _addReturnGlyph(builder, new Vector2(4.8f, -0.7f), 180f);
            _addReturnGlyph(builder, new Vector2(-7.2f, -2f), 180f);
            return builder.Build();
        }

        private static void _addCentralSymbol(MeshBuilder builder, Vector2 center)
        {
            builder.AddBox(_position(center, 0.035f), new Vector3(2.4f, 0.045f, 0.38f));
            builder.AddBox(_position(center, 0.035f), new Vector3(0.38f, 0.045f, 2.4f));
        }

        private static void _addRelaySymbol(MeshBuilder builder, Vector2 center)
        {
            builder.AddBox(_position(center, 0.035f), new Vector3(2.3f, 0.045f, 0.32f));
            builder.AddBox(_position(center + new Vector2(0.55f, 0.55f), 0.035f), new Vector3(1.2f, 0.045f, 0.32f), -42f);
            builder.AddBox(_position(center + new Vector2(0.55f, -0.55f), 0.035f), new Vector3(1.2f, 0.045f, 0.32f), 42f);
        }

        private static void _addSpineSymbol(MeshBuilder builder, Vector2 center)
        {
            for (var index = -1; index <= 1; index++)
            {
                builder.AddBox(_position(center + new Vector2(index * 0.58f, 0f), 0.035f),
                    new Vector3(0.28f, 0.045f, 2.5f));
            }
        }

        private static void _addCoreSymbol(MeshBuilder builder, Vector2 center)
        {
            builder.AddBox(_position(center + new Vector2(-0.55f, 0f), 0.035f), new Vector3(1.5f, 0.045f, 0.3f), 45f);
            builder.AddBox(_position(center + new Vector2(0.55f, 0f), 0.035f), new Vector3(1.5f, 0.045f, 0.3f), -45f);
            builder.AddBox(_position(center + new Vector2(0f, 0.55f), 0.035f), new Vector3(1.5f, 0.045f, 0.3f), -45f);
            builder.AddBox(_position(center + new Vector2(0f, -0.55f), 0.035f), new Vector3(1.5f, 0.045f, 0.3f), 45f);
        }

        private static void _addTrialSymbol(MeshBuilder builder, Vector2 center)
        {
            builder.AddBox(_position(center + new Vector2(-0.72f, 0f), 0.035f), new Vector3(0.3f, 0.045f, 2.35f));
            builder.AddBox(_position(center, 0.035f), new Vector3(0.3f, 0.045f, 2.35f));
            builder.AddBox(_position(center + new Vector2(0.72f, 0f), 0.035f), new Vector3(0.3f, 0.045f, 2.35f));
        }

        private static void _addDockSymbol(MeshBuilder builder, Vector2 center)
        {
            builder.AddBox(_position(center + new Vector2(-0.75f, 0f), 0.035f), new Vector3(0.3f, 0.045f, 2f));
            builder.AddBox(_position(center + new Vector2(0.75f, 0f), 0.035f), new Vector3(0.3f, 0.045f, 2f));
            builder.AddBox(_position(center + new Vector2(0f, -0.85f), 0.035f), new Vector3(1.75f, 0.045f, 0.3f));
        }

        private static void _addBand(MeshBuilder builder, Vector2 center, float width, float rotationY)
        {
            for (var index = -3; index <= 3; index++)
            {
                var local = new Vector2(index * width / 7f, 0f);
                var rotation = Quaternion.Euler(0f, rotationY, 0f);
                var offset = rotation * new Vector3(local.x, 0f, local.y);
                builder.AddBox(_position(center + new Vector2(offset.x, offset.z), 0.04f),
                    new Vector3(0.62f, 0.05f, 0.22f), rotationY + 34f);
            }
        }

        private static void _addChevron(MeshBuilder builder, Vector2 center, float rotationY)
        {
            var rotation = Quaternion.Euler(0f, rotationY, 0f);
            var left = rotation * new Vector3(-0.35f, 0f, -0.32f);
            var right = rotation * new Vector3(-0.35f, 0f, 0.32f);
            builder.AddBox(_position(center + new Vector2(left.x, left.z), 0.045f),
                new Vector3(0.9f, 0.055f, 0.24f), rotationY - 42f);
            builder.AddBox(_position(center + new Vector2(right.x, right.z), 0.045f),
                new Vector3(0.9f, 0.055f, 0.24f), rotationY + 42f);
        }

        private static void _addIdentifier(MeshBuilder builder, Vector2 center, int count, float rotationY)
        {
            var rotation = Quaternion.Euler(0f, rotationY, 0f);
            for (var index = 0; index < count; index++)
            {
                var offset = rotation * new Vector3((index - (count - 1) * 0.5f) * 0.38f, 0f, 0f);
                builder.AddBox(_position(center + new Vector2(offset.x, offset.z), 0.04f),
                    new Vector3(0.2f, 0.05f, 0.65f), rotationY);
            }
        }

        private static void _addReturnGlyph(MeshBuilder builder, Vector2 center, float rotationY)
        {
            _addChevron(builder, center, rotationY);
            var rotation = Quaternion.Euler(0f, rotationY, 0f);
            var tail = rotation * new Vector3(0.62f, 0f, 0f);
            builder.AddBox(_position(center + new Vector2(tail.x, tail.z), 0.035f),
                new Vector3(1.3f, 0.045f, 0.16f), rotationY);
        }

        private static void _ensurePrefab()
        {
            var root = new GameObject(KIT_NAME);
            try
            {
                var layers = new[]
                {
                    _addLayer(root.transform, "Sector Symbols", SECTOR_MESH_PATH, WHITE_MATERIAL_PATH),
                    _addLayer(root.transform, "Hazard Bands", HAZARD_MESH_PATH, AMBER_MATERIAL_PATH),
                    _addLayer(root.transform, "Directional Chevrons", CHEVRON_MESH_PATH, WHITE_MATERIAL_PATH),
                    _addLayer(root.transform, "Room Identifiers", IDENTIFIER_MESH_PATH, WHITE_MATERIAL_PATH),
                    _addLayer(root.transform, "Powered Return Decals", RETURN_MESH_PATH, CYAN_MATERIAL_PATH)
                };
                root.AddComponent<AuthoredStationNavigationSignage>().Configure(layers);
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
            renderer.receiveShadows = false;
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

            var existing = environment.transform.Find(KIT_NAME);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            var instance = PrefabUtility.InstantiatePrefab(prefab, environment.transform) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("Could not place the station navigation-signage kit in SampleScene.");
            }

            instance.name = KIT_NAME;
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

        private static void _ensureMaterial(string path, string materialName, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Unlit shader for station signage.");
                }

                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
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
