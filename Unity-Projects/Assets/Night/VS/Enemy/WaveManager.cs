using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    public GameObject preparationPanel;
    public EnemySpawner enemySpawner;

    public bool gameStarted = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        preparationPanel.SetActive(true);
        gameStarted = false;
    }

    public void StartWave()
    {
        gameStarted = true;

        preparationPanel.SetActive(false);

        enemySpawner.StartWave();
    }
}