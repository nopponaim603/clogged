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

    void Die()
    {
        Debug.Log("GAME OVER");

        NightGameManager.Instance.GameOver();
    }

    public int GetHP()
    {
        return currentHP;
    }
}