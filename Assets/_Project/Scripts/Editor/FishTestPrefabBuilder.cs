#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using VirtualFishing.Data;

namespace VirtualFishing.EditorTools
{
    public static class FishTestPrefabBuilder
    {
        private const string RootFolder = "Assets/_Project/Art/TestFish";
        private const string MaterialFolder = RootFolder + "/Materials";
        private const string PrefabFolder = RootFolder + "/Prefabs";
        private const string FishDbFolder = "Assets/_Project/SO/FishDB/Test";

        [MenuItem("VirtualFishing/Fish/Build Test Fish Prefabs")]
        public static void BuildTestFishPrefabs()
        {
            EnsureFolder("Assets/_Project/Art", "TestFish");
            EnsureFolder(RootFolder, "Materials");
            EnsureFolder(RootFolder, "Prefabs");

            BuildAndAssign(
                fishAssetName: "Fish_Crucian",
                materialName: "MAT_Fish_Crucian",
                prefabName: "PF_Fish_Crucian",
                bodyColor: new Color(0.82f, 0.67f, 0.32f),
                bodyScale: new Vector3(0.35f, 0.2f, 0.9f),
                tailScale: new Vector3(0.28f, 0.18f, 0.15f),
                dorsalScale: new Vector3(0.15f, 0.14f, 0.1f));

            BuildAndAssign(
                fishAssetName: "Fish_Bass",
                materialName: "MAT_Fish_Bass",
                prefabName: "PF_Fish_Bass",
                bodyColor: new Color(0.25f, 0.62f, 0.32f),
                bodyScale: new Vector3(0.4f, 0.24f, 1.05f),
                tailScale: new Vector3(0.32f, 0.2f, 0.16f),
                dorsalScale: new Vector3(0.18f, 0.16f, 0.1f));

            BuildAndAssign(
                fishAssetName: "Fish_Catfish",
                materialName: "MAT_Fish_Catfish",
                prefabName: "PF_Fish_Catfish",
                bodyColor: new Color(0.32f, 0.42f, 0.75f),
                bodyScale: new Vector3(0.45f, 0.28f, 1.25f),
                tailScale: new Vector3(0.26f, 0.16f, 0.14f),
                dorsalScale: new Vector3(0.14f, 0.12f, 0.08f),
                addWhiskers: true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FishTestPrefabBuilder] Test fish prefabs and materials created/updated.");
        }

        private static void BuildAndAssign(
            string fishAssetName,
            string materialName,
            string prefabName,
            Color bodyColor,
            Vector3 bodyScale,
            Vector3 tailScale,
            Vector3 dorsalScale,
            bool addWhiskers = false)
        {
            Material material = CreateOrUpdateMaterial(materialName, bodyColor);
            GameObject prefabRoot = BuildFishModel(prefabName, material, bodyScale, tailScale, dorsalScale, addWhiskers);

            string prefabPath = $"{PrefabFolder}/{prefabName}.prefab";
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            Object.DestroyImmediate(prefabRoot);

            FishSpeciesDataSO speciesData =
                AssetDatabase.LoadAssetAtPath<FishSpeciesDataSO>($"{FishDbFolder}/{fishAssetName}.asset");

            if (speciesData != null)
            {
                SerializedObject serializedObject = new SerializedObject(speciesData);
                serializedObject.FindProperty("fishPrefab").objectReferenceValue = prefabAsset;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(speciesData);
            }
            else
            {
                Debug.LogWarning($"[FishTestPrefabBuilder] Could not find species asset: {fishAssetName}");
            }
        }

        private static Material CreateOrUpdateMaterial(string materialName, Color color)
        {
            string materialPath = $"{MaterialFolder}/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject BuildFishModel(
            string prefabName,
            Material material,
            Vector3 bodyScale,
            Vector3 tailScale,
            Vector3 dorsalScale,
            bool addWhiskers)
        {
            GameObject root = new GameObject(prefabName);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = bodyScale;
            SetupRenderer(body, material);

            GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tail.name = "Tail";
            tail.transform.SetParent(root.transform);
            tail.transform.localPosition = new Vector3(0f, 0f, -bodyScale.z * 0.62f);
            tail.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            tail.transform.localScale = tailScale;
            SetupRenderer(tail, material);

            GameObject finTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            finTop.name = "DorsalFin";
            finTop.transform.SetParent(root.transform);
            finTop.transform.localPosition = new Vector3(0f, bodyScale.y * 0.52f, -0.05f);
            finTop.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
            finTop.transform.localScale = dorsalScale;
            SetupRenderer(finTop, material);

            GameObject eyeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeL.name = "Eye_L";
            eyeL.transform.SetParent(root.transform);
            eyeL.transform.localPosition = new Vector3(-bodyScale.x * 0.3f, bodyScale.y * 0.1f, bodyScale.z * 0.34f);
            eyeL.transform.localScale = Vector3.one * 0.08f;
            SetupRenderer(eyeL, CreateEyeMaterial());

            GameObject eyeR = Object.Instantiate(eyeL, root.transform);
            eyeR.name = "Eye_R";
            eyeR.transform.localPosition = new Vector3(bodyScale.x * 0.3f, bodyScale.y * 0.1f, bodyScale.z * 0.34f);

            if (addWhiskers)
            {
                CreateWhisker(root.transform, new Vector3(-0.12f, -0.05f, bodyScale.z * 0.42f), -25f);
                CreateWhisker(root.transform, new Vector3(0.12f, -0.05f, bodyScale.z * 0.42f), 25f);
            }

            RemoveColliderRecursive(root);
            return root;
        }

        private static void CreateWhisker(Transform parent, Vector3 localPosition, float zAngle)
        {
            GameObject whisker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            whisker.name = "Whisker";
            whisker.transform.SetParent(parent);
            whisker.transform.localPosition = localPosition;
            whisker.transform.localRotation = Quaternion.Euler(90f, 0f, zAngle);
            whisker.transform.localScale = new Vector3(0.01f, 0.18f, 0.01f);
            SetupRenderer(whisker, CreateEyeMaterial());
        }

        private static void SetupRenderer(GameObject target, Material material)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void RemoveColliderRecursive(GameObject root)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static Material CreateEyeMaterial()
        {
            const string eyeMaterialPath = MaterialFolder + "/MAT_Fish_Eye.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(eyeMaterialPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            material.color = Color.black;
            AssetDatabase.CreateAsset(material, eyeMaterialPath);
            return material;
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
