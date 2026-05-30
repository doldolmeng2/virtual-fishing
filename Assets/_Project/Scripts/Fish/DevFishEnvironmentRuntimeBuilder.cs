#if UNITY_EDITOR
using System.IO;
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
        private const string KoreanReservoirBackdropPath = "Assets/Art/Environment/Backdrops/fish_eagle_hill_polyhaven_4k.jpg";
        private const string MountainTriplanarShaderName = "VirtualFishing/FishMountainTriplanar";
        private const float ScenicForwardOffset = -9f;
        private const float BackdropRadius = 155f;

        private static readonly Color EarthColor = new(0.34f, 0.31f, 0.26f);
        private static readonly Color ShoreMudColor = new(0.42f, 0.39f, 0.31f);
        private static readonly Color SoftGrassColor = new(0.36f, 0.47f, 0.25f);
        private static readonly Color ReedColor = new(0.45f, 0.5f, 0.25f);
        private static readonly Color FarRidgeColor = new(0.28f, 0.39f, 0.29f);

        private static readonly string[] MountainForestTexturePaths =
        {
            "Assets/Art/Environment/Pond/Textures/MountainForestTiles/korean_forest_tile_jirisan_01.png",
            "Assets/Art/Environment/Pond/Textures/MountainForestTiles/korean_forest_tile_jirisan_02.png",
            "Assets/Art/Environment/Pond/Textures/MountainForestTiles/korean_forest_tile_jirisan_03.png",
            "Assets/Art/Environment/Pond/Textures/MountainForestTiles/korean_forest_tile_jirisan_04.png",
            "Assets/Art/Environment/Pond/Textures/MountainForestTiles/korean_forest_tile_jirisan_05.png",
            "Assets/Art/Environment/Pond/Textures/MountainForestTiles/korean_forest_tile_jirisan_06.png",
            "Assets/Art/Environment/Pond/Textures/MountainForestTiles/korean_forest_tile_jirisan_07.png",
            "Assets/Art/Environment/Pond/Textures/MountainForestTiles/korean_forest_tile_jirisan_08.png",
            "Assets/Art/Environment/Pond/Textures/MountainForestTiles/korean_forest_tile_bukhansan_01.png",
            "Assets/Art/Environment/Pond/Textures/MountainForestTiles/korean_forest_tile_bukhansan_02.png",
            "Assets/Art/Environment/Pond/Textures/MountainForestTiles/korean_forest_tile_bukhansan_03.png",
            "Assets/Art/Environment/Pond/Textures/MountainForestTiles/korean_forest_tile_bukhansan_04.png",
            "Assets/Art/Environment/Pond/Textures/MountainForestTiles/korean_forest_tile_seoraksan_01.png",
            "Assets/Art/Environment/Pond/Textures/MountainForestTiles/korean_forest_tile_seoraksan_02.png",
            "Assets/Art/Environment/Pond/Textures/MountainForestTiles/korean_forest_tile_seoraksan_03.png",
            "Assets/Art/Environment/Pond/Textures/MountainForestTiles/korean_forest_tile_seoraksan_04.png"
        };

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

            ApplySoftReservoirAtmosphere();
            CreateGround(root.transform);
            CreateWater(root.transform);
            CreateWaterDepthBands(root.transform);
            CreateKoreanFishingSiteBackdrop(root.transform);
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
            AssignMaterial(ground, GroundMaterialPath, EarthColor);
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

        private static void ApplySoftReservoirAtmosphere()
        {
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.9f, 0.96f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.7f, 0.76f, 0.72f);
            RenderSettings.ambientGroundColor = new Color(0.46f, 0.44f, 0.4f);

            Light[] lights = Object.FindObjectsOfType<Light>();
            foreach (Light sun in lights)
            {
                if (sun.type != LightType.Directional)
                {
                    continue;
                }

                sun.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
                sun.color = new Color(1f, 0.96f, 0.86f);
                sun.intensity = 1.08f;
                break;
            }
        }

        private static void CreateWaterDepthBands(Transform parent)
        {
            Transform root = NewRoot("Pond_WaterDepthBands", parent).transform;
            CreateFlatTint(root, "Shallow_Edge_Left", OffsetForward(new Vector3(-44f, 0.16f, 11.4f)), new Vector3(5.5f, 1f, 5.8f), new Color(0.47f, 0.66f, 0.62f, 0.24f));
            CreateFlatTint(root, "Shallow_Edge_Right", OffsetForward(new Vector3(44f, 0.16f, 11.8f)), new Vector3(5.5f, 1f, 5.8f), new Color(0.47f, 0.66f, 0.62f, 0.24f));
            CreateFlatTint(root, "Shallow_Rear", OffsetForward(new Vector3(0f, 0.165f, 30f)), new Vector3(13f, 1f, 2.4f), new Color(0.48f, 0.66f, 0.58f, 0.18f));
            CreateFlatTint(root, "Deep_Center", OffsetForward(new Vector3(0f, 0.155f, 13.4f)), new Vector3(8.5f, 1f, 4.8f), new Color(0.18f, 0.34f, 0.42f, 0.16f));
        }

        private static void CreateFlatTint(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = name;
            plane.transform.SetParent(parent, false);
            plane.transform.localPosition = position;
            plane.transform.localScale = scale;

            Renderer renderer = plane.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial = CreateTransparentMaterial(color);
            }
        }

        private static void CreateKoreanFishingSiteBackdrop(Transform parent)
        {
            Transform root = NewRoot("Pond_KoreanFishingSiteBackdrop", parent).transform;
            Texture2D backdrop = AssetDatabase.LoadAssetAtPath<Texture2D>(KoreanReservoirBackdropPath);

            if (backdrop != null)
            {
                CreatePanoramaRing(root, backdrop);
                return;
            }

            CreateFallbackHorizonRing(root);
        }

        private static void CreatePanoramaRing(Transform parent, Texture2D backdrop)
        {
            const int verticalBandCount = 48;
            const float bottom = -20f;
            const float height = 96f;
            const float uvBottom = 0.42f;
            const float uvTop = 0.98f;

            GameObject ring = new("Far_Panorama_Ring");
            ring.transform.SetParent(parent, false);
            ring.transform.localPosition = Vector3.zero;

            for (int band = 0; band < verticalBandCount; band++)
            {
                float bandStart = band / (float)verticalBandCount;
                float bandEnd = (band + 1) / (float)verticalBandCount;
                float bandMiddle = (bandStart + bandEnd) * 0.5f;
                float fade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 1f, bandMiddle));
                float alpha = bandMiddle < 0.42f
                    ? 1f
                    : Mathf.Lerp(0.98f, 0.03f, fade);

                CreatePanoramaBand(
                    ring.transform,
                    backdrop,
                    $"Far_Panorama_Band_{band + 1:00}",
                    Mathf.Lerp(bottom, height, bandStart),
                    Mathf.Lerp(bottom, height, bandEnd),
                    Mathf.Lerp(uvBottom, uvTop, bandStart),
                    Mathf.Lerp(uvBottom, uvTop, bandEnd),
                    alpha);
            }
        }

        private static void CreatePanoramaBand(
            Transform parent,
            Texture2D backdrop,
            string name,
            float bottom,
            float top,
            float uvBottom,
            float uvTop,
            float alpha)
        {
            const int segmentCount = 64;

            GameObject band = new(name);
            band.transform.SetParent(parent, false);
            band.transform.localPosition = Vector3.zero;

            Mesh mesh = new();
            Vector3[] vertices = new Vector3[(segmentCount + 1) * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[segmentCount * 6];

            for (int i = 0; i <= segmentCount; i++)
            {
                float t = i / (float)segmentCount;
                float angle = t * Mathf.PI * 2f;
                float x = Mathf.Sin(angle) * BackdropRadius;
                float z = Mathf.Cos(angle) * BackdropRadius;
                vertices[i * 2] = new Vector3(x, bottom, z);
                vertices[i * 2 + 1] = new Vector3(x, top, z);
                uv[i * 2] = new Vector2(t, uvBottom);
                uv[i * 2 + 1] = new Vector2(t, uvTop);
            }

            for (int i = 0; i < segmentCount; i++)
            {
                int vertex = i * 2;
                int triangle = i * 6;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 3;
                triangles[triangle + 2] = vertex + 1;
                triangles[triangle + 3] = vertex;
                triangles[triangle + 4] = vertex + 2;
                triangles[triangle + 5] = vertex + 3;
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            MeshFilter filter = band.AddComponent<MeshFilter>();
            MeshRenderer renderer = band.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = CreateBackdropImageMaterial(backdrop, new Color(1.14f, 1.18f, 1.2f, alpha));
        }

        private static void CreateFallbackHorizonRing(Transform parent)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                float radian = angle * Mathf.Deg2Rad;
                Vector3 position = new(Mathf.Sin(radian) * BackdropRadius, 17f, Mathf.Cos(radian) * BackdropRadius);
                CreateRidgeStrip(parent, $"Far_Fallback_Horizon_{i + 1:00}", position, 82f, 12f, new Vector3(0f, angle, 0f), new Color(0.52f, 0.61f, 0.58f), 1.1f);
            }
        }

        private static void CreateBackdropImagePanel(Transform parent, string name, Vector3 position, Vector2 size, Vector3 euler, Texture2D texture, Color tint)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panel.name = name;
            panel.transform.SetParent(parent, false);
            panel.transform.localPosition = position;
            panel.transform.localRotation = Quaternion.Euler(euler);
            panel.transform.localScale = new Vector3(size.x, size.y, 1f);

            Renderer renderer = panel.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial = CreateBackdropImageMaterial(texture, tint);
            }
        }

        private static void CreateBackdropCube(Transform parent, string name, Vector3 position, Vector3 scale, Vector3 euler, Color color)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localRotation = Quaternion.Euler(euler);
            cube.transform.localScale = scale;

            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateLitMaterial(color);
            }
        }

        private static void CreateRidgeStrip(Transform parent, string name, Vector3 center, float width, float height, Vector3 euler, Color color, float roughness)
        {
            const int segmentCount = 18;
            GameObject ridge = new(name);
            ridge.transform.SetParent(parent, false);
            ridge.transform.localPosition = center;
            ridge.transform.localRotation = Quaternion.Euler(euler);

            Mesh mesh = new();
            Vector3[] vertices = new Vector3[(segmentCount + 1) * 2];
            int[] triangles = new int[segmentCount * 6];

            for (int i = 0; i <= segmentCount; i++)
            {
                float t = i / (float)segmentCount;
                float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, t);
                float noise = Mathf.Sin(i * 1.37f) * roughness + Mathf.Cos(i * 0.61f) * roughness * 0.45f;
                float top = height + noise;
                vertices[i * 2] = new Vector3(x, 0f, 0f);
                vertices[i * 2 + 1] = new Vector3(x, Mathf.Max(0.4f, top), Mathf.Sin(i * 0.9f) * 0.7f);
            }

            for (int i = 0; i < segmentCount; i++)
            {
                int vertex = i * 2;
                int triangle = i * 6;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 1;
                triangles[triangle + 2] = vertex + 3;
                triangles[triangle + 3] = vertex;
                triangles[triangle + 4] = vertex + 3;
                triangles[triangle + 5] = vertex + 2;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            MeshFilter filter = ridge.AddComponent<MeshFilter>();
            MeshRenderer renderer = ridge.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = CreateLitMaterial(color);
        }

        private static void CreateFarRidges(Transform parent)
        {
            Transform root = NewRoot("Pond_FarRidges", parent).transform;

            CreateRidgeStrip(root, "Rear_LowForest_Ridge", new Vector3(0f, 1.2f, 94f), 176f, 13f, Vector3.zero, FarRidgeColor, 1.6f);
            CreateRidgeStrip(root, "Rear_Earth_Shoreline", new Vector3(0f, 0.08f, 38f), 134f, 2.8f, Vector3.zero, ShoreMudColor, 0.45f);
            CreateRidgeStrip(root, "Left_LowForest_Ridge", new Vector3(-86f, 1.1f, 32f), 92f, 10f, new Vector3(0f, 90f, 0f), new Color(0.26f, 0.37f, 0.26f), 1.2f);
            CreateRidgeStrip(root, "Right_LowForest_Ridge", new Vector3(86f, 1.1f, 32f), 92f, 10f, new Vector3(0f, 90f, 0f), new Color(0.26f, 0.37f, 0.26f), 1.2f);
        }

        private static void CreateMountains(Transform parent)
        {
            Transform root = NewRoot("Pond_Mountains", parent).transform;
            PlacePrefab(Mountain01Path, "Mountain_BackLeft", root, new Vector3(-54f, -10f, 70f), new Vector3(0f, 13f, 0f), Vector3.one * 1.82f);
            PlacePrefab(Mountain02Path, "Mountain_BackRight", root, new Vector3(54f, -10f, 72f), new Vector3(0f, -13f, 0f), Vector3.one * 1.78f);
            PlacePrefab(Mountain02Path, "Mountain_BackMidLeft", root, new Vector3(-24f, -16f, 92f), new Vector3(0f, 5f, 0f), Vector3.one * 1.42f);
            PlacePrefab(Mountain01Path, "Mountain_BackMidRight", root, new Vector3(27f, -16f, 94f), new Vector3(0f, -6f, 0f), Vector3.one * 1.36f);
            PlacePrefab(Mountain01Path, "Mountain_BackLowCenter", root, new Vector3(0f, -19f, 105f), new Vector3(0f, 0f, 0f), Vector3.one * 1.22f);
            PlacePrefab(Mountain02Path, "Mountain_FarLayer_Left", root, new Vector3(-82f, -22f, 124f), new Vector3(0f, 18f, 0f), Vector3.one * 1.52f);
            PlacePrefab(Mountain01Path, "Mountain_FarLayer_MidLeft", root, new Vector3(-38f, -24f, 138f), new Vector3(0f, 8f, 0f), Vector3.one * 1.26f);
            PlacePrefab(Mountain02Path, "Mountain_FarLayer_Center", root, new Vector3(8f, -25f, 146f), new Vector3(0f, -2f, 0f), Vector3.one * 1.18f);
            PlacePrefab(Mountain01Path, "Mountain_FarLayer_MidRight", root, new Vector3(43f, -24f, 137f), new Vector3(0f, -8f, 0f), Vector3.one * 1.28f);
            PlacePrefab(Mountain02Path, "Mountain_FarLayer_Right", root, new Vector3(86f, -22f, 124f), new Vector3(0f, -18f, 0f), Vector3.one * 1.5f);
            PlacePrefab(Mountain01Path, "Mountain_Distant_Silhouette_Left", root, new Vector3(-118f, -30f, 160f), new Vector3(0f, 24f, 0f), Vector3.one * 1.65f);
            PlacePrefab(Mountain02Path, "Mountain_Distant_Silhouette_Right", root, new Vector3(120f, -30f, 162f), new Vector3(0f, -24f, 0f), Vector3.one * 1.62f);
            PlacePrefab(Mountain01Path, "Mountain_LeftWrap", root, new Vector3(-98f, -15f, 62f), new Vector3(0f, 30f, 0f), Vector3.one * 1.7f);
            PlacePrefab(Mountain02Path, "Mountain_RightWrap", root, new Vector3(98f, -15f, 62f), new Vector3(0f, -30f, 0f), Vector3.one * 1.66f);
            ApplyKoreanMountainForestMaterials(root.gameObject);
        }

        private static void CreateRocks(Transform parent)
        {
            Transform root = NewRoot("Pond_Rocks", parent).transform;
            CreateShoreMudPatches(root);

            Vector3[] clusterCenters =
            {
                new(-52f, 0.03f, 10f),
                new(-35f, 0.03f, 29f),
                new(31f, 0.03f, 31f),
                new(55f, 0.03f, 11f),
                new(-10f, 0.03f, 34f),
                new(15f, 0.03f, 35f),
                new(-22f, 0.03f, 15f),
                new(22f, 0.03f, 15f),
                new(-12f, 0.03f, 24f),
                new(12f, 0.03f, 24f),
                new(-64f, 0.03f, 27f),
                new(64f, 0.03f, 27f)
            };

            int index = 0;
            foreach (Vector3 center in clusterCenters)
            {
                int count = Mathf.Abs(center.x) > 45f ? 9 : 7;
                for (int i = 0; i < count; i++)
                {
                    float angle = (i * 137f + index * 19f) * Mathf.Deg2Rad;
                    float radius = 1.2f + (i % 4) * 0.9f;
                    Vector3 position = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius * 0.65f);
                    float scale = 0.36f + (i % 5) * 0.08f;
                    string path = (i + index) % 3 == 0 ? Rock05Path : Rock10Path;
                    PlacePrefab(path, $"ShoreRock_{index + 1:00}", root, position, new Vector3(0f, index * 41f, 0f), Vector3.one * scale);
                    index++;
                }
            }
        }

        private static void CreateShoreMudPatches(Transform parent)
        {
            CreateShorePatch(parent, "Rear_Shore_MudBand", OffsetForward(new Vector3(0f, 0.035f, 39f)), 132f, 5f, Vector3.zero, ShoreMudColor, 0.9f);
            CreateShorePatch(parent, "Left_Shore_MudPatch", OffsetForward(new Vector3(-55f, 0.035f, 18f)), 24f, 14f, new Vector3(0f, -8f, 0f), new Color(0.39f, 0.36f, 0.29f), 0.7f);
            CreateShorePatch(parent, "Right_Shore_MudPatch", OffsetForward(new Vector3(55f, 0.035f, 18f)), 24f, 14f, new Vector3(0f, 8f, 0f), new Color(0.39f, 0.36f, 0.29f), 0.7f);
        }

        private static void CreateShorePatch(Transform parent, string name, Vector3 center, float width, float depth, Vector3 euler, Color color, float roughness)
        {
            const int segmentCount = 18;
            GameObject shore = new(name);
            shore.transform.SetParent(parent, false);
            shore.transform.localPosition = center;
            shore.transform.localRotation = Quaternion.Euler(euler);

            Mesh mesh = new();
            Vector3[] vertices = new Vector3[(segmentCount + 1) * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[segmentCount * 6];

            for (int i = 0; i <= segmentCount; i++)
            {
                float t = i / (float)segmentCount;
                float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, t);
                float wobble = Mathf.Sin(i * 1.17f) * roughness + Mathf.Cos(i * 0.73f) * roughness * 0.55f;
                vertices[i * 2] = new Vector3(x, 0f, -depth * 0.5f + wobble);
                vertices[i * 2 + 1] = new Vector3(x, 0f, depth * 0.5f + wobble * 0.35f);
                uv[i * 2] = new Vector2(t, 0f);
                uv[i * 2 + 1] = new Vector2(t, 1f);
            }

            for (int i = 0; i < segmentCount; i++)
            {
                int vertex = i * 2;
                int triangle = i * 6;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 1;
                triangles[triangle + 2] = vertex + 3;
                triangles[triangle + 3] = vertex;
                triangles[triangle + 4] = vertex + 3;
                triangles[triangle + 5] = vertex + 2;
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            MeshFilter filter = shore.AddComponent<MeshFilter>();
            MeshRenderer renderer = shore.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = CreateLitMaterial(color);
        }

        private static void CreateTrees(Transform parent, DevFishEnvironmentLayoutSO layout)
        {
            Transform root = NewRoot("Pond_Trees", parent).transform;
            int index = 0;
            index = CreateTreeCluster(root, index, new Vector3(-60f, 0f, 17f), 22, 16f, 10f, new Vector2(0.82f, 1.18f), true);
            index = CreateTreeCluster(root, index, new Vector3(60f, 0f, 18f), 22, 16f, 10f, new Vector2(0.82f, 1.18f), true);
            index = CreateTreeCluster(root, index, new Vector3(-43f, 0f, 38f), 24, 20f, 8f, new Vector2(0.62f, 0.98f), false);
            index = CreateTreeCluster(root, index, new Vector3(43f, 0f, 38f), 24, 20f, 8f, new Vector2(0.62f, 0.98f), false);
            index = CreateTreeCluster(root, index, new Vector3(-16f, 0f, 45f), 15, 14f, 5f, new Vector2(0.5f, 0.78f), false);
            index = CreateTreeCluster(root, index, new Vector3(18f, 0f, 45f), 15, 14f, 5f, new Vector2(0.5f, 0.78f), false);
            index = CreateTreeCluster(root, index, new Vector3(-82f, 0f, 34f), 12, 10f, 9f, new Vector2(0.66f, 0.96f), true);
            CreateTreeCluster(root, index, new Vector3(82f, 0f, 34f), 12, 10f, 9f, new Vector2(0.66f, 0.96f), true);
        }

        private static void CreateGrass(Transform parent, DevFishEnvironmentLayoutSO layout)
        {
            Transform root = NewRoot("Pond_Grass", parent).transform;
            string[] grassPaths = { Grass11Path, Grass15Path };

            int index = 0;
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(-48f, 0f, 10f), 58, 10f, 4.8f, new Vector2(0.46f, 0.68f), false);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(48f, 0f, 11f), 58, 10f, 4.8f, new Vector2(0.46f, 0.68f), false);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(-36f, 0f, 31f), 66, 14f, 4.4f, new Vector2(0.46f, 0.64f), true);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(36f, 0f, 32f), 66, 14f, 4.4f, new Vector2(0.46f, 0.64f), true);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(-10f, 0f, 36f), 42, 11f, 3.2f, new Vector2(0.44f, 0.62f), true);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(12f, 0f, 36f), 42, 11f, 3.2f, new Vector2(0.44f, 0.62f), true);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(-28f, -0.04f, 7f), 48, 8f, 2.4f, new Vector2(0.46f, 0.7f), true);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(26f, -0.04f, 8f), 48, 8f, 2.4f, new Vector2(0.46f, 0.7f), true);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(0f, -0.04f, 29f), 42, 15f, 2f, new Vector2(0.44f, 0.66f), true);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(-15f, -0.04f, 13f), 42, 7f, 2f, new Vector2(0.42f, 0.64f), true);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(15f, -0.04f, 13f), 42, 7f, 2f, new Vector2(0.42f, 0.64f), true);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(-62f, 0f, 25f), 36, 8f, 4f, new Vector2(0.44f, 0.62f), false);
            CreateGrassCluster(root, layout, grassPaths, index, new Vector3(62f, 0f, 25f), 36, 8f, 4f, new Vector2(0.44f, 0.62f), false);
        }

        private static int CreateTreeCluster(Transform parent, int startIndex, Vector3 center, int count, float radiusX, float radiusZ, Vector2 scaleRange, bool frameOnly)
        {
            int index = startIndex;
            for (int i = 0; i < count; i++)
            {
                float angle = (i * 137.5f + startIndex * 11f) * Mathf.Deg2Rad;
                float radius = Mathf.Sqrt((i + 1f) / count);
                float x = center.x + Mathf.Cos(angle) * radiusX * radius;
                float z = center.z + Mathf.Sin(angle) * radiusZ * radius;

                if (!frameOnly && Mathf.Abs(x) < 10f && z < 45f)
                {
                    x += x < 0f ? -10f : 10f;
                }

                float scaleT = (i % 7) / 6f;
                float scale = Mathf.Lerp(scaleRange.x, scaleRange.y, scaleT);
                PlaceTree(parent, index++, new Vector3(x, 0f, z), scale);
            }

            return index;
        }

        private static int CreateGrassCluster(Transform parent, DevFishEnvironmentLayoutSO layout, string[] grassPaths, int startIndex, Vector3 center, int count, float radiusX, float radiusZ, Vector2 scaleRange, bool reeds)
        {
            int index = startIndex;
            for (int i = 0; i < count; i++)
            {
                float angle = (i * 109f + startIndex * 7f) * Mathf.Deg2Rad;
                float radius = Mathf.Sqrt((i + 0.5f) / count);
                Vector3 position = center + new Vector3(Mathf.Cos(angle) * radiusX * radius, 0f, Mathf.Sin(angle) * radiusZ * radius);
                float scale = Mathf.Lerp(scaleRange.x, scaleRange.y, (i % 6) / 5f);

                if (reeds || i % 3 == 0)
                {
                    Vector3 reedScale = Vector3.one * scale;
                    reedScale.y *= 1.15f;
                    CreateFallbackGrass(parent, layout, position, new Vector3(0f, index * 29f, 0f), reedScale);
                }
                else
                {
                    PlacePrefab(grassPaths[index % grassPaths.Length], $"Grass_{index + 1:00}", parent, position, new Vector3(0f, index * 37f, 0f), Vector3.one * scale);
                }

                index++;
            }

            return index;
        }

        private static void CleanupLegacySceneScenery()
        {
            string[] names =
            {
                "Pond_BackMountain",
                "Pond_LeftHill",
                "Pond_RightHill",
                "Pond_Mountains",
                "Pond_KoreanFishingSiteBackdrop",
                "Pond_FarRidges",
                "Pond_Trees",
                "Pond_Rocks",
                "Pond_Ground",
                "Pond_Water",
                "Pond_WaterDepthBands"
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
            float rotation = index * 47f + Mathf.Sin(index * 1.37f) * 18f;
            PlacePrefab(TreePaths[index % TreePaths.Length], $"Tree_{index + 1:00}", parent, position, new Vector3(0f, rotation, 0f), Vector3.one * scale);
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

        private static void ApplyKoreanMountainForestMaterials(GameObject mountainRoot)
        {
            Material[] materials = LoadMountainForestMaterials();
            if (materials.Length == 0)
            {
                Debug.LogWarning("[DevFishEnvironmentRuntimeBuilder] Missing Korean mountain forest tile textures.");
                return;
            }

            int rendererIndex = 0;
            foreach (Renderer renderer in mountainRoot.GetComponentsInChildren<Renderer>(true))
            {
                Material selected = materials[rendererIndex % materials.Length];
                Material[] slots = renderer.sharedMaterials;
                if (slots == null || slots.Length == 0)
                {
                    renderer.sharedMaterial = selected;
                }
                else
                {
                    for (int i = 0; i < slots.Length; i++)
                    {
                        slots[i] = selected;
                    }

                    renderer.sharedMaterials = slots;
                }

                rendererIndex++;
            }
        }

        private static Material[] LoadMountainForestMaterials()
        {
            Material[] materials = new Material[MountainForestTexturePaths.Length];
            int count = 0;

            foreach (string path in MountainForestTexturePaths)
            {
                ConfigureMountainTextureImport(path);

                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null)
                {
                    continue;
                }

                materials[count] = CreateMountainForestMaterial(texture, count);
                count++;
            }

            if (count == materials.Length)
            {
                return materials;
            }

            if (count == 0)
            {
                return CreateProceduralMountainForestMaterials();
            }

            Material[] compact = new Material[count];
            for (int i = 0; i < count; i++)
            {
                compact[i] = materials[i];
            }

            return compact;
        }

        private static void ConfigureMountainTextureImport(string assetPath)
        {
            if (!File.Exists(ToProjectAbsolutePath(assetPath)))
            {
                return;
            }

            bool changed = false;
            if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
            {
                if (importer.textureType != TextureImporterType.Default)
                {
                    importer.textureType = TextureImporterType.Default;
                    changed = true;
                }

                if (!importer.sRGBTexture)
                {
                    importer.sRGBTexture = true;
                    changed = true;
                }

                if (importer.wrapMode != TextureWrapMode.Repeat)
                {
                    importer.wrapMode = TextureWrapMode.Repeat;
                    changed = true;
                }

                if (importer.filterMode != FilterMode.Trilinear)
                {
                    importer.filterMode = FilterMode.Trilinear;
                    changed = true;
                }

                if (importer.maxTextureSize < 2048)
                {
                    importer.maxTextureSize = 2048;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }

            if (!changed)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            }
        }

        private static string ToProjectAbsolutePath(string assetPath)
        {
            if (!assetPath.StartsWith("Assets/"))
            {
                return assetPath;
            }

            string relativePath = assetPath.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(Application.dataPath, relativePath);
        }

        private static Material[] CreateProceduralMountainForestMaterials()
        {
            Material[] materials = new Material[4];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = CreateMountainForestMaterial(CreateProceduralJirisanTexture(i), i);
            }

            return materials;
        }

        private static Texture2D CreateProceduralJirisanTexture(int seed)
        {
            const int size = 256;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, true)
            {
                name = $"Generated_JirisanForest_{seed + 1}",
                hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 2
            };

            Color low = new(0.16f, 0.36f, 0.13f);
            Color mid = new(0.33f, 0.58f, 0.21f);
            Color high = new(0.62f, 0.78f, 0.32f);
            float phase = seed * 12.37f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + phase) / size;
                    float ny = (y - phase) / size;
                    float canopy = Mathf.PerlinNoise(nx * 8.5f, ny * 8.5f);
                    canopy += Mathf.PerlinNoise(nx * 23.0f + 5.1f, ny * 23.0f + 9.7f) * 0.35f;
                    canopy = Mathf.Clamp01(canopy * 0.72f);

                    Color color = Color.Lerp(low, mid, canopy);
                    color = Color.Lerp(color, high, Mathf.Clamp01((canopy - 0.48f) * 1.35f));
                    color *= RandomShade(x, y, seed);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(true, false);
            return texture;
        }

        private static float RandomShade(int x, int y, int seed)
        {
            int hash = x * 73856093 ^ y * 19349663 ^ seed * 83492791;
            hash = (hash << 13) ^ hash;
            return 0.92f + (1f - ((hash * (hash * hash * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f) * 0.08f;
        }

        private static Material CreateMountainForestMaterial(Texture2D texture, int index)
        {
            Shader shader = Shader.Find(MountainTriplanarShaderName)
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");
            Material material = new(shader)
            {
                name = $"MAT_Runtime_MountainForest_{index + 1:00}",
                hideFlags = HideFlags.DontSave,
                mainTexture = texture,
                color = new Color(0.34f, 0.52f, 0.24f)
            };

            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = Mathf.Max(texture.anisoLevel, 2);

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", new Color(0.34f, 0.52f, 0.24f));
            }

            if (material.HasProperty("_ForestTint"))
            {
                material.SetColor("_ForestTint", new Color(0.34f, 0.52f, 0.24f));
            }

            if (material.HasProperty("_RockTint"))
            {
                material.SetColor("_RockTint", new Color(0.34f, 0.31f, 0.24f));
            }

            if (material.HasProperty("_HeightTint"))
            {
                material.SetColor("_HeightTint", new Color(0.46f, 0.52f, 0.4f));
            }

            if (material.HasProperty("_FogColor"))
            {
                material.SetColor("_FogColor", new Color(0.55f, 0.66f, 0.7f));
            }

            if (material.HasProperty("_TextureScale"))
            {
                material.SetFloat("_TextureScale", 0.075f + (index % 4) * 0.0075f);
            }

            if (material.HasProperty("_DetailScale"))
            {
                material.SetFloat("_DetailScale", 0.26f + (index % 3) * 0.035f);
            }

            if (material.HasProperty("_BlendSharpness"))
            {
                material.SetFloat("_BlendSharpness", 4.5f);
            }

            if (material.HasProperty("_Brightness"))
            {
                material.SetFloat("_Brightness", 1.05f);
            }

            if (material.HasProperty("_PhotoStrength"))
            {
                material.SetFloat("_PhotoStrength", 0.78f);
            }

            if (material.HasProperty("_Contrast"))
            {
                material.SetFloat("_Contrast", 1.08f);
            }

            if (material.HasProperty("_Saturation"))
            {
                material.SetFloat("_Saturation", 1.05f);
            }

            if (material.HasProperty("_NoiseStrength"))
            {
                material.SetFloat("_NoiseStrength", 0.16f);
            }

            if (material.HasProperty("_SlopeRockStrength"))
            {
                material.SetFloat("_SlopeRockStrength", 0.38f);
            }

            if (material.HasProperty("_HeightRockStrength"))
            {
                material.SetFloat("_HeightRockStrength", 0.24f);
            }

            if (material.HasProperty("_FogBlend"))
            {
                material.SetFloat("_FogBlend", 0.18f);
            }

            if (material.HasProperty("_HeightStart"))
            {
                material.SetFloat("_HeightStart", -18f);
            }

            if (material.HasProperty("_HeightRange"))
            {
                material.SetFloat("_HeightRange", 58f);
            }

            if (material.HasProperty("_DebugMode"))
            {
                material.SetFloat("_DebugMode", 0f);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.12f);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            return material;
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
                blade.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(i % 2 == 0 ? ReedColor : SoftGrassColor);
            }
        }

        private static void AssignMaterial(GameObject target, string path)
        {
            AssignMaterial(target, path, Color.white);
        }

        private static void AssignMaterial(GameObject target, string path, Color tint)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null && target.TryGetComponent(out Renderer renderer))
            {
                Material instance = new(material) { hideFlags = HideFlags.DontSave };

                if (tint != Color.white)
                {
                    if (instance.HasProperty("_Color"))
                    {
                        instance.color = tint;
                    }

                    if (instance.HasProperty("_BaseColor"))
                    {
                        instance.SetColor("_BaseColor", tint);
                    }
                }

                renderer.sharedMaterial = instance;
            }
        }

        private static Material CreateLitMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader) { color = color, hideFlags = HideFlags.DontSave };
            return material;
        }

        private static Material CreateTransparentMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            Material material = new(shader) { color = color, hideFlags = HideFlags.DontSave };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetInt("_ZWrite", 0);
            }

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return material;
        }

        private static Material CreateBackdropImageMaterial(Texture2D texture, Color tint)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
            Material material = new(shader) { color = tint, hideFlags = HideFlags.DontSave };
            material.mainTexture = texture;

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tint);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            }

            if (tint.a < 0.999f)
            {
                ConfigureTransparentMaterial(material);
            }

            return material;
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetInt("_ZWrite", 0);
            }

            if (material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 0f);
            }

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
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
