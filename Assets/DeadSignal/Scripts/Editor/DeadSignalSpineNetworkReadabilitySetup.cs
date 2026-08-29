using System;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalSpineNetworkReadabilitySetup
    {
        private const string SPINE_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/CapacitorSpineRegion.prefab";
        private const string TRENCH_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineDischargeTrenchRegion.prefab";
        private const string PRESSURE_TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineDischargeTrenchRouteDecal.png";
        private const string TOWER_TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/CapacitorSpineActivationDecal.png";
        private const string PRESSURE_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/SpineDischargeTrench/SpinePressureStatus.mat";
        private const string TOWER_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/CapacitorSpine/SpineTowerStatus.mat";
        private const string CONSOLE_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/SpinePressureConsoleReadability.asset";
        private const string SELECTOR_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/SpinePressureSelectorReadability.asset";

        public static bool HasAssets
        {
            get
            {
                var trench = AssetDatabase.LoadAssetAtPath<GameObject>(TRENCH_PREFAB_PATH);
                var spine = AssetDatabase.LoadAssetAtPath<GameObject>(SPINE_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Mesh>(CONSOLE_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(SELECTOR_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(PRESSURE_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(TOWER_MATERIAL_PATH) != null &&
                       trench != null &&
                       trench.GetComponent<AuthoredSpineVentingObjective>()?.HasReadabilityAssets == true &&
                       trench.transform.Find("Spine Berth Discharge Control/Pressure Status Console") != null &&
                       spine != null &&
                       spine.GetComponent<AuthoredSpineTowerReadability>()?.IsConfigured == true &&
                       spine.transform.Find("Spine Tower Status Console") != null;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Spine Network Readability")]
        public static void EnsureAssets()
        {
            var consoleMesh = _ensureConsoleMesh();
            var selectorMesh = _ensureSelectorMesh();
            var pressureMaterial = _ensureMaterial(
                PRESSURE_MATERIAL_PATH, PRESSURE_TEXTURE_PATH, "SpinePressureStatus");
            var towerMaterial = _ensureMaterial(
                TOWER_MATERIAL_PATH, TOWER_TEXTURE_PATH, "SpineTowerStatus");
            _upgradeTrench(consoleMesh, selectorMesh, pressureMaterial);
            _upgradeSpine(consoleMesh, selectorMesh, towerMaterial);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The Spine network readability assets are incomplete.");
            }
        }

        private static Mesh _ensureConsoleMesh()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(CONSOLE_MESH_PATH);
            var createAsset = mesh == null;
            if (createAsset)
            {
                mesh = new Mesh { name = "SpinePressureConsoleReadability" };
            }

            mesh.vertices = new[]
            {
                new Vector3(-0.6f, 0f, -0.42f), new Vector3(-0.42f, 0f, -0.6f),
                new Vector3(0.42f, 0f, -0.6f), new Vector3(0.6f, 0f, -0.42f),
                new Vector3(0.6f, 0f, 0.42f), new Vector3(0.42f, 0f, 0.6f),
                new Vector3(-0.42f, 0f, 0.6f), new Vector3(-0.6f, 0f, 0.42f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0.15f), new Vector2(0.15f, 0f), new Vector2(0.85f, 0f), new Vector2(1f, 0.15f),
                new Vector2(1f, 0.85f), new Vector2(0.85f, 1f), new Vector2(0.15f, 1f), new Vector2(0f, 0.85f)
            };
            mesh.triangles = new[]
            {
                0, 7, 6, 0, 6, 1, 1, 6, 2, 2, 6, 5, 2, 5, 3, 3, 5, 4
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (createAsset)
            {
                AssetDatabase.CreateAsset(mesh, CONSOLE_MESH_PATH);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }

            return mesh;
        }

        private static Mesh _ensureSelectorMesh()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SELECTOR_MESH_PATH);
            var createAsset = mesh == null;
            if (createAsset)
            {
                mesh = new Mesh { name = "SpinePressureSelectorReadability" };
            }

            mesh.vertices = new[]
            {
                new Vector3(-0.09f, 0f, -0.36f), new Vector3(0.09f, 0f, -0.36f),
                new Vector3(0.09f, 0f, 0.16f), new Vector3(0.22f, 0f, 0.16f),
                new Vector3(0f, 0f, 0.4f), new Vector3(-0.22f, 0f, 0.16f),
                new Vector3(-0.09f, 0f, 0.16f)
            };
            mesh.uv = new[]
            {
                new Vector2(0.38f, 0f), new Vector2(0.62f, 0f), new Vector2(0.62f, 0.64f),
                new Vector2(0.82f, 0.64f), new Vector2(0.5f, 1f), new Vector2(0.18f, 0.64f),
                new Vector2(0.38f, 0.64f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 6, 2, 6, 3, 2, 6, 5, 3, 5, 4, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (createAsset)
            {
                AssetDatabase.CreateAsset(mesh, SELECTOR_MESH_PATH);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }

            return mesh;
        }

        private static Material _ensureMaterial(string materialPath, string texturePath, string materialName)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for Spine readability.");
                }

                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_EmissionColor", new Color(1f, 0.46f, 0.055f));
            material.SetFloat("_Metallic", 0.42f);
            material.SetFloat("_Smoothness", 0.38f);
            material.EnableKeyword("_EMISSION");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void _upgradeTrench(Mesh consoleMesh, Mesh selectorMesh, Material material)
        {
            var root = PrefabUtility.LoadPrefabContents(TRENCH_PREFAB_PATH);
            try
            {
                var objective = root.GetComponent<AuthoredSpineVentingObjective>();
                var control = root.transform.Find("Spine Berth Discharge Control");
                if (objective == null || control == null)
                {
                    throw new InvalidOperationException("The Spine discharge control authority is missing.");
                }

                var console = _ensureMeshPart(
                    control, "Pressure Status Console", new Vector3(0f, 0.035f, 0f), consoleMesh, material);
                console.localScale = new Vector3(1.5f, 1f, 1.15f);
                var selector = _ensureMeshPart(
                    control, "Pressure Selector", new Vector3(0f, 0.055f, 0f), selectorMesh, material);
                selector.localScale = new Vector3(0.9f, 1f, 0.9f);
                objective.ConfigureReadability(
                    new[] { console.GetComponent<Renderer>(), selector.GetComponent<Renderer>() }, selector);
                PrefabUtility.SaveAsPrefabAsset(root, TRENCH_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _upgradeSpine(Mesh consoleMesh, Mesh selectorMesh, Material material)
        {
            var root = PrefabUtility.LoadPrefabContents(SPINE_PREFAB_PATH);
            try
            {
                var console = _ensureMeshPart(
                    root.transform, "Spine Tower Status Console", new Vector3(5f, 0.035f, -2.05f),
                    consoleMesh, material);
                console.localScale = new Vector3(1.65f, 1f, 1.25f);
                var selector = _ensureMeshPart(
                    root.transform, "Spine Tower Network Selector", new Vector3(5f, 0.055f, -2.05f),
                    selectorMesh, material);
                selector.localScale = Vector3.one;
                var towerCore = root.transform.Find("Third Tower Berth/Tower Core")?.GetComponent<Renderer>();
                if (towerCore == null)
                {
                    throw new InvalidOperationException("The Spine Tower core presentation is missing.");
                }

                var readability = root.GetComponent<AuthoredSpineTowerReadability>() ??
                                  root.AddComponent<AuthoredSpineTowerReadability>();
                readability.Configure(
                    new[] { towerCore, console.GetComponent<Renderer>(), selector.GetComponent<Renderer>() }, selector);
                PrefabUtility.SaveAsPrefabAsset(root, SPINE_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform _ensureMeshPart(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Mesh mesh,
            Material material)
        {
            var part = parent.Find(objectName);
            if (part == null)
            {
                part = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer)).transform;
                part.SetParent(parent, false);
            }

            part.localPosition = localPosition;
            part.localRotation = Quaternion.identity;
            part.localScale = Vector3.one;
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            foreach (var collider in part.GetComponents<Collider>())
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return part;
        }
    }
}
