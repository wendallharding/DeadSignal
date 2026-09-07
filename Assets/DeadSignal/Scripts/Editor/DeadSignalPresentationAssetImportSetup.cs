using System;
using UnityEditor;

namespace DeadSignal.Editor
{
    public static class DeadSignalPresentationAssetImportSetup
    {
        private static readonly string[] s_staticModelPaths =
        {
            "Assets/DeadSignal/Resources/Environment/ArcFurnaceModel.fbx",
            "Assets/DeadSignal/Resources/Environment/ConvergenceBusbarModel.fbx",
            "Assets/DeadSignal/Resources/Environment/QuenchCondenserModel.fbx",
            "Assets/DeadSignal/Resources/Environment/RelayFoundryTurbineModel.fbx"
        };

        [MenuItem("DEAD SIGNAL/Setup/Normalize Presentation Model Imports")]
        public static void EnsureAssets()
        {
            foreach (var path in s_staticModelPaths)
            {
                ConfigureStaticModel(path, "presentation");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void ConfigureStaticModel(string path, string label)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the {label} model at {path}.");
            }

            importer.addCollider = false;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.meshCompression = ModelImporterMeshCompression.Low;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.SaveAndReimport();
        }
    }
}
