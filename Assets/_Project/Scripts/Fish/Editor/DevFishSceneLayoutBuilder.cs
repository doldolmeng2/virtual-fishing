#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualFishing.Core.Fish;

namespace VirtualFishing.EditorTools
{
    public static class DevFishSceneLayoutBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Dev_Fish.unity";

        private const string Mountain01Path = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs/PP_Forest_Mountain_Moss_01.prefab";
        private const string Mountain02Path = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs/PP_Forest_Mountain_Moss_02.prefab";
        private const string Rock05Path = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs/PP_Rock_Pile_Forest_Moss_05.prefab";
        private const string Rock10Path = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs/PP_Rock_Pile_Forest_Moss_10.prefab";
        private const string Grass11Path = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs/PP_Grass_11.prefab";
        private const string Grass15Path = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs/PP_Grass_15.prefab";

        private const string Grass11SourcePrefabPath = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Grass_11.prefab";
        private const string Grass15SourcePrefabPath = "Assets/Pure Poly/Free Low Poly Nature Pack/Prefabs/PP_Grass_15.prefab";
        private const string Grass11SourceMeshPath = "Assets/Pure Poly/Free Low Poly Nature Pack/Meshes/PP_Grass_11.fbx";
        private const string Grass15SourceMeshPath = "Assets/Pure Poly/Free Low Poly Nature Pack/Meshes/PP_Grass_15.fbx";
        private const string Grass11TargetMeshPath = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Meshes/PP_Grass_11.fbx";
        private const string Grass15TargetMeshPath = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Meshes/PP_Grass_15.fbx";

        private const string Tree01Path = "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_1_Smooth.prefab";
        private const string Tree04Path = "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_4_Smooth.prefab";
        private const string Tree07Path = "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_7_Smooth.prefab";

        private const string GroundMaterialPath = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Materials/PP_Ground.mat";
        private const string WaterMaterialPath = "Assets/Art/Environment/Water/Simple Water Shader/Resources/Water_mat_03.mat";

        [MenuItem("VirtualFishing/Fish/Rebuild Dev_Fish Scenic Layout")]
        public static void RebuildDevFishScenicLayout()
        {
            ConsolidateOptionalNatureAssets();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            RemoveExistingEnvironmentObjects();

            GameObject environmentRoot = new GameObject("EnvironmentRoot");
            CreateGround(environmentRoot.transform);
            CreateWater(environmentRoot.transform);
            CreateMountains(environmentRoot.transform);
            CreateRocks(environmentRoot.transform);
            CreateTrees(environmentRoot.transform);
            CreateGrass(environmentRoot.transform);

            ConfigureCamera();
            ConfigureDirectionalLight();
            ConfigureFishComponents(environmentRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[DevFishSceneLayoutBuilder] Dev_Fish scenic layout rebuilt with pond foreground, shore vegetation, and distant mountains.");
        }

        [MenuItem("VirtualFishing/Fish/Consolidate Pond Scenic Assets")]
        public static void ConsolidateOptionalNatureAssets()
        {
            EnsureFolderPath("Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs");
            EnsureFolderPath("Assets/Art/Environment/Pond/Models/PurePoly_Selected/Meshes");

            EnsureAssetMoved(Grass11SourceMeshPath, Grass11TargetMeshPath);
            EnsureAssetMoved(Grass15SourceMeshPath, Grass15TargetMeshPath);
            EnsureAssetMoved(Grass11SourcePrefabPath, Grass11Path);
            EnsureAssetMoved(Grass15SourcePrefabPath, Grass15Path);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void RemoveExistingEnvironmentObjects()
        {
            string[] names =
            {
                "EnvironmentRoot",
                "Pond_Ground",
                "Pond_Water",
                "Pond_BackMountain",
                "Pond_LeftHill",
                "Pond_RightHill",
                "Pond_Rocks",
                "Pond_Trees",
                "Pond_Reeds",
                "Pond_Grass",
                "Pond_Mountains"
            };

            foreach (string name in names)
            {
                GameObject existing = GameObject.Find(name);
                if (existing != null)
                {
                    Object.DestroyImmediate(existing);
                }
            }
        }

        private static void CreateGround(Transform parent)
        {
            Material groundMaterial = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Pond_Ground";
            ground.transform.SetParent(parent);
            ground.transform.localPosition = new Vector3(0f, -0.02f, 20f);
            ground.transform.localRotation = Quaternion.identity;
            ground.transform.localScale = new Vector3(15f, 1f, 10.5f);

            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null && groundMaterial != null)
            {
                renderer.sharedMaterial = groundMaterial;
            }
        }

        private static void CreateWater(Transform parent)
        {
            Material waterMaterial = AssetDatabase.LoadAssetAtPath<Material>(WaterMaterialPath);
            GameObject water = new GameObject("Pond_Water");
            water.name = "Pond_Water";
            water.transform.SetParent(parent);
            water.transform.localPosition = new Vector3(0f, 0.14f, 11.2f);
            water.transform.localRotation = Quaternion.identity;
            water.transform.localScale = Vector3.one;

            MeshFilter meshFilter = water.AddComponent<MeshFilter>();
            MeshRenderer renderer = water.AddComponent<MeshRenderer>();
            MeshCollider collider = water.AddComponent<MeshCollider>();
            PondWaterSurface surface = water.AddComponent<PondWaterSurface>();
            surface.RebuildMesh();

            if (renderer != null && waterMaterial != null)
            {
                renderer.sharedMaterial = waterMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            if (collider != null)
            {
                collider.convex = false;
            }
        }

        private static void CreateMountains(Transform parent)
        {
            GameObject mountainRoot = new GameObject("Pond_Mountains");
            mountainRoot.transform.SetParent(parent);
            mountainRoot.transform.localPosition = Vector3.zero;
            mountainRoot.transform.localRotation = Quaternion.identity;

            GameObject mountain01 = InstantiatePrefab(Mountain01Path, "Mountain_Center", mountainRoot.transform);
            ApplyTransform(mountain01.transform, new Vector3(-38f, -8f, 160f), Vector3.zero, new Vector3(2.6f, 2.6f, 2.6f));

            GameObject mountain02Left = InstantiatePrefab(Mountain02Path, "Mountain_Left", mountainRoot.transform);
            ApplyTransform(mountain02Left.transform, new Vector3(-74f, -12f, 148f), new Vector3(0f, 18f, 0f), new Vector3(3.6f, 3.6f, 3.6f));

            GameObject mountain02Right = InstantiatePrefab(Mountain02Path, "Mountain_Right", mountainRoot.transform);
            ApplyTransform(mountain02Right.transform, new Vector3(-4f, -10f, 154f), new Vector3(0f, -14f, 0f), new Vector3(2.7f, 2.7f, 2.7f));

            GameObject mountainFarLeft = InstantiatePrefab(Mountain01Path, "Mountain_FarLeft", mountainRoot.transform);
            ApplyTransform(mountainFarLeft.transform, new Vector3(-102f, -16f, 170f), new Vector3(0f, 10f, 0f), new Vector3(4.2f, 4.2f, 4.2f));

            GameObject mountainFarRight = InstantiatePrefab(Mountain01Path, "Mountain_FarRight", mountainRoot.transform);
            ApplyTransform(mountainFarRight.transform, new Vector3(26f, -13f, 174f), new Vector3(0f, -12f, 0f), new Vector3(3.2f, 3.2f, 3.2f));

            GameObject mountainExtraLeft = InstantiatePrefab(Mountain02Path, "Mountain_ExtraLeft", mountainRoot.transform);
            ApplyTransform(mountainExtraLeft.transform, new Vector3(-120f, -17f, 186f), new Vector3(0f, 8f, 0f), new Vector3(4.6f, 4.6f, 4.6f));

            GameObject mountainExtraRight = InstantiatePrefab(Mountain02Path, "Mountain_ExtraRight", mountainRoot.transform);
            ApplyTransform(mountainExtraRight.transform, new Vector3(58f, -15f, 188f), new Vector3(0f, -9f, 0f), new Vector3(3.8f, 3.8f, 3.8f));
        }

        private static void CreateRocks(Transform parent)
        {
            GameObject rockRoot = new GameObject("Pond_Rocks");
            rockRoot.transform.SetParent(parent);
            rockRoot.transform.localPosition = Vector3.zero;
            rockRoot.transform.localRotation = Quaternion.identity;

            PlacePrefab(Rock05Path, "Rock_Left_A", rockRoot.transform, new Vector3(-12f, 0.08f, 31.5f), new Vector3(0f, 20f, 0f), new Vector3(1.1f, 1.1f, 1.1f));
            PlacePrefab(Rock10Path, "Rock_Left_B", rockRoot.transform, new Vector3(-8.2f, 0.05f, 33.4f), new Vector3(0f, 60f, 0f), new Vector3(0.78f, 0.78f, 0.78f));
            PlacePrefab(Rock05Path, "Rock_Left_C", rockRoot.transform, new Vector3(-16.8f, 0.09f, 36.2f), new Vector3(0f, 32f, 0f), new Vector3(1.25f, 1.25f, 1.25f));
            PlacePrefab(Rock10Path, "Rock_Center_A", rockRoot.transform, new Vector3(-2.8f, 0.05f, 35.8f), new Vector3(0f, 145f, 0f), new Vector3(0.7f, 0.7f, 0.7f));
            PlacePrefab(Rock10Path, "Rock_Right_A", rockRoot.transform, new Vector3(6.8f, 0.05f, 32.1f), new Vector3(0f, 110f, 0f), new Vector3(0.82f, 0.82f, 0.82f));
            PlacePrefab(Rock05Path, "Rock_Right_B", rockRoot.transform, new Vector3(11.8f, 0.08f, 34f), new Vector3(0f, 200f, 0f), new Vector3(1.05f, 1.05f, 1.05f));
            PlacePrefab(Rock10Path, "Rock_Right_C", rockRoot.transform, new Vector3(16.5f, 0.05f, 36.5f), new Vector3(0f, 232f, 0f), new Vector3(0.88f, 0.88f, 0.88f));
            PlacePrefab(Rock05Path, "Rock_FarRight", rockRoot.transform, new Vector3(21.8f, 0.08f, 39.5f), new Vector3(0f, 250f, 0f), new Vector3(1.15f, 1.15f, 1.15f));
        }

        private static void CreateTrees(Transform parent)
        {
            GameObject treeRoot = new GameObject("Pond_Trees");
            treeRoot.transform.SetParent(parent);
            treeRoot.transform.localPosition = Vector3.zero;
            treeRoot.transform.localRotation = Quaternion.identity;

            treeRoot.transform.localPosition = new Vector3(0f, 0f, 24f);

            int treeIndex = 1;

            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    float x = -34f + col * 5.5f + (row % 2 == 0 ? 0f : 1.6f);
                    float z = 2f + row * 5.8f;
                    float scale = 1.1f + ((row + col) % 4) * 0.18f;
                    PlaceTreeVariant(treeRoot.transform, treeIndex++, new Vector3(x, 0f, z), 25f + row * 18f + col * 21f, scale);
                }
            }

            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    float x = 12f + col * 5.4f + (row % 2 == 0 ? 0.8f : -1.1f);
                    float z = 1f + row * 5.9f;
                    float scale = 1.05f + ((row + col + 1) % 4) * 0.2f;
                    PlaceTreeVariant(treeRoot.transform, treeIndex++, new Vector3(x, 0f, z), 160f + row * 17f + col * 19f, scale);
                }
            }

            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    float x = -22f + col * 10f + (row == 0 ? 0f : 2.5f);
                    float z = 33f + row * 6f;
                    float scale = 1f + ((row + col) % 3) * 0.14f;
                    PlaceTreeVariant(treeRoot.transform, treeIndex++, new Vector3(x, 0f, z), 70f + row * 33f + col * 14f, scale);
                }
            }
        }

        private static void CreateGrass(Transform parent)
        {
            GameObject grassRoot = new GameObject("Pond_Grass");
            grassRoot.transform.SetParent(parent);
            grassRoot.transform.localPosition = Vector3.zero;
            grassRoot.transform.localRotation = Quaternion.identity;

            string[] grassPrefabPaths =
            {
                Grass11Path,
                Grass15Path
            };

            const int grassCount = 100;
            for (int i = 0; i < grassCount; i++)
            {
                string prefabPath = grassPrefabPaths[i % grassPrefabPaths.Length];
                float angle = i / (float)grassCount * Mathf.PI * 2f;
                float radiusX = 34f + Mathf.Sin(i * 0.7f) * 4.5f;
                float radiusZ = 16f + Mathf.Cos(i * 0.5f) * 3.4f;
                float x = Mathf.Cos(angle) * radiusX;
                float z = 12f + Mathf.Sin(angle) * radiusZ;
                Vector3 position = new Vector3(x, 0f, z);
                Vector3 rotation = new Vector3(0f, (i * 37f) % 360f, 0f);
                float scaleValue = 2.4f + (i % 5) * 0.28f;
                Vector3 scale = new Vector3(scaleValue, scaleValue, scaleValue);

                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                {
                    CreateFallbackGrassClump(grassRoot.transform, position, rotation, scale);
                    continue;
                }

                PlacePrefab(prefabPath, $"Grass_{i + 1:00}", grassRoot.transform, position, rotation, scale);
            }
        }

        private static void CreateFallbackGrassClump(Transform parent, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
        {
            GameObject grassRoot = new GameObject("Grass_Fallback");
            grassRoot.transform.SetParent(parent, false);
            grassRoot.transform.localPosition = localPosition;
            grassRoot.transform.localRotation = Quaternion.Euler(localEulerAngles);
            grassRoot.transform.localScale = localScale;

            for (int i = 0; i < 7; i++)
            {
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = $"Blade_{i + 1}";
                blade.transform.SetParent(grassRoot.transform, false);
                blade.transform.localPosition = new Vector3((i - 3) * 0.22f, 0.9f + i * 0.07f, (i - 3) * 0.08f);
                blade.transform.localRotation = Quaternion.Euler(0f, i * 24f, 24f - i * 4f);
                blade.transform.localScale = new Vector3(0.18f, 1.9f, 0.18f);

                Renderer renderer = blade.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    renderer.sharedMaterial.color = new Color(0.24f, 0.5f, 0.2f);
                }
            }
        }

        private static void ConfigureCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObject = GameObject.Find("Main Camera");
                if (cameraObject != null)
                {
                    mainCamera = cameraObject.GetComponent<Camera>();
                }
            }

            if (mainCamera == null)
            {
                return;
            }

            mainCamera.gameObject.tag = "MainCamera";
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.74f, 0.83f, 0.91f);
            mainCamera.fieldOfView = 52f;
            mainCamera.transform.position = new Vector3(0f, 4.2f, -9.5f);
            mainCamera.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        }

        private static void ConfigureDirectionalLight()
        {
            GameObject lightObject = GameObject.Find("Directional Light");
            if (lightObject == null)
            {
                return;
            }

            Light light = lightObject.GetComponent<Light>();
            if (light != null)
            {
                light.color = new Color(1f, 0.95f, 0.87f);
                light.intensity = 1.1f;
            }

            lightObject.transform.rotation = Quaternion.Euler(34f, -28f, 0f);
        }

        private static void ConfigureFishComponents(Transform environmentRoot)
        {
            GameObject fishTestRoot = GameObject.Find("FishTestRoot");
            if (fishTestRoot == null)
            {
                return;
            }

            FishController fishController = fishTestRoot.GetComponent<FishController>();
            if (fishController != null)
            {
                SerializedObject serializedFishController = new SerializedObject(fishController);
                SerializedProperty spawnOffsetProperty = serializedFishController.FindProperty("spawnOffset");
                SerializedProperty moveLimitProperty = serializedFishController.FindProperty("horizontalMoveLimit");

                if (spawnOffsetProperty != null)
                {
                    spawnOffsetProperty.vector3Value = new Vector3(0f, 0.7f, 10f);
                }

                if (moveLimitProperty != null)
                {
                    moveLimitProperty.floatValue = 6.5f;
                }

                serializedFishController.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(fishController);
            }

            FishEnvironmentController environmentController = fishTestRoot.GetComponent<FishEnvironmentController>();
            if (environmentController != null)
            {
                SerializedObject serializedEnvironmentController = new SerializedObject(environmentController);
                Camera mainCamera = Camera.main ?? GameObject.Find("Main Camera")?.GetComponent<Camera>();
                Light directionalLight = GameObject.Find("Directional Light")?.GetComponent<Light>();

                SerializedProperty targetCameraProperty = serializedEnvironmentController.FindProperty("targetCamera");
                SerializedProperty directionalLightProperty = serializedEnvironmentController.FindProperty("directionalLight");
                SerializedProperty environmentRootProperty = serializedEnvironmentController.FindProperty("environmentRoot");
                SerializedProperty createDebugProperty = serializedEnvironmentController.FindProperty("createDebugEnvironment");
                SerializedProperty applyOnStartProperty = serializedEnvironmentController.FindProperty("applyOnStart");

                if (targetCameraProperty != null)
                {
                    targetCameraProperty.objectReferenceValue = mainCamera;
                }

                if (directionalLightProperty != null)
                {
                    directionalLightProperty.objectReferenceValue = directionalLight;
                }

                if (environmentRootProperty != null)
                {
                    environmentRootProperty.objectReferenceValue = environmentRoot;
                }

                if (createDebugProperty != null)
                {
                    createDebugProperty.boolValue = false;
                }

                if (applyOnStartProperty != null)
                {
                    applyOnStartProperty.boolValue = true;
                }

                serializedEnvironmentController.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(environmentController);
            }
        }

        private static GameObject InstantiatePrefab(string prefabPath, string objectName, Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[DevFishSceneLayoutBuilder] Missing prefab: {prefabPath}");
                GameObject fallback = new GameObject(objectName);
                fallback.transform.SetParent(parent, false);
                return fallback;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                instance = Object.Instantiate(prefab);
            }

            instance.name = objectName;
            instance.transform.SetParent(parent, false);
            return instance;
        }

        private static void PlacePrefab(string prefabPath, string objectName, Transform parent, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
        {
            GameObject instance = InstantiatePrefab(prefabPath, objectName, parent);
            ApplyTransform(instance.transform, localPosition, localEulerAngles, localScale);
        }

        private static void PlaceTreeVariant(Transform parent, int treeIndex, Vector3 localPosition, float yRotation, float scale)
        {
            string prefabPath = (treeIndex % 3) switch
            {
                1 => Tree01Path,
                2 => Tree04Path,
                _ => Tree07Path
            };

            PlacePrefab(
                prefabPath,
                $"Tree_{treeIndex:00}",
                parent,
                localPosition,
                new Vector3(0f, yRotation, 0f),
                new Vector3(scale, scale, scale));
        }

        private static void ApplyTransform(Transform target, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
        {
            target.localPosition = localPosition;
            target.localRotation = Quaternion.Euler(localEulerAngles);
            target.localScale = localScale;
        }

        private static void EnsureAssetMoved(string sourcePath, string targetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(targetPath) != null)
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(sourcePath) == null)
            {
                return;
            }

            EnsureFolderPath(Path.GetDirectoryName(targetPath)?.Replace('\\', '/'));
            string result = AssetDatabase.MoveAsset(sourcePath, targetPath);

            if (!string.IsNullOrEmpty(result))
            {
                Debug.LogWarning($"[DevFishSceneLayoutBuilder] Asset move skipped: {sourcePath} -> {targetPath}. {result}");
            }
        }

        private static void EnsureFolderPath(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string folderName = Path.GetFileName(folderPath);

            EnsureFolderPath(parent);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
#endif
