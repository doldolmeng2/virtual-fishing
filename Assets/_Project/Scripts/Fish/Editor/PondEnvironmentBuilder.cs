#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VirtualFishing.Data;

namespace VirtualFishing.EditorTools
{
    public static class PondEnvironmentBuilder
    {
        private const string ArtEnvironmentRoot = "Assets/Art/Environment";
        private const string ArtWaterFolder = "Assets/Art/Environment/Water";
        private const string ArtPondModelFolder = "Assets/Art/Environment/Pond/Models";
        private const string ArtPondTextureFolder = "Assets/Art/Environment/Pond/Textures";
        private const string ArtPondAudioFolder = "Assets/Art/Audio/Ambience/Pond";

        private const string ProjectMaterialFolder = "Assets/_Project/Materials/Environment";
        private const string ProjectPrefabFolder = "Assets/_Project/Prefabs/Environment";
        private const string SitePondAssetPath = "Assets/_Project/SO/FishDB/Test/Site_Pond.asset";

        [MenuItem("VirtualFishing/Fish/Build Site Pond Environment")]
        public static void BuildSitePondEnvironment()
        {
            EnsureFolder("Assets/Art", "Environment");
            EnsureFolder("Assets/Art/Environment", "Water");
            EnsureFolder("Assets/Art/Environment", "Pond");
            EnsureFolder("Assets/Art/Environment/Pond", "Models");
            EnsureFolder("Assets/Art/Environment/Pond", "Textures");
            EnsureFolder("Assets/Art", "Audio");
            EnsureFolder("Assets/Art/Audio", "Ambience");
            EnsureFolder("Assets/Art/Audio/Ambience", "Pond");

            EnsureFolder("Assets/_Project/Materials", "Environment");
            EnsureFolder("Assets/_Project/Prefabs", "Environment");

            Material groundMaterial = CreateOrUpdateMaterial("MAT_Pond_Ground", new Color(0.41f, 0.48f, 0.28f), 0f);
            Material waterMaterial = CreateOrUpdateMaterial("MAT_Pond_Water", new Color(0.22f, 0.52f, 0.67f), 0.25f);
            Material rockMaterial = CreateOrUpdateMaterial("MAT_Pond_Rock", new Color(0.45f, 0.46f, 0.44f), 0f);
            Material mountainMaterial = CreateOrUpdateMaterial("MAT_Pond_Mountain", new Color(0.4f, 0.55f, 0.42f), 0f);
            Material trunkMaterial = CreateOrUpdateMaterial("MAT_Pond_TreeTrunk", new Color(0.33f, 0.22f, 0.14f), 0f);
            Material leafMaterial = CreateOrUpdateMaterial("MAT_Pond_TreeLeaves", new Color(0.24f, 0.45f, 0.23f), 0f);
            Material reedMaterial = CreateOrUpdateMaterial("MAT_Pond_Reed", new Color(0.55f, 0.66f, 0.34f), 0f);

            GameObject waterPrefab = SaveAsPrefab(BuildWaterPrefab(waterMaterial), "PF_Pond_Water");
            GameObject groundPrefab = SaveAsPrefab(BuildGroundPrefab(groundMaterial), "PF_Pond_Ground");
            GameObject rockClusterPrefab = SaveAsPrefab(BuildRockClusterPrefab(rockMaterial), "PF_Pond_RockCluster_A");
            GameObject treeClusterPrefab = SaveAsPrefab(BuildTreeClusterPrefab(trunkMaterial, leafMaterial), "PF_Pond_TreeCluster_A");
            GameObject pondEnvironmentPrefab = SaveAsPrefab(
                BuildPondEnvironmentPrefab(groundMaterial, waterMaterial, rockMaterial, mountainMaterial, trunkMaterial, leafMaterial, reedMaterial),
                "PF_Site_Pond_Environment");

            AssignEnvironmentPrefabToSite(pondEnvironmentPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[PondEnvironmentBuilder] Site_Pond environment built. " +
                $"Art folders ready: {ArtEnvironmentRoot}, {ArtWaterFolder}, {ArtPondModelFolder}, {ArtPondTextureFolder}, {ArtPondAudioFolder}. " +
                $"Project prefabs created: {groundPrefab.name}, {waterPrefab.name}, {rockClusterPrefab.name}, {treeClusterPrefab.name}, {pondEnvironmentPrefab.name}.");
        }

        private static GameObject BuildGroundPrefab(Material material)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Plane);
            root.name = "PF_Pond_Ground";
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = new Vector3(8f, 1f, 8f);
            SetupRenderer(root, material);
            RemoveColliderRecursive(root);
            return root;
        }

        private static GameObject BuildWaterPrefab(Material material)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Plane);
            root.name = "PF_Pond_Water";
            root.transform.localPosition = new Vector3(0f, 0.06f, 2f);
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = new Vector3(4.5f, 1f, 4.5f);
            SetupRenderer(root, material);
            RemoveColliderRecursive(root);
            return root;
        }

        private static GameObject BuildRockClusterPrefab(Material material)
        {
            GameObject root = new GameObject("PF_Pond_RockCluster_A");

            CreateRock(root.transform, "Rock_01", new Vector3(-10f, 0.4f, 6f), new Vector3(2.4f, 1.8f, 2.2f), 25f, material);
            CreateRock(root.transform, "Rock_02", new Vector3(-7f, 0.3f, 10f), new Vector3(1.8f, 1.3f, 1.7f), 70f, material);
            CreateRock(root.transform, "Rock_03", new Vector3(8f, 0.35f, 7f), new Vector3(2.1f, 1.5f, 1.9f), 120f, material);
            CreateRock(root.transform, "Rock_04", new Vector3(11f, 0.4f, 11f), new Vector3(2.8f, 1.9f, 2.4f), 200f, material);
            CreateRock(root.transform, "Rock_05", new Vector3(-3f, 0.25f, 13f), new Vector3(1.6f, 1.1f, 1.5f), 145f, material);
            CreateRock(root.transform, "Rock_06", new Vector3(3f, 0.25f, 13.5f), new Vector3(1.7f, 1.2f, 1.6f), 300f, material);

            RemoveColliderRecursive(root);
            return root;
        }

        private static GameObject BuildTreeClusterPrefab(Material trunkMaterial, Material leafMaterial)
        {
            GameObject root = new GameObject("PF_Pond_TreeCluster_A");

            CreateTree(root.transform, "Tree_01", new Vector3(-15f, 0f, 9f), 10f, 1.8f, trunkMaterial, leafMaterial);
            CreateTree(root.transform, "Tree_02", new Vector3(-17f, 0f, 14f), 55f, 1.5f, trunkMaterial, leafMaterial);
            CreateTree(root.transform, "Tree_03", new Vector3(-11f, 0f, 16f), 95f, 1.6f, trunkMaterial, leafMaterial);
            CreateTree(root.transform, "Tree_04", new Vector3(12f, 0f, 15f), 180f, 1.7f, trunkMaterial, leafMaterial);
            CreateTree(root.transform, "Tree_05", new Vector3(16f, 0f, 10f), 240f, 1.9f, trunkMaterial, leafMaterial);
            CreateTree(root.transform, "Tree_06", new Vector3(18f, 0f, 16f), 315f, 1.4f, trunkMaterial, leafMaterial);
            CreateTree(root.transform, "Tree_07", new Vector3(-20f, 0f, 3f), 45f, 1.6f, trunkMaterial, leafMaterial);
            CreateTree(root.transform, "Tree_08", new Vector3(20f, 0f, 4f), 135f, 1.6f, trunkMaterial, leafMaterial);

            RemoveColliderRecursive(root);
            return root;
        }

        private static GameObject BuildPondEnvironmentPrefab(
            Material groundMaterial,
            Material waterMaterial,
            Material rockMaterial,
            Material mountainMaterial,
            Material trunkMaterial,
            Material leafMaterial,
            Material reedMaterial)
        {
            GameObject root = new GameObject("PF_Site_Pond_Environment");

            GameObject ground = BuildGroundPrefab(groundMaterial);
            ground.name = "Pond_Ground";
            ground.transform.SetParent(root.transform);

            GameObject water = BuildWaterPrefab(waterMaterial);
            water.name = "Pond_Water";
            water.transform.SetParent(root.transform);

            GameObject backMountain = CreateMountain("Pond_BackMountain", new Vector3(0f, 3.5f, 21f), new Vector3(24f, 7f, 4f), 0f, mountainMaterial);
            backMountain.transform.SetParent(root.transform);

            GameObject leftHill = CreateMountain("Pond_LeftHill", new Vector3(-13f, 2.2f, 14f), new Vector3(10f, 4f, 6f), 20f, mountainMaterial);
            leftHill.transform.SetParent(root.transform);

            GameObject rightHill = CreateMountain("Pond_RightHill", new Vector3(13f, 2.4f, 13f), new Vector3(11f, 4.5f, 6f), -18f, mountainMaterial);
            rightHill.transform.SetParent(root.transform);

            GameObject rocks = BuildRockClusterPrefab(rockMaterial);
            rocks.name = "Pond_Rocks";
            rocks.transform.SetParent(root.transform);

            GameObject trees = BuildTreeClusterPrefab(trunkMaterial, leafMaterial);
            trees.name = "Pond_Trees";
            trees.transform.SetParent(root.transform);

            GameObject reeds = BuildReedClusterPrefab(reedMaterial);
            reeds.name = "Pond_Reeds";
            reeds.transform.SetParent(root.transform);

            RemoveColliderRecursive(root);
            return root;
        }

        private static GameObject BuildReedClusterPrefab(Material material)
        {
            GameObject root = new GameObject("Pond_Reeds");
            CreateReed(root.transform, "Reed_01", new Vector3(-5f, 0f, 5.5f), 0f, 1.1f, material);
            CreateReed(root.transform, "Reed_02", new Vector3(-3.5f, 0f, 5.8f), 25f, 0.9f, material);
            CreateReed(root.transform, "Reed_03", new Vector3(4.5f, 0f, 5.7f), 80f, 1f, material);
            CreateReed(root.transform, "Reed_04", new Vector3(6f, 0f, 6.2f), 120f, 1.2f, material);
            RemoveColliderRecursive(root);
            return root;
        }

        private static GameObject CreateMountain(string name, Vector3 position, Vector3 scale, float yRotation, Material material)
        {
            GameObject mountain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mountain.name = name;
            mountain.transform.localPosition = position;
            mountain.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            mountain.transform.localScale = scale;
            SetupRenderer(mountain, material);
            return mountain;
        }

        private static void CreateRock(Transform parent, string name, Vector3 position, Vector3 scale, float yRotation, Material material)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = name;
            rock.transform.SetParent(parent);
            rock.transform.localPosition = position;
            rock.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            rock.transform.localScale = scale;
            SetupRenderer(rock, material);
        }

        private static void CreateTree(Transform parent, string name, Vector3 position, float yRotation, float uniformScale, Material trunkMaterial, Material leafMaterial)
        {
            GameObject treeRoot = new GameObject(name);
            treeRoot.transform.SetParent(parent);
            treeRoot.transform.localPosition = position;
            treeRoot.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            treeRoot.transform.localScale = Vector3.one * uniformScale;

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(treeRoot.transform);
            trunk.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            trunk.transform.localScale = new Vector3(0.18f, 1.1f, 0.18f);
            SetupRenderer(trunk, trunkMaterial);

            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.name = "Leaves";
            leaves.transform.SetParent(treeRoot.transform);
            leaves.transform.localPosition = new Vector3(0f, 2.8f, 0f);
            leaves.transform.localScale = new Vector3(1.5f, 1.8f, 1.5f);
            SetupRenderer(leaves, leafMaterial);
        }

        private static void CreateReed(Transform parent, string name, Vector3 position, float yRotation, float uniformScale, Material material)
        {
            GameObject reedRoot = new GameObject(name);
            reedRoot.transform.SetParent(parent);
            reedRoot.transform.localPosition = position;
            reedRoot.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            reedRoot.transform.localScale = Vector3.one * uniformScale;

            for (int i = 0; i < 4; i++)
            {
                GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = $"Stem_{i + 1}";
                stem.transform.SetParent(reedRoot.transform);
                stem.transform.localPosition = new Vector3((i - 1.5f) * 0.08f, 0.35f + (i * 0.03f), (i % 2 == 0 ? 0.05f : -0.04f));
                stem.transform.localRotation = Quaternion.Euler(0f, i * 14f, 8f);
                stem.transform.localScale = new Vector3(0.02f, 0.35f + (i * 0.04f), 0.02f);
                SetupRenderer(stem, material);
            }
        }

        private static Material CreateOrUpdateMaterial(string materialName, Color color, float alpha)
        {
            string materialPath = $"{ProjectMaterialFolder}/{materialName}.mat";
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

            Color tintedColor = new Color(color.r, color.g, color.b, 1f - alpha);
            material.color = tintedColor;

            if (alpha > 0f)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject SaveAsPrefab(GameObject source, string prefabName)
        {
            string prefabPath = $"{ProjectPrefabFolder}/{prefabName}.prefab";
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(source, prefabPath);
            Object.DestroyImmediate(source);
            return prefabAsset;
        }

        private static void AssignEnvironmentPrefabToSite(GameObject environmentPrefab)
        {
            FishingSiteDataSO siteData = AssetDatabase.LoadAssetAtPath<FishingSiteDataSO>(SitePondAssetPath);
            if (siteData == null)
            {
                Debug.LogWarning($"[PondEnvironmentBuilder] Could not find site asset: {SitePondAssetPath}");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(siteData);
            serializedObject.FindProperty("environmentPrefab").objectReferenceValue = environmentPrefab;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(siteData);
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
