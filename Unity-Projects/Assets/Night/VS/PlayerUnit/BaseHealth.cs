using UnityEngine;

public class BaseHealth : MonoBehaviour
{
    [Header("Base HP")]
    public int maxHP = 100;
    private int currentHP;

    void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;

        Debug.Log("Base HP : " + currentHP);

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    public int GetHP()
    {
        return currentHP;
    }

    public int GetMaxHP()
    {
        return maxHP;
    }

    void Die()
    {
        Debug.Log("Game Over");
    }
}