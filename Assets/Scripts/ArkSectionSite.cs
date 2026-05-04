using UnityEngine;

public class ArkSectionSite : MonoBehaviour, IArkInteractable
{
    public string DisplayName;
    public int LogsCost;
    public int PlanksCost;
    public bool IsBuilt;
    public GameObject GhostVisual;
    public GameObject BuiltVisual;

    public string Prompt => IsBuilt
        ? DisplayName + " готова"
        : "E: построить " + DisplayName + " (" + ArkGameManager.FormatCost(LogsCost, PlanksCost) + ")";

    private void Start()
    {
        if (ArkGameManager.Instance != null)
        {
            ArkGameManager.Instance.RegisterArkSection(this);
        }

        RefreshVisuals();
    }

    public void Interact(ArkPlayerController player)
    {
        if (IsBuilt || ArkGameManager.Instance == null)
        {
            return;
        }

        if (ArkGameManager.Instance.TrySpend(LogsCost, PlanksCost))
        {
            IsBuilt = true;
            RefreshVisuals();
            ArkGameManager.Instance.ShowMessage(DisplayName + " построена.");
            ArkGameManager.Instance.OnArkSectionBuilt();
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
