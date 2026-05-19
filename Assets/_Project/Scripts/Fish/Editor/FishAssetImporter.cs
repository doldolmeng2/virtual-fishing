#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using VirtualFishing.Data;

namespace VirtualFishing.EditorTools
{
    public static class FishAssetImporter
    {
        private const string SourceRoot = "Assets/Floreswa";
        private const string TargetRoot = "Assets/Art/Fishes/Floreswa";
        private const string FishDbRoot = "Assets/_Project/SO/FishDB/Test";

        [MenuItem("VirtualFishing/Fish/Import Floreswa Fish Assets")]
        public static void ImportFloreswaFishAssets()
        {
            if (!AssetDatabase.IsValidFolder(SourceRoot))
            {
                Debug.LogWarning($"[FishAssetImporter] Source folder not found: {SourceRoot}");
                return;
            }

            EnsureFolder("Assets/Art", "Fishes");
            EnsureFolder("Assets/Art/Fishes", "Floreswa");
            EnsureFolder(TargetRoot, "Materials");
            EnsureFolder(TargetRoot, "Models");
            EnsureFolder(TargetRoot, "Prefabs");

            CopyAssetIfNeeded($"{SourceRoot}/Materials/Sea.mat", $"{TargetRoot}/Materials/Sea.mat");

            CopyFishAssetSet("fish01");
            CopyFishAssetSet("fish02");
            CopyFishAssetSet("fish03");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AssignFishPrefab("Fish_Crucian", $"{TargetRoot}/Prefabs/fish01.prefab");
            AssignFishPrefab("Fish_Bass", $"{TargetRoot}/Prefabs/fish02.prefab");
            AssignFishPrefab("Fish_Catfish", $"{TargetRoot}/Prefabs/fish03.prefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[FishAssetImporter] Floreswa fish assets copied to Assets/Art/Fishes/Floreswa and assigned to fish species data.");
        }

        private static void CopyFishAssetSet(string fishName)
        {
            CopyAssetIfNeeded($"{SourceRoot}/Models/{fishName}.fbx", $"{TargetRoot}/Models/{fishName}.fbx");
            CopyAssetIfNeeded($"{SourceRoot}/Models/{fishName}_shade.fbx", $"{TargetRoot}/Models/{fishName}_shade.fbx");
            CopyAssetIfNeeded($"{SourceRoot}/Prefabs/{fishName}.prefab", $"{TargetRoot}/Prefabs/{fishName}.prefab");
            CopyAssetIfNeeded($"{SourceRoot}/Prefabs/{fishName}_shade.prefab", $"{TargetRoot}/Prefabs/{fishName}_shade.prefab");
        }

        private static void CopyAssetIfNeeded(string sourcePath, string targetPath)
        {
            if (!File.Exists(sourcePath))
            {
                Debug.LogWarning($"[FishAssetImporter] Missing source asset: {sourcePath}");
                return;
            }

            if (File.Exists(targetPath))
            {
                return;
            }

            string targetDirectory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
            {
                Debug.LogWarning($"[FishAssetImporter] Failed to copy asset: {sourcePath} -> {targetPath}");
            }
        }

        private static void AssignFishPrefab(string speciesAssetName, string prefabPath)
        {
            FishSpeciesDataSO speciesData =
                AssetDatabase.LoadAssetAtPath<FishSpeciesDataSO>($"{FishDbRoot}/{speciesAssetName}.asset");
            GameObject fishPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (speciesData == null)
            {
                Debug.LogWarning($"[FishAssetImporter] Missing species asset: {speciesAssetName}");
                return;
            }

            if (fishPrefab == null)
            {
                Debug.LogWarning($"[FishAssetImporter] Missing copied fish prefab: {prefabPath}");
                return;
            }

            SerializedObject serializedObject = new(speciesData);
            SerializedProperty fishPrefabProperty = serializedObject.FindProperty("fishPrefab");
            fishPrefabProperty.objectReferenceValue = fishPrefab;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(speciesData);
        }

        private static void EnsureFolder(string parentFolder, string folderName)
        {
            string targetFolder = $"{parentFolder}/{folderName}";
            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }
    }
}
#endif
