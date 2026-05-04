using UnityEngine;

public class ArkDogWorker : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4.2f;
    [SerializeField] private float biteDamage = 10f;
    [SerializeField] private float biteInterval = 1f;
    [SerializeField] private float groundOffset = 0.08f;

    private ArkTree targetTree;
    private ArkLogPickup targetLog;
    private bool carryingLog;
    private float biteTimer;
    private Vector3 lastSafePosition;

    private void Awake()
    {
        lastSafePosition = ArkWorldRules.ClampToWalkableGround(transform.position, groundOffset);
        transform.position = lastSafePosition;
    }

    private void Update()
    {
        KeepOnGround();

        ArkGameManager manager = ArkGameManager.Instance;
        if (manager == null || manager.GameOver || manager.MenuOpen)
        {
            return;
        }

        if (carryingLog)
        {
            MoveToStockpile(manager);
            return;
        }

        if (targetLog == null)
        {
            targetLog = FindNearestLog();
        }

        if (targetLog != null)
        {
            MoveToLog();
            return;
        }

        if (targetTree == null || targetTree.IsFelled)
        {
            targetTree = manager.FindNearestTree(transform.position);
            if (targetTree == null)
            {
                return;
            }
        }

        float distance = Vector3.Distance(transform.position, targetTree.transform.position);
        if (distance > 1.55f)
        {
            MoveToward(targetTree.transform.position);
            return;
        }

        biteTimer -= Time.deltaTime;
        if (biteTimer <= 0f)
        {
            biteTimer = biteInterval;
            targetTree.Hit(biteDamage);
            if (targetTree == null || targetTree.IsFelled)
            {
                targetTree = null;
                targetLog = FindNearestLog();
            }
        }
    }

    private void MoveToLog()
    {
        if (targetLog == null)
        {
            return;
        }

        if (Vector3.Distance(transform.position, targetLog.transform.position) > 1.1f)
        {
            MoveToward(targetLog.transform.position);
            return;
        }

        Destroy(targetLog.gameObject);
        targetLog = null;
        carryingLog = true;
    }

    private void MoveToStockpile(ArkGameManager manager)
    {
        ArkStockpile stockpile = manager.FindNearestStockpile(transform.position);
        if (stockpile == null)
        {
            return;
        }

        if (Vector3.Distance(transform.position, stockpile.transform.position) > 1.6f)
        {
            MoveToward(stockpile.transform.position);
            return;
        }

        carryingLog = false;
        manager.DogDepositLog(this);
    }

    private ArkLogPickup FindNearestLog()
    {
        ArkLogPickup best = null;
        float bestDistance = float.MaxValue;
        foreach (ArkLogPickup log in FindObjectsOfType<ArkLogPickup>())
        {
            if (log == null)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(log.transform.position - transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = log;
            }
        }

        return best;
    }

    private void MoveToward(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, 720f * Time.deltaTime);
        transform.position = ArkWorldRules.ClampToWalkableGround(transform.position + direction.normalized * moveSpeed * Time.deltaTime, groundOffset);
        lastSafePosition = transform.position;
    }

    private void KeepOnGround()
    {
        Vector3 position = transform.position;
        if (ArkWorldRules.IsOutsideWorld(position) || position.y < ArkWorldRules.FallRespawnY)
        {
            transform.position = ArkWorldRules.ClampToWalkableGround(lastSafePosition, groundOffset);
            return;
        }

        Vector3 groundedPosition = ArkWorldRules.ClampToWalkableGround(position, groundOffset);
        transform.position = groundedPosition;
        lastSafePosition = groundedPosition;
    }
}
