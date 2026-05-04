using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using System.IO;

public static class ArkPrototypeSceneBuilder
{
    private const float IslandWidth = 170f;
    private const float IslandDepth = 145f;
    private const float IslandHeight = 10.5f;
    private const float WaterHeight = -0.72f;
    private static readonly Vector2 PlayerClearing = new Vector2(0f, -8f);
    private static readonly Vector2 CampClearing = new Vector2(-4.5f, -3.5f);
    private static readonly Vector2 ArkClearing = new Vector2(6.5f, 10.5f);

    [MenuItem("Tools/Ark Prototype/Rebuild Scene")]
    public static void RebuildScene()
    {
        const string materialsFolder = "Assets/Materials";
        const string prefabsFolder = "Assets/Prefabs";
        const string generatedFolder = "Assets/Generated";

        if (!AssetDatabase.IsValidFolder(materialsFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        if (!AssetDatabase.IsValidFolder(prefabsFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        if (!AssetDatabase.IsValidFolder(generatedFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Generated");
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material groundMat = CreateMaterial(materialsFolder, "Ark_Ground", new Color(0.27f, 0.42f, 0.23f), shader);
        Material sandMat = CreateMaterial(materialsFolder, "Ark_Sand", new Color(0.55f, 0.47f, 0.32f), shader);
        Material trunkMat = CreateMaterial(materialsFolder, "Ark_Tree_Trunk", new Color(0.23f, 0.13f, 0.07f), shader);
        Material leavesMat = CreateMaterial(materialsFolder, "Ark_Tree_Leaves", new Color(0.07f, 0.34f, 0.13f), shader);
        Material logMat = CreateMaterial(materialsFolder, "Ark_Log", new Color(0.34f, 0.18f, 0.08f), shader);
        Material rockMat = CreateMaterial(materialsFolder, "Ark_Nature_Rock", new Color(0.24f, 0.25f, 0.24f), shader);
        Material grassAssetMat = CreateMaterial(materialsFolder, "Ark_Nature_Grass", new Color(0.13f, 0.38f, 0.14f), shader);
        Material bushAssetMat = CreateMaterial(materialsFolder, "Ark_Nature_Bush", new Color(0.08f, 0.29f, 0.10f), shader);
        Material flowerAssetMat = CreateMaterial(materialsFolder, "Ark_Nature_Flower", new Color(0.78f, 0.58f, 0.22f), shader);
        Material campMat = CreateMaterial(materialsFolder, "Ark_Campfire", new Color(1f, 0.38f, 0.08f), shader);
        Material playerMat = CreateMaterial(materialsFolder, "Ark_Player", new Color(0.92f, 0.92f, 0.84f), shader);
        Material buildGhostMat = CreateMaterial(materialsFolder, "Ark_Build_Ghost", new Color(0.3f, 0.9f, 0.65f, 0.42f), shader, true);
        Material invalidGhostMat = CreateMaterial(materialsFolder, "Ark_Build_Ghost_Invalid", new Color(1f, 0.2f, 0.12f, 0.42f), shader, true);
        Material waterMat = LoadMaterial("Assets/IgniteCoders/Simple Water Shader/Resources/Water_mat_01.mat");
        if (waterMat == null)
        {
            waterMat = CreateMaterial(materialsFolder, "Ark_Flood_Water", new Color(0.05f, 0.38f, 0.86f, 0.42f), shader, true);
        }

        ApplyBaseTexture(groundMat, "Assets/LowlyPoly/Stylized Grass Texture/Textures/Vol_42_1_Base_Color.png");
        ApplyBaseTexture(trunkMat, "Assets/HandPaintedBarkTextures/Textures/bark_03.png");
        ApplyBaseTexture(logMat, "Assets/HandPaintedBarkTextures/Textures/bark_01.png");
        ApplyBaseTexture(leavesMat, "Assets/Proxy Games/Stylized Nature Kit Lite/Textures/Spruce Tree Branch.png");
        ApplyBaseTexture(rockMat, "Assets/Proxy Games/Stylized Nature Kit Lite/Textures/Terrain Rock.png");
        ApplyBaseTexture(grassAssetMat, "Assets/Proxy Games/Stylized Nature Kit Lite/Textures/Grass.png");
        ApplyBaseTexture(bushAssetMat, "Assets/Proxy Games/Stylized Nature Kit Lite/Textures/Bush Leaves.png");
        ApplyBaseTexture(flowerAssetMat, "Assets/Proxy Games/Stylized Nature Kit Lite/Textures/Flower.png");

        ArkSceneMaterials.Log = logMat;

        DeleteIfExists("Sphere Arena");
        DeleteIfExists("Runner Player");
        DeleteIfExists("Survival Builder Prototype");
        DeleteIfExists("Прототип ковчега");
        DeleteIfExists("Player Camera");
        DeleteIfExists("Main Camera");
        DeleteIfExists("Заполняющий свет");

        GameObject root = new GameObject("Прототип ковчега");
        GameObject world = new GameObject("Остров");
        world.transform.SetParent(root.transform);
        GameObject forest = new GameObject("Лес");
        forest.transform.SetParent(world.transform);
        GameObject rocks = new GameObject("Камни");
        rocks.transform.SetParent(world.transform);
        GameObject plants = new GameObject("Трава и растения");
        plants.transform.SetParent(world.transform);

        Terrain terrain = CreateIslandTerrain(world.transform, generatedFolder);
        GameObject campMarker = new GameObject("Площадка будущего лагеря");
        campMarker.transform.SetParent(world.transform);
        campMarker.transform.position = new Vector3(-4.5f, TerrainY(terrain, new Vector3(-4.5f, 0f, -3.5f)), -3.5f);
        GameObject arkMarker = new GameObject("Берег для будущего ковчега");
        arkMarker.transform.SetParent(world.transform);
        arkMarker.transform.position = new Vector3(6.5f, TerrainY(terrain, new Vector3(6.5f, 0f, 10.5f)), 10.5f);
        GameObject water = CreateWater(world.transform, waterMat);

        ScatterTrees(forest.transform, terrain, trunkMat, leavesMat);
        ScatterRocks(rocks.transform, terrain, rockMat);
        ScatterPlants(plants.transform, terrain, grassAssetMat, bushAssetMat, flowerAssetMat, logMat);

        Vector3 playerStart = new Vector3(0f, TerrainY(terrain, new Vector3(0f, 0f, -8f)) + 1.05f, -8f);
        GameObject player = Primitive(PrimitiveType.Capsule, "Игрок", root.transform, playerStart, Vector3.one, playerMat);
        CapsuleCollider playerCollider = player.GetComponent<CapsuleCollider>();
        if (playerCollider != null)
        {
            Object.DestroyImmediate(playerCollider);
        }

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.45f;
        controller.center = Vector3.zero;
        controller.stepOffset = 0.35f;
        controller.slopeLimit = 50f;

        ArkPlayerController playerController = player.AddComponent<ArkPlayerController>();
        ArkBuildPlanner buildPlanner = player.AddComponent<ArkBuildPlanner>();
        GameObject cameraObject = new GameObject("Камера игрока");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(player.transform);
        cameraObject.transform.localPosition = new Vector3(0f, 0.7f, 0.12f);
        cameraObject.transform.localRotation = Quaternion.identity;
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 72f;
        camera.nearClipPlane = 0.03f;
        cameraObject.AddComponent<AudioListener>();
        AttachHeldAxe(cameraObject.transform, campMat);
        GameObject reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        reticle.name = "Прицел 3D";
        reticle.transform.SetParent(cameraObject.transform);
        reticle.transform.localPosition = new Vector3(0f, 0f, 2.2f);
        reticle.transform.localScale = new Vector3(0.035f, 0.035f, 0.035f);
        reticle.GetComponent<Collider>().enabled = false;
        Renderer reticleRenderer = reticle.GetComponent<Renderer>();
        if (reticleRenderer != null)
        {
            reticleRenderer.sharedMaterial = campMat;
        }
        SerializedObject playerSO = new SerializedObject(playerController);
        playerSO.FindProperty("cameraRoot").objectReferenceValue = cameraObject.transform;
        playerSO.FindProperty("mouseSensitivity").floatValue = 6.2f;
        playerSO.ApplyModifiedPropertiesWithoutUndo();
        SerializedObject plannerSO = new SerializedObject(buildPlanner);
        plannerSO.FindProperty("cameraRoot").objectReferenceValue = cameraObject.transform;
        plannerSO.FindProperty("ghostMaterial").objectReferenceValue = buildGhostMat;
        plannerSO.FindProperty("invalidGhostMaterial").objectReferenceValue = invalidGhostMat;
        plannerSO.FindProperty("campfireBuiltMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/PolygonPilots/Campfire/Art/Materials/Mat_Campfire_Atlas.mat");
        plannerSO.FindProperty("stockpileBuiltMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/free Storage House/Materials/Storage_House.mat");
        plannerSO.FindProperty("workshopBuiltMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/WorkshopMachine2/WorkshopMachine2_Unity.mat");
        plannerSO.FindProperty("dockBuiltMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/EmaceArt/Roadside Tales Bridges/Materials/EA03_LPNP-Slavika_Colorsheet_v1.mat");
        plannerSO.FindProperty("shipBuiltMaterial").objectReferenceValue = null;
        plannerSO.FindProperty("campfirePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonPilots/Campfire/Prefabs/CampFire.prefab");
        plannerSO.FindProperty("stockpilePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/free Storage House/Prefab/Storage_House.prefab");
        plannerSO.FindProperty("lumberHousePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LowpolyBakersHouse/Prefabs/Baker_house.prefab");
        plannerSO.FindProperty("workshopPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/WorkshopMachine2/WorkshopMachine2.fbx");
        plannerSO.FindProperty("dockPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmaceArt/Roadside Tales Bridges/Prefabs/EA03_Prop_Pier_05a_PRE.prefab");
        plannerSO.FindProperty("shipPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sailing Ships/Prefabs/Sailing Ship1.prefab");
        SerializedProperty dogPrefabs = plannerSO.FindProperty("dogPrefabs");
        string[] dogPaths =
        {
            "Assets/Bublisher/3D Stylized Animated Dogs Kit/Prefabs/pug.prefab",
            "Assets/Bublisher/3D Stylized Animated Dogs Kit/Prefabs/chihuahua.prefab",
            "Assets/Bublisher/3D Stylized Animated Dogs Kit/Prefabs/corgi.prefab",
            "Assets/Bublisher/3D Stylized Animated Dogs Kit/Prefabs/cur.prefab",
            "Assets/Bublisher/3D Stylized Animated Dogs Kit/Prefabs/germanshepherd.prefab"
        };
        dogPrefabs.arraySize = dogPaths.Length;
        for (int i = 0; i < dogPaths.Length; i++)
        {
            dogPrefabs.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(dogPaths[i]);
        }
        plannerSO.ApplyModifiedPropertiesWithoutUndo();

        GameObject managerObject = new GameObject("Игровые правила");
        managerObject.transform.SetParent(root.transform);
        ArkGameManager manager = managerObject.AddComponent<ArkGameManager>();
        manager.storedLogs = 0;
        manager.storedPlanks = 0;
        manager.gameDuration = 1800f;
        manager.waterStartHeight = WaterHeight;
        manager.waterEndHeight = WaterHeight;
        manager.water = water.transform;
        manager.logMaterial = logMat;

        BuildHud(root.transform, manager);
        ConfigureLighting();

        Selection.activeGameObject = player;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }

    private static Material CreateMaterial(string folder, string name, Color color, Shader shader, bool transparent = false)
    {
        string path = folder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (transparent)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
        }
        else
        {
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_ZWrite", 1f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = -1;
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.04f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material LoadMaterial(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    private static Terrain CreateIslandTerrain(Transform parent, string generatedFolder)
    {
        const string terrainPath = "Assets/Generated/ПроцедурныйОстров.asset";
        TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainPath);
        if (terrainData == null)
        {
            terrainData = new TerrainData();
            AssetDatabase.CreateAsset(terrainData, terrainPath);
        }

        const int resolution = 257;
        const int alphaResolution = 256;
        Vector3 terrainSize = new Vector3(IslandWidth, IslandHeight, IslandDepth);
        Vector3 terrainOrigin = new Vector3(-terrainSize.x * 0.5f, -1.15f, -terrainSize.z * 0.5f);

        terrainData.heightmapResolution = resolution;
        terrainData.alphamapResolution = alphaResolution;
        terrainData.size = terrainSize;
        terrainData.SetHeights(0, 0, GenerateIslandHeights(resolution, terrainSize));
        terrainData.terrainLayers = CreateTerrainLayers(generatedFolder);
        terrainData.SetAlphamaps(0, 0, GenerateIslandAlphamaps(alphaResolution, terrainSize));
        EditorUtility.SetDirty(terrainData);

        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
        terrainObject.name = "Рельеф острова";
        terrainObject.transform.SetParent(parent);
        terrainObject.transform.position = terrainOrigin;

        Terrain terrain = terrainObject.GetComponent<Terrain>();
        terrain.drawInstanced = true;
        terrain.heightmapPixelError = 4f;
        terrain.basemapDistance = 120f;
        terrain.Flush();

        TerrainCollider collider = terrainObject.GetComponent<TerrainCollider>();
        if (collider != null)
        {
            collider.terrainData = terrainData;
        }

        return terrain;
    }

    private static float[,] GenerateIslandHeights(int resolution, Vector3 terrainSize)
    {
        float[,] heights = new float[resolution, resolution];
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float u = x / (float)(resolution - 1);
                float v = z / (float)(resolution - 1);
                float worldX = (u - 0.5f) * terrainSize.x;
                float worldZ = (v - 0.5f) * terrainSize.z;
                float nx = worldX / (terrainSize.x * 0.5f);
                float nz = worldZ / (terrainSize.z * 0.5f);
                float radius = Mathf.Sqrt(nx * nx + nz * nz);

                float island = 1f - SmoothRange(0.72f, 1.04f, radius);
                float ridge = Mathf.PerlinNoise(worldX * 0.026f + 17.3f, worldZ * 0.026f + 41.7f);
                float detail = Mathf.PerlinNoise(worldX * 0.095f + 103.2f, worldZ * 0.095f + 5.1f);
                float highlands = Mathf.PerlinNoise(worldX * 0.014f + 6.2f, worldZ * 0.014f + 91.4f);
                float hills = (ridge * 0.58f + detail * 0.27f + highlands * 0.15f) * island;
                float height = 0.03f + island * 0.42f + hills * 0.17f;
                height = FlattenArea(height, worldX, worldZ, PlayerClearing, 9.5f, 0.31f);
                height = FlattenArea(height, worldX, worldZ, CampClearing, 8.8f, 0.33f);
                height = FlattenArea(height, worldX, worldZ, ArkClearing, 11.5f, 0.30f);

                heights[z, x] = Mathf.Clamp01(height);
            }
        }

        return heights;
    }

    private static float FlattenArea(float currentHeight, float worldX, float worldZ, Vector2 center, float radius, float targetHeight)
    {
        float distance = Vector2.Distance(new Vector2(worldX, worldZ), center);
        float weight = 1f - SmoothRange(radius * 0.45f, radius, distance);
        return Mathf.Lerp(currentHeight, targetHeight, weight);
    }

    private static float SmoothRange(float edge0, float edge1, float value)
    {
        float t = Mathf.Clamp01((value - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    private static TerrainLayer[] CreateTerrainLayers(string generatedFolder)
    {
        TerrainLayer grass = LoadOrCreateTerrainLayer(generatedFolder + "/СлойТравы.terrainlayer");
        grass.diffuseTexture = CreateNoiseTexture(generatedFolder + "/ТекстураТравы.png", new Color(0.16f, 0.34f, 0.15f), new Color(0.045f, 0.085f, 0.035f), 16f, 0f);
        grass.normalMapTexture = null;
        grass.tileSize = new Vector2(7f, 7f);
        grass.smoothness = 0.015f;
        grass.metallic = 0f;
        EditorUtility.SetDirty(grass);

        TerrainLayer sand = LoadOrCreateTerrainLayer(generatedFolder + "/СлойБерега.terrainlayer");
        sand.diffuseTexture = LoadTexture("Assets/YughuesFreeSandMaterials/Textures/T_YFSM_08_d.tga");
        if (sand.diffuseTexture == null)
        {
            sand.diffuseTexture = CreateNoiseTexture(generatedFolder + "/ТекстураПеска.png", new Color(0.64f, 0.55f, 0.36f), new Color(0.12f, 0.1f, 0.06f), 14f, 0f);
        }
        sand.normalMapTexture = LoadTexture("Assets/YughuesFreeSandMaterials/Textures/T_YFSM_08_n.tga");
        sand.tileSize = new Vector2(5f, 5f);
        sand.smoothness = 0.025f;
        sand.metallic = 0f;
        EditorUtility.SetDirty(sand);

        return new[] { grass, sand };
    }

    private static TerrainLayer LoadOrCreateTerrainLayer(string path)
    {
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if (layer == null)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, path);
        }

        return layer;
    }

    private static Texture2D CreateNoiseTexture(string path, Color baseColor, Color variation, float scale, float alpha = 1f)
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float noise = Mathf.PerlinNoise(x / (float)size * scale + 11.7f, y / (float)size * scale + 3.4f);
                Color pixel = baseColor + variation * (noise - 0.5f);
                pixel.a = alpha;
                pixels[y * size + x] = pixel;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Texture2D LoadTexture(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static float[,,] GenerateIslandAlphamaps(int resolution, Vector3 terrainSize)
    {
        float[,,] alpha = new float[resolution, resolution, 2];
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float u = x / (float)(resolution - 1);
                float v = z / (float)(resolution - 1);
                float worldX = (u - 0.5f) * terrainSize.x;
                float worldZ = (v - 0.5f) * terrainSize.z;
                float nx = worldX / (terrainSize.x * 0.5f);
                float nz = worldZ / (terrainSize.z * 0.5f);
                float radius = Mathf.Sqrt(nx * nx + nz * nz);
                float sand = SmoothRange(0.54f, 0.98f, radius);
                sand = Mathf.Max(sand, 1f - SmoothRange(0.34f, 0.48f, GenerateIslandHeightAt(worldX, worldZ, terrainSize)));
                alpha[z, x, 0] = 1f - sand;
                alpha[z, x, 1] = sand;
            }
        }

        return alpha;
    }

    private static float GenerateIslandHeightAt(float worldX, float worldZ, Vector3 terrainSize)
    {
        float nx = worldX / (terrainSize.x * 0.5f);
        float nz = worldZ / (terrainSize.z * 0.5f);
        float radius = Mathf.Sqrt(nx * nx + nz * nz);
        float island = 1f - SmoothRange(0.72f, 1.04f, radius);
        float ridge = Mathf.PerlinNoise(worldX * 0.026f + 17.3f, worldZ * 0.026f + 41.7f);
        float detail = Mathf.PerlinNoise(worldX * 0.095f + 103.2f, worldZ * 0.095f + 5.1f);
        float highlands = Mathf.PerlinNoise(worldX * 0.014f + 6.2f, worldZ * 0.014f + 91.4f);
        float hills = (ridge * 0.58f + detail * 0.27f + highlands * 0.15f) * island;
        float height = 0.03f + island * 0.42f + hills * 0.17f;
        height = FlattenArea(height, worldX, worldZ, PlayerClearing, 9.5f, 0.31f);
        height = FlattenArea(height, worldX, worldZ, CampClearing, 8.8f, 0.33f);
        height = FlattenArea(height, worldX, worldZ, ArkClearing, 11.5f, 0.30f);
        return Mathf.Clamp01(height);
    }

    private static float TerrainY(Terrain terrain, Vector3 worldPosition)
    {
        if (terrain == null)
        {
            return 0f;
        }

        return terrain.SampleHeight(worldPosition) + terrain.transform.position.y;
    }

    private static GameObject CreateWater(Transform parent, Material waterMaterial)
    {
        GameObject water = new GameObject("Живая вода");
        water.transform.SetParent(parent);
        water.transform.position = new Vector3(0f, WaterHeight, 0f);

        const float outerWidth = 360f;
        const float outerDepth = 320f;
        const float innerWidth = 124f;
        const float innerDepth = 98f;

        float sideWidth = (outerWidth - innerWidth) * 0.5f;
        float sideCenterX = innerWidth * 0.5f + sideWidth * 0.5f;
        float endDepth = (outerDepth - innerDepth) * 0.5f;
        float endCenterZ = innerDepth * 0.5f + endDepth * 0.5f;

        AddWaterPlane(water.transform, "Вода север", new Vector3(0f, 0f, endCenterZ), new Vector2(outerWidth, endDepth), waterMaterial);
        AddWaterPlane(water.transform, "Вода юг", new Vector3(0f, 0f, -endCenterZ), new Vector2(outerWidth, endDepth), waterMaterial);
        AddWaterPlane(water.transform, "Вода восток", new Vector3(sideCenterX, 0f, 0f), new Vector2(sideWidth, innerDepth), waterMaterial);
        AddWaterPlane(water.transform, "Вода запад", new Vector3(-sideCenterX, 0f, 0f), new Vector2(sideWidth, innerDepth), waterMaterial);
        return water;
    }

    private static void AddWaterPlane(Transform parent, string name, Vector3 localPosition, Vector2 size, Material waterMaterial)
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = name;
        plane.transform.SetParent(parent);
        plane.transform.localPosition = localPosition;
        plane.transform.localRotation = Quaternion.identity;
        plane.transform.localScale = new Vector3(size.x / 10f, 1f, size.y / 10f);

        Renderer renderer = plane.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = waterMaterial;
        }

        Collider collider = plane.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    private static void ApplyBaseTexture(Material material, string texturePath)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null || material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }

        material.mainTextureScale = new Vector2(4f, 4f);
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.035f);
        }

        EditorUtility.SetDirty(material);
    }

    private static void AttachHeldAxe(Transform cameraTransform, Material fallbackMaterial)
    {
        GameObject axePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Feyloom/Wooden_Axe/Renders/URP/Prefab/SM_Wooden_Axe.prefab");
        GameObject axe;

        if (axePrefab != null)
        {
            axe = (GameObject)PrefabUtility.InstantiatePrefab(axePrefab);
            axe.name = "Топор в руках";
        }
        else
        {
            axe = Primitive(PrimitiveType.Cube, "Топор в руках", null, Vector3.zero, Vector3.one, fallbackMaterial);
        }

        axe.transform.SetParent(cameraTransform);
        axe.transform.localPosition = new Vector3(0.32f, -0.27f, 0.306f);
        axe.transform.localRotation = Quaternion.Euler(359.2805f, 269.1501f, 2.6441f);
        axe.transform.localScale = Vector3.one * 0.54f;

        foreach (Collider collider in axe.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }
    }

    private static void DeleteIfExists(string name)
    {
        GameObject old = GameObject.Find(name);
        if (old != null)
        {
            Object.DestroyImmediate(old);
        }
    }

    private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.position = position;
        obj.transform.localScale = scale;
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }

        return obj;
    }

    private static void ScatterTrees(Transform parent, Terrain terrain, Material trunkMat, Material leavesMat)
    {
        string[] treePrefabs =
        {
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Trees/Spruce 1.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Trees/Spruce 2.prefab"
        };

        int placed = 0;
        for (int i = 0; placed < 92 && i < 260; i++)
        {
            Vector3 position = RingPosition(i, 260, 20f, 70f, 1.08f, 0.92f);
            if (!CanPlaceScenery(position, 11.5f, terrain))
            {
                continue;
            }

            position.y = TerrainY(terrain, position) + 0.02f;
            float scale = 0.92f + Seeded01(i, 4.9f) * 0.42f;
            float yaw = Seeded01(i, 8.1f) * 360f;
            string prefabPath = treePrefabs[placed % treePrefabs.Length];
            MakeAssetTree(parent, "Ель " + (placed + 1).ToString("00"), prefabPath, position, yaw, scale, trunkMat, leavesMat, i);
            placed++;
        }
    }

    private static void ScatterRocks(Transform parent, Terrain terrain, Material rockMat)
    {
        string[] standardRocks =
        {
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Standard Rocks/Standard Rock 1.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Standard Rocks/Standard Rock 2.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Standard Rocks/Standard Rock 3.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Standard Rocks/Standard Rock 4.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Standard Rocks/Standard Rock 5.prefab"
        };

        string[] cliffRocks =
        {
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Rock Cliffs/Rock Cliff 1.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Rock Cliffs/Rock Cliff 2.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Rock Cliffs/Rock Cliff 3.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Rock Cliffs/Rock Cliff 4.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Rock Cliffs/Rock Cliff 5.prefab"
        };

        int placed = 0;
        for (int i = 0; placed < 44 && i < 150; i++)
        {
            bool cliff = i % 6 == 0;
            Vector3 position = RingPosition(i + 31, 150, cliff ? 48f : 24f, cliff ? 77f : 72f, 1.12f, 0.94f);
            if (!CanPlaceScenery(position, cliff ? 13f : 8f, terrain))
            {
                continue;
            }

            position.y = TerrainY(terrain, position) - 0.03f;
            string[] paths = cliff ? cliffRocks : standardRocks;
            string path = paths[placed % paths.Length];
            float scale = cliff ? 1.35f + Seeded01(i, 12.8f) * 1.05f : 0.75f + Seeded01(i, 13.6f) * 0.95f;
            GameObject rock = InstantiateAssetPrefab(path, (cliff ? "Скала " : "Камень ") + (placed + 1).ToString("00"), parent, position, new Vector3(0f, Seeded01(i, 14.1f) * 360f, 0f), Vector3.one * scale);
            ApplyUniformMaterial(rock, rockMat);
            placed++;
        }
    }

    private static void ScatterPlants(Transform parent, Terrain terrain, Material grassMat, Material bushMat, Material flowerMat, Material logMat)
    {
        string[] plantPrefabs =
        {
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Grass/Grass.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Grass/Grass.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Grass/Grass.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Bush/Bush.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Flower/Flower.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Mushroom/Mushrooms Patch.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Stump/Stump.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Log/Log.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Branch/Branch.prefab"
        };

        int placed = 0;
        for (int i = 0; placed < 170 && i < 360; i++)
        {
            Vector3 position = RingPosition(i + 73, 360, 11f, 67f, 1.1f, 0.93f);
            if (!CanPlaceScenery(position, 4.8f, terrain))
            {
                continue;
            }

            string path = plantPrefabs[(i + placed) % plantPrefabs.Length];
            position.y = TerrainY(terrain, position) + 0.01f;
            float scale = path.Contains("Grass") ? 0.72f + Seeded01(i, 18.3f) * 0.78f : 0.65f + Seeded01(i, 19.4f) * 0.62f;
            GameObject plant = InstantiateAssetPrefab(path, PlantName(path) + " " + (placed + 1).ToString("000"), parent, position, new Vector3(0f, Seeded01(i, 20.5f) * 360f, 0f), Vector3.one * scale);
            ApplyUniformMaterial(plant, PlantMaterial(path, grassMat, bushMat, flowerMat, logMat));
            placed++;
        }
    }

    private static void MakeAssetTree(Transform parent, string name, string prefabPath, Vector3 position, float yaw, float scale, Material trunkMat, Material leavesMat, int seed)
    {
        GameObject tree = new GameObject(name);
        tree.transform.SetParent(parent);
        tree.transform.position = position;

        ArkTree treeComponent = tree.AddComponent<ArkTree>();
        float height = 5.2f * scale;
        CapsuleCollider collider = tree.AddComponent<CapsuleCollider>();
        collider.radius = 0.58f * scale;
        collider.height = height;
        collider.center = new Vector3(0f, height * 0.5f, 0f);

        GameObject visual = InstantiateAssetPrefab(prefabPath, "Визуал", tree.transform, position, new Vector3(0f, yaw, 0f), Vector3.one * scale);
        if (visual == null)
        {
            MakeTree(parent, name + " fallback", position, height, 2.2f * scale, trunkMat, leavesMat);
            Object.DestroyImmediate(tree);
            return;
        }

        ApplyTreeMaterials(visual, trunkMat, leavesMat);

        SerializedObject so = new SerializedObject(treeComponent);
        so.FindProperty("maxHp").intValue = Mathf.RoundToInt(55f + scale * 28f + Seeded01(seed, 7.7f) * 18f);
        so.FindProperty("logYield").intValue = scale > 1.12f ? 4 : 3;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject InstantiateAssetPrefab(string path, string name, Transform parent, Vector3 position, Vector3 eulerAngles, Vector3 scale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(eulerAngles);
        instance.transform.localScale = scale;
        return instance;
    }

    private static void ApplyUniformMaterial(GameObject root, Material material)
    {
        if (root == null || material == null)
        {
            return;
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private static void ApplyTreeMaterials(GameObject root, Material trunkMat, Material leavesMat)
    {
        if (root == null)
        {
            return;
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                string materialName = materials[i] != null ? materials[i].name : string.Empty;
                materials[i] = materialName.Contains("Branch") || materialName.Contains("Leaf") || materialName.Contains("Leaves") ? leavesMat : trunkMat;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private static Material PlantMaterial(string path, Material grassMat, Material bushMat, Material flowerMat, Material logMat)
    {
        if (path.Contains("Bush")) return bushMat;
        if (path.Contains("Flower") || path.Contains("Mushroom")) return flowerMat;
        if (path.Contains("Log") || path.Contains("Stump") || path.Contains("Branch")) return logMat;
        return grassMat;
    }

    private static Vector3 RingPosition(int index, int count, float minRadius, float maxRadius, float xStretch, float zStretch)
    {
        float t = (index + 0.5f) / count;
        float angle = (index * 137.508f + Seeded01(index, 2.4f) * 18f) * Mathf.Deg2Rad;
        float radius = Mathf.Lerp(minRadius, maxRadius, Mathf.Sqrt(t));
        radius *= 0.92f + Seeded01(index, 3.1f) * 0.18f;
        return new Vector3(Mathf.Cos(angle) * radius * xStretch, 0f, Mathf.Sin(angle) * radius * zStretch);
    }

    private static bool CanPlaceScenery(Vector3 position, float clearingPadding, Terrain terrain)
    {
        if (NormalizedIslandRadius(position) > 0.88f)
        {
            return false;
        }

        float y = TerrainY(terrain, position);
        if (y < WaterHeight + 0.32f)
        {
            return false;
        }

        Vector2 point = new Vector2(position.x, position.z);
        return Vector2.Distance(point, PlayerClearing) > 10.5f + clearingPadding
            && Vector2.Distance(point, CampClearing) > 9f + clearingPadding
            && Vector2.Distance(point, ArkClearing) > 11f + clearingPadding;
    }

    private static float NormalizedIslandRadius(Vector3 position)
    {
        float nx = position.x / (IslandWidth * 0.5f);
        float nz = position.z / (IslandDepth * 0.5f);
        return Mathf.Sqrt(nx * nx + nz * nz);
    }

    private static float Seeded01(int index, float salt)
    {
        return Mathf.Repeat(Mathf.Sin((index + 1) * 12.9898f + salt * 78.233f) * 43758.5453f, 1f);
    }

    private static string PlantName(string path)
    {
        if (path.Contains("Bush")) return "Куст";
        if (path.Contains("Flower")) return "Цветы";
        if (path.Contains("Mushroom")) return "Грибы";
        if (path.Contains("Stump")) return "Пень";
        if (path.Contains("Log")) return "Бревно";
        if (path.Contains("Branch")) return "Ветка";
        return "Трава";
    }

    private static void MakeTree(Transform parent, string name, Vector3 position, float height, float canopyScale, Material trunkMat, Material leavesMat)
    {
        GameObject tree = new GameObject(name);
        tree.transform.SetParent(parent);
        tree.transform.position = position;
        ArkTree treeComponent = tree.AddComponent<ArkTree>();

        Primitive(PrimitiveType.Cylinder, "Ствол", tree.transform, position + new Vector3(0f, height * 0.42f, 0f), new Vector3(0.45f, height * 0.42f, 0.45f), trunkMat);
        Primitive(PrimitiveType.Sphere, "Крона", tree.transform, position + new Vector3(0f, height * 0.95f, 0f), new Vector3(canopyScale, canopyScale, canopyScale), leavesMat);

        SerializedObject so = new SerializedObject(treeComponent);
        so.FindProperty("maxHp").intValue = Mathf.RoundToInt(45f + height * 8f);
        so.FindProperty("logYield").intValue = height > 4.8f ? 4 : 3;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void MakeBuildingSite(Transform parent, ArkGameManager manager, string name, BuildingKind kind, Vector3 position, int logs, int planks, Vector3 builtScale, Material builtMat, Material ghostMat, Material campMat)
    {
        GameObject site = new GameObject(name + " Site");
        site.transform.SetParent(parent);
        site.transform.position = position;
        BoxCollider collider = site.AddComponent<BoxCollider>();
        collider.size = new Vector3(3.2f, 2.2f, 3.2f);
        collider.center = new Vector3(0f, 1f, 0f);

        ArkBuildSite buildSite = site.AddComponent<ArkBuildSite>();
        buildSite.Kind = kind;
        buildSite.DisplayName = name;
        buildSite.LogsCost = logs;
        buildSite.PlanksCost = planks;

        GameObject ghost = Primitive(PrimitiveType.Cube, "Ghost", site.transform, position + new Vector3(0f, 0.65f, 0f), builtScale, ghostMat);
        GameObject built = Primitive(PrimitiveType.Cube, "Built", site.transform, position + new Vector3(0f, 0.65f, 0f), builtScale, builtMat);
        buildSite.GhostVisual = ghost;
        buildSite.BuiltVisual = built;

        if (kind == BuildingKind.Campfire)
        {
            Primitive(PrimitiveType.Sphere, "Flame", built.transform, position + new Vector3(0f, 1.25f, 0f), new Vector3(0.55f, 0.55f, 0.55f), campMat);
        }

        if (kind == BuildingKind.Stockpile)
        {
            ArkStockpile stockpile = built.AddComponent<ArkStockpile>();
            manager.stockpile = stockpile;
        }
    }

    private static void MakeArkSection(Transform parent, string name, Vector3 position, Vector3 scale, int logs, int planks, Material builtMat, Material ghostMat)
    {
        GameObject site = new GameObject(name + " Site");
        site.transform.SetParent(parent);
        site.transform.position = position;
        BoxCollider collider = site.AddComponent<BoxCollider>();
        collider.size = new Vector3(scale.x + 1f, scale.y + 1.2f, scale.z + 1f);
        collider.center = new Vector3(0f, 0.8f, 0f);

        ArkSectionSite section = site.AddComponent<ArkSectionSite>();
        section.DisplayName = name;
        section.LogsCost = logs;
        section.PlanksCost = planks;
        section.GhostVisual = Primitive(PrimitiveType.Cube, "Ghost", site.transform, position + new Vector3(0f, scale.y * 0.5f, 0f), scale, ghostMat);
        section.BuiltVisual = Primitive(PrimitiveType.Cube, "Built", site.transform, position + new Vector3(0f, scale.y * 0.5f, 0f), scale, builtMat);
    }

    private static void BuildHud(Transform root, ArkGameManager manager)
    {
        GameObject canvasObject = new GameObject("Интерфейс");
        canvasObject.transform.SetParent(root);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvasObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem(root);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject statsPanel = MakePanel(canvasObject.transform, "Панель статистики", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 520f), new Color(0.05f, 0.07f, 0.06f, 0.86f));
        manager.statsPanel = statsPanel;
        manager.statsText = MakeText(statsPanel.transform, "Текст статистики", font, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-56f, -48f), 22, TextAnchor.UpperLeft);
        manager.promptText = MakeText(canvasObject.transform, "Подсказка действия", font, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(760f, 56f), 24, TextAnchor.MiddleCenter);
        manager.messageText = MakeText(canvasObject.transform, "Сообщение", font, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(850f, 48f), 26, TextAnchor.MiddleCenter);

        GameObject menuPanel = MakePanel(canvasObject.transform, "Меню паузы", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 340f), new Color(0.04f, 0.055f, 0.045f, 0.92f));
        MakeText(menuPanel.transform, "Заголовок меню", font, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(-48f, 64f), 34, TextAnchor.MiddleCenter).text = "КОВЧЕГ";
        MakeText(menuPanel.transform, "Подзаголовок меню", font, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(-56f, 42f), 18, TextAnchor.MiddleCenter).text = "Esc - пауза";
        manager.continueButton = MakeButton(menuPanel.transform, "Кнопка продолжить", font, "Продолжить", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(280f, 56f), new Color(0.16f, 0.34f, 0.22f, 0.95f));
        manager.quitButton = MakeButton(menuPanel.transform, "Кнопка выход", font, "Выйти из игры", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -92f), new Vector2(280f, 56f), new Color(0.36f, 0.16f, 0.13f, 0.95f));
        manager.menuPanel = menuPanel;

        statsPanel.SetActive(false);
        menuPanel.SetActive(false);
    }

    private static GameObject MakePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = color;

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return panel;
    }

    private static Text MakeText(Transform parent, string name, Font font, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Shadow shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
        shadow.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    private static Button MakeButton(Transform parent, string name, Font font, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject buttonObject = MakePanel(parent, name, anchorMin, anchorMax, pivot, anchoredPosition, size, color);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.12f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text text = MakeText(buttonObject.transform, "Текст", font, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-24f, -12f), 24, TextAnchor.MiddleCenter);
        text.text = label;
        text.raycastTarget = false;
        return button;
    }

    private static void EnsureEventSystem(Transform root)
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.transform.SetParent(root);
        eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private static void ConfigureLighting()
    {
        Light light = Object.FindObjectOfType<Light>();
        if (light != null)
        {
            light.name = "Солнце";
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(31f, -42f, 0f);
            light.color = new Color(1f, 0.76f, 0.48f);
            light.intensity = 1.32f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.72f;
            light.shadowBias = 0.035f;
            light.shadowNormalBias = 0.28f;
        }

        GameObject fillObject = new GameObject("Заполняющий свет");
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.transform.rotation = Quaternion.Euler(26f, 138f, 0f);
        fill.color = new Color(0.42f, 0.55f, 0.70f);
        fill.intensity = 0.24f;
        fill.shadows = LightShadows.None;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.38f, 0.48f, 0.62f);
        RenderSettings.ambientEquatorColor = new Color(0.22f, 0.31f, 0.27f);
        RenderSettings.ambientGroundColor = new Color(0.10f, 0.12f, 0.10f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.43f, 0.56f, 0.66f);
        RenderSettings.fogDensity = 0.0065f;
        RenderSettings.reflectionIntensity = 0.38f;
        RenderSettings.flareStrength = 0.45f;

        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.shadowDistance = 90f;
    }
}
