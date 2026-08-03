using UnityEngine;

public class TestDamage : MonoBehaviour
{
    public BaseHealth baseHealth;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            baseHealth.TakeDamage(10);
        }
    }
}
