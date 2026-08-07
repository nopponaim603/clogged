using UnityEngine;
using System.Collections;

public class EnemyMove : MonoBehaviour
{
    public Transform target;
    public float moveSpeed = 2f;

    [Header("Target Search")]
    public float searchRange = 2f;
    public string enemyTag = "PlayerUnit";

    private Transform currentTarget;
    [Header("Attack")]
    public int damage = 10;
    public float attackRate = 1f;

    private bool reachedBase = false;
    private BaseHealth baseHealth;
    private PlayerHealth playerHealth;

    void Start()
    {
        EnemyManager.Instance.RegisterEnemy(this);
        if (target == null)
        {
            GameObject baseObj = GameObject.FindGameObjectWithTag("Base");

            if (baseObj != null)
                target = baseObj.transform;
        }
    }

    void Update()
    {
        if (reachedBase)
            return;

        FindTarget();

        Transform moveTarget = currentTarget != null ? currentTarget : target;

        if (moveTarget == null)
            return;

        transform.position = Vector2.MoveTowards(
            transform.position,
            moveTarget.position,
            moveSpeed * Time.deltaTime
        );
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, searchRange);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerUnit"))
        {
            reachedBase = true;

            playerHealth = other.GetComponent<PlayerHealth>();

            StartCoroutine(AttackPlayer());
        }
        else if (other.CompareTag("Base"))
        {
            reachedBase = true;

            baseHealth = other.GetComponent<BaseHealth>();

            StartCoroutine(AttackBase());
        }
    }

    IEnumerator AttackBase()
    {
        while (baseHealth != null)
        {
            baseHealth.TakeDamage(damage);

            yield return new WaitForSeconds(attackRate);
        }
        PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
    }
    IEnumerator AttackPlayer()
    {
        while (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);

            yield return new WaitForSeconds(attackRate);
        }

        reachedBase = false;
    }
    void FindTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, searchRange);

        float closestDistance = Mathf.Infinity;
        Transform closestTarget = null;

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag(enemyTag))
            {
                float distance = Vector2.Distance(transform.position, hit.transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = hit.transform;
                }
            }
        }

        currentTarget = closestTarget;
    }
    void OnDestroy()
    {
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.RemoveEnemy(this);
    }
}