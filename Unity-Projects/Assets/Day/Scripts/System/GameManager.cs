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
    public DayEndSummaryUI DayEndSummaryUI;
    public ShipNavigatorUI ShipNavigatorUI;

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

    /// <summary>
    /// Set by a Bonus Ship node (via SetNextDayResourceBias) to weight what
    /// spawns in today's GenerateWorld(). Cleared automatically once BeginDay()
    /// consumes it, so it only ever affects a single day.
    /// </summary>
    public string PendingResourceBias { get; private set; }

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
        // Show the free intake screen first — the Ship Navigator (and any
        // Recruit event within it) needs at least a starting crew to make sense.
        if (CrewManager.Crews.Count == 0 && CrewIntakeUI != null)
        {
            CrewIntakeUI.ShowIntake(ShowShipNavigatorThenBeginDay);
        }
        else
        {
            ShowShipNavigatorThenBeginDay();
        }
    }

    /// <summary>
    /// Ship Phase step: show the node-picker (Normal/Bonus/Recruit), resolve
    /// whichever the player picks, then start the day. Falls straight through
    /// to BeginDay() if no ShipNavigatorUI is wired up.
    /// </summary>
    private void ShowShipNavigatorThenBeginDay()
    {
        if (ShipNavigatorUI != null)
        {
            ShipNavigatorUI.ShowNavigator(BeginDay);
        }
        else
        {
            BeginDay();
        }
    }

    /// <summary>Called by a Bonus Ship node — biases today's GenerateWorld() toward this resource type.</summary>
    public void SetNextDayResourceBias(string resourceType)
    {
        PendingResourceBias = resourceType;
    }

    private void BeginDay()
    {
        DayEnded = false;
        MissionManager.ClearActiveWorkers();
        TimeManager.StartDay();
        GenerateWorld();
        PendingResourceBias = null; // consumed — only ever applies to the day it was picked for
    }

    /// <summary>
    /// Fired once by TimeManager.OnDayEnded when the clock hits zero (or when
    /// the day is ended early once the mission queue is empty). Resolves food
    /// consumption and day-resource loss, then shows the end-of-day summary
    /// panel. The player reviews it and presses Continue (wired to GoToNight())
    /// to move on.
    /// </summary>
    private void HandleDayEnded()
    {
        int crewCount = CrewManager.Crews.Count;
        bool fedEveryone = ResourceManager.ConsumeFood(crewCount);

        // Snapshot what was gained today BEFORE LoseDayResources() clears it.
        var gainedToday = new Dictionary<string, int>(ResourceManager.DayResources);
        var lost = ResourceManager.LoseDayResources();

        DayEnded = true;

        if (DayEndSummaryUI != null)
        {
            DayEndSummaryUI.Show(gainedToday, lost, fedEveryone, GoToNight);
        }
        else
        {
            // Fallback if no summary panel is wired up: plain text via UIManager.
            string summary = fedEveryone
                ? "The day is over. Everyone ate."
                : "The day is over. Food ran out — the crew went hungry!";

            foreach (var kvp in lost)
            {
                summary += $" Lost {kvp.Value} {kvp.Key} overnight.";
            }

            UIManager?.ShowMessage(summary);
        }
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

    /// <summary>
    /// Call this from an "End Day" UI button. Ends the day right now —
    /// regardless of remaining CurrentTime — as long as no missions are
    /// currently active (i.e. the dispatched queue has finished running).
    /// This is the queue-based alternative to waiting for the clock to hit
    /// zero.
    /// </summary>
    public void EndDayEarly()
    {
        if (DayEnded)
            return;

        if (TimeManager.IsMissionRunning)
        {
            UIManager?.ShowMessage("Wait for all crew to return before ending the day.");
            return;
        }

        TimeManager.EndDay();
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
        if (!string.IsNullOrEmpty(PendingResourceBias))
        {
            // 70% chance to spawn the biased type; falls through to the
            // normal even split the rest of the time so it isn't guaranteed
            // every single node.
            if (Random.value < 0.7f)
            {
                GameObject biased = GetPrefabForType(PendingResourceBias);
                if (biased != null) return biased;
            }
        }

        float roll = Random.value;

        if (roll < 0.33f) return NodeWoodPrefab;
        if (roll < 0.66f) return NodeFoodPrefab;
        return NodeRelicPrefab;
    }

    private GameObject GetPrefabForType(string type)
    {
        switch (type)
        {
            case "wood": return NodeWoodPrefab;
            case "food": return NodeFoodPrefab;
            case "relic": return NodeRelicPrefab;
            default: return null;
        }
    }
}
