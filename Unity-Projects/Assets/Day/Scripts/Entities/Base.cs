using UnityEngine;

public class Base : MonoBehaviour
{
    public Vector2 Position;
    public float Hp;
    public float MaxHp;

    public bool TakeDamage(float damage)
    {
        Hp -= damage;
        return Hp <= 0;
    }
}
