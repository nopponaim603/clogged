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
    [SerializeField] private float marginRight = 300f;
    [Tooltip("Width of the red-fog gauge, matched to the cards' height so the two read as one row.")]
    [SerializeField] private float fogGaugeWidth = 66f;
    [Tooltip("Gap between the fog gauge and the first dungeon card.")]
    [SerializeField] private float fogGaugeGap = 26f;
    [Tooltip("Top of the usable band, measured down from the top of the panel.")]
    [SerializeField] private float marginTop = 110f;
    [Tooltip("Bottom of the usable band, measured up from the bottom of the panel. The log and hand row live here; " +
             "the dungeon cards keep a private dead band at their own bottom (MapNodeView.bottomSafeBand) so a " +
             "lifted card can only ever cover backdrop, never a label.")]
    [SerializeField] private float marginBottom = 330f;
    [Tooltip("Vertical nudge for the whole row, relative to the usable band.")]
    [SerializeField] private float verticalOffset = 0f;
    [Tooltip("Upper bound on card width. Narrower than the free space so the row can shift right past the fog gauge.")]
    [SerializeField] private float maxCardWidth = 450f;
    [SerializeField] private float maxCardHeight = 700f;
    [SerializeField] private float minCardHeight = 380f;

    [Header("Card Art")]
    [Tooltip("The cave arch every card wears. One picture, three badges.")]
    [SerializeField] private Sprite caveArt;
    [Tooltip("Badge stamped in the cave mouth: the shallows - a hut.")]
    [SerializeField] private Sprite easyBadge;
    [Tooltip("Badge for the middle cave - a tower.")]
    [SerializeField] private Sprite normalBadge;
    [Tooltip("Badge for the deepest cave - a skull.")]
    [SerializeField] private Sprite hardBadge;

    private readonly List<MapNodeData> nodeData = new List<MapNodeData>();
    private readonly List<MapNodeView> nodeViews = new List<MapNodeView>();
    private MapNodeView selectedNode;
    private FogGaugeView fogGauge;
    private bool refreshingInteractable;
    private int lastPickCount = -1;
    private bool lastHadSword;

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
        lastPickCount = rosterView != null ? rosterView.SelectedCount : 0;
        lastHadSword = AnyPickedUnitHasSword();
    }
    private void OnDestroy()
    {
        MapNodeView.OnAnyNodeClicked -= HandleNodeClicked;
        if (rosterView != null) rosterView.OnSelectionChanged -= RefreshReadouts;
    }

    /// <summary>
    /// Polls the one thing the events cannot report: a unit's sword can be spent
    /// by a trip, a load or a debug call without the roster announcing a selection
    /// change. A signature check is cheap and keeps the row honest.
    /// </summary>
    private void Update()
    {
        int picked = rosterView != null ? rosterView.SelectedCount : 0;
        bool sword = AnyPickedUnitHasSword();

        if (picked == lastPickCount && sword == lastHadSword) return;

        lastPickCount = picked;
        lastHadSword = sword;
        RefreshInteractable();
    }

    /// <summary>
    /// One card per tier in reading order, sized to fill the free area between
    /// the HUD panels: the width left of the skill column minus the fog tube,
    /// and the full height between the status bar and the log/hand row.
    /// </summary>
    private void SpawnNodeViews()
    {
        Canvas.ForceUpdateCanvases();

        int count = nodeData.Count;

        float panelWidth = mapPanel != null ? mapPanel.rect.width : 1920f;
        float panelHeight = mapPanel != null ? mapPanel.rect.height : 1080f;

        // The tube takes the outermost slice of the left margin; the cards get
        // what is left of it, so adding the gauge does not squeeze the HUD.
        float bandLeft = -panelWidth * 0.5f + marginLeft;
        float gaugeCenterX = bandLeft + fogGaugeWidth * 0.5f;
        float cardsLeft = bandLeft + fogGaugeWidth + fogGaugeGap;
        float bandRight = panelWidth * 0.5f - marginRight;
        float usableWidth = Mathf.Max(0f, bandRight - cardsLeft);

        float usableHeight = panelHeight - marginTop - marginBottom;

        float cardWidth = Mathf.Clamp((usableWidth - columnGap * (count - 1)) / Mathf.Max(1, count), 200f, maxCardWidth);
        float cardHeight = Mathf.Clamp(usableHeight, minCardHeight, maxCardHeight);

        float totalWidth = count * cardWidth + (count - 1) * columnGap;

        // Centre the row inside the cards' band, not inside the whole panel, so
        // the right-hand card can never slide under the Core Item column.
        float startX = cardsLeft + cardWidth * 0.5f + Mathf.Max(0f, (usableWidth - totalWidth) * 0.5f);

        // Centre the row inside the usable band rather than in the whole panel.
        // marginTop is small on purpose so the cards can use the empty space
        // between the status bar and the log - the band reaches well above the
        // middle of the screen.
        float bandTop = panelHeight * 0.5f - marginTop;
        float bandBottom = -panelHeight * 0.5f + marginBottom;
        float centreY = (bandTop + bandBottom) * 0.5f + verticalOffset;

        BuildFogGauge(new Vector2(gaugeCenterX, centreY), cardHeight);

        for (int i = 0; i < count; i++)
        {
            var data = nodeData[i];
            data.anchoredPosition = new Vector2(startX + i * (cardWidth + columnGap), centreY);

            MapNodeView view = nodeViewPrefab != null
                ? Instantiate(nodeViewPrefab, mapPanel)
                : MapNodeView.CreateOn(mapPanel);

            view.SetArt(caveArt, BadgeFor(data.zone));
            view.SetCardSize(new Vector2(cardWidth, cardHeight));
            view.Initialize(data);
            nodeViews.Add(view);
        }

        RefreshInteractable();
    }

    /// <summary>The tier badge that tells the three otherwise identical arches apart.</summary>
    private Sprite BadgeFor(GatherZone zone)
    {
        switch (zone)
        {
            case GatherZone.Normal: return normalBadge;
            case GatherZone.Hard: return hardBadge;
            default: return easyBadge;
        }
    }

    /// <summary>
    /// Cards only invite a click when the trip could actually be paid for: one
    /// unit picked, and that unit still holding the sword a trip costs. Until
    /// then the whole row is grey, which answers "why can I not press this"
    /// before the player has to ask.
    /// </summary>
    public void RefreshInteractable()
    {
        // A locked row also drops the picked cave, and dropping it tells the
        // roster - which calls back in here. The guard breaks that loop.
        if (refreshingInteractable) return;

        refreshingInteractable = true;
        try
        {
            bool hasSword = AnyPickedUnitHasSword();

            if (!hasSword && selectedNode != null)
            {
                selectedNode.SetSelected(false);
                selectedNode = null;
                OnSelectionChanged?.Invoke(null);
            }

            foreach (var view in nodeViews) view.SetInteractable(hasSword);
        }
        finally
        {
            refreshingInteractable = false;
        }
    }

    /// <summary>True when at least one picked unit still holds the sword a trip costs.</summary>
    private bool AnyPickedUnitHasSword()
    {
        List<UnitCharacter> picked = rosterView != null ? rosterView.GetSelectedUnits() : null;
        if (picked == null) return false;

        foreach (var unit in picked)
        {
            if (unit != null && unit.HasSword) return true;
        }
        return false;
    }
    /// <summary>
    /// The red-fog tube: exactly as tall as the cards, one column to their left.
    /// It is parented above the fog layer itself (the overlay's own root), so the
    /// reading is never seen through the mist it is measuring.
    /// </summary>
    private void BuildFogGauge(Vector2 position, float height)
    {
        Transform parent = RedFogOverlay.Instance != null ? RedFogOverlay.Instance.OverlayRoot : mapPanel;
        if (parent == null) return;

        if (fogGauge == null) fogGauge = FogGaugeView.Create(parent);
        fogGauge.SetGeometry(position, new Vector2(fogGaugeWidth, height));
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

            // The live team value already has this cave's share of the Risk
            // subtracted, so the card shows the number the roll will actually
            // use - and whether that reaches the Jackpot line.
            int chance = hasTeam && view == selectedNode
                ? GatherResolver.ComputeSuccessValue(config, selectedUnits)
                : config.baseSuccessValue - RiskPenalty(view.Data.zone);

            view.RefreshReadout(chance, config);
        }

        // Grey-out rides along with every readout refresh: picking a unit, spending
        // the last sword and starting a day all land here.
        RefreshInteractable();
    }

    /// <summary>
    /// Risk each cave pays: the run's Risk minus that cave's Handicap, floored at
    /// 0 - so a shallow cave stops being penalised before a deep one does.
    /// </summary>
    private static int RiskPenalty(GatherZone zone)
    {
        return RunResources.Instance != null
            ? GatherResolver.RiskPenalty(zone, RunResources.Instance.Risk)
            : 0;
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