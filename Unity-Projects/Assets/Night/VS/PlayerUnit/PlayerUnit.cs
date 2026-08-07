using UnityEngine;
using System.Collections;

public class PlayerUnit : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float searchRange = 10f;

    [Header("Attack")]
    public float attackRange = 1f;
    public int damage = 10;
    public float attackRate = 1f;

    private Transform target;
    private bool isAttacking = false;

    void Update()
    {
        FindClosestEnemy();

        if (target == null)
            return;

        float distance = Vector2.Distance(transform.position, target.position);

        if (distance > attackRange)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                target.position,
                moveSpeed * Time.deltaTime
            );
        }
        else
        {
            if (!isAttacking)
            {
                StartCoroutine(Attack());
            }
        }
    }

    void FindClosestEnemy()
    {
        if (EnemyManager.Instance == null)
            return;

        EnemyMove closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (EnemyMove enemy in EnemyManager.Instance.enemies)
        {
            if (enemy == null)
                continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);

            if (distance <= searchRange && distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }

        target = (closest != null) ? closest.transform : null;
    }

    IEnumerator Attack()
    {
        isAttacking = true;

        while (true)
        {
            if (target == null)
                break;

            float distance = Vector2.Distance(transform.position, target.position);

            if (distance > attackRange)
                break;

            EnemyHealth enemy = target.GetComponent<EnemyHealth>();

            if (enemy == null)
                break;

            enemy.TakeDamage(damage);

            yield return new WaitForSeconds(attackRate);
        }

        isAttacking = false;
    }
}