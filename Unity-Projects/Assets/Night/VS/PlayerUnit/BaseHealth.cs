using UnityEngine;

public class BaseHealth : MonoBehaviour
{
    public int maxHP = 100;
    private int currentHP;

    void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    void Die()
    {
        Time.timeScale = 0f;
    }

    public int GetHP()
    {
        return currentHP;
    }
}