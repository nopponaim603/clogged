using UnityEngine;
using System.Collections;

public class Turret : MonoBehaviour
{
    [Header("Search")]
    public float attackRange = 5f;

    [Header("Attack")]
    public int damage = 20;
    public float attackRate = 1f;

    private Transform target;
    private bool isAttacking = false;

    void Update()
    {
        FindTarget();

        if (target == null)
            return;

        float distance = Vector2.Distance(transform.position, target.position);

        if (distance <= attackRange)
        {
            if (!isAttacking)
            {
                StartCoroutine(Attack());
            }
        }
    }

    void FindTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        float closestDistance = Mathf.Infinity;
        Transform closestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);

            if (distance < closestDistance && distance <= attackRange)
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

            EnemyHealth enemyHealth = target.GetComponent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }

            yield return new WaitForSeconds(attackRate);
        }

        isAttacking = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}