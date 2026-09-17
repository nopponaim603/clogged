using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;

    // จุดเกิดทั้งหมด (A, B, C ...)
    public Transform[] spawnPoints;

    // เป้าหมายของมอน (Base)
    public Transform baseTarget;

    [Header("Spawn Amount")]
    public int minSpawn = 5;
    public int maxSpawn = 10;

    [Header("Spawn Delay")]
    public float spawnDelay = 0.5f;

    void Start()
    {
        //StartCoroutine(SpawnEnemies());
    }
    public void StartWave()
    {
        StartCoroutine(SpawnEnemies());
    }
    IEnumerator SpawnEnemies()
    {
        // สุ่มจำนวนมอนที่จะเกิด
        int amount = Random.Range(minSpawn, maxSpawn + 1);

        for (int i = 0; i < amount; i++)
        {
            // สุ่มจุดเกิด
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

            // สร้างมอน
            GameObject enemy = Instantiate(
                enemyPrefab,
                point.position,
                Quaternion.identity
            );

            // กำหนด Base ให้มอน
            EnemyMove move = enemy.GetComponent<EnemyMove>();

            if (move != null)
            {
                move.target = baseTarget;
            }

            yield return new WaitForSeconds(spawnDelay);
        }
    }
}