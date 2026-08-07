using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    public List<EnemyMove> enemies = new List<EnemyMove>();

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterEnemy(EnemyMove enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    public void RemoveEnemy(EnemyMove enemy)
    {
        enemies.Remove(enemy);
    }
}