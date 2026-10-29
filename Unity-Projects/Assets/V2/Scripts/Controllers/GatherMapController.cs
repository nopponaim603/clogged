using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual map layer on top of the existing gather system. Nodes are purely
/// a selection UI - clicking any node just picks its zone, and ConfirmTrip
/// still goes through GatherDispatchController for all the actual logic
/// (AP, roll, loot). This class only handles layout and selection; the trip
/// resolves inside <see cref="ConfirmTrip"/> with no travel animation.
/// </summary>
public class GatherMapController : MonoBehaviour
{
    public static GatherMapController Instance { get; private set; }

    [Header("Map Setup")]
    [SerializeField] private RectTransform mapPanel;
    [SerializeField] private MapNodeView nodeViewPrefab;
    [Tooltip("Node layout is measured from the centre of MapPanel plus this offset. Leave at (0,0) for dead centre.")]
    [SerializeField] private Vector2 baseOffset = Vector2.zero;

    private readonly List<MapNodeData> nodeData = new List<MapNodeData>();
    private readonly List<MapNodeView> nodeViews = new List<MapNodeView>();
    private MapNodeView selectedNode;

    /// <summary>Fired as soon as a confirmed trip resolves - the same frame the button was pressed.</summary>
    public event Action<GatherDispatchResult> OnTripResolved;

    /// <summary>Fired whenever the selected cave changes (null when nothing is selected).</summary>
    public event Action<MapNodeView> OnSelectionChanged;

    public MapNodeView SelectedNode => selectedNode;

    /// <summary>Node layout origin, in MapPanel-centre-relative anchored coordinates.</summary>
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

        // Spots too close to the base are skipped without using up one of the candidatesPerNode tries.
        int accepted = 0;
        int maxAttempts = candidatesPerNode * 20;

        for (int attempt = 0; attempt < maxAttempts && accepted < candidatesPerNode; attempt++)
        {
            Vector2 candidate = new Vector2(
                UnityEngine.Random.Range(min.x, max.x),
                UnityEngine.Random.Range(min.y, max.y));

            float distToBase = Vector2.Distance(candidate, BasePosition);
            if (distToBase < baseClearRadius) continue;

            accepted++;

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
            // No usable spot found: put the node just outside the base clear radius.
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            if (direction == Vector2.zero) direction = Vector2.right;
            best = BasePosition + direction * (baseClearRadius + 20f);
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
        if (clicked == null) return;

        // Clicking the already-selected node deselects it, so the cave
        // behaves like a toggle instead of always keeping one selected.
        if (selectedNode == clicked)
        {
            clicked.SetSelected(false);
            selectedNode = null;
            OnSelectionChanged?.Invoke(null);
            return;
        }

        if (selectedNode != null) selectedNode.SetSelected(false);

        selectedNode = clicked;
        selectedNode.SetSelected(true);
        OnSelectionChanged?.Invoke(selectedNode);
    }

    /// <summary>Clears the current cave selection (used after a trip resolves or on day start).</summary>
    public void ClearSelection()
    {
        if (selectedNode != null) selectedNode.SetSelected(false);
        selectedNode = null;
        OnSelectionChanged?.Invoke(null);
    }

    /// <summary>
    /// Call once the player has picked a node and 1-2 units and hit confirm.
    ///
    /// The trip resolves immediately: AP is spent, the roll happens, and
    /// <see cref="OnTripResolved"/> fires inside this call, so the player can
    /// keep playing (or end the day) in the same frame.
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
        GatherDispatchResult result = GatherDispatchController.Instance.Dispatch(zone, selectedUnits);
        OnTripResolved?.Invoke(result);
    }
}
