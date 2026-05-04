using UnityEngine;

public class ArkBuildSite : MonoBehaviour, IArkInteractable, IArkSecondaryInteractable
{
    public BuildingKind Kind;
    public string DisplayName;
    public int LogsCost;
    public int PlanksCost;
    public int LogsDelivered;
    public int PlanksDelivered;
    public bool IsBuilt;
    public GameObject GhostVisual;
    public GameObject BuiltVisual;

    public string Prompt
    {
        get
        {
            if (IsBuilt)
            {
                if (Kind == BuildingKind.Workshop)
                {
                    return "E: распилить брёвна | R: улучшить топор";
                }

                if (Kind == BuildingKind.LumberHouse)
                {
                    return "Дом лесника: собака работает";
                }

                if (Kind == BuildingKind.Stockpile)
                {
                    return "E: сложить бревна на склад";
                }

                if (Kind == BuildingKind.Dock)
                {
                    return "Причал построен";
                }

                if (Kind == BuildingKind.Ship)
                {
                    return "E: подняться на корабль";
                }

                return DisplayName + " построено";
            }

            return "E: добавить ресурсы в " + DisplayName + " (" + ArkGameManager.FormatProgress(LogsDelivered, LogsCost, PlanksDelivered, PlanksCost) + ")";
        }
    }

    private void Start()
    {
        if (ArkGameManager.Instance != null)
        {
            ArkGameManager.Instance.RegisterBuildSite(this);
        }

        RefreshVisuals();
    }

    public void Interact(ArkPlayerController player)
    {
        if (ArkGameManager.Instance == null)
        {
            return;
        }

        if (!IsBuilt)
        {
            if (ArkGameManager.Instance.DeliverResourcesTo(this))
            {
                RefreshVisuals();
                if (IsBuilt && Kind == BuildingKind.LumberHouse)
                {
                    ArkForesterHouse house = GetComponent<ArkForesterHouse>();
                    if (house != null)
                    {
                        house.SpawnDog();
                    }
                }
            }

            return;
        }

        if (Kind == BuildingKind.Workshop)
        {
            ArkGameManager.Instance.ConvertLogsToPlanks();
        }
        else if (Kind == BuildingKind.LumberHouse)
        {
            ArkGameManager.Instance.ShowMessage("Собака сама добывает брёвна.");
        }
        else if (Kind == BuildingKind.Stockpile)
        {
            ArkGameManager.Instance.DepositCarriedLogs();
        }
        else if (Kind == BuildingKind.Ship)
        {
            ArkGameManager.Instance.BoardShip(player, this);
        }
    }

    public void SecondaryInteract(ArkPlayerController player)
    {
        if (!IsBuilt || ArkGameManager.Instance == null)
        {
            return;
        }

        if (Kind == BuildingKind.Workshop)
        {
            ArkGameManager.Instance.UpgradeAxe();
        }
    }

    private void RefreshVisuals()
    {
        if (GhostVisual != null)
        {
            GhostVisual.SetActive(!IsBuilt);
        }

        if (BuiltVisual != null)
        {
            BuiltVisual.SetActive(IsBuilt);
        }
    }
}
