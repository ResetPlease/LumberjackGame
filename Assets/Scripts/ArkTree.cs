using UnityEngine;

public class ArkTree : MonoBehaviour, IArkInteractable
{
    [SerializeField] private int maxHp = 60;
    [SerializeField] private int logYield = 3;

    private int hp;
    private bool felled;

    public bool IsFelled => felled;
    public string Prompt => felled ? "Срублено" : "ЛКМ: рубить дерево (" + hp + "/" + maxHp + " HP)";

    private void Awake()
    {
        hp = maxHp;
    }

    private void OnEnable()
    {
        if (ArkGameManager.Instance != null)
        {
            ArkGameManager.Instance.RegisterTree(this);
        }
    }

    private void OnDisable()
    {
        if (ArkGameManager.Instance != null)
        {
            ArkGameManager.Instance.UnregisterTree(this);
        }
    }

    public void Interact(ArkPlayerController player)
    {
        if (ArkGameManager.Instance != null)
        {
            Hit(ArkGameManager.Instance.AxeDamage);
        }
    }

    public void Hit(float damage)
    {
        if (felled)
        {
            return;
        }

        hp -= Mathf.CeilToInt(damage);
        if (ArkGameManager.Instance != null)
        {
            ArkGameManager.Instance.ShowMessage("Дерево: " + Mathf.Max(0, hp) + " HP");
        }

        if (hp > 0)
        {
            return;
        }

        felled = true;
        if (ArkGameManager.Instance != null)
        {
            for (int i = 0; i < logYield; i++)
            {
                Vector3 offset = Random.insideUnitSphere;
                offset.y = 0f;
                ArkGameManager.Instance.SpawnLog(transform.position + offset);
            }

            ArkGameManager.Instance.ShowMessage("Дерево срублено. Бревна выпали.");
            ArkGameManager.Instance.UnregisterTree(this);
        }

        gameObject.SetActive(false);
    }
}
