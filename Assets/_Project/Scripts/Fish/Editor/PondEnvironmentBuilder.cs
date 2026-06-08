#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using VirtualFishing.Core.Fish;
using VirtualFishing.Data;

namespace VirtualFishing.EditorTools
{
    public static class PondEnvironmentBuilder
    {
        // ── Folder paths ──────────────────────────────────────────────────────────
        private const string MaterialFolder    = "Assets/Art/Environment/Materials/Baked";
        private const string MeshFolder        = "Assets/Art/Environment/Pond/Meshes";
        private const string PrefabFolder      = "Assets/Art/Environment/Pond/Prefabs";
        private const string SitePondAssetPath = "Assets/_Project/SO/FishDB/Test/Site_Pond.asset";
        private const string LayoutAssetPath   = "Assets/Art/Environment/Resources/DevFish/DevFishReservoirLayout.asset";

        // ── Art asset paths (mirrors DevFishEnvironmentRuntimeBuilder) ────────────
        private const string Mountain01Path      = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs/PP_Forest_Mountain_Moss_01.prefab";
        private const string Mountain02Path      = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs/PP_Forest_Mountain_Moss_02.prefab";
        private const string Rock05Path          = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs/PP_Rock_Pile_Forest_Moss_05.prefab";
        private const string Rock10Path          = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs/PP_Rock_Pile_Forest_Moss_10.prefab";
        private const string Grass11Path         = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs/PP_Grass_11.prefab";
        private const string Grass15Path         = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Prefabs/PP_Grass_15.prefab";
        private const string GroundMaterialPath  = "Assets/Art/Environment/Pond/Models/PurePoly_Selected/Materials/PP_Ground.mat";
        private const string WaterMaterialPath   = "Assets/Art/Environment/Water/Simple Water Shader/Resources/Water_mat_03.mat";
        private const string BackdropTexturePath = "Assets/Art/Environment/Backdrops/fish_eagle_hill_polyhaven_4k.jpg";
        private const string MountainShaderName  = "VirtualFishing/FishMountainTriplanar";
        private const float  ScenicForwardOffset = -9f;
        private const float  BackdropRadius      = 155f;

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
            "Assets/Art/Environment/Pond/Textures/MountainForestTiles/korean_forest_tile_seoraksan_04.png",
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
            "Assets/Art/Environment/Pond/Models/Pack_FREE_Trees/Prefabs/Tree_20_Smooth.prefab",
        };

        private static readonly Color EarthColor     = new(0.34f, 0.31f, 0.26f);
        private static readonly Color ShoreMudColor  = new(0.42f, 0.39f, 0.31f);
        private static readonly Color SoftGrassColor = new(0.36f, 0.47f, 0.25f);
        private static readonly Color ReedColor      = new(0.45f, 0.50f, 0.25f);
        private static readonly Color FarRidgeColor  = new(0.28f, 0.39f, 0.29f);

        private static readonly Dictionary<string, Material> s_matCache = new();

        // ─────────────────────────────────────────────────────────────────────────
        [MenuItem("VirtualFishing/Fish/Build Site Pond Environment")]
        public static void BuildSitePondEnvironment()
        {
            s_matCache.Clear();

            EnsureFolder("Assets/Art/Environment/Materials", "Baked");
            EnsureFolder("Assets/Art/Environment/Pond",      "Meshes");
            EnsureFolder("Assets/Art/Environment/Pond",      "Prefabs");

            DevFishEnvironmentLayoutSO layout =
                AssetDatabase.LoadAssetAtPath<DevFishEnvironmentLayoutSO>(LayoutAssetPath);

            if (layout == null)
                Debug.LogWarning($"[PondEnvironmentBuilder] Layout SO not found at {LayoutAssetPath}. Fallback grass will be skipped.");

            GameObject root = new("PF_Site_Pond_Environment");

            ApplySoftReservoirAtmosphere();
            CreateGround(root.transform);
            CreateWater(root.transform);
            CreateWaterDepthBands(root.transform);
            CreateKoreanFishingSiteBackdrop(root.transform);
            CreateMountains(root.transform);
            CreateRocks(root.transform);
            CreateTrees(root.transform);
            CreateGrass(root.transform, layout);

            string prefabPath = $"{PrefabFolder}/PF_Site_Pond_Environment.prefab";
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            AssignEnvironmentPrefabToSite(prefabAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PondEnvironmentBuilder] Site_Pond environment baked successfully.");
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Atmosphere
        // ─────────────────────────────────────────────────────────────────────────

        private static void ApplySoftReservoirAtmosphere()
        {
            RenderSettings.fog                 = false;
            RenderSettings.ambientMode         = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = new Color(0.90f, 0.96f, 1.00f);
            RenderSettings.ambientEquatorColor = new Color(0.70f, 0.76f, 0.72f);
            RenderSettings.ambientGroundColor  = new Color(0.46f, 0.44f, 0.40f);

            foreach (Light sun in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (sun.type != LightType.Directional) continue;
                sun.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
                sun.color              = new Color(1f, 0.96f, 0.86f);
                sun.intensity          = 1.08f;
                break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Ground
        // ─────────────────────────────────────────────────────────────────────────

        private static void CreateGround(Transform parent)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Pond_Ground";
            ground.transform.SetParent(parent, false);
            ground.transform.localPosition = OffsetForward(new Vector3(0f, -0.02f, 18f));
            ground.transform.localScale    = new Vector3(17f, 1f, 11.5f);
            RemoveColliders(ground);
            AssignMaterialFromSource(ground, "MAT_Baked_Ground", GroundMaterialPath, EarthColor);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Water  (PondWaterSurface re-builds its mesh automatically on OnEnable)
        // ─────────────────────────────────────────────────────────────────────────

        private static void CreateWater(Transform parent)
        {
            GameObject water = new("Pond_Water");
            water.transform.SetParent(parent, false);
            water.transform.localPosition = OffsetForward(new Vector3(0f, 0.14f, 11.2f));

            water.AddComponent<MeshFilter>();
            MeshRenderer rend = water.AddComponent<MeshRenderer>();
            water.AddComponent<MeshCollider>();
            water.AddComponent<PondWaterSurface>().RebuildMesh();

            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows    = false;
            AssignMaterialFromSource(water, "MAT_Baked_Water", WaterMaterialPath, Color.white);

            int waterLayer = LayerMask.NameToLayer("Water");
            if (waterLayer >= 0) water.layer = waterLayer;
            try { water.tag = "Water"; } catch (UnityException) { }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Water depth bands
        // ─────────────────────────────────────────────────────────────────────────

        private static void CreateWaterDepthBands(Transform parent)
        {
            Transform root = NewChild("Pond_WaterDepthBands", parent);
            CreateFlatTint(root, "Shallow_Edge_Left",
                OffsetForward(new Vector3(-44f, 0.160f, 11.4f)), new Vector3(5.5f, 1f, 5.8f), new Color(0.47f, 0.66f, 0.62f, 0.24f));
            CreateFlatTint(root, "Shallow_Edge_Right",
                OffsetForward(new Vector3( 44f, 0.160f, 11.8f)), new Vector3(5.5f, 1f, 5.8f), new Color(0.47f, 0.66f, 0.62f, 0.24f));
            CreateFlatTint(root, "Shallow_Rear",
                OffsetForward(new Vector3(  0f, 0.165f, 30.0f)), new Vector3(13f,  1f, 2.4f), new Color(0.48f, 0.66f, 0.58f, 0.18f));
            CreateFlatTint(root, "Deep_Center",
                OffsetForward(new Vector3(  0f, 0.155f, 13.4f)), new Vector3(8.5f, 1f, 4.8f), new Color(0.18f, 0.34f, 0.42f, 0.16f));
        }

        private static void CreateFlatTint(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = name;
            plane.transform.SetParent(parent, false);
            plane.transform.localPosition = position;
            plane.transform.localScale    = scale;
            RemoveColliders(plane);

            MeshRenderer rend = plane.GetComponent<MeshRenderer>();
            if (rend == null) return;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows    = false;
            rend.sharedMaterial    = GetOrCreateTransparentMaterial($"MAT_Baked_DepthTint_{name}", color);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Backdrop
        // ─────────────────────────────────────────────────────────────────────────

        private static void CreateKoreanFishingSiteBackdrop(Transform parent)
        {
            Transform root     = NewChild("Pond_KoreanFishingSiteBackdrop", parent);
            Texture2D backdrop = AssetDatabase.LoadAssetAtPath<Texture2D>(BackdropTexturePath);

            if (backdrop != null)
                CreatePanoramaRing(root, backdrop);
            else
                CreateFallbackHorizonRing(root);
        }

        private static void CreatePanoramaRing(Transform parent, Texture2D backdrop)
        {
            const int   bandCount = 48;
            const float bottom    = -20f;
            const float height    = 96f;
            const float uvBottom  = 0.42f;
            const float uvTop     = 0.98f;

            Transform ring = NewChild("Far_Panorama_Ring", parent);

            for (int band = 0; band < bandCount; band++)
            {
                float bandStart  =  band      / (float)bandCount;
                float bandEnd    = (band + 1) / (float)bandCount;
                float bandMiddle = (bandStart + bandEnd) * 0.5f;
                float fade       = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 1f, bandMiddle));
                float alpha      = bandMiddle < 0.42f
                    ? 1f
                    : Mathf.Lerp(0.98f, 0.03f, fade);

                CreatePanoramaBand(
                    ring, backdrop,
                    $"Far_Panorama_Band_{band + 1:00}",
                    Mathf.Lerp(bottom, height, bandStart),
                    Mathf.Lerp(bottom, height, bandEnd),
                    Mathf.Lerp(uvBottom, uvTop, bandStart),
                    Mathf.Lerp(uvBottom, uvTop, bandEnd),
                    alpha);
            }
        }

        private static void CreatePanoramaBand(
            Transform parent, Texture2D backdrop, string name,
            float bottom, float top, float uvBottom, float uvTop, float alpha)
        {
            const int segCount = 64;

            GameObject band = new(name);
            band.transform.SetParent(parent, false);
            band.transform.localPosition = Vector3.zero;

            Vector3[] vertices  = new Vector3[(segCount + 1) * 2];
            Vector2[] uv        = new Vector2[vertices.Length];
            int[]     triangles = new int[segCount * 6];

            for (int i = 0; i <= segCount; i++)
            {
                float t     = i / (float)segCount;
                float angle = t * Mathf.PI * 2f;
                float x     = Mathf.Sin(angle) * BackdropRadius;
                float z     = Mathf.Cos(angle) * BackdropRadius;
                vertices[i * 2]     = new Vector3(x, bottom, z);
                vertices[i * 2 + 1] = new Vector3(x, top,    z);
                uv[i * 2]     = new Vector2(t, uvBottom);
                uv[i * 2 + 1] = new Vector2(t, uvTop);
            }

            for (int i = 0; i < segCount; i++)
            {
                int v = i * 2, t = i * 6;
                triangles[t]     = v;     triangles[t + 1] = v + 3; triangles[t + 2] = v + 1;
                triangles[t + 3] = v;     triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }

            Mesh mesh = new() { name = name };
            mesh.vertices  = vertices;
            mesh.uv        = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            SaveMeshAsset(mesh, name);

            MeshFilter   filter = band.AddComponent<MeshFilter>();
            MeshRenderer rend   = band.AddComponent<MeshRenderer>();
            filter.sharedMesh      = mesh;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows    = false;
            rend.sharedMaterial    = GetOrCreateBackdropMaterial(
                $"MAT_Baked_Backdrop_{name}", backdrop, new Color(1.14f, 1.18f, 1.2f, alpha));
        }

        private static void CreateFallbackHorizonRing(Transform parent)
        {
            for (int i = 0; i < 8; i++)
            {
                float   angle  = i * 45f;
                float   rad    = angle * Mathf.Deg2Rad;
                Vector3 pos    = new(Mathf.Sin(rad) * BackdropRadius, 17f, Mathf.Cos(rad) * BackdropRadius);
                CreateRidgeStrip(parent, $"Far_Fallback_Horizon_{i + 1:00}", pos, 82f, 12f,
                    new Vector3(0f, angle, 0f), new Color(0.52f, 0.61f, 0.58f), 1.1f);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Mountains
        // ─────────────────────────────────────────────────────────────────────────

        private static void CreateMountains(Transform parent)
        {
            Transform root = NewChild("Pond_Mountains", parent);
            PlacePrefab(Mountain01Path, "Mountain_BackLeft",               root, new Vector3( -54f,  -10f,  70f), new Vector3(0f,   13f, 0f), Vector3.one * 1.82f);
            PlacePrefab(Mountain02Path, "Mountain_BackRight",              root, new Vector3(  54f,  -10f,  72f), new Vector3(0f,  -13f, 0f), Vector3.one * 1.78f);
            PlacePrefab(Mountain02Path, "Mountain_BackMidLeft",            root, new Vector3( -24f,  -16f,  92f), new Vector3(0f,    5f, 0f), Vector3.one * 1.42f);
            PlacePrefab(Mountain01Path, "Mountain_BackMidRight",           root, new Vector3(  27f,  -16f,  94f), new Vector3(0f,   -6f, 0f), Vector3.one * 1.36f);
            PlacePrefab(Mountain01Path, "Mountain_BackLowCenter",          root, new Vector3(   0f,  -19f, 105f), new Vector3(0f,    0f, 0f), Vector3.one * 1.22f);
            PlacePrefab(Mountain02Path, "Mountain_FarLayer_Left",          root, new Vector3( -82f,  -22f, 124f), new Vector3(0f,   18f, 0f), Vector3.one * 1.52f);
            PlacePrefab(Mountain01Path, "Mountain_FarLayer_MidLeft",       root, new Vector3( -38f,  -24f, 138f), new Vector3(0f,    8f, 0f), Vector3.one * 1.26f);
            PlacePrefab(Mountain02Path, "Mountain_FarLayer_Center",        root, new Vector3(   8f,  -25f, 146f), new Vector3(0f,   -2f, 0f), Vector3.one * 1.18f);
            PlacePrefab(Mountain01Path, "Mountain_FarLayer_MidRight",      root, new Vector3(  43f,  -24f, 137f), new Vector3(0f,   -8f, 0f), Vector3.one * 1.28f);
            PlacePrefab(Mountain02Path, "Mountain_FarLayer_Right",         root, new Vector3(  86f,  -22f, 124f), new Vector3(0f,  -18f, 0f), Vector3.one * 1.50f);
            PlacePrefab(Mountain01Path, "Mountain_Distant_Silhouette_Left",  root, new Vector3(-118f, -30f, 160f), new Vector3(0f,  24f, 0f), Vector3.one * 1.65f);
            PlacePrefab(Mountain02Path, "Mountain_Distant_Silhouette_Right", root, new Vector3( 120f, -30f, 162f), new Vector3(0f, -24f, 0f), Vector3.one * 1.62f);
            PlacePrefab(Mountain01Path, "Mountain_LeftWrap",               root, new Vector3( -98f,  -15f,  62f), new Vector3(0f,   30f, 0f), Vector3.one * 1.70f);
            PlacePrefab(Mountain02Path, "Mountain_RightWrap",              root, new Vector3(  98f,  -15f,  62f), new Vector3(0f,  -30f, 0f), Vector3.one * 1.66f);
            ApplyKoreanMountainForestMaterials(root.gameObject);
        }

        private static void ApplyKoreanMountainForestMaterials(GameObject mountainRoot)
        {
            Material[] materials = LoadMountainForestMaterials();
            if (materials.Length == 0)
            {
                Debug.LogWarning("[PondEnvironmentBuilder] No Korean mountain forest materials could be loaded.");
                return;
            }

            int idx = 0;
            foreach (Renderer rend in mountainRoot.GetComponentsInChildren<Renderer>(true))
            {
                Material selected = materials[idx % materials.Length];
                Material[] slots  = rend.sharedMaterials;
                if (slots == null || slots.Length == 0)
                {
                    rend.sharedMaterial = selected;
                }
                else
                {
                    for (int i = 0; i < slots.Length; i++) slots[i] = selected;
                    rend.sharedMaterials = slots;
                }
                idx++;
            }
        }

        private static Material[] LoadMountainForestMaterials()
        {
            var result = new List<Material>();
            for (int i = 0; i < MountainForestTexturePaths.Length; i++)
            {
                string texPath = MountainForestTexturePaths[i];
                ConfigureMountainTextureImport(texPath);
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (tex == null) continue;
                result.Add(GetOrCreateMountainForestMaterial($"MAT_Baked_MountainForest_{i + 1:00}", tex, i));
            }

            if (result.Count > 0) return result.ToArray();

            var fallback = new List<Material>();
            for (int i = 0; i < 4; i++)
            {
                Texture2D tex    = CreateProceduralJirisanTexture(i);
                string    texKey = $"TEX_Baked_JirisanProc_{i + 1:00}";
                SaveTextureAsset(tex, texKey);
                fallback.Add(GetOrCreateMountainForestMaterial($"MAT_Baked_MountainForest_Proc_{i + 1:00}", tex, i));
            }
            return fallback.ToArray();
        }

        private static void ConfigureMountainTextureImport(string assetPath)
        {
            if (!File.Exists(ToProjectAbsolutePath(assetPath))) return;
            bool changed = false;
            if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
            {
                if (importer.textureType    != TextureImporterType.Default) { importer.textureType    = TextureImporterType.Default; changed = true; }
                if (!importer.sRGBTexture)                                  { importer.sRGBTexture    = true;                        changed = true; }
                if (importer.wrapMode       != TextureWrapMode.Repeat)      { importer.wrapMode       = TextureWrapMode.Repeat;      changed = true; }
                if (importer.filterMode     != FilterMode.Trilinear)        { importer.filterMode     = FilterMode.Trilinear;        changed = true; }
                if (importer.maxTextureSize  < 2048)                        { importer.maxTextureSize = 2048;                        changed = true; }
                if (changed) importer.SaveAndReimport();
            }
            if (!changed) AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Rocks + shore mud
        // ─────────────────────────────────────────────────────────────────────────

        private static void CreateRocks(Transform parent)
        {
            Transform root = NewChild("Pond_Rocks", parent);
            CreateShoreMudPatches(root);

            Vector3[] clusterCenters =
            {
                new(-52f, 0.03f, 10f), new(-35f, 0.03f, 29f), new( 31f, 0.03f, 31f),
                new( 55f, 0.03f, 11f), new(-10f, 0.03f, 34f), new( 15f, 0.03f, 35f),
                new(-22f, 0.03f, 15f), new( 22f, 0.03f, 15f), new(-12f, 0.03f, 24f),
                new( 12f, 0.03f, 24f), new(-64f, 0.03f, 27f), new( 64f, 0.03f, 27f),
            };

            int index = 0;
            foreach (Vector3 center in clusterCenters)
            {
                int count = Mathf.Abs(center.x) > 45f ? 9 : 7;
                for (int i = 0; i < count; i++)
                {
                    float   angle = (i * 137f + index * 19f) * Mathf.Deg2Rad;
                    float   rad   = 1.2f + (i % 4) * 0.9f;
                    Vector3 pos   = center + new Vector3(Mathf.Cos(angle) * rad, 0f, Mathf.Sin(angle) * rad * 0.65f);
                    float   scale = 0.36f + (i % 5) * 0.08f;
                    string  path  = (i + index) % 3 == 0 ? Rock05Path : Rock10Path;
                    PlacePrefab(path, $"ShoreRock_{index + 1:00}", root, pos, new Vector3(0f, index * 41f, 0f), Vector3.one * scale);
                    index++;
                }
            }
        }

        private static void CreateShoreMudPatches(Transform parent)
        {
            CreateShorePatch(parent, "Rear_Shore_MudBand",
                OffsetForward(new Vector3(  0f, 0.035f, 39f)), 132f, 5f,  Vector3.zero,              ShoreMudColor,                     0.9f);
            CreateShorePatch(parent, "Left_Shore_MudPatch",
                OffsetForward(new Vector3(-55f, 0.035f, 18f)),  24f, 14f, new Vector3(0f, -8f, 0f), new Color(0.39f, 0.36f, 0.29f), 0.7f);
            CreateShorePatch(parent, "Right_Shore_MudPatch",
                OffsetForward(new Vector3( 55f, 0.035f, 18f)),  24f, 14f, new Vector3(0f,  8f, 0f), new Color(0.39f, 0.36f, 0.29f), 0.7f);
        }

        private static void CreateShorePatch(Transform parent, string name, Vector3 center, float width, float depth, Vector3 euler, Color color, float roughness)
        {
            const int segCount = 18;
            GameObject shore = new(name);
            shore.transform.SetParent(parent, false);
            shore.transform.localPosition = center;
            shore.transform.localRotation = Quaternion.Euler(euler);

            Vector3[] vertices  = new Vector3[(segCount + 1) * 2];
            Vector2[] uv        = new Vector2[vertices.Length];
            int[]     triangles = new int[segCount * 6];

            for (int i = 0; i <= segCount; i++)
            {
                float t      = i / (float)segCount;
                float x      = Mathf.Lerp(-width * 0.5f, width * 0.5f, t);
                float wobble = Mathf.Sin(i * 1.17f) * roughness + Mathf.Cos(i * 0.73f) * roughness * 0.55f;
                vertices[i * 2]     = new Vector3(x, 0f, -depth * 0.5f + wobble);
                vertices[i * 2 + 1] = new Vector3(x, 0f,  depth * 0.5f + wobble * 0.35f);
                uv[i * 2]     = new Vector2(t, 0f);
                uv[i * 2 + 1] = new Vector2(t, 1f);
            }

            for (int i = 0; i < segCount; i++)
            {
                int v = i * 2, t = i * 6;
                triangles[t]     = v;     triangles[t + 1] = v + 1; triangles[t + 2] = v + 3;
                triangles[t + 3] = v;     triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
            }

            Mesh mesh = new() { name = name };
            mesh.vertices  = vertices;
            mesh.uv        = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            SaveMeshAsset(mesh, $"MSH_Shore_{name}");

            MeshFilter   filter = shore.AddComponent<MeshFilter>();
            MeshRenderer rend   = shore.AddComponent<MeshRenderer>();
            filter.sharedMesh   = mesh;
            rend.sharedMaterial = GetOrCreateLitMaterial($"MAT_Baked_Shore_{name}", color);
        }

        private static void CreateRidgeStrip(Transform parent, string name, Vector3 center, float width, float height, Vector3 euler, Color color, float roughness)
        {
            const int segCount = 18;
            GameObject ridge = new(name);
            ridge.transform.SetParent(parent, false);
            ridge.transform.localPosition = center;
            ridge.transform.localRotation = Quaternion.Euler(euler);

            Vector3[] vertices  = new Vector3[(segCount + 1) * 2];
            int[]     triangles = new int[segCount * 6];

            for (int i = 0; i <= segCount; i++)
            {
                float t     = i / (float)segCount;
                float x     = Mathf.Lerp(-width * 0.5f, width * 0.5f, t);
                float noise = Mathf.Sin(i * 1.37f) * roughness + Mathf.Cos(i * 0.61f) * roughness * 0.45f;
                float top   = height + noise;
                vertices[i * 2]     = new Vector3(x, 0f, 0f);
                vertices[i * 2 + 1] = new Vector3(x, Mathf.Max(0.4f, top), Mathf.Sin(i * 0.9f) * 0.7f);
            }

            for (int i = 0; i < segCount; i++)
            {
                int v = i * 2, t = i * 6;
                triangles[t]     = v;     triangles[t + 1] = v + 1; triangles[t + 2] = v + 3;
                triangles[t + 3] = v;     triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
            }

            Mesh mesh = new() { name = name };
            mesh.vertices  = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            SaveMeshAsset(mesh, $"MSH_Ridge_{name}");

            MeshFilter   filter = ridge.AddComponent<MeshFilter>();
            MeshRenderer rend   = ridge.AddComponent<MeshRenderer>();
            filter.sharedMesh      = mesh;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows    = false;
            rend.sharedMaterial    = GetOrCreateLitMaterial($"MAT_Baked_Ridge_{name}", color);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Trees
        // ─────────────────────────────────────────────────────────────────────────

        private static void CreateTrees(Transform parent)
        {
            Transform root  = NewChild("Pond_Trees", parent);
            int       index = 0;
            index = CreateTreeCluster(root, index, new Vector3(-60f, 0f, 17f), 22, 16f, 10f, new Vector2(0.82f, 1.18f), true);
            index = CreateTreeCluster(root, index, new Vector3( 60f, 0f, 18f), 22, 16f, 10f, new Vector2(0.82f, 1.18f), true);
            index = CreateTreeCluster(root, index, new Vector3(-43f, 0f, 38f), 24, 20f,  8f, new Vector2(0.62f, 0.98f), false);
            index = CreateTreeCluster(root, index, new Vector3( 43f, 0f, 38f), 24, 20f,  8f, new Vector2(0.62f, 0.98f), false);
            index = CreateTreeCluster(root, index, new Vector3(-16f, 0f, 45f), 15, 14f,  5f, new Vector2(0.50f, 0.78f), false);
            index = CreateTreeCluster(root, index, new Vector3( 18f, 0f, 45f), 15, 14f,  5f, new Vector2(0.50f, 0.78f), false);
            index = CreateTreeCluster(root, index, new Vector3(-82f, 0f, 34f), 12, 10f,  9f, new Vector2(0.66f, 0.96f), true);
                    CreateTreeCluster(root, index, new Vector3( 82f, 0f, 34f), 12, 10f,  9f, new Vector2(0.66f, 0.96f), true);
        }

        private static int CreateTreeCluster(Transform parent, int startIndex, Vector3 center, int count, float radiusX, float radiusZ, Vector2 scaleRange, bool frameOnly)
        {
            int index = startIndex;
            for (int i = 0; i < count; i++)
            {
                float angle  = (i * 137.5f + startIndex * 11f) * Mathf.Deg2Rad;
                float radius = Mathf.Sqrt((i + 1f) / count);
                float x      = center.x + Mathf.Cos(angle) * radiusX * radius;
                float z      = center.z + Mathf.Sin(angle) * radiusZ * radius;

                if (!frameOnly && Mathf.Abs(x) < 10f && z < 45f)
                    x += x < 0f ? -10f : 10f;

                float scale = Mathf.Lerp(scaleRange.x, scaleRange.y, (i % 7) / 6f);
                PlaceTree(parent, index++, new Vector3(x, 0f, z), scale);
            }
            return index;
        }

        private static void PlaceTree(Transform parent, int index, Vector3 position, float scale = 1f)
        {
            float rotation = index * 47f + Mathf.Sin(index * 1.37f) * 18f;
            PlacePrefab(TreePaths[index % TreePaths.Length], $"Tree_{index + 1:00}", parent,
                position, new Vector3(0f, rotation, 0f), Vector3.one * scale);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Grass
        // ─────────────────────────────────────────────────────────────────────────

        private static void CreateGrass(Transform parent, DevFishEnvironmentLayoutSO layout)
        {
            Transform root       = NewChild("Pond_Grass", parent);
            string[]  grassPaths = { Grass11Path, Grass15Path };

            int index = 0;
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(-48f,  0f,     10f), 58, 10f, 4.8f, new Vector2(0.46f, 0.68f), false);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3( 48f,  0f,     11f), 58, 10f, 4.8f, new Vector2(0.46f, 0.68f), false);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(-36f,  0f,     31f), 66, 14f, 4.4f, new Vector2(0.46f, 0.64f), true);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3( 36f,  0f,     32f), 66, 14f, 4.4f, new Vector2(0.46f, 0.64f), true);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(-10f,  0f,     36f), 42, 11f, 3.2f, new Vector2(0.44f, 0.62f), true);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3( 12f,  0f,     36f), 42, 11f, 3.2f, new Vector2(0.44f, 0.62f), true);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(-28f, -0.04f,   7f), 48,  8f, 2.4f, new Vector2(0.46f, 0.70f), true);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3( 26f, -0.04f,   8f), 48,  8f, 2.4f, new Vector2(0.46f, 0.70f), true);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(  0f, -0.04f,  29f), 42, 15f, 2.0f, new Vector2(0.44f, 0.66f), true);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(-15f, -0.04f,  13f), 42,  7f, 2.0f, new Vector2(0.42f, 0.64f), true);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3( 15f, -0.04f,  13f), 42,  7f, 2.0f, new Vector2(0.42f, 0.64f), true);
            index = CreateGrassCluster(root, layout, grassPaths, index, new Vector3(-62f,  0f,     25f), 36,  8f, 4.0f, new Vector2(0.44f, 0.62f), false);
                    CreateGrassCluster(root, layout, grassPaths, index, new Vector3( 62f,  0f,     25f), 36,  8f, 4.0f, new Vector2(0.44f, 0.62f), false);
        }

        private static int CreateGrassCluster(
            Transform parent, DevFishEnvironmentLayoutSO layout, string[] grassPaths,
            int startIndex, Vector3 center, int count,
            float radiusX, float radiusZ, Vector2 scaleRange, bool reeds)
        {
            int index = startIndex;
            for (int i = 0; i < count; i++)
            {
                float   angle  = (i * 109f + startIndex * 7f) * Mathf.Deg2Rad;
                float   radius = Mathf.Sqrt((i + 0.5f) / count);
                Vector3 pos    = center + new Vector3(Mathf.Cos(angle) * radiusX * radius, 0f, Mathf.Sin(angle) * radiusZ * radius);
                float   scale  = Mathf.Lerp(scaleRange.x, scaleRange.y, (i % 6) / 5f);

                if (reeds || i % 3 == 0)
                {
                    if (layout != null)
                    {
                        Vector3 reedScale  = Vector3.one * scale;
                        reedScale.y       *= 1.15f;
                        CreateFallbackGrass(parent, layout, pos, new Vector3(0f, index * 29f, 0f), reedScale);
                    }
                }
                else
                {
                    PlacePrefab(grassPaths[index % grassPaths.Length], $"Grass_{index + 1:00}", parent,
                        pos, new Vector3(0f, index * 37f, 0f), Vector3.one * scale);
                }
                index++;
            }
            return index;
        }

        private static void CreateFallbackGrass(Transform parent, DevFishEnvironmentLayoutSO layout, Vector3 position, Vector3 euler, Vector3 scale)
        {
            GameObject grass = new("Grass_Fallback");
            grass.transform.SetParent(parent, false);
            grass.transform.localPosition = position;
            grass.transform.localRotation = Quaternion.Euler(euler);
            grass.transform.localScale    = scale;

            for (int i = 0; i < 7; i++)
            {
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = $"Blade_{i + 1}";
                blade.transform.SetParent(grass.transform, false);
                blade.transform.localPosition = new Vector3((i - 3) * 0.22f, layout.BladeBaseHeight + i * layout.BladeHeightStep, (i - 3) * 0.08f);
                blade.transform.localRotation = Quaternion.Euler(0f, i * 24f, 16f - i * 3f);
                blade.transform.localScale    = layout.BladeScale;
                RemoveColliders(blade);
                blade.GetComponent<Renderer>().sharedMaterial = i % 2 == 0
                    ? GetOrCreateLitMaterial("MAT_Baked_Reed",      ReedColor)
                    : GetOrCreateLitMaterial("MAT_Baked_SoftGrass", SoftGrassColor);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  GameObject helpers
        // ─────────────────────────────────────────────────────────────────────────

        private static Transform NewChild(string name, Transform parent)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void PlacePrefab(string path, string name, Transform parent, Vector3 position, Vector3 euler, Vector3 scale)
        {
            GameObject prefab    = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject instance  = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : new GameObject(name);
            instance.name                    = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(euler);
            instance.transform.localScale    = scale;
        }

        private static void RemoveColliders(GameObject go)
        {
            foreach (Collider col in go.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);
        }

        private static Vector3 OffsetForward(Vector3 pos)
        {
            pos.z += ScenicForwardOffset;
            return pos;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Material helpers  –  all materials are saved as persistent assets
        // ─────────────────────────────────────────────────────────────────────────

        private static void AssignMaterialFromSource(GameObject target, string matName, string sourcePath, Color tint)
        {
            if (!s_matCache.TryGetValue(matName, out Material mat))
            {
                string assetPath = $"{MaterialFolder}/{matName}.mat";
                mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                if (mat == null)
                {
                    Material source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
                    mat      = source != null ? new Material(source) : new Material(FindShader("Universal Render Pipeline/Lit"));
                    mat.name = matName;
                    if (tint != Color.white) ApplyColor(mat, tint);
                    AssetDatabase.CreateAsset(mat, assetPath);
                }
                s_matCache[matName] = mat;
            }
            Renderer rend = target.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = mat;
        }

        private static Material GetOrCreateLitMaterial(string matName, Color color)
        {
            if (s_matCache.TryGetValue(matName, out Material cached)) return cached;
            string assetPath = $"{MaterialFolder}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(FindShader("Universal Render Pipeline/Lit")) { name = matName };
                ApplyColor(mat, color);
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            s_matCache[matName] = mat;
            return mat;
        }

        private static Material GetOrCreateTransparentMaterial(string matName, Color color)
        {
            if (s_matCache.TryGetValue(matName, out Material cached)) return cached;
            string assetPath = $"{MaterialFolder}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(FindShader("Universal Render Pipeline/Unlit") ?? FindShader("Standard")) { name = matName };
                ApplyColor(mat, color);
                ConfigureTransparent(mat);
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            s_matCache[matName] = mat;
            return mat;
        }

        private static Material GetOrCreateBackdropMaterial(string matName, Texture2D texture, Color tint)
        {
            if (s_matCache.TryGetValue(matName, out Material cached)) return cached;
            string assetPath = $"{MaterialFolder}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat             = new Material(FindShader("Universal Render Pipeline/Unlit") ?? FindShader("Standard")) { name = matName };
                mat.mainTexture = texture;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
                ApplyColor(mat, tint);
                if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)CullMode.Off);
                if (tint.a < 0.999f) ConfigureTransparent(mat);
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            s_matCache[matName] = mat;
            return mat;
        }

        private static Material GetOrCreateMountainForestMaterial(string matName, Texture2D texture, int index)
        {
            if (s_matCache.TryGetValue(matName, out Material cached)) return cached;
            string assetPath = $"{MaterialFolder}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                Shader shader = Shader.Find(MountainShaderName) ?? FindShader("Universal Render Pipeline/Lit");
                mat           = new Material(shader) { name = matName, mainTexture = texture };
                ApplyColor(mat, new Color(0.34f, 0.52f, 0.24f));
                if (mat.HasProperty("_BaseMap"))           mat.SetTexture("_BaseMap",           texture);
                if (mat.HasProperty("_ForestTint"))        mat.SetColor("_ForestTint",           new Color(0.34f, 0.52f, 0.24f));
                if (mat.HasProperty("_RockTint"))          mat.SetColor("_RockTint",             new Color(0.34f, 0.31f, 0.24f));
                if (mat.HasProperty("_HeightTint"))        mat.SetColor("_HeightTint",           new Color(0.46f, 0.52f, 0.40f));
                if (mat.HasProperty("_FogColor"))          mat.SetColor("_FogColor",             new Color(0.55f, 0.66f, 0.70f));
                if (mat.HasProperty("_TextureScale"))      mat.SetFloat("_TextureScale",         0.075f + (index % 4) * 0.0075f);
                if (mat.HasProperty("_DetailScale"))       mat.SetFloat("_DetailScale",          0.260f + (index % 3) * 0.035f);
                if (mat.HasProperty("_BlendSharpness"))    mat.SetFloat("_BlendSharpness",       4.5f);
                if (mat.HasProperty("_Brightness"))        mat.SetFloat("_Brightness",           1.05f);
                if (mat.HasProperty("_PhotoStrength"))     mat.SetFloat("_PhotoStrength",        0.78f);
                if (mat.HasProperty("_Contrast"))          mat.SetFloat("_Contrast",             1.08f);
                if (mat.HasProperty("_Saturation"))        mat.SetFloat("_Saturation",           1.05f);
                if (mat.HasProperty("_NoiseStrength"))     mat.SetFloat("_NoiseStrength",        0.16f);
                if (mat.HasProperty("_SlopeRockStrength")) mat.SetFloat("_SlopeRockStrength",    0.38f);
                if (mat.HasProperty("_HeightRockStrength"))mat.SetFloat("_HeightRockStrength",   0.24f);
                if (mat.HasProperty("_FogBlend"))          mat.SetFloat("_FogBlend",             0.18f);
                if (mat.HasProperty("_HeightStart"))       mat.SetFloat("_HeightStart",        -18f);
                if (mat.HasProperty("_HeightRange"))       mat.SetFloat("_HeightRange",          58f);
                if (mat.HasProperty("_DebugMode"))         mat.SetFloat("_DebugMode",            0f);
                if (mat.HasProperty("_Smoothness"))        mat.SetFloat("_Smoothness",           0.12f);
                if (mat.HasProperty("_Metallic"))          mat.SetFloat("_Metallic",             0f);
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            s_matCache[matName] = mat;
            return mat;
        }

        private static void ApplyColor(Material mat, Color color)
        {
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        }

        private static void ConfigureTransparent(Material mat)
        {
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (mat.HasProperty("_Surface"))   mat.SetFloat("_Surface",  1f);
            if (mat.HasProperty("_Blend"))     mat.SetFloat("_Blend",    0f);
            if (mat.HasProperty("_SrcBlend"))  mat.SetInt("_SrcBlend",   (int)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend"))  mat.SetInt("_DstBlend",   (int)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite"))    mat.SetInt("_ZWrite",     0);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        private static Shader FindShader(string name) => Shader.Find(name);

        // ─────────────────────────────────────────────────────────────────────────
        //  Mesh helpers  –  all meshes are saved as persistent assets
        // ─────────────────────────────────────────────────────────────────────────

        private static void SaveMeshAsset(Mesh mesh, string meshName)
        {
            string assetPath = $"{MeshFolder}/{meshName}.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(mesh, assetPath);
        }

        private static void SaveTextureAsset(Texture2D texture, string texName)
        {
            string assetPath = $"{MaterialFolder}/{texName}.asset";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(texture, assetPath);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Procedural texture generation (fallback when no file textures are found)
        // ─────────────────────────────────────────────────────────────────────────

        private static Texture2D CreateProceduralJirisanTexture(int seed)
        {
            const int size = 256;
            Texture2D tex  = new(size, size, TextureFormat.RGBA32, true)
            {
                name       = $"Generated_JirisanForest_{seed + 1}",
                wrapMode   = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 2,
            };

            Color low   = new(0.16f, 0.36f, 0.13f);
            Color mid   = new(0.33f, 0.58f, 0.21f);
            Color high  = new(0.62f, 0.78f, 0.32f);
            float phase = seed * 12.37f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx     = (x + phase) / size;
                    float ny     = (y - phase) / size;
                    float canopy = Mathf.PerlinNoise(nx * 8.5f,  ny * 8.5f)
                                 + Mathf.PerlinNoise(nx * 23.0f + 5.1f, ny * 23.0f + 9.7f) * 0.35f;
                    canopy = Mathf.Clamp01(canopy * 0.72f);
                    Color c = Color.Lerp(low, mid,  canopy);
                    c       = Color.Lerp(c,   high, Mathf.Clamp01((canopy - 0.48f) * 1.35f));
                    c      *= RandomShade(x, y, seed);
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply(true, false);
            return tex;
        }

        private static float RandomShade(int x, int y, int seed)
        {
            int hash = x * 73856093 ^ y * 19349663 ^ seed * 83492791;
            hash = (hash << 13) ^ hash;
            return 0.92f + (1f - ((hash * (hash * hash * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f) * 0.08f;
        }

        private static string ToProjectAbsolutePath(string assetPath)
        {
            if (!assetPath.StartsWith("Assets/")) return assetPath;
            string rel = assetPath.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(Application.dataPath, rel);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Site assignment
        // ─────────────────────────────────────────────────────────────────────────

        private static void AssignEnvironmentPrefabToSite(GameObject environmentPrefab)
        {
            FishingSiteDataSO siteData = AssetDatabase.LoadAssetAtPath<FishingSiteDataSO>(SitePondAssetPath);
            if (siteData == null)
            {
                Debug.LogWarning($"[PondEnvironmentBuilder] Could not find site asset: {SitePondAssetPath}");
                return;
            }
            SerializedObject so = new(siteData);
            so.FindProperty("environmentPrefab").objectReferenceValue = environmentPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(siteData);
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string target = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(target))
                AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
