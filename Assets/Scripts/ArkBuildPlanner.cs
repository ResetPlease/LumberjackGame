using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ArkBuildPlanner : MonoBehaviour
{
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private float placementRange = 8f;
    [SerializeField] private LayerMask placementMask = ~0;
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private Material invalidGhostMaterial;
    [SerializeField] private Material campfireBuiltMaterial;
    [SerializeField] private Material stockpileBuiltMaterial;
    [SerializeField] private Material workshopBuiltMaterial;
    [SerializeField] private Material dockBuiltMaterial;
    [SerializeField] private Material shipBuiltMaterial;
    [SerializeField] private GameObject campfirePrefab;
    [SerializeField] private GameObject stockpilePrefab;
    [SerializeField] private GameObject lumberHousePrefab;
    [SerializeField] private GameObject workshopPrefab;
    [SerializeField] private GameObject dockPrefab;
    [SerializeField] private GameObject shipPrefab;
    [SerializeField] private GameObject[] dogPrefabs;

    private BuildChoice selectedChoice = BuildChoice.None;
    private GameObject ghostInstance;
    private bool menuOpen;
    private bool hasValidPlacement;
    private Vector3 placementPosition;
    private Quaternion placementRotation;

    public bool IsPlanning => selectedChoice != BuildChoice.None;

    private enum BuildChoice
    {
        None,
        Campfire,
        Stockpile,
        LumberHouse,
        Workshop,
        Dock,
        Ship
    }

    private void Awake()
    {
        if (cameraRoot == null && Camera.main != null)
        {
            cameraRoot = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (ArkGameManager.Instance != null && ArkGameManager.Instance.GameOver)
        {
            return;
        }

        if (ArkGameManager.Instance != null && ArkGameManager.Instance.MenuOpen)
        {
            return;
        }

        if (WasBuildMenuPressed())
        {
            ToggleMenu();
        }

        if (!menuOpen && selectedChoice == BuildChoice.None)
        {
            return;
        }

        if (WasCampfirePressed())
        {
            Select(BuildChoice.Campfire);
        }

        if (WasStockpilePressed())
        {
            Select(BuildChoice.Stockpile);
        }

        if (WasLumberHousePressed())
        {
            Select(BuildChoice.LumberHouse);
        }

        if (WasWorkshopPressed())
        {
            Select(BuildChoice.Workshop);
        }

        if (WasDockPressed())
        {
            Select(BuildChoice.Dock);
        }

        if (WasShipPressed())
        {
            Select(BuildChoice.Ship);
        }

        if (WasCancelPressed())
        {
            Cancel();
            return;
        }

        if (selectedChoice == BuildChoice.None)
        {
            return;
        }

        UpdateGhost();
        if (WasPlacePressed())
        {
            PlaceSelected();
        }
    }

    private void LateUpdate()
    {
        if (ArkGameManager.Instance == null)
        {
            return;
        }

        if (selectedChoice != BuildChoice.None)
        {
            if (hasValidPlacement)
            {
                ArkGameManager.Instance.ShowPrompt("ЛКМ: поставить " + DisplayName(selectedChoice) + " | 1: костёр | 2: склад | 3: причал | 4: корабль | 5: лесопилка | 6: дом лесника | Esc/ПКМ: отмена");
            }
            else
            {
                ArkGameManager.Instance.ShowPrompt(PlacementHint(selectedChoice) + " | 1: костёр | 2: склад | 3: причал | 4: корабль | 5: лесопилка | 6: дом лесника");
            }
        }
        else if (menuOpen)
        {
            ArkGameManager.Instance.ShowPrompt("Строительство: 1 - костёр, 2 - склад, 3 - причал, 4 - корабль, 5 - лесопилка, 6 - дом лесника, Esc - закрыть");
        }
    }

    private void ToggleMenu()
    {
        menuOpen = !menuOpen;
        if (!menuOpen && selectedChoice == BuildChoice.None && ArkGameManager.Instance != null)
        {
            ArkGameManager.Instance.ClearPrompt();
        }
    }

    private void Select(BuildChoice choice)
    {
        if (!CanSelect(choice))
        {
            return;
        }

        selectedChoice = choice;
        menuOpen = true;
        RecreateGhost();
    }

    private void Cancel()
    {
        menuOpen = false;
        selectedChoice = BuildChoice.None;
        DestroyGhost();
        if (ArkGameManager.Instance != null)
        {
            ArkGameManager.Instance.ClearPrompt();
        }
    }

    private void RecreateGhost()
    {
        DestroyGhost();
        GameObject prefab = PrefabFor(selectedChoice);
        if (prefab != null)
        {
            ghostInstance = Instantiate(prefab);
            ghostInstance.name = "Ghost " + DisplayName(selectedChoice);
            ghostInstance.transform.localScale = Vector3.Scale(ghostInstance.transform.localScale, VisualScale(selectedChoice));
        }
        else
        {
            ghostInstance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghostInstance.name = "Ghost " + DisplayName(selectedChoice);
            ghostInstance.transform.localScale = GhostFallbackScale(selectedChoice);
        }

        DisableGameplayComponents(ghostInstance);
        SetLayerRecursively(ghostInstance, LayerMask.NameToLayer("Ignore Raycast"));
        ApplyGhostMaterial(ghostInstance, ghostMaterial);
    }

    private void DestroyGhost()
    {
        if (ghostInstance == null)
        {
            return;
        }

        Destroy(ghostInstance);
        ghostInstance = null;
    }

    private void UpdateGhost()
    {
        if (ghostInstance == null)
        {
            RecreateGhost();
        }

        hasValidPlacement = TryFindPlacement(out placementPosition, out placementRotation);
        if (ghostInstance == null)
        {
            return;
        }

        ghostInstance.SetActive(hasValidPlacement);
        if (!hasValidPlacement)
        {
            return;
        }

        ghostInstance.transform.position = placementPosition;
        ghostInstance.transform.rotation = placementRotation * VisualRotation(selectedChoice);
        SnapVisualBottomTo(ghostInstance, placementPosition.y);
        ApplyGhostMaterial(ghostInstance, hasValidPlacement ? ghostMaterial : invalidGhostMaterial);
    }

    private bool TryFindPlacement(out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        if (cameraRoot == null)
        {
            return false;
        }

        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            return false;
        }

        Ray ray = new Ray(cameraRoot.position, cameraRoot.forward);
        bool waterChoice = RequiresWater(selectedChoice);
        Vector3 hitPoint;

        if (waterChoice)
        {
            Plane waterPlane = new Plane(Vector3.up, new Vector3(0f, ArkWorldRules.WaterHeight, 0f));
            if (!waterPlane.Raycast(ray, out float waterDistance) || waterDistance > 34f)
            {
                return false;
            }

            hitPoint = ray.GetPoint(waterDistance);
        }
        else
        {
            TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
            if (terrainCollider == null || !terrainCollider.Raycast(ray, out RaycastHit hit, placementRange))
            {
                return false;
            }

            hitPoint = hit.point;
        }

        Vector3 terrainLocal = hitPoint - terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        if (terrainLocal.x < 0f || terrainLocal.z < 0f || terrainLocal.x > terrainSize.x || terrainLocal.z > terrainSize.z)
        {
            return false;
        }

        float normalizedX = Mathf.Clamp01(terrainLocal.x / terrainSize.x);
        float normalizedZ = Mathf.Clamp01(terrainLocal.z / terrainSize.z);
        Vector3 normal = terrain.terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
        if (Vector3.Angle(normal, Vector3.up) > 42f)
        {
            return false;
        }

        float terrainY = terrain.SampleHeight(hitPoint) + terrain.transform.position.y;
        if (waterChoice && terrainY > ArkWorldRules.WaterHeight + 0.65f)
        {
            return false;
        }

        if (!waterChoice && hitPoint.y <= ArkWorldRules.WaterHeight + 0.12f)
        {
            return false;
        }

        position = hitPoint;
        if (waterChoice)
        {
            position.y = ArkWorldRules.WaterHeight;
        }

        rotation = Quaternion.Euler(0f, cameraRoot.eulerAngles.y, 0f);
        return true;
    }

    private void PlaceSelected()
    {
        if (!hasValidPlacement)
        {
            if (ArkGameManager.Instance != null)
            {
                ArkGameManager.Instance.ShowMessage("Здесь нельзя поставить постройку.");
            }
            return;
        }

        GameObject site = new GameObject(DisplayName(selectedChoice) + " Site");
        site.transform.position = placementPosition;
        site.transform.rotation = placementRotation;

        BoxCollider collider = site.AddComponent<BoxCollider>();
        collider.size = ColliderSize(selectedChoice);
        collider.center = ColliderCenter(selectedChoice, collider.size);
        AddInteractionTrigger(selectedChoice, site.transform);

        ArkBuildSite buildSite = site.AddComponent<ArkBuildSite>();
        buildSite.Kind = KindFor(selectedChoice);
        buildSite.DisplayName = DisplayName(selectedChoice);
        buildSite.LogsCost = LogsCost(selectedChoice);
        buildSite.PlanksCost = PlanksCost(selectedChoice);

        buildSite.GhostVisual = CreateSiteVisual(selectedChoice, site.transform, true);
        buildSite.BuiltVisual = CreateSiteVisual(selectedChoice, site.transform, false);
        buildSite.GhostVisual.SetActive(true);
        buildSite.BuiltVisual.SetActive(false);

        if (buildSite.Kind == BuildingKind.Stockpile)
        {
            site.AddComponent<ArkStockpile>();
        }
        else if (buildSite.Kind == BuildingKind.LumberHouse)
        {
            ArkForesterHouse house = site.AddComponent<ArkForesterHouse>();
            house.SetDogPrefabs(dogPrefabs);
        }

        DestroyGhost();
        selectedChoice = BuildChoice.None;
        menuOpen = false;

        if (ArkGameManager.Instance != null)
        {
            ArkGameManager.Instance.RegisterBuildSite(buildSite);
            ArkGameManager.Instance.ShowMessage("Поставлена стройка: " + buildSite.DisplayName + ". Принеси ресурсы.");
        }
    }

    private GameObject CreateSiteVisual(BuildChoice choice, Transform parent, bool ghost)
    {
        GameObject prefab = PrefabFor(choice);
        GameObject visual = prefab != null ? Instantiate(prefab, parent) : GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = ghost ? "Ghost" : "Built";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = VisualRotation(choice);
        if (prefab == null)
        {
            visual.transform.localScale = GhostFallbackScale(choice);
        }
        else
        {
            visual.transform.localScale = Vector3.Scale(visual.transform.localScale, VisualScale(choice));
        }

        DisableColliders(visual);
        DisableGameplayComponents(visual);
        SnapVisualBottomTo(visual, parent.position.y);
        if (ghost)
        {
            ApplyGhostMaterial(visual, ghostMaterial);
        }
        else
        {
            ApplyBuiltMaterial(choice, visual);
        }

        return visual;
    }

    private static void SnapVisualBottomTo(GameObject root, float groundY)
    {
        if (root == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float deltaY = groundY - bounds.min.y;
        root.transform.position += Vector3.up * deltaY;
    }

    private GameObject PrefabFor(BuildChoice choice)
    {
        if (choice == BuildChoice.Campfire) return campfirePrefab;
        if (choice == BuildChoice.Stockpile) return stockpilePrefab;
        if (choice == BuildChoice.LumberHouse) return lumberHousePrefab;
        if (choice == BuildChoice.Workshop) return workshopPrefab;
        if (choice == BuildChoice.Dock) return dockPrefab;
        if (choice == BuildChoice.Ship) return shipPrefab;
        return null;
    }

    private void ApplyBuiltMaterial(BuildChoice choice, GameObject visual)
    {
        if (choice == BuildChoice.Campfire && campfireBuiltMaterial != null)
        {
            ApplyGhostMaterial(visual, campfireBuiltMaterial);
        }
        else if (choice == BuildChoice.Stockpile && stockpileBuiltMaterial != null)
        {
            ApplyGhostMaterial(visual, stockpileBuiltMaterial);
        }
        else if (choice == BuildChoice.Workshop && workshopBuiltMaterial != null)
        {
            ApplyGhostMaterial(visual, workshopBuiltMaterial);
        }
        else if (choice == BuildChoice.Dock && dockBuiltMaterial != null)
        {
            ApplyGhostMaterial(visual, dockBuiltMaterial);
        }
        else if (choice == BuildChoice.Ship && shipBuiltMaterial != null)
        {
            ApplyGhostMaterial(visual, shipBuiltMaterial);
        }
    }

    private static BuildingKind KindFor(BuildChoice choice)
    {
        if (choice == BuildChoice.Stockpile) return BuildingKind.Stockpile;
        if (choice == BuildChoice.LumberHouse) return BuildingKind.LumberHouse;
        if (choice == BuildChoice.Workshop) return BuildingKind.Workshop;
        if (choice == BuildChoice.Dock) return BuildingKind.Dock;
        if (choice == BuildChoice.Ship) return BuildingKind.Ship;
        return BuildingKind.Campfire;
    }

    private static string DisplayName(BuildChoice choice)
    {
        if (choice == BuildChoice.Campfire) return "Костёр";
        if (choice == BuildChoice.Stockpile) return "Склад";
        if (choice == BuildChoice.LumberHouse) return "Дом лесника";
        if (choice == BuildChoice.Workshop) return "Лесопилка";
        if (choice == BuildChoice.Dock) return "Причал";
        if (choice == BuildChoice.Ship) return "Корабль";
        return "Постройка";
    }

    private static int LogsCost(BuildChoice choice)
    {
        if (choice == BuildChoice.Campfire) return 3;
        if (choice == BuildChoice.Stockpile) return 6;
        if (choice == BuildChoice.LumberHouse) return 10;
        if (choice == BuildChoice.Workshop) return 10;
        if (choice == BuildChoice.Dock) return 14;
        if (choice == BuildChoice.Ship) return 0;
        return 0;
    }

    private static int PlanksCost(BuildChoice choice)
    {
        if (choice == BuildChoice.Dock) return 8;
        if (choice == BuildChoice.Ship) return 100;
        return 0;
    }

    private static Vector3 ColliderSize(BuildChoice choice)
    {
        if (choice == BuildChoice.Campfire) return new Vector3(2.4f, 1.4f, 2.4f);
        if (choice == BuildChoice.Stockpile) return new Vector3(4.0f, 2.8f, 3.4f);
        if (choice == BuildChoice.LumberHouse) return new Vector3(4.2f, 3.2f, 4.4f);
        if (choice == BuildChoice.Workshop) return new Vector3(2.6f, 1.5f, 1.8f);
        if (choice == BuildChoice.Dock) return new Vector3(4.5f, 0.55f, 14.5f);
        if (choice == BuildChoice.Ship) return new Vector3(6.4f, 5.0f, 16.0f);
        return Vector3.one * 2f;
    }

    private static Vector3 ColliderCenter(BuildChoice choice, Vector3 size)
    {
        if (choice == BuildChoice.Dock)
        {
            return new Vector3(0f, 1.4f, 0f);
        }

        return new Vector3(0f, size.y * 0.5f, 0f);
    }

    private static Vector3 InteractionTriggerSize(BuildChoice choice)
    {
        if (choice == BuildChoice.Dock) return new Vector3(5.6f, 2.2f, 15.6f);
        if (choice == BuildChoice.Ship) return new Vector3(7.4f, 4.0f, 17.5f);
        return Vector3.zero;
    }

    private static void AddInteractionTrigger(BuildChoice choice, Transform parent)
    {
        Vector3 size = InteractionTriggerSize(choice);
        if (size == Vector3.zero)
        {
            return;
        }

        GameObject triggerObject = new GameObject("Зона взаимодействия");
        triggerObject.transform.SetParent(parent, false);
        BoxCollider trigger = triggerObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = size;
        trigger.center = new Vector3(0f, size.y * 0.5f, 0f);
    }

    private static Vector3 GhostFallbackScale(BuildChoice choice)
    {
        if (choice == BuildChoice.Campfire) return new Vector3(1.3f, 0.45f, 1.3f);
        if (choice == BuildChoice.Stockpile) return new Vector3(3.0f, 1.8f, 2.4f);
        if (choice == BuildChoice.LumberHouse) return new Vector3(3.6f, 3.2f, 3.8f);
        if (choice == BuildChoice.Workshop) return new Vector3(2.0f, 1.0f, 1.1f);
        if (choice == BuildChoice.Dock) return new Vector3(4.5f, 0.8f, 14.0f);
        if (choice == BuildChoice.Ship) return new Vector3(5.2f, 3.8f, 12.0f);
        return Vector3.one;
    }

    private static Vector3 VisualScale(BuildChoice choice)
    {
        if (choice == BuildChoice.LumberHouse)
        {
            return Vector3.one * 0.38f;
        }

        if (choice == BuildChoice.Ship)
        {
            return Vector3.one * 0.48f;
        }

        return Vector3.one;
    }

    private static Quaternion VisualRotation(BuildChoice choice)
    {
        if (choice == BuildChoice.Dock)
        {
            return Quaternion.Euler(0f, 90f, 0f);
        }

        if (choice == BuildChoice.Workshop)
        {
            return Quaternion.Euler(-90f, 0f, 0f);
        }

        return Quaternion.identity;
    }

    private bool CanSelect(BuildChoice choice)
    {
        if (choice == BuildChoice.LumberHouse && (ArkGameManager.Instance == null || !ArkGameManager.Instance.StockpileBuilt))
        {
            if (ArkGameManager.Instance != null)
            {
                ArkGameManager.Instance.ShowMessage("Сначала построй склад.");
            }

            return false;
        }

        if (choice == BuildChoice.Ship && (ArkGameManager.Instance == null || !ArkGameManager.Instance.DockBuilt))
        {
            if (ArkGameManager.Instance != null)
            {
                ArkGameManager.Instance.ShowMessage("Сначала построй причал.");
            }

            return false;
        }

        return true;
    }

    private static bool RequiresWater(BuildChoice choice)
    {
        return choice == BuildChoice.Dock || choice == BuildChoice.Ship;
    }

    private static string PlacementHint(BuildChoice choice)
    {
        if (RequiresWater(choice))
        {
            return "Наведи точку на воду у берега";
        }

        return "Наведи точку на землю";
    }

    private static void DisableGameplayComponents(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        foreach (ArkBuildSite site in root.GetComponentsInChildren<ArkBuildSite>())
        {
            site.enabled = false;
        }

        foreach (ArkStockpile stockpile in root.GetComponentsInChildren<ArkStockpile>())
        {
            stockpile.enabled = false;
        }
    }

    private static void DisableColliders(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        foreach (Collider collider in root.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }
    }

    private static void ApplyGhostMaterial(GameObject root, Material material)
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

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null || layer < 0)
        {
            return;
        }

        root.layer = layer;
        foreach (Transform child in root.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static bool WasBuildMenuPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.bKey.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.B);
#else
        return false;
#endif
    }

    private static bool WasCampfirePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
#else
        return false;
#endif
    }

    private static bool WasStockpilePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
#else
        return false;
#endif
    }

    private static bool WasDockPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
#else
        return false;
#endif
    }

    private static bool WasShipPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4);
#else
        return false;
#endif
    }

    private static bool WasWorkshopPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.digit5Key.wasPressedThisFrame || Keyboard.current.numpad5Key.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5);
#else
        return false;
#endif
    }

    private static bool WasLumberHousePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.digit6Key.wasPressedThisFrame || Keyboard.current.numpad6Key.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6);
#else
        return false;
#endif
    }

    private static bool WasPlacePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private static bool WasCancelPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return true;
        }

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1);
#else
        return false;
#endif
    }
}
