#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace VirtualFishing.Core.Fish
{
    public static class DevFishEnvironmentRuntimeBuilder
    {
        private const string LayoutResourcePath = "DevFish/DevFishReservoirLayout";
        private const string RootName = "DevFish_RuntimeEnvironment";

        private const string Mountain01Path = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs/PP_Forest_Mountain_Moss_01.prefab";
        private const string Mountain02Path = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs/PP_Forest_Mountain_Moss_02.prefab";
        private const string Rock05Path = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs/PP_Rock_Pile_Forest_Moss_05.prefab";
        private const string Rock10Path = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs/PP_Rock_Pile_Forest_Moss_10.prefab";
        private const string Grass11Path = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs/PP_Grass_11.prefab";
        private const string Grass15Path = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs/PP_Grass_15.prefab";
        private const string GroundMaterialPath = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Materials/PP_Ground.mat";
        private const string WaterMaterialPath = "Assets/Art/Environment/Water/Simple Water Shader/Resources/Water_mat_03.mat";
        private const float ScenicForwardOffset = -9f;

        private static readonly string[] TreePaths =
        {
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_1_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_2_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_3_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_4_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_5_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_6_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_7_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_8_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_9_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_10_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_11_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_12_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_13_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_14_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_15_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_16_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_17_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_18_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_19_Smooth.prefab",
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_20_Smooth.prefab"
        };

        public static GameObject Build(Transform parent)
        {
            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
            {
                Object.Destroy(existing);
            }

            CleanupLegacySceneScenery();

            DevFishEnvironmentLayoutSO layout = Resources.Load<DevFishEnvironmentLayoutSO>(LayoutResourcePath);
            if (layout == null)
            {
                Debug.LogWarning($"[DevFishEnvironmentRuntimeBuilder] Missing layout resource: {LayoutResourcePath}");
                return null;
            }

            GameObject root = new(RootName);
            root.hideFlags = HideFlags.DontSave;
            root.transform.SetParent(parent, false);

            CreateGround(root.transform);
            CreateWater(root.transform);
            CreateMountains(root.transform);
            CreateRocks(root.transform);
            CreateTrees(root.transform, layout);
            CreateGrass(root.transform, layout);
            SetDontSaveRecursive(root);
            return root;
        }

        private static void CreateGround(Transform parent)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Pond_Ground";
            ground.transform.SetParent(parent, false);
            ground.transform.localPosition = OffsetForward(new Vector3(0f, -0.02f, 18f));
            ground.transform.localScale = new Vector3(17f, 1f, 11.5f);
            AssignMaterial(ground, GroundMaterialPath);
        }

        private static void CreateWater(Transform parent)
        {
            GameObject water = new("Pond_Water");
            water.transform.SetParent(parent, false);
            water.transform.localPosition = OffsetForward(new Vector3(0f, 0.14f, 11.2f));
            water.AddComponent<MeshFilter>();
            MeshRenderer renderer = water.AddComponent<MeshRenderer>();
            water.AddComponent<MeshCollider>();
            water.AddComponent<PondWaterSurface>().RebuildMesh();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            AssignMaterial(water, WaterMaterialPath);
        }

        private static void CreateMountains(Transform parent)
        {
            Transform root = NewRoot("Pond_Mountains", parent).transform;
            PlacePrefab(Mountain01Path, "Mountain_BackCenter", root, new Vector3(-20f, -11f, 68f), new Vector3(0f, 6f, 0f), Vector3.one * 1.65f);
            PlacePrefab(Mountain02Path, "Mountain_BackLeft", root, new Vector3(-70f, -14f, 66f), new Vector3(0f, 18f, 0f), Vector3.one * 2.25f);
            PlacePrefab(Mountain02Path, "Mountain_BackRight", root, new Vector3(42f, -13f, 67f), new Vector3(0f, -16f, 0f), Vector3.one * 2.05f);
            PlacePrefab(Mountain01Path, "Mountain_FarLeft", root, new Vector3(-108f, -18f, 86f), new Vector3(0f, 12f, 0f), Vector3.one * 2.85f);
            PlacePrefab(Mountain01Path, "Mountain_FarRight", root, new Vector3(92f, -17f, 88f), new Vector3(0f, -10f, 0f), Vector3.one * 2.65f);
            PlacePrefab(Mountain02Path, "Mountain_FarCenter", root, new Vector3(6f, -19f, 100f), new Vector3(0f, -4f, 0f), Vector3.one * 2.75f);
        }

        private static void CreateRocks(Transform parent)
        {
            Transform root = NewRoot("Pond_Rocks", parent).transform;
            for (int i = 0; i < 40; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float band = (i % 10) / 9f;
                float x = side * (13f + band * 48f + Mathf.Sin(i * 1.3f) * 3.4f);
                float z = 5f + (i % 8) * 4.2f + Mathf.Cos(i * 0.71f) * 2.4f;
                float scale = 0.5f + (i % 6) * 0.1f;
                string path = i % 3 == 0 ? Rock05Path : Rock10Path;
                PlacePrefab(path, $"Rock_{i + 1:00}", root, new Vector3(x, 0.05f, z), new Vector3(0f, i * 37f, 0f), Vector3.one * scale);
            }

            for (int i = 0; i < 28; i++)
            {
                float t = i / 27f;
                float x = Mathf.Lerp(-68f, 68f, t) + Mathf.Sin(i * 1.9f) * 4.8f;
                float z = 27f + Mathf.Sin(t * Mathf.PI * 6f) * 3.4f + Mathf.Cos(i * 0.46f) * 1.8f;
                float scale = 0.46f + (i % 5) * 0.09f;
                string path = i % 2 == 0 ? Rock10Path : Rock05Path;
                PlacePrefab(path, $"RearRock_{i + 1:00}", root, new Vector3(x, 0.04f, z), new Vector3(0f, 19f + i * 41f, 0f), Vector3.one * scale);
            }
        }

        private static void CreateTrees(Transform parent, DevFishEnvironmentLayoutSO layout)
        {
            Transform root = NewRoot("Pond_Trees", parent).transform;
            int index = 0;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    float leftX = -58f + col * 6.4f + Mathf.Sin((row + col) * 1.4f) * 2.4f;
                    float rightX = 20f + col * 6.2f + Mathf.Cos((row + col) * 1.2f) * 2f;
                    float z = 8f + row * 5.1f + Mathf.Sin(col * 1.7f) * 1.8f;
                    PlaceTree(root, index++, new Vector3(leftX, 0f, z), 0.78f + (index % 6) * 0.055f);
                    PlaceTree(root, index++, new Vector3(rightX, 0f, z + 0.8f), 0.78f + (index % 6) * 0.055f);
                }
            }

            for (int i = 0; i < 44; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float t = i / 43f;
                float x = side * (Mathf.Lerp(48f, 73f, (i % 6) / 5f) + Mathf.Sin(i * 1.61f) * 2.8f);
                float z = Mathf.Lerp(-2f, 47f, t) + Mathf.Cos(i * 0.83f) * 3f;
                float scale = Mathf.Lerp(layout.SideTreeScaleRange.x, layout.SideTreeScaleRange.y, (i % 4) / 3f);
                PlaceTree(root, index++, new Vector3(x, 0f, z), scale);
            }

            for (int i = 0; i < 34; i++)
            {
                float t = i / 33f;
                float x = Mathf.Lerp(-76f, 76f, t) + Mathf.Sin(i * 2.05f) * 4.4f;
                float z = 36f + Mathf.Sin(t * Mathf.PI * 7f) * 4.2f + Mathf.Cos(i * 0.64f) * 2f;
                float scale = 0.68f + (i % 6) * 0.065f;
                PlaceTree(root, index++, new Vector3(x, 0f, z), scale);
            }
        }

        private static void CreateGrass(Transform parent, DevFishEnvironmentLayoutSO layout)
        {
            Transform root = NewRoot("Pond_Grass", parent).transform;
            string[] grassPaths = { Grass11Path, Grass15Path };

            for (int i = 0; i < layout.SideGrassCount; i++)
            {
                float t = i / Mathf.Max(1f, layout.SideGrassCount - 1f);
                float side = i % 2 == 0 ? -1f : 1f;
                float clump = (i % 9) / 8f;
                float x = side * (24f + Mathf.Sin(i * 1.73f) * 6.2f + clump * 38f);
                float z = 1f + t * 46f + Mathf.Sin(i * 0.65f) * 3.1f + Mathf.Cos(i * 1.17f) * 1.8f;
                float scale = Mathf.Lerp(layout.SideGrassScaleRange.x, layout.SideGrassScaleRange.y, (i % 5) / 4f);
                PlacePrefab(grassPaths[i % grassPaths.Length], $"Grass_{i + 1:00}", root, new Vector3(x, 0f, z), new Vector3(0f, i * 37f, 0f), Vector3.one * scale);
            }

            for (int i = 0; i < layout.RearGrassCount; i++)
            {
                float t = i / Mathf.Max(1f, layout.RearGrassCount - 1f);
                float x = Mathf.Lerp(-72f, 72f, t) + Mathf.Sin(i * 1.91f) * 4.8f;
                float z = 27f + Mathf.Sin(t * Mathf.PI * 9f) * 4f + Mathf.Cos(i * 0.77f) * 2.2f;
                float scale = Mathf.Lerp(layout.RearGrassScaleRange.x, layout.RearGrassScaleRange.y, (i % 4) / 3f);
                CreateFallbackGrass(root, layout, new Vector3(x, 0f, z), new Vector3(0f, i * 31f, 0f), Vector3.one * scale);
            }

            for (int i = 0; i < layout.AquaticGrassCount; i++)
            {
                float t = i / Mathf.Max(1f, layout.AquaticGrassCount - 1f);
                float x = Mathf.Lerp(-53f, 53f, t) + Mathf.Sin(i * 2.11f) * 3.5f;
                float z = 4.5f + Mathf.Sin(t * Mathf.PI * 10f) * 2.8f + Mathf.Cos(i * 0.93f) * 1.2f;
                float scale = Mathf.Lerp(layout.AquaticGrassScaleRange.x, layout.AquaticGrassScaleRange.y, (i % 5) / 4f);
                CreateFallbackGrass(root, layout, new Vector3(x, -0.05f, z), new Vector3(0f, i * 43f, 0f), Vector3.one * scale);
            }
        }

        private static void CleanupLegacySceneScenery()
        {
            string[] names =
            {
                "Pond_BackMountain",
                "Pond_LeftHill",
                "Pond_RightHill",
                "Pond_Mountains",
                "Pond_Trees",
                "Pond_Rocks",
                "Pond_Ground",
                "Pond_Water"
            };

            foreach (string name in names)
            {
                GameObject legacy = GameObject.Find(name);
                if (legacy != null)
                {
                    Object.Destroy(legacy);
                }
            }
        }

        private static Vector3 OffsetForward(Vector3 position)
        {
            position.z += ScenicForwardOffset;
            return position;
        }

        private static void PlaceTree(Transform parent, int index, Vector3 position, float scale = 1f)
        {
            PlacePrefab(TreePaths[index % TreePaths.Length], $"Tree_{index + 1:00}", parent, position, new Vector3(0f, index * 29f, 0f), Vector3.one * scale);
        }

        private static GameObject NewRoot(string name, Transform parent)
        {
            GameObject root = new(name);
            root.transform.SetParent(parent, false);
            return root;
        }

        private static void PlacePrefab(string path, string name, Transform parent, Vector3 position, Vector3 euler, Vector3 scale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject instance = prefab != null ? PrefabUtility.InstantiatePrefab(prefab) as GameObject : null;
            if (instance == null)
            {
                instance = new GameObject(name);
            }

            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(euler);
            instance.transform.localScale = scale;
        }

        private static void CreateFallbackGrass(Transform parent, DevFishEnvironmentLayoutSO layout, Vector3 position, Vector3 euler, Vector3 scale)
        {
            GameObject grass = NewRoot("Grass_Fallback", parent);
            grass.transform.localPosition = position;
            grass.transform.localRotation = Quaternion.Euler(euler);
            grass.transform.localScale = scale;

            for (int i = 0; i < 7; i++)
            {
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = $"Blade_{i + 1}";
                blade.transform.SetParent(grass.transform, false);
                blade.transform.localPosition = new Vector3((i - 3) * 0.22f, layout.BladeBaseHeight + i * layout.BladeHeightStep, (i - 3) * 0.08f);
                blade.transform.localRotation = Quaternion.Euler(0f, i * 24f, 16f - i * 3f);
                blade.transform.localScale = layout.BladeScale;
                blade.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(new Color(0.24f, 0.5f, 0.2f));
            }
        }

        private static void AssignMaterial(GameObject target, string path)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null && target.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = material;
            }
        }

        private static Material CreateLitMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader) { color = color, hideFlags = HideFlags.DontSave };
            return material;
        }

        private static void SetDontSaveRecursive(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.hideFlags = HideFlags.DontSave;
            }
        }
    }
}
#endif
