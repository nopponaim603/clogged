using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual map layer on top of the existing gather system. Nodes are purely
/// a selection UI - clicking any node just picks its zone, and ConfirmTrip
/// still goes through GatherDispatchController for all the actual logic
/// (AP, roll, loot). This class only handles layout, selection, and the
/// walk-out/walk-back animation.
/// </summary>
public class GatherMapController : MonoBehaviour
{
    public static GatherMapController Instance { get; private set; }

    [Header("Map Setup")]
    [SerializeField] private RectTransform mapPanel;
    [SerializeField] private MapNodeView nodeViewPrefab;
    [Tooltip("The base sits at the centre of MapPanel, plus this offset. Leave at (0,0) for dead centre.")]
    [SerializeField] private Vector2 baseOffset = Vector2.zero;

    [Header("Scout Walker")]
    [SerializeField] private ScoutWalkerView scoutWalker;

    private readonly List<MapNodeData> nodeData = new List<MapNodeData>();
    private readonly List<MapNodeView> nodeViews = new List<MapNodeView>();
    private MapNodeView selectedNode;

    /// <summary>Fired once a confirmed trip fully resolves (after the walk-back animation).</summary>
    public event Action<GatherDispatchResult> OnTripResolved;

    public MapNodeView SelectedNode => selectedNode;

    /// <summary>Where the scout rests and trips start/end, in MapPanel-centre-relative anchored coordinates.</summary>
    private Vector2 BasePosition => baseOffset;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SetupDefaultNodes();
        SpawnNodeViews();

        if (scoutWalker != null)
        {
            scoutWalker.SnapTo(BasePosition);
        }

        MapNodeView.OnAnyNodeClicked += HandleNodeClicked;
    }

    private void OnDestroy()
    {
        MapNodeView.OnAnyNodeClicked -= HandleNodeClicked;
    }

    [Header("Scatter Layout")]
    [SerializeField] private int nodesPerDifficulty = 2;
    [Tooltip("Keeps nodes away from the screen edges (anchored units). Bottom is larger because the roster, toggles and buttons live there; top-right holds the message text.")]
    [SerializeField] private float marginLeft = 120f;
    [SerializeField] private float marginRight = 120f;
    [SerializeField] private float marginTop = 120f;
    [SerializeField] private float marginBottom = 260f;
    [Tooltip("No node spawns closer than this to the base, so the scout always has a clear walk.")]
    [SerializeField] private float baseClearRadius = 180f;
    [Tooltip("How many random spots are tried per node; the one furthest from everything else wins. Higher = more evenly spread.")]
    [SerializeField] private int candidatesPerNode = 60;

    /// <summary>
    /// Base in the middle, dungeons spread all around it. Each node is placed
    /// by "best candidate" sampling: try several random spots and keep the one
    /// furthest from the base and from every node already placed, so nodes
    /// end up evenly spread instead of clumping. Difficulty is shown by color,
    /// not position. Nodes are created round-robin (Easy, Normal, Hard, Easy...)
    /// so each difficulty is spread out too.
    /// </summary>
    private void SetupDefaultNodes()
    {
        GetScatterBounds(out Vector2 min, out Vector2 max);

        var difficulties = new[] { GatherZone.Easy, GatherZone.Normal, GatherZone.Hard };
        for (int i = 0; i < nodesPerDifficulty; i++)
        {
            foreach (var difficulty in difficulties)
            {
                nodeData.Add(new MapNodeData
                {
                    nodeId = $"{difficulty}_{i}",
                    zone = difficulty,
                    anchoredPosition = FindScatterPosition(min, max)
                });
            }
        }
    }

    /// <summary>Bounds are read from MapPanel's real size, so they follow the canvas/resolution automatically.</summary>
    private void GetScatterBounds(out Vector2 min, out Vector2 max)
    {
        Canvas.ForceUpdateCanvases();

        float halfW = mapPanel.rect.width * 0.5f;
        float halfH = mapPanel.rect.height * 0.5f;

        min = new Vector2(-halfW + marginLeft, -halfH + marginBottom);
        max = new Vector2(halfW - marginRight, halfH - marginTop);

        // Panel too small for the margins (or not laid out yet): fall back to the inner 60% so we never get an inverted box.
        if (max.x <= min.x || max.y <= min.y)
        {
            min = new Vector2(-halfW * 0.6f, -halfH * 0.6f);
            max = new Vector2(halfW * 0.6f, halfH * 0.6f);
        }
    }

    private Vector2 FindScatterPosition(Vector2 min, Vector2 max)
    {
        Vector2 best = Vector2.zero;
        float bestScore = -1f;

        for (int i = 0; i < candidatesPerNode; i++)
        {
            Vector2 candidate = new Vector2(
                UnityEngine.Random.Range(min.x, max.x),
                UnityEngine.Random.Range(min.y, max.y));

            float distToBase = Vector2.Distance(candidate, BasePosition);
            if (distToBase < baseClearRadius) continue;

            // Score = distance to the nearest neighbour (base or placed node). Biggest wins.
            float score = distToBase;
            foreach (var existing in nodeData)
            {
                score = Mathf.Min(score, Vector2.Distance(candidate, existing.anchoredPosition));
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        if (bestScore < 0f)
        {
            // Every candidate landed inside the base radius (tiny panel) - just drop one somewhere in bounds.
            best = new Vector2(
                UnityEngine.Random.Range(min.x, max.x),
                UnityEngine.Random.Range(min.y, max.y));
        }

        return best;
    }

    private void SpawnNodeViews()
    {
        foreach (var data in nodeData)
        {
            MapNodeView view = Instantiate(nodeViewPrefab, mapPanel);
            view.Initialize(data);
            nodeViews.Add(view);
        }
    }

    private void HandleNodeClicked(MapNodeView clicked)
    {
        if (selectedNode != null)
        {
            selectedNode.SetSelected(false);
        }
        selectedNode = clicked;
        selectedNode.SetSelected(true);
    }

    /// <summary>
    /// Call once the player has picked a node and 1-2 units and hit confirm.
    /// Walks the scout out, resolves the real dispatch on arrival, walks
    /// back, then fires OnTripResolved.
    /// </summary>
    public void ConfirmTrip(List<UnitCharacter> selectedUnits)
    {
        if (selectedNode == null)
        {
            Debug.LogWarning("[GatherMapController] No node selected.");
            return;
        }

        if (GatherDispatchController.Instance == null)
        {
            Debug.LogWarning("[GatherMapController] GatherDispatchController.Instance is missing.");
            return;
        }

        if (!GatherDispatchController.Instance.CanDispatch(selectedNode.Data.zone, selectedUnits, out string reason))
        {
            Debug.LogWarning($"[GatherMapController] Cannot dispatch: {reason}");
            return;
        }

        GatherZone zone = selectedNode.Data.zone;
        Vector2 targetPos = selectedNode.Data.anchoredPosition;

        scoutWalker.MoveTo(targetPos, () =>
        {
            GatherDispatchResult result = GatherDispatchController.Instance.Dispatch(zone, selectedUnits);

            scoutWalker.MoveTo(BasePosition, () =>
            {
                OnTripResolved?.Invoke(result);
            });
        });
    }
}
