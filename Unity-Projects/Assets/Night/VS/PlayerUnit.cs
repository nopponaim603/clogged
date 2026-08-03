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
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        float closestDistance = Mathf.Infinity;
        Transform closestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);

            if (distance < closestDistance && distance <= searchRange)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }

        target = closestEnemy;
    }

    IEnumerator Attack()
    {
        isAttacking = true;

        while (target != null)
        {
            float distance = Vector2.Distance(transform.position, target.position);

            if (distance > attackRange)
                break;

            // ลดเลือดศัตรู
            EnemyHealth enemy = target.GetComponent<EnemyHealth>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            yield return new WaitForSeconds(attackRate);
        }

        isAttacking = false;
    }
}