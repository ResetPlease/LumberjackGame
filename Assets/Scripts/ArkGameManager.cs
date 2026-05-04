using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public class ArkGameManager : MonoBehaviour
{
    public static ArkGameManager Instance { get; private set; }

    [Header("Economy")]
    public int storedLogs;
    public int storedPlanks;
    public int playerCarriedLogs;
    public int maxCarriedLogs = 3;
    public int hiredWorkers;
    public int axeLevel = 1;
    public int workerLevel = 1;

    [Header("Flood")]
    public float gameDuration = 600f;
    public float waterStartHeight = -1.2f;
    public float waterEndHeight = 1.5f;
    public Transform water;
    public Transform campHeart;

    [Header("World")]
    public ArkStockpile stockpile;
    public ArkWorker workerPrefab;
    public Material logMaterial;
    public Text hudText;
    public Text promptText;
    public Text messageText;
    public GameObject statsPanel;
    public Text statsText;
    public GameObject menuPanel;
    public Button continueButton;
    public Button quitButton;
    public GameObject voyagePanel;
    public Button sailButton;
    public bool showMenuOnStart = true;

    private readonly List<ArkTree> trees = new List<ArkTree>();
    private readonly List<ArkBuildSite> buildSites = new List<ArkBuildSite>();
    private readonly List<ArkSectionSite> arkSections = new List<ArkSectionSite>();
    private readonly List<ArkWorker> workers = new List<ArkWorker>();
    private readonly List<ArkDogWorker> dogs = new List<ArkDogWorker>();
    private float timeRemaining;
    private float messageTimer;
    private bool gameOver;
    private bool lumberHouseUnlocked;
    private bool stockpileBuilt;
    private bool workshopUnlocked;
    private bool dockBuilt;
    private bool shipBuilt;
    private bool menuOpen;
    private bool voyageOpen;

    public bool LumberHouseUnlocked => lumberHouseUnlocked;
    public bool StockpileBuilt => stockpileBuilt;
    public bool WorkshopUnlocked => workshopUnlocked;
    public bool DockBuilt => dockBuilt;
    public bool ShipBuilt => shipBuilt;
    public bool MenuOpen => menuOpen || voyageOpen;
    public bool GameOver => gameOver;
    public int AxeDamage => 5 + (axeLevel - 1) * 5;
    public float WorkerDamage => 9f + workerLevel * 6f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        timeRemaining = gameDuration;
    }

    private void Start()
    {
        BindMenuButtons();
        foreach (ArkTree tree in FindObjectsOfType<ArkTree>())
        {
            RegisterTree(tree);
        }

        RefreshUnlocks();
        UpdateStatsPanel();
        SetMenuOpen(showMenuOnStart);
    }

    private void Update()
    {
        HandleMenuInput();

        if (gameOver)
        {
            UpdateStatsPanel();
            return;
        }

        if (menuOpen || voyageOpen)
        {
            UpdateStatsPanel();
            return;
        }

        timeRemaining -= Time.deltaTime;
        float floodProgress = Mathf.Clamp01(1f - timeRemaining / gameDuration);
        if (water != null)
        {
            Vector3 position = water.position;
            position.y = Mathf.Lerp(waterStartHeight, waterEndHeight, floodProgress);
            water.position = position;
        }

        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f && messageText != null)
            {
                messageText.text = string.Empty;
            }
        }

        if (timeRemaining <= 0f)
        {
            Lose("Время вышло. Ковчег не готов.");
        }

        if (water != null && campHeart != null && water.position.y >= campHeart.position.y)
        {
            Lose("Вода затопила лагерь.");
        }

        UpdateStatsPanel();
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;
        }
    }

    private void BindMenuButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ContinueGame);
            continueButton.onClick.AddListener(ContinueGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }

        if (sailButton != null)
        {
            sailButton.onClick.RemoveListener(SailAway);
            sailButton.onClick.AddListener(SailAway);
        }
    }

    private void HandleMenuInput()
    {
        if (voyageOpen)
        {
            return;
        }

        if (!WasMenuPressed())
        {
            return;
        }

        if (menuOpen)
        {
            SetMenuOpen(false);
            return;
        }

        if (IsAnyBuildPlannerPlanning())
        {
            return;
        }

        SetMenuOpen(true);
    }

    public void ContinueGame()
    {
        SetMenuOpen(false);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void BoardShip(ArkPlayerController player, ArkBuildSite shipSite)
    {
        if (player == null || shipSite == null || !shipSite.IsBuilt || shipSite.Kind != BuildingKind.Ship)
        {
            return;
        }

        Vector3 deckPosition = shipSite.transform.TransformPoint(new Vector3(0f, 2.7f, -2.2f));
        player.TeleportTo(deckPosition);
        ShowMessage("Ты поднялся на корабль.");
        SetVoyageOpen(true);
    }

    public void SailAway()
    {
        Time.timeScale = 1f;
        gameOver = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void SetMenuOpen(bool open)
    {
        menuOpen = open;
        if (menuPanel != null)
        {
            menuPanel.SetActive(open);
        }

        Time.timeScale = open ? 0f : 1f;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
        ClearPrompt();
    }

    private void SetVoyageOpen(bool open)
    {
        EnsureVoyagePanel();
        voyageOpen = open;
        if (voyagePanel != null)
        {
            voyagePanel.SetActive(open);
        }

        Time.timeScale = open ? 0f : 1f;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
        ClearPrompt();
    }

    private void EnsureVoyagePanel()
    {
        if (voyagePanel != null)
        {
            if (sailButton != null)
            {
                sailButton.onClick.RemoveListener(SailAway);
                sailButton.onClick.AddListener(SailAway);
            }

            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        EnsureEventSystem();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        voyagePanel = new GameObject("Панель отплытия");
        voyagePanel.transform.SetParent(canvas.transform, false);
        Image background = voyagePanel.AddComponent<Image>();
        background.color = new Color(0.035f, 0.06f, 0.075f, 0.94f);
        RectTransform panelRect = voyagePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(430f, 250f);

        Text title = CreateVoyageText(voyagePanel.transform, "Заголовок отплытия", font, "КОРАБЛЬ ГОТОВ", 28, new Vector2(0f, 70f), new Vector2(360f, 52f));
        title.fontStyle = FontStyle.Bold;
        CreateVoyageText(voyagePanel.transform, "Текст отплытия", font, "Можно покинуть остров до потопа.", 18, new Vector2(0f, 20f), new Vector2(340f, 42f));

        sailButton = CreateVoyageButton(voyagePanel.transform, font);
        sailButton.onClick.RemoveListener(SailAway);
        sailButton.onClick.AddListener(SailAway);
        voyagePanel.SetActive(false);
    }

    private static Text CreateVoyageText(Transform parent, string name, Font font, string value, int size, Vector2 position, Vector2 rectSize)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.92f, 0.9f, 0.78f, 1f);
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = rectSize;
        return text;
    }

    private static Button CreateVoyageButton(Transform parent, Font font)
    {
        GameObject buttonObject = new GameObject("Кнопка уплыть");
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.16f, 0.35f, 0.34f, 0.96f);
        Button button = buttonObject.AddComponent<Button>();
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -70f);
        rect.sizeDelta = new Vector2(250f, 58f);

        Text label = CreateVoyageText(buttonObject.transform, "Текст кнопки", font, "Уплыть", 22, Vector2.zero, new Vector2(230f, 54f));
        label.color = Color.white;
        return button;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<InputSystemUIInputModule>();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif
    }

    private static bool IsAnyBuildPlannerPlanning()
    {
        foreach (ArkBuildPlanner planner in FindObjectsOfType<ArkBuildPlanner>())
        {
            if (planner != null && planner.IsPlanning)
            {
                return true;
            }
        }

        return false;
    }

    public void RegisterTree(ArkTree tree)
    {
        if (!trees.Contains(tree))
        {
            trees.Add(tree);
        }
    }

    public void UnregisterTree(ArkTree tree)
    {
        trees.Remove(tree);
    }

    public void RegisterBuildSite(ArkBuildSite site)
    {
        if (!buildSites.Contains(site))
        {
            buildSites.Add(site);
        }
    }

    public void RegisterArkSection(ArkSectionSite section)
    {
        if (!arkSections.Contains(section))
        {
            arkSections.Add(section);
        }
    }

    public void RegisterDog(ArkDogWorker dog)
    {
        if (dog != null && !dogs.Contains(dog))
        {
            dogs.Add(dog);
        }
    }

    public ArkStockpile FindNearestStockpile(Vector3 from)
    {
        ArkStockpile best = null;
        float bestDistance = float.MaxValue;

        foreach (ArkBuildSite site in buildSites)
        {
            if (site == null || !site.IsBuilt || site.Kind != BuildingKind.Stockpile)
            {
                continue;
            }

            ArkStockpile candidate = site.GetComponentInChildren<ArkStockpile>(true);
            if (candidate == null)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(candidate.transform.position - from);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best != null ? best : stockpile;
    }

    public ArkTree FindNearestTree(Vector3 from)
    {
        ArkTree best = null;
        float bestDistance = float.MaxValue;

        foreach (ArkTree tree in trees)
        {
            if (tree == null || tree.IsFelled)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(tree.transform.position - from);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = tree;
            }
        }

        return best;
    }

    public void SpawnLog(Vector3 position)
    {
        GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        log.name = "Бревно";
        log.transform.position = position + Vector3.up * 0.35f;
        log.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        log.transform.localScale = new Vector3(0.28f, 0.95f, 0.28f);
        log.AddComponent<ArkLogPickup>();
        Rigidbody body = log.AddComponent<Rigidbody>();
        body.mass = 0.6f;
        Renderer renderer = log.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = logMaterial != null ? logMaterial : ArkSceneMaterials.Log;
        }
    }

    public bool TryCarryLog()
    {
        if (playerCarriedLogs >= maxCarriedLogs)
        {
            ShowMessage("Руки заняты. Отнеси бревна на склад.");
            return false;
        }

        playerCarriedLogs++;
        ShowMessage("Бревно подобрано.");
        return true;
    }

    public void DepositCarriedLogs()
    {
        if (playerCarriedLogs <= 0)
        {
            ShowMessage("Нечего складывать.");
            return;
        }

        storedLogs += playerCarriedLogs;
        ShowMessage("Бревна на складе: +" + playerCarriedLogs);
        playerCarriedLogs = 0;
    }

    public bool TrySpend(int logs, int planks)
    {
        if (AvailableLogs < logs || storedPlanks < planks)
        {
            ShowMessage("Не хватает ресурсов: " + FormatCost(logs, planks));
            return false;
        }

        SpendLogs(logs);
        storedPlanks -= planks;
        return true;
    }

    public bool DeliverResourcesTo(ArkBuildSite site)
    {
        if (site == null || site.IsBuilt)
        {
            return false;
        }

        int missingLogs = Mathf.Max(0, site.LogsCost - site.LogsDelivered);
        int missingPlanks = Mathf.Max(0, site.PlanksCost - site.PlanksDelivered);
        bool delivered = false;

        if (missingLogs > 0 && playerCarriedLogs > 0)
        {
            int amount = Mathf.Min(playerCarriedLogs, missingLogs);
            playerCarriedLogs -= amount;
            site.LogsDelivered += amount;
            missingLogs -= amount;
            delivered = true;
            ShowMessage("Добавлено брёвен: " + amount);
        }

        if (missingLogs > 0 && storedLogs > 0)
        {
            int amount = Mathf.Min(storedLogs, missingLogs);
            storedLogs -= amount;
            site.LogsDelivered += amount;
            missingLogs -= amount;
            delivered = true;
            ShowMessage("Со склада добавлено брёвен: " + amount);
        }

        if (missingPlanks > 0 && storedPlanks > 0)
        {
            int amount = Mathf.Min(storedPlanks, missingPlanks);
            storedPlanks -= amount;
            site.PlanksDelivered += amount;
            missingPlanks -= amount;
            delivered = true;
            ShowMessage("Со склада добавлено досок: " + amount);
        }

        if (site.LogsDelivered >= site.LogsCost && site.PlanksDelivered >= site.PlanksCost)
        {
            site.IsBuilt = true;
            if (site.Kind == BuildingKind.Stockpile)
            {
                ArkStockpile builtStockpile = site.GetComponentInChildren<ArkStockpile>(true);
                if (builtStockpile != null)
                {
                    builtStockpile.enabled = true;
                    stockpile = builtStockpile;
                }
            }

            OnBuildingBuilt(site.Kind);
            return true;
        }

        if (!delivered)
        {
            if (site.PlanksCost > site.PlanksDelivered)
            {
                ShowMessage("Нужны ресурсы: принеси брёвна или сделай доски на лесопилке.");
            }
            else
            {
                ShowMessage("Принеси бревна к стройке.");
            }
        }

        return delivered;
    }

    public void OnBuildingBuilt(BuildingKind kind)
    {
        RefreshUnlocks();
        if (kind == BuildingKind.Campfire) ShowMessage("Костёр готов. Лагерь основан.");
        if (kind == BuildingKind.Stockpile) ShowMessage("Склад готов. Теперь ресурсы можно хранить.");
        if (kind == BuildingKind.LumberHouse) ShowMessage("Дом лесника готов. Собака начинает добывать брёвна.");
        if (kind == BuildingKind.Workshop) ShowMessage("Лесопилка готова. Можно делать доски и улучшать топор.");
        if (kind == BuildingKind.Dock) ShowMessage("Причал готов. Теперь можно строить корабль.");
        if (kind == BuildingKind.Ship) ShowMessage("Корабль готов. Осталось пережить потоп.");
    }

    public void ConvertLogsToPlanks()
    {
        if (!workshopUnlocked)
        {
            ShowMessage("Сначала построй лесопилку.");
            return;
        }

        if (AvailableLogs < 2)
        {
            ShowMessage("Для досок нужно 2 бревна.");
            return;
        }

        SpendLogs(2);
        storedPlanks += 3;
        ShowMessage("Лесопилка сделала 3 доски.");
    }

    public void UpgradeAxe()
    {
        if (!workshopUnlocked)
        {
            ShowMessage("Сначала построй лесопилку.");
            return;
        }

        int cost = 4 + axeLevel * 3;
        if (!TrySpend(0, cost))
        {
            return;
        }

        axeLevel++;
        ShowMessage("Топор улучшен. Уровень " + axeLevel);
    }

    public void HireWorker()
    {
        if (!lumberHouseUnlocked)
        {
            ShowMessage("Сначала построй дом лесоруба.");
            return;
        }

        int costLogs = 3 + hiredWorkers;
        int costPlanks = 1 + hiredWorkers;
        if (!TrySpend(costLogs, costPlanks))
        {
            return;
        }

        if (workerPrefab == null)
        {
            ShowMessage("Prefab рабочего не назначен.");
            return;
        }

        hiredWorkers++;
        Vector3 spawn = stockpile != null ? stockpile.transform.position + new Vector3(hiredWorkers * 1.2f, 0f, -2.5f) : transform.position;
        ArkWorker worker = Instantiate(workerPrefab, spawn, Quaternion.identity);
        worker.name = "Рабочий " + hiredWorkers;
        worker.gameObject.SetActive(true);
        workers.Add(worker);
        ShowMessage("Нанят рабочий #" + hiredWorkers);
    }

    public void UpgradeWorkers()
    {
        if (!lumberHouseUnlocked)
        {
            ShowMessage("Сначала построй дом лесоруба.");
            return;
        }

        int cost = 5 + workerLevel * 4;
        if (!TrySpend(0, cost))
        {
            return;
        }

        workerLevel++;
        ShowMessage("Рабочие улучшены. Уровень " + workerLevel);
    }

    public void WorkerDepositLog(ArkWorker worker)
    {
        storedLogs++;
        ShowMessage(worker.name + " принёс бревно.");
    }

    public void DogDepositLog(ArkDogWorker dog)
    {
        storedLogs++;
        ShowMessage(dog.name + " принесла бревно на склад.");
    }

    public void OnArkSectionBuilt()
    {
        foreach (ArkSectionSite section in arkSections)
        {
            if (section != null && !section.IsBuilt)
            {
                return;
            }
        }

        Win();
    }

    public void ShowPrompt(string prompt)
    {
        if (promptText != null)
        {
            promptText.text = prompt;
        }
    }

    public void ClearPrompt()
    {
        if (promptText != null)
        {
            promptText.text = string.Empty;
        }
    }

    public void ShowMessage(string message)
    {
        if (messageText == null)
        {
            return;
        }

        messageText.text = message;
        messageTimer = 3f;
    }

    public static string FormatCost(int logs, int planks)
    {
        return logs + " брёвен / " + planks + " досок";
    }

    public static string FormatProgress(int logsDone, int logsTotal, int planksDone, int planksTotal)
    {
        return logsDone + "/" + logsTotal + " брёвен, " + planksDone + "/" + planksTotal + " досок";
    }

    private int AvailableLogs => storedLogs + playerCarriedLogs;

    private void SpendLogs(int amount)
    {
        int fromHands = Mathf.Min(playerCarriedLogs, amount);
        playerCarriedLogs -= fromHands;
        amount -= fromHands;

        if (amount > 0)
        {
            storedLogs -= amount;
        }
    }

    private void RefreshUnlocks()
    {
        lumberHouseUnlocked = false;
        stockpileBuilt = false;
        workshopUnlocked = false;
        dockBuilt = false;
        shipBuilt = false;
        foreach (ArkBuildSite site in buildSites)
        {
            if (site == null || !site.IsBuilt)
            {
                continue;
            }

            if (site.Kind == BuildingKind.Stockpile) stockpileBuilt = true;
            if (site.Kind == BuildingKind.LumberHouse) lumberHouseUnlocked = true;
            if (site.Kind == BuildingKind.Workshop) workshopUnlocked = true;
            if (site.Kind == BuildingKind.Dock) dockBuilt = true;
            if (site.Kind == BuildingKind.Ship) shipBuilt = true;
        }
    }

    private void UpdateStatsPanel()
    {
        bool statsVisible = IsStatsPressed();
        if (statsPanel != null)
        {
            statsPanel.SetActive(statsVisible);
        }

        if (!statsVisible || statsText == null)
        {
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("СТАТУС ЛАГЕРЯ");
        builder.AppendLine();
        builder.AppendLine("Цель: разведать остров и заготовить первые брёвна");
        builder.AppendLine("До потопа: " + Mathf.CeilToInt(Mathf.Max(0f, timeRemaining)) + " сек.");
        builder.AppendLine();
        builder.AppendLine("Ресурсы");
        builder.AppendLine("  Брёвна на складе: " + storedLogs);
        builder.AppendLine("  Доски: " + storedPlanks);
        builder.AppendLine("  В руках: " + playerCarriedLogs + " / " + maxCarriedLogs + " брёвен");
        builder.AppendLine();
        builder.AppendLine("Инструменты");
        builder.AppendLine("  Топор: уровень " + axeLevel);
        builder.AppendLine("  Урон по дереву: " + AxeDamage);
        builder.AppendLine();
        builder.AppendLine("Остров");
        builder.AppendLine("  Деревья доступны: " + ActiveTreeCount());
        builder.AppendLine("  Собаки лесника: " + dogs.Count);
        builder.AppendLine("  Склад: " + (stockpileBuilt ? "построен" : "не построен"));
        builder.AppendLine("  Лесопилка: " + (workshopUnlocked ? "построена" : "не построена"));
        builder.AppendLine("  Причал: " + (dockBuilt ? "построен" : "не построен"));
        builder.AppendLine("  Корабль: " + (shipBuilt ? "построен" : "не построен"));
        builder.AppendLine("  Ковчег: " + BuiltArkSections() + " / " + arkSections.Count + " секций");
        if (gameOver)
        {
            builder.AppendLine();
            builder.AppendLine("Игра окончена.");
        }

        statsText.text = builder.ToString();
    }

    private int ActiveTreeCount()
    {
        int count = 0;
        foreach (ArkTree tree in trees)
        {
            if (tree != null && !tree.IsFelled)
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsStatsPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.tabKey.isPressed;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.Tab);
#else
        return false;
#endif
    }

    private static bool WasMenuPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.escapeKey.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    private int BuiltArkSections()
    {
        int count = 0;
        foreach (ArkSectionSite section in arkSections)
        {
            if (section != null && section.IsBuilt)
            {
                count++;
            }
        }

        return count;
    }

    private void Win()
    {
        if (gameOver)
        {
            return;
        }

        gameOver = true;
        ShowMessage("Победа! Все основные части ковчега построены до потопа.");
    }

    private void Lose(string reason)
    {
        if (gameOver)
        {
            return;
        }

        gameOver = true;
        ShowMessage("Поражение: " + reason);
    }
}
