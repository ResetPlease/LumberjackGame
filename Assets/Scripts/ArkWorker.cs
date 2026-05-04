using UnityEngine;

public class ArkWorker : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float chopInterval = 1.15f;

    private ArkTree targetTree;
    private bool carryingLog;
    private float chopTimer;

    private void Update()
    {
        ArkGameManager manager = ArkGameManager.Instance;
        if (manager == null || manager.GameOver)
        {
            return;
        }

        if (carryingLog)
        {
            MoveToStockpile(manager);
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
        if (distance > 1.75f)
        {
            MoveToward(targetTree.transform.position);
            return;
        }

        chopTimer -= Time.deltaTime;
        if (chopTimer <= 0f)
        {
            chopTimer = chopInterval;
            targetTree.Hit(manager.WorkerDamage);
            if (targetTree == null || targetTree.IsFelled)
            {
                carryingLog = true;
            }
        }
    }

    private void MoveToStockpile(ArkGameManager manager)
    {
        if (manager.stockpile == null)
        {
            return;
        }

        Vector3 target = manager.stockpile.transform.position;
        if (Vector3.Distance(transform.position, target) > 1.6f)
        {
            MoveToward(target);
            return;
        }

        carryingLog = false;
        manager.WorkerDepositLog(this);
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
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, 540f * Time.deltaTime);
        transform.position += direction.normalized * moveSpeed * Time.deltaTime;
    }
}
