using UnityEngine;

public class ArkLogPickup : MonoBehaviour, IArkInteractable
{
    [SerializeField] private float groundOffset = 0.35f;

    private Rigidbody body;
    private Vector3 lastSafePosition;

    public string Prompt => "E: подобрать бревно";

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        lastSafePosition = ArkWorldRules.ClampToWalkableGround(transform.position, groundOffset);
    }

    private void FixedUpdate()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        Vector3 position = transform.position;
        if (!ArkWorldRules.IsOutsideWorld(position) && position.y > ArkWorldRules.WaterHeight + groundOffset)
        {
            lastSafePosition = position;
            return;
        }

        Vector3 safePosition = ArkWorldRules.ClampToWalkableGround(lastSafePosition, groundOffset);
        if (ArkWorldRules.IsOutsideWorld(position))
        {
            safePosition = ArkWorldRules.ClampToWalkableGround(position, groundOffset);
        }

        MoveToSafePosition(safePosition);
    }

    public void Interact(ArkPlayerController player)
    {
        if (ArkGameManager.Instance != null && ArkGameManager.Instance.TryCarryLog())
        {
            Destroy(gameObject);
        }
    }

    private void MoveToSafePosition(Vector3 position)
    {
        transform.position = position;
        if (body == null)
        {
            return;
        }

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.position = position;
    }
}
