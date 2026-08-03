using UnityEngine;
using System.Collections;


public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    public Transform[] spawnPoints;

    [Header("Amount")]
    public int minSpawn = 5;
    public int maxSpawn = 10;

    [Header("Delay")]
    public float spawnDelay = 0.5f;

    void Start()
    {
        StartCoroutine(SpawnEnemies());
    }

    IEnumerator SpawnEnemies()
    {
        int amount = Random.Range(minSpawn, maxSpawn + 1);

        for (int i = 0; i < amount; i++)
        {
            Instantiate(enemyPrefab,
                        spawnPoint.position,
                        Quaternion.identity);

            yield return new WaitForSeconds(spawnDelay);
        }
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

        Instantiate(enemyPrefab,
                    point.position,
                    Quaternion.identity);
    }
        
}