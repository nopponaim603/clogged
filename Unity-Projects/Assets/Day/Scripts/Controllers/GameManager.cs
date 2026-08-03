using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public CrewManager CrewManager;
    public ResourceManager ResourceManager;
    public MissionManager MissionManager;
    public TimeManager TimeManager;
    public CrewIntakeUI CrewIntakeUI;
    public UIManager UIManager;

    public Transform BaseTransform;

    public GameObject NodeWoodPrefab;
    public GameObject NodeFoodPrefab;
    public GameObject NodeRelicPrefab;

    public List<ResourceNode> Nodes = new List<ResourceNode>();

    [Header("Test Map Settings")]
    public int NodeCount = 3;
    public float MinDistanceFromBase = 5f;
    public float MinNodeSpacing = 3f;

    [Header("Map Size")]
    public float MapWidth = 30f;
    public float MapHeight = 20f;

    [Header("Scene Names")]
    public string ShipDaySceneName = "ShipDay";
    public string NightSceneName = "Night";

    [Header("Crew Recovery")]
    [Tooltip("Hp restored to every Down crew each time the ShipDay scene starts (i.e. once per day).")]
    public float DailyHealAmount = 40f;

    // Cross-scene run state that used to live in RunData
    public int CurrentBlockIndex { get; private set; } = 0;

    /// <summary>True once the day's timer has run out and end-of-day resolution has run.</summary>
    public bool DayEnded { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (CrewManager == null) CrewManager = GetComponentInChildren<CrewManager>();
        if (ResourceManager == null) ResourceManager = GetComponentInChildren<ResourceManager>();
        if (MissionManager == null) MissionManager = GetComponentInChildren<MissionManager>();
        if (TimeManager == null) TimeManager = GetComponentInChildren<TimeManager>();

        TimeManager.OnDayEnded += HandleDayEnded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            if (TimeManager != null)
                TimeManager.OnDayEnded -= HandleDayEnded;
        }
    }

    /// <summary>
    /// Call this from ShipDaySceneBootstrap.Start() — AFTER it has assigned
    /// BaseTransform and the node prefabs. Doing this explicitly (instead of
    /// reacting to SceneManager.sceneLoaded) avoids a race: that event fires
    /// before the bootstrap's own Start() runs, which would mean GenerateWorld()
    /// ran with a still-null BaseTransform.
    /// </summary>
    public void InitializeShipDayScene()
    {
        CrewManager.HealDownCrews(DailyHealAmount);

        // First time the run reaches ShipDay, no crew have been hired yet.
        // Show the free intake screen and only start the day once it's confirmed.
        if (CrewManager.Crews.Count == 0 && CrewIntakeUI != null)
        {
            CrewIntakeUI.ShowIntake(BeginDay);
        }
        else
        {
            BeginDay();
        }
    }

    private void BeginDay()
    {
        DayEnded = false;
        MissionManager.ClearActiveWorkers();
        TimeManager.StartDay();
        GenerateWorld();
    }

    /// <summary>
    /// Fired once by TimeManager.OnDayEnded when the clock hits zero.
    /// Resolves food consumption and day-resource loss, then waits for the
    /// player to confirm before moving on (see ProceedToNight()).
    /// </summary>
    private void HandleDayEnded()
    {
        int crewCount = CrewManager.Crews.Count;
        bool fedEveryone = ResourceManager.ConsumeFood(crewCount);
        var lost = ResourceManager.LoseDayResources();

        string summary = fedEveryone
            ? "The day is over. Everyone ate."
            : "The day is over. Food ran out — the crew went hungry!";

        if (lost.Count > 0)
        {
            foreach (var kvp in lost)
            {
                summary += $" Lost {kvp.Value} {kvp.Key} overnight.";
            }
        }

        UIManager?.ShowMessage(summary);
        DayEnded = true;
    }

    /// <summary>
    /// Call this from a "Go to Night" UI button. Only proceeds once the day
    /// has actually ended and been resolved by HandleDayEnded().
    /// </summary>
    public void ProceedToNight()
    {
        if (!DayEnded)
        {
            UIManager?.ShowMessage("The day isn't over yet.");
            return;
        }

        GoToNight();
    }

    private void Update()
    {
        // Only tick the day clock while we're actually in the ShipDay scene.
        if (SceneManager.GetActiveScene().name == ShipDaySceneName)
        {
            TimeManager.AdvanceTime(Time.deltaTime);
        }
    }

    public void GoToNight()
    {
        SceneManager.LoadScene(NightSceneName);
    }

    public void GoToShip(int blockIndex)
    {
        CurrentBlockIndex = blockIndex;
        SceneManager.LoadScene(ShipDaySceneName);
    }

    private void GenerateWorld()
    {
        Nodes.Clear();

        if (BaseTransform == null)
        {
            Debug.LogWarning("BaseTransform is missing. Assign it via a scene bootstrap component after load.");
            return;
        }

        for (int i = 0; i < NodeCount; i++)
        {
            Vector3 position = GetRandomNodePosition();
            SpawnRandomNode($"RandomNode_{i + 1}", position);
        }
    }

    private Vector3 GetRandomNodePosition()
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            float x = Random.Range(-MapWidth / 2f, MapWidth / 2f);
            float y = Random.Range(-MapHeight / 2f, MapHeight / 2f);

            Vector3 candidate = new Vector3(x, y, 0f);

            if (Vector3.Distance(candidate, BaseTransform.position) < MinDistanceFromBase)
                continue;

            bool tooClose = false;
            foreach (var node in Nodes)
            {
                if (Vector3.Distance(candidate, node.transform.position) < MinNodeSpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
                return candidate;
        }

        return BaseTransform.position + Vector3.right * 5f;
    }

    private void SpawnRandomNode(string name, Vector3 position)
    {
        GameObject prefab = GetRandomPrefab();

        if (prefab == null)
        {
            Debug.LogWarning("A node prefab is missing.");
            return;
        }

        GameObject nodeObj = Instantiate(prefab, position, Quaternion.identity);
        nodeObj.name = name;

        ResourceNode node = nodeObj.GetComponent<ResourceNode>();
        if (node == null)
        {
            Debug.LogWarning("Spawned node prefab missing ResourceNode.");
            return;
        }

        node.Position = new Vector2(position.x, position.y);
        Nodes.Add(node);
    }

    private GameObject GetRandomPrefab()
    {
        float roll = Random.value;

        if (roll < 0.33f) return NodeWoodPrefab;
        if (roll < 0.66f) return NodeFoodPrefab;
        return NodeRelicPrefab;
    }
}
