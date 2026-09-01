using System;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalSecurityLockdownReadabilitySetup
    {
        private const string REGION_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SecurityTrialWingRegion.prefab";
        private const string TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/SecurityTrialLockdownStatusAtlas.png";
        private const string CHAMBER_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/SecurityTrialLockdownStatusReadability.asset";
        private const string DOOR_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/SecurityTrialDoorStatusReadability.asset";
        private const string CAPACITOR_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/SecurityTrialCapacitorStatusReadability.asset";
        private const string MATERIAL_FOLDER = "Assets/DeadSignal/Resources/Materials/SecurityTrialReadability";
        private const string MATERIAL_PATH = MATERIAL_FOLDER + "/SecurityTrialLockdownStatus.mat";

        public static bool HasAssets
        {
            get
            {
                var region = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(CHAMBER_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(DOOR_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(CAPACITOR_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH) != null &&
                       region?.GetComponent<AuthoredCombatChamber>() is
                       { IsComplete: true, HasLockdownReadabilityAssets: true };
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Security Trial Lockdown Readability")]
        public static void EnsureAssets()
        {
            _configureTexture();
            var material = _ensureMaterial();
            var chamberMesh = _ensureOctagonalMesh(
                CHAMBER_MESH_PATH,
                "SecurityTrialLockdownStatusReadability",
                0f,
                1f / 3f);
            var doorMesh = _ensureDoorMesh();
            var capacitorMesh = _ensureOctagonalMesh(
                CAPACITOR_MESH_PATH,
                "SecurityTrialCapacitorStatusReadability",
                2f / 3f,
                1f);
            _upgradeRegion(chamberMesh, doorMesh, capacitorMesh, material);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The Security Trial lockdown readability assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Security Trial atlas at {TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static Material _ensureMaterial()
        {
            if (!AssetDatabase.IsValidFolder(MATERIAL_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "SecurityTrialReadability");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for Security Trial readability.");
                }

                material = new Material(shader) { name = "SecurityTrialLockdownStatus" };
                AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_EmissionColor", new Color(0.92f, 0.035f, 0.08f));
            material.SetFloat("_Metallic", 0.34f);
            material.SetFloat("_Smoothness", 0.48f);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.08f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_EMISSION");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh _ensureOctagonalMesh(string path, string name, float minimumU, float maximumU)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var createAsset = mesh == null;
            if (createAsset)
            {
                mesh = new Mesh { name = name };
            }

            mesh.vertices = new[]
            {
                new Vector3(-0.72f, 0f, -0.5f), new Vector3(-0.5f, 0f, -0.72f),
                new Vector3(0.5f, 0f, -0.72f), new Vector3(0.72f, 0f, -0.5f),
                new Vector3(0.72f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.72f),
                new Vector3(-0.5f, 0f, 0.72f), new Vector3(-0.72f, 0f, 0.5f)
            };
            var middleU = (minimumU + maximumU) * 0.5f;
            var halfU = (maximumU - minimumU) * 0.5f;
            mesh.uv = new[]
            {
                new Vector2(minimumU, 0.15f), new Vector2(middleU - halfU * 0.7f, 0f),
                new Vector2(middleU + halfU * 0.7f, 0f), new Vector2(maximumU, 0.15f),
                new Vector2(maximumU, 0.85f), new Vector2(middleU + halfU * 0.7f, 1f),
                new Vector2(middleU - halfU * 0.7f, 1f), new Vector2(minimumU, 0.85f)
            };
            mesh.triangles = new[] { 0, 7, 6, 0, 6, 1, 1, 6, 2, 2, 6, 5, 2, 5, 3, 3, 5, 4 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (createAsset)
            {
                AssetDatabase.CreateAsset(mesh, path);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }

            return mesh;
        }

        private static Mesh _ensureDoorMesh()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(DOOR_MESH_PATH);
            var createAsset = mesh == null;
            if (createAsset)
            {
                mesh = new Mesh { name = "SecurityTrialDoorStatusReadability" };
            }

            mesh.vertices = new[]
            {
                new Vector3(-0.82f, 0f, -0.58f), new Vector3(0.82f, 0f, -0.58f),
                new Vector3(0.82f, 0f, 0.58f), new Vector3(-0.82f, 0f, 0.58f)
            };
            mesh.uv = new[]
            {
                new Vector2(1f / 3f, 0f), new Vector2(2f / 3f, 0f),
                new Vector2(2f / 3f, 1f), new Vector2(1f / 3f, 1f)
            };
            mesh.triangles = new[] { 0, 3, 2, 0, 2, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (createAsset)
            {
                AssetDatabase.CreateAsset(mesh, DOOR_MESH_PATH);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }

            return mesh;
        }

        private static void _upgradeRegion(Mesh chamberMesh, Mesh doorMesh, Mesh capacitorMesh, Material material)
        {
            var root = PrefabUtility.LoadPrefabContents(REGION_PREFAB_PATH);
            try
            {
                var chamber = root.GetComponent<AuthoredCombatChamber>();
                var arena = root.transform.Find("Lockdown Arena");
                var vault = root.transform.Find("Reward Vault");
                var entryDoor = root.transform.Find("Lockdown Entry Door");
                var rewardDoor = root.transform.Find("Reward Vault Door");
                if (chamber == null || arena == null || vault == null || entryDoor == null || rewardDoor == null)
                {
                    throw new InvalidOperationException("The Security Trial chamber, arena, vault, or doors are missing.");
                }

                var chamberGlyph = _ensureGlyph(
                    arena,
                    "Lockdown Chamber Status",
                    chamberMesh,
                    material,
                    new Vector3(0f, 0.02f, 0f),
                    new Vector3(4.2f, 1f, 4.2f));
                var capacitorGlyph = _ensureGlyph(
                    vault,
                    "Capacitor Vault Status",
                    capacitorMesh,
                    material,
                    new Vector3(0f, 0.03f, 0f),
                    new Vector3(1.8f, 1f, 1.8f));
                var entryReadability = _upgradeDoor(entryDoor, "Entry", doorMesh, material);
                var rewardReadability = _upgradeDoor(rewardDoor, "Reward", doorMesh, material);
                chamber.ConfigureLockdownReadability(
                    chamberGlyph.GetComponent<Renderer>(),
                    chamberGlyph,
                    entryReadability,
                    rewardReadability,
                    capacitorGlyph.GetComponent<Renderer>(),
                    capacitorGlyph);
                PrefabUtility.SaveAsPrefabAsset(root, REGION_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform _ensureGlyph(
            Transform parent,
            string name,
            Mesh mesh,
            Material material,
            Vector3 position,
            Vector3 scale)
        {
            var glyph = parent.Find(name);
            if (glyph == null)
            {
                glyph = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)).transform;
                glyph.SetParent(parent, false);
            }

            glyph.localPosition = position;
            glyph.localRotation = Quaternion.identity;
            glyph.localScale = scale;
            glyph.GetComponent<MeshFilter>().sharedMesh = mesh;
            glyph.GetComponent<MeshRenderer>().sharedMaterial = material;
            foreach (var collider in glyph.GetComponents<Collider>())
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return glyph;
        }

        private static AuthoredRouteDoorReadability _upgradeDoor(
            Transform door,
            string prefix,
            Mesh mesh,
            Material material)
        {
            var slab = door.Find(prefix + " Door Slab")?.gameObject;
            if (slab == null)
            {
                throw new InvalidOperationException($"The Security Trial {prefix.ToLowerInvariant()} door slab is missing.");
            }

            var threshold = _ensureGlyph(
                door,
                prefix + " Threshold Readability",
                mesh,
                material,
                new Vector3(0f, -0.08f, slab.transform.localPosition.z),
                new Vector3(2.35f, 1f, 1.55f));
            threshold.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var openMarker = door.Find(prefix + " Door Open");
            if (openMarker == null)
            {
                openMarker = new GameObject(prefix + " Door Open").transform;
                openMarker.SetParent(door, false);
            }

            openMarker.localPosition = threshold.localPosition;
            var readability = door.GetComponent<AuthoredRouteDoorReadability>();
            if (readability == null)
            {
                readability = door.gameObject.AddComponent<AuthoredRouteDoorReadability>();
            }

            readability.Configure(slab, openMarker.gameObject, threshold.GetComponent<Renderer>());
            return readability;
        }
    }
}
