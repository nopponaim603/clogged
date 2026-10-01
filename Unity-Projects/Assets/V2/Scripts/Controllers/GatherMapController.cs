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
    [SerializeField] private Vector2 basePosition = new Vector2(0f, -350f);

    [Header("Scout Walker")]
    [SerializeField] private ScoutWalkerView scoutWalker;

    private readonly List<MapNodeData> nodeData = new List<MapNodeData>();
    private readonly List<MapNodeView> nodeViews = new List<MapNodeView>();
    private MapNodeView selectedNode;

    /// <summary>Fired once a confirmed trip fully resolves (after the walk-back animation).</summary>
    public event Action<GatherDispatchResult> OnTripResolved;

    public MapNodeView SelectedNode => selectedNode;

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
            scoutWalker.SnapTo(basePosition);
        }

        MapNodeView.OnAnyNodeClicked += HandleNodeClicked;
    }

    private void OnDestroy()
    {
        MapNodeView.OnAnyNodeClicked -= HandleNodeClicked;
    }

    /// <summary>
    /// Placeholder layout: 2 nodes per zone, arranged in bands by distance
    /// from the base (Near closest, Far furthest). Reposition freely once
    /// real map art exists - nothing else depends on these exact coordinates.
    /// </summary>
    private void SetupDefaultNodes()
    {
        nodeData.Add(new MapNodeData { nodeId = "near_a", zone = GatherZone.Near, anchoredPosition = new Vector2(-150f, -150f) });
        nodeData.Add(new MapNodeData { nodeId = "near_b", zone = GatherZone.Near, anchoredPosition = new Vector2(150f, -150f) });
        nodeData.Add(new MapNodeData { nodeId = "mid_a", zone = GatherZone.Mid, anchoredPosition = new Vector2(-200f, 50f) });
        nodeData.Add(new MapNodeData { nodeId = "mid_b", zone = GatherZone.Mid, anchoredPosition = new Vector2(200f, 50f) });
        nodeData.Add(new MapNodeData { nodeId = "far_a", zone = GatherZone.Far, anchoredPosition = new Vector2(-100f, 280f) });
        nodeData.Add(new MapNodeData { nodeId = "far_b", zone = GatherZone.Far, anchoredPosition = new Vector2(150f, 280f) });
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

            scoutWalker.MoveTo(basePosition, () =>
            {
                OnTripResolved?.Invoke(result);
            });
        });
    }
}
