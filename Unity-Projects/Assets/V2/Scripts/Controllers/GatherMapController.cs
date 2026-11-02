using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual map layer on top of the existing gather system. One fixed card per
/// difficulty tier, laid out left-to-right (Easy, Normal, Hard) - the old
/// random scatter of six caves is gone.
///
/// Nodes are purely a selection UI: clicking any node just picks its zone, and
/// ConfirmTrip still goes through GatherDispatchController for all the actual
/// logic (AP, roll, loot).
/// </summary>
public class GatherMapController : MonoBehaviour
{
    public static GatherMapController Instance { get; private set; }

    [Header("Map Setup")]
    [SerializeField] private RectTransform mapPanel;
    [SerializeField] private MapNodeView nodeViewPrefab;
    [Tooltip("Drives the live success-value readout. Found automatically when left empty.")]
    [SerializeField] private UnitRosterView rosterView;

    [Header("Layout")]
    [Tooltip("Space between the three dungeon cards.")]
    [SerializeField] private float columnGap = 24f;
    [Tooltip("Keeps the row clear of the HUD: the log and hand sit at the bottom, the skill column on the right.")]
    [SerializeField] private float marginLeft = 32f;
    [SerializeField] private float marginRight = 380f;
    [Tooltip("Top of the usable band, measured down from the top of the panel.")]
    [SerializeField] private float marginTop = 110f;
    [Tooltip("Bottom of the usable band, measured up from the bottom of the panel. The log and hand row live here; " +
             "the dungeon cards keep a private dead band at their own bottom (MapNodeView.bottomSafeBand) so a " +
             "lifted card can only ever cover backdrop, never a label.")]
    [SerializeField] private float marginBottom = 330f;
    [Tooltip("Vertical nudge for the whole row, relative to the usable band.")]
    [SerializeField] private float verticalOffset = 0f;
    [SerializeField] private float maxCardWidth = 480f;
    [SerializeField] private float maxCardHeight = 700f;
    [SerializeField] private float minCardHeight = 380f;

    private readonly List<MapNodeData> nodeData = new List<MapNodeData>();
    private readonly List<MapNodeView> nodeViews = new List<MapNodeView>();
    private MapNodeView selectedNode;

    /// <summary>Fired as soon as a confirmed trip resolves - the same frame the button was pressed.</summary>
    public event Action<GatherDispatchResult> OnTripResolved;

    /// <summary>Fired whenever the selected cave changes (null when nothing is selected).</summary>
    public event Action<MapNodeView> OnSelectionChanged;

    public MapNodeView SelectedNode => selectedNode;
    public IReadOnlyList<MapNodeView> Nodes => nodeViews;

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
        if (rosterView == null) rosterView = FindFirstObjectByType<UnitRosterView>();
        if (rosterView != null) rosterView.OnSelectionChanged += RefreshReadouts;

        SetupDefaultNodes();
        SpawnNodeViews();
        RefreshReadouts();

        MapNodeView.OnAnyNodeClicked += HandleNodeClicked;
    }

    private void OnDestroy()
    {
        MapNodeView.OnAnyNodeClicked -= HandleNodeClicked;
        if (rosterView != null) rosterView.OnSelectionChanged -= RefreshReadouts;
    }

    /// <summary>
    /// One card per tier in reading order, sized to fill the free area between
    /// the HUD panels: full width of the space left of the skill column and full
    /// height between the status bar and the log/hand row.
    /// </summary>
    private void SpawnNodeViews()
    {
        Canvas.ForceUpdateCanvases();

        int count = nodeData.Count;

        float panelWidth = mapPanel != null ? mapPanel.rect.width : 1920f;
        float panelHeight = mapPanel != null ? mapPanel.rect.height : 1080f;

        float usableWidth = panelWidth - marginLeft - marginRight;
        float usableHeight = panelHeight - marginTop - marginBottom;

        float cardWidth = Mathf.Clamp((usableWidth - columnGap * (count - 1)) / Mathf.Max(1, count), 200f, maxCardWidth);
        float cardHeight = Mathf.Clamp(usableHeight, minCardHeight, maxCardHeight);

        float totalWidth = count * cardWidth + (count - 1) * columnGap;

        // Centre the row inside the usable band, not inside the whole panel, so
        // the right-hand card can never slide under the Core Item column.
        float bandLeft = -panelWidth * 0.5f + marginLeft;
        float bandRight = panelWidth * 0.5f - marginRight;
        float startX = bandLeft + cardWidth * 0.5f + Mathf.Max(0f, ((bandRight - bandLeft) - totalWidth) * 0.5f);

        // Centre the row inside the usable band rather than in the whole panel.
        // marginTop is small on purpose so the cards can use the empty space
        // between the status bar and the log - the band reaches well above the
        // middle of the screen.
        float bandTop = panelHeight * 0.5f - marginTop;
        float bandBottom = -panelHeight * 0.5f + marginBottom;
        float centreY = (bandTop + bandBottom) * 0.5f + verticalOffset;

        for (int i = 0; i < count; i++)
        {
            var data = nodeData[i];
            data.anchoredPosition = new Vector2(startX + i * (cardWidth + columnGap), centreY);

            MapNodeView view = nodeViewPrefab != null
                ? Instantiate(nodeViewPrefab, mapPanel)
                : MapNodeView.CreateOn(mapPanel);

            view.SetCardSize(new Vector2(cardWidth, cardHeight));
            view.Initialize(data);
            nodeViews.Add(view);
        }
    }
 

    /// <summary>
    /// One slot per tier in reading order. The tier is no longer conveyed by
    /// random placement - the three cards always read Easy, Normal, Hard.
    /// </summary>
    private void SetupDefaultNodes()
    {
        nodeData.Clear();

        var difficulties = new[] { GatherZone.Easy, GatherZone.Normal, GatherZone.Hard };
        for (int i = 0; i < difficulties.Length; i++)
        {
            nodeData.Add(new MapNodeData
            {
                nodeId = difficulties[i].ToString(),
                zone = difficulties[i],
                anchoredPosition = Vector2.zero
            });
        }
    }

    private void HandleNodeClicked(MapNodeView clicked)
    {
        if (clicked == null) return;

        // Clicking the already-selected node deselects it, so the cave
        // behaves like a toggle instead of always keeping one selected.
        if (selectedNode == clicked)
        {
            ClearSelection();
            return;
        }

        // Picking a cave replaces whatever skill was queued, so the two plans
        // can never both be pending.
        SkillFlowController.Instance?.CancelActiveSkill();

        if (selectedNode != null) selectedNode.SetSelected(false);

        selectedNode = clicked;
        selectedNode.SetSelected(true);

        RefreshReadouts();
        OnSelectionChanged?.Invoke(selectedNode);
    }

    /// <summary>Clears the current cave selection (used after a trip resolves, on day start, or when a skill is armed).</summary>
    public void ClearSelection()
    {
        if (selectedNode != null) selectedNode.SetSelected(false);
        selectedNode = null;

        RefreshReadouts();
        OnSelectionChanged?.Invoke(null);
    }

    /// <summary>
    /// Pushes each card's live numbers. Base success value by default; once a
    /// cave is picked and units are chosen, the selected card shows the real
    /// team success value so the player sees the odds before confirming.
    /// </summary>
    public void RefreshReadouts()
    {
        List<UnitCharacter> selectedUnits = rosterView != null ? rosterView.GetSelectedUnits() : null;
        bool hasTeam = selectedUnits != null && selectedUnits.Count > 0;

        foreach (var view in nodeViews)
        {
            GatherZoneConfig config = GatherZoneDatabase.Instance != null
                ? GatherZoneDatabase.Instance.GetConfig(view.Data.zone)
                : null;
            if (config == null) continue;

            int chance = config.baseSuccessValue;
            if (hasTeam && view == selectedNode)
            {
                chance = GatherResolver.ComputeSuccessValue(config, selectedUnits);
            }

            int pity = GatherDispatchController.Instance != null
                ? GatherDispatchController.Instance.PityCounter(view.Data.zone)
                : 0;

            view.RefreshReadout(chance, pity, config.coreItemPityTrips, config);
        }
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

        // The trip is over, so the cave drops out of selection - otherwise the
        // card stayed highlighted and the next plan looked half-entered.
        ClearSelection();

        OnTripResolved?.Invoke(result);

        RefreshReadouts();
    }
}