using UnityEngine;

public class ArkStockpile : MonoBehaviour, IArkInteractable
{
    public string Prompt => "E: сложить бревна на склад";

    public void Interact(ArkPlayerController player)
    {
        if (ArkGameManager.Instance != null)
        {
            ArkGameManager.Instance.DepositCarriedLogs();
        }
    }
}
