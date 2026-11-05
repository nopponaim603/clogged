using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One dungeon card on the map: tier title, cave art, and the three readouts
/// the sprint sketch asked for --- the odds, what drops here, and which Core
/// Item piece a Hard Success hands over (plus the JACKPOT marker).
///
/// The card is built entirely in code so the prefab only needs the component.
/// A downward marker appears over the selected card, which is how "which one am
/// I picking" is shown (the three columns are in fixed Easy/Normal/Hard order).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MapNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Legacy refs (optional)")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image selectedHighlight;

    [Header("Card")]
    [SerializeField] private Vector2 cardSize = new Vector2(240f, 380f);
    [SerializeField] private Sprite caveSprite;

    /// <summary>Cave art, so the help page can show the same picture as the map card.</summary>
    public Sprite CaveSprite => caveArt != null ? caveArt : caveSprite != null ? caveSprite : PlaceholderIcons.Cave;

    /// <summary>The tier badge stamped on the cave mouth (house / tower / skull).</summary>
    public Sprite BadgeSprite => badgeArt;

    [Header("Item icons (optional --- placeholders are used when empty)")]
    [SerializeField] private Sprite cookingMaterialSprite;
    [SerializeField] private Sprite medicineSprite;
    [SerializeField] private Sprite materialsSprite;

    [Header("Colours")]
    [SerializeField] private Color panelColor = new Color(0.09f, 0.10f, 0.15f, 0.96f);
    [SerializeField] private Color labelColor = new Color(0.78f, 0.82f, 0.92f, 1f);
    [Tooltip("Tint of the JACKPOT marker - the same accent the options page uses.")]
    [SerializeField] private Color jackpotColor = new Color(1f, 0.88f, 0.55f, 1f);

    [Header("Hover")]
    [Tooltip("How far the card panel lifts toward white while the pointer is over it.")]
    [Range(0f, 1f)]
    [SerializeField] private float hoverLift = 0.10f;
    [SerializeField] private float hoverSpeed = 7f;

    private static readonly Color EasyColor = new Color(0.45f, 0.75f, 0.45f);
    private static readonly Color NormalColor = new Color(0.85f, 0.75f, 0.30f);
    private static readonly Color HardColor = new Color(0.80f, 0.30f, 0.30f);

    // Success-value ramp: red at 0%, through orange and yellow, green at 100%.
    private static readonly Color ChanceLow = new Color(0.91f, 0.30f, 0.30f);
    private static readonly Color ChanceMidLow = new Color(0.95f, 0.58f, 0.24f);
    private static readonly Color ChanceMid = new Color(0.93f, 0.84f, 0.30f);
    private static readonly Color ChanceHigh = new Color(0.42f, 0.85f, 0.45f);

    // Lifted fill behind the selected card's success value. Brighter than the
    // card panel on purpose - a panel-coloured fill was invisible against it.
    private static readonly Color SelectedBackdrop = new Color(0.21f, 0.25f, 0.35f, 1f);

    // The "you cannot pick this yet" palette: every tier colour collapses to the
    // same grey, so a locked row reads as one blocked thing rather than three.
    private static readonly Color LockedPanel = new Color(0.13f, 0.13f, 0.16f, 0.94f);
    private static readonly Color LockedInk = new Color(0.44f, 0.46f, 0.50f, 1f);
    private static readonly Color LockedFrame = new Color(0.44f, 0.46f, 0.50f, 0.30f);
    private static readonly Color LockedArt = new Color(0.58f, 0.60f, 0.64f, 0.45f);

    private const float ChanceFontSize = 26f;
    private const float ChanceSelectedFontSize = 34f;
    private const float PopTargetScale = 1.18f;
    private const float PopSpeed = 6.5f;

    private TMP_Text titleText;
    private TMP_Text chanceText;
    private TMP_Text coreText;
    private Image chanceHighlight;
    private Image selector;
    private Image caveImage;

    /// <summary>Tier badge drawn on the cave mouth, so the three cards differ at a glance.</summary>
    private Image badgeImage;
    private Sprite caveArt;
    private Sprite badgeArt;
    private RectTransform dropRow;

    /// <summary>The piece this cave hands over, drawn in the drop row beside the loot it also gives.</summary>
    private Image coreDropIcon;

    /// <summary>HUD that owns the Core Item art, so the card repeats the same box and key the run is hunting.</summary>
    private StatusBarView hud;
    private readonly List<Image> frame = new List<Image>();
    private readonly List<Image> dropIcons = new List<Image>();
    private readonly List<TMP_Text> dropLabels = new List<TMP_Text>();

    /// <summary>Art the loot row is drawing, in slot order, for tests and tooling.</summary>
    public IReadOnlyList<Image> DropIcons => dropIcons;

    private float popScale = 1f;
    private float popAlpha;
    private Image panelImage;
    private bool hovered;
    private float hoverAmount;

    private GatherZoneConfig shownConfig;
    private int shownChance;
    private bool shownJackpot;

    /// <summary>True while the card is greyed out and cannot be picked.</summary>
    private bool locked;

    public MapNodeData Data { get; private set; }

    /// <summary>The zone config the card is currently showing, for tooling and tests.</summary>
    public GatherZoneConfig Config => shownConfig;
    public bool IsSelected { get; private set; }

    /// <summary>True when the card is currently greyed out (no unit, or nobody holding a sword).</summary>
    public bool IsLocked => locked;

    /// <summary>The success-value label, so tests can read the colour it currently carries.</summary>
    public TMP_Text ChanceLabel => chanceText;

    /// <summary>Current pop amount of the success-value label (1 = resting, &gt;1 = lifted by selection).</summary>
    public float PopScale => popScale;

    /// <summary>Red → orange → yellow → green ramp used for the success-value readout.</summary>
    public static Color ColorForChance(int chance)
    {
        float t = Mathf.Clamp(chance, 0, 100);

        if (t <= 40f) return Color.Lerp(ChanceLow, ChanceMidLow, t / 40f);
        if (t <= 70f) return Color.Lerp(ChanceMidLow, ChanceMid, (t - 40f) / 30f);
        return Color.Lerp(ChanceMid, ChanceHigh, (t - 70f) / 30f);
    }

    /// <summary>Fired whenever any node is clicked, passing the node that was clicked. GatherMapController subscribes to this.</summary>
    public static event Action<MapNodeView> OnAnyNodeClicked;

    /// <summary>Builds a view on an empty GameObject, for scenes without the prefab wired.</summary>
    public static MapNodeView CreateOn(Transform parent)
    {
        var go = new GameObject("DungeonCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MapNodeView));
        go.transform.SetParent(parent, false);
        return go.GetComponent<MapNodeView>();
    }

    private void Awake()
    {
        if (button != null) button.onClick.AddListener(HandleClick);

        // Picking a cave is a real choice in the run.
        UiClickSound.Attach(button, UiClickKind.Select);
    }

    /// <summary>Card size is driven by the map controller so the three cards fill the free area.</summary>
    public void SetCardSize(Vector2 size)
    {
        if (size.x > 0f && size.y > 0f) cardSize = size;
    }

    /// <summary>
    /// Supplies the shared cave art plus this tier's badge. Called before
    /// <see cref="Initialize"/> so <see cref="BuildCard"/> paints them straight
    /// away; the badge is what makes the three cards tell themselves apart now
    /// that they all wear the same arch.
    /// </summary>
    public void SetArt(Sprite cave, Sprite badge)
    {
        caveArt = cave;
        badgeArt = badge;
    }

    /// <summary>
    /// Greys the card out and stops it taking clicks. Used while the player has
    /// nobody picked, and while the picked crew has no sword left - the trip
    /// could not be paid for, so the card should not invite the click.
    /// </summary>
    public void SetInteractable(bool value)
    {
        locked = !value;

        if (button != null) button.interactable = value;

        if (locked)
        {
            hovered = false;
            hoverAmount = 0f;
        }

        // Every painter reads `locked`, so repainting from the live readouts is
        // enough to swap the whole card between the tier palette and the grey one.
        RefreshReadout(shownChance, shownConfig);
        ApplySelectionLook();
        ApplyHover();
    }

    public void Initialize(MapNodeData data)
    {
        Data = data;

        var rect = (RectTransform)transform;
        // The old prefab was stretched to the whole map; the card owns its size now.
        rect.localScale = Vector3.one;
        rect.sizeDelta = cardSize;
        rect.anchoredPosition = data.anchoredPosition;

        BuildCard();

        // The root doubles as the click surface.
        var hit = GetComponent<Image>();
        if (hit == null) hit = gameObject.AddComponent<Image>();
        hit.color = new Color(0f, 0f, 0f, 0f);
        hit.raycastTarget = true;

        if (button == null)
        {
            button = GetComponent<Button>();
            if (button == null) button = gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(HandleClick);

            // Cards built at runtime get the same select clip as the prefab ones.
            UiClickSound.Attach(button, UiClickKind.Select);
        }

        // The prefab's iconImage IS the root image (the click surface), so it must
        // stay enabled - only its colour is cleared. The old highlight overlay is
        // replaced by the frame + selector, so that one is switched off.
        if (selectedHighlight != null) selectedHighlight.enabled = false;

        SetSelected(false);
    }

    /// <summary>
    /// Repaints the three readouts. <paramref name="chance"/> is already resolved
    /// by the controller (base value, or the live team success value once a cave
    /// and units are picked), and it drives the label colour on a red→green ramp.
    ///
    /// The second line is the Core Item this cave hands out on a Hard Success,
    /// plus a JACKPOT marker once the success value is high enough to trigger it.
    /// </summary>
    public void RefreshReadout(int chance, GatherZoneConfig config)
    {
        shownChance = chance;
        shownConfig = config;
        shownJackpot = chance >= GatherDispatchController.JackpotSuccessValue;

        if (chanceText != null)
        {
            int ap = config != null ? config.apCost : 0;
            chanceText.text = $"{Mathf.Max(0, chance)}  --  {ap} AP";
            chanceText.color = locked ? LockedInk : ColorForChance(chance);
        }

        if (coreText != null)
        {
            string label = CoreDropLabel(config != null ? config.zone : GatherZone.Easy);
            bool visible = !string.IsNullOrEmpty(label);
            coreText.gameObject.SetActive(visible);

            if (visible)
            {
                coreText.text = shownJackpot ? $"{label}  ·  JACKPOT" : label;
                coreText.color = locked ? LockedInk : shownJackpot ? jackpotColor : labelColor;
            }
        }

        BuildDrops(config);
    }

    /// <summary>What a cleared cave's Hard Success is guaranteed to hand over.</summary>
    private static string CoreDropLabel(GatherZone zone)
    {
        switch (zone)
        {
            case GatherZone.Normal: return "Hard Success: KEY";
            case GatherZone.Hard: return "Hard Success: BOX";
            default: return "";
        }
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;

        if (selector != null) selector.gameObject.SetActive(selected && !locked);

        // The success value is the number the player is actually deciding on, so
        // the selected card pops it: bigger type plus a fill behind it. Both are
        // animated in Update so the change reads as a movement, not a jump.
        if (chanceText != null) chanceText.fontSize = selected ? ChanceSelectedFontSize : ChanceFontSize;

        var rect = (RectTransform)transform;
        rect.sizeDelta = selected ? cardSize + new Vector2(8f, 8f) : cardSize;

        ApplySelectionLook();
    }

    /// <summary>
    /// Paints everything that depends on "picked", "hovered" or "locked". Kept in
    /// one place because all three states land on the same handful of graphics,
    /// and a stale colour here is the easiest bug to ship.
    /// </summary>
    private void ApplySelectionLook()
    {
        float thickness = IsSelected ? 5f : 3f;
        for (int i = 0; i < frame.Count; i++)
        {
            var fr = (RectTransform)frame[i].transform;
            fr.sizeDelta = i < 2 ? new Vector2(0f, thickness) : new Vector2(thickness, 0f);
        }

        Color color = locked
            ? LockedFrame
            : ColorForZone(Data != null ? Data.zone : GatherZone.Easy);
        if (!locked) color.a = IsSelected ? 1f : 0.45f;
        for (int i = 0; i < frame.Count; i++) frame[i].color = color;

        if (titleText != null)
        {
            titleText.color = locked ? LockedInk : ColorForZone(Data != null ? Data.zone : GatherZone.Easy);
        }

        if (caveImage != null) caveImage.color = ArtTint();
        if (badgeImage != null) badgeImage.color = BadgeTint();
    }

    /// <summary>Tint of the cave art: its own colours, or the grey "locked" wash.</summary>
    private Color ArtTint() => locked ? LockedArt : new Color(1f, 1f, 1f, 0.9f);

    /// <summary>Tint of the tier badge: the tier colour, so the three cards differ.</summary>
    private Color BadgeTint()
    {
        if (locked) return LockedArt;

        Color tint = ColorForZone(Data != null ? Data.zone : GatherZone.Easy);
        tint.a = 0.95f;
        return tint;
    }

    /// <summary>Eases the selected-labelled pop. Idle when nothing is changing, so it costs nothing at rest.</summary>
    private void Update()
    {
        AnimateHover();

        float targetScale = IsSelected ? PopTargetScale : 1f;
        float targetAlpha = IsSelected ? 1f : 0f;

        if (Mathf.Approximately(popScale, targetScale) && Mathf.Approximately(popAlpha, targetAlpha)) return;

        float step = PopSpeed * Time.unscaledDeltaTime;
        popScale = Mathf.MoveTowards(popScale, targetScale, step);
        popAlpha = Mathf.MoveTowards(popAlpha, targetAlpha, step * 1.6f);

        ApplyPop();
    }

    /// <summary>
    /// Lightens the card panel while the pointer is over it. The three cards sit
    /// on a dark map, so the lift is the whole answer: no extra outline, which
    /// would fight the Easy/Normal/Hard frame colour.
    /// </summary>
    private void AnimateHover()
    {
        float wanted = hovered ? 1f : 0f;
        if (Mathf.Approximately(hoverAmount, wanted)) return;

        hoverAmount = Mathf.MoveTowards(hoverAmount, wanted, hoverSpeed * Time.unscaledDeltaTime);
        ApplyHover();
    }

    private void ApplyHover()
    {
        if (panelImage == null) return;

        if (locked)
        {
            panelImage.color = LockedPanel;
            return;
        }

        Color lifted = Color.Lerp(panelColor, Color.white, hoverLift);
        lifted.a = panelColor.a;
        panelImage.color = Color.Lerp(panelColor, lifted, hoverAmount);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (locked) return;

        hovered = true;
        AudioDirector.Instance?.PlayHover();
    }

    public void OnPointerExit(PointerEventData eventData) => hovered = false;

    private void ApplyPop()
    {
        var scale = new Vector3(popScale, popScale, 1f);

        if (chanceText != null) chanceText.rectTransform.localScale = scale;
        if (chanceHighlight != null)
        {
            chanceHighlight.rectTransform.localScale = scale;

            Color c = SelectedBackdrop;
            c.a = 0.95f * popAlpha;
            chanceHighlight.color = c;
        }
    }

    /// <summary>
    /// Jumps the pop to its target without waiting for Update. The test suite
    /// runs every check inside a single frame, where the animation would
    /// otherwise still be at its starting value.
    /// </summary>
    public void SnapPop()
    {
        popScale = IsSelected ? PopTargetScale : 1f;
        popAlpha = IsSelected ? 1f : 0f;
        ApplyPop();
    }

    // ------------------------------------------------------------- painting

    private void BuildCard()
    {
        var root = (RectTransform)transform;

        Image background = NewImage("Panel", root);
        background.sprite = PlaceholderIcons.RoundedSquare;
        background.type = Image.Type.Sliced;
        background.color = panelColor;
        background.raycastTarget = false;
        Stretch((RectTransform)background.transform);
        panelImage = background;

        BuildFrame(root);

        // Selection marker sits just above the card.
        selector = NewImage("Selector", root);
        selector.sprite = PlaceholderIcons.Selector;
        selector.preserveAspect = true;
        selector.raycastTarget = false;
        selector.color = ColorForZone(Data != null ? Data.zone : GatherZone.Easy);
        var selectorRect = (RectTransform)selector.transform;
        selectorRect.anchorMin = selectorRect.anchorMax = new Vector2(0.5f, 1f);
        selectorRect.pivot = new Vector2(0.5f, 0f);
        selectorRect.sizeDelta = new Vector2(30f, 22f);
        selectorRect.anchoredPosition = new Vector2(0f, 10f);

        titleText = NewText("Title", root, 34f);
        var titleRect = (RectTransform)titleText.transform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(-24f, 46f);
        titleRect.anchoredPosition = new Vector2(0f, -14f);
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = ColorForZone(Data != null ? Data.zone : GatherZone.Easy);
        titleText.text = Data != null ? Data.zone.ToString().ToUpperInvariant() : "";

        caveImage = NewImage("Cave", root);
        caveImage.sprite = CaveSprite;
        caveImage.preserveAspect = true;
        caveImage.raycastTarget = false;
        caveImage.color = ArtTint();

        // The tier badge sits in the cave mouth: same arch on all three cards, so
        // the badge is what says "this one is the killer".
        badgeImage = NewImage("TierBadge", root);
        badgeImage.sprite = badgeArt;
        badgeImage.preserveAspect = true;
        badgeImage.raycastTarget = false;
        badgeImage.enabled = badgeArt != null;
        badgeImage.color = BadgeTint();

        // Fill behind the success value, shown only while this card is selected.
        // Created before the label because a sibling earlier in the list draws
        // first; a child of the text would paint over the number.
        chanceHighlight = NewImage("ChanceHighlight", root);
        chanceHighlight.sprite = PlaceholderIcons.RoundedSquare;
        chanceHighlight.type = Image.Type.Sliced;
        chanceHighlight.raycastTarget = false;
        Color highlight = SelectedBackdrop;
        highlight.a = 0f;
        chanceHighlight.color = highlight;
        var highlightRect = (RectTransform)chanceHighlight.transform;
        highlightRect.pivot = new Vector2(0.5f, 0.5f);

        chanceText = NewText("Chance", root, ChanceFontSize);
        chanceText.color = Color.white;

        BuildDropRow(root);

        coreText = NewText("CoreDrop", root, 22f);
        coreText.color = labelColor;

        HudTooltipTrigger.Attach(titleText, TitleTooltip);
        HudTooltipTrigger.Attach(chanceText, ChanceTooltip);
        HudTooltipTrigger.Attach(coreText, CoreTooltip);

        // Everything is positioned from the final card size, so a bigger card
        // spreads the same elements instead of leaving a big empty middle.
        LayoutCard();
    }

    /// <summary>
    /// Vertical stack, measured from the card's own height so a bigger card
    /// spreads the same elements instead of leaving an empty middle.
    ///
    /// Order is title → core drop → cave art → odds → drops. The core-drop line
    /// used to sit at the very bottom, which is exactly where a lifted character
    /// card lands, so it was the first thing covered. Above the art it always
    /// stays readable.
    /// </summary>
    private void LayoutCard()
    {
        float w = cardSize.x;
        float h = cardSize.y;

        const float titleBand = 46f;
        const float coreBand = 28f;
        const float oddsBand = 40f;
        const float dropBand = 46f;

        // The character hand lifts its cards to y = 401, so every label has to
        // stay above that line: the card backdrop may be hidden, the text may
        // not. The card is pinned to the bottom of the map area, so this is the
        // measured clearance (401 − card bottom 330) plus a small margin.
        // The band below is dead space on purpose - only the backdrop lives
        // there, which is exactly what a lifted card is allowed to cover.
        const float bottomSafeBand = 98f;

        // --- top-down: title, then the core-drop line -----------------------
        float fromTop = 8f;

        var titleRect = (RectTransform)titleText.transform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(-24f, titleBand);
        titleRect.anchoredPosition = new Vector2(0f, -fromTop);
        fromTop += titleBand + 2f;

        var coreRect = (RectTransform)coreText.transform;
        coreRect.anchorMin = new Vector2(0f, 1f);
        coreRect.anchorMax = new Vector2(1f, 1f);
        coreRect.pivot = new Vector2(0.5f, 1f);
        coreRect.sizeDelta = new Vector2(-24f, coreBand);
        coreRect.anchoredPosition = new Vector2(0f, -fromTop);
        // Reserved even on Easy (which drops nothing) so all three cards line up.
        fromTop += coreBand + 6f;

        // --- bottom-up: drops, then odds ------------------------------------
        float fromBottom = bottomSafeBand;

        dropRow.anchorMin = dropRow.anchorMax = new Vector2(0.5f, 0f);
        dropRow.pivot = new Vector2(0.5f, 0f);
        dropRow.sizeDelta = new Vector2(w - 24f, dropBand);
        dropRow.anchoredPosition = new Vector2(0f, fromBottom);
        fromBottom += dropBand + 6f;

        var oddsRect = (RectTransform)chanceText.transform;
        oddsRect.anchorMin = new Vector2(0f, 0f);
        oddsRect.anchorMax = new Vector2(1f, 0f);
        // Centre pivot so the selection pop grows evenly instead of only upwards.
        oddsRect.pivot = new Vector2(0.5f, 0.5f);
        oddsRect.sizeDelta = new Vector2(-24f, oddsBand);
        oddsRect.anchoredPosition = new Vector2(0f, fromBottom + oddsBand * 0.5f);
        fromBottom += oddsBand + 6f;

        // The fill sits on the same rect as the label so the two scale together.
        var highlightRect = (RectTransform)chanceHighlight.transform;
        highlightRect.anchorMin = oddsRect.anchorMin;
        highlightRect.anchorMax = oddsRect.anchorMax;
        highlightRect.pivot = oddsRect.pivot;
        highlightRect.sizeDelta = new Vector2(-90f, oddsBand + 6f);
        highlightRect.anchoredPosition = oddsRect.anchoredPosition;

        // --- the cave art takes whatever is left ---------------------------
        float caveBand = Mathf.Max(80f, h - fromTop - fromBottom);
        float caveSize = Mathf.Min(w - 40f, caveBand);

        var caveRect = (RectTransform)caveImage.transform;
        caveRect.anchorMin = caveRect.anchorMax = new Vector2(0.5f, 1f);
        caveRect.pivot = new Vector2(0.5f, 1f);
        caveRect.sizeDelta = new Vector2(caveSize, caveSize);
        caveRect.anchoredPosition = new Vector2(0f, -(fromTop + (caveBand - caveSize) * 0.5f));

        // Badge: centred in the doorway, which sits low in the arch - a badge on
        // the keystone would read as part of the stonework.
        if (badgeImage != null)
        {
            float badgeSize = Mathf.Clamp(caveSize * 0.26f, 26f, 96f);
            var badgeRect = (RectTransform)badgeImage.transform;
            badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(0.5f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.sizeDelta = new Vector2(badgeSize, badgeSize);
            badgeRect.anchoredPosition = new Vector2(0f, caveRect.anchoredPosition.y - caveSize * 0.64f);
        }

        LayoutDrops();
    }

    private void LayoutDrops()
    {
        if (dropRow == null) return;

        const int slots = 3;
        const float coreSize = 50f;
        const float coreGap = 10f;

        // The Core Item piece owns the right end of the row, so the three loot
        // slots share what is left - otherwise a third drop would run under it.
        float lootWidth = Mathf.Max(60f, dropRow.sizeDelta.x - coreSize - coreGap);
        float slotWidth = lootWidth / slots;
        float iconSize = Mathf.Clamp(slotWidth * 0.5f, 26f, 60f);

        for (int i = 0; i < dropIcons.Count; i++)
        {
            float x = i * slotWidth;
            ((RectTransform)dropIcons[i].transform).sizeDelta = new Vector2(iconSize, iconSize);
            ((RectTransform)dropIcons[i].transform).anchoredPosition = new Vector2(x, 0f);

            var labelRect = (RectTransform)dropLabels[i].transform;
            labelRect.sizeDelta = new Vector2(slotWidth - iconSize - 4f, 24f);
            labelRect.anchoredPosition = new Vector2(x + iconSize + 4f, -4f);
        }

        if (coreDropIcon == null) return;

        var coreRect = (RectTransform)coreDropIcon.transform;
        coreRect.sizeDelta = new Vector2(coreSize, coreSize);
        coreRect.anchoredPosition = new Vector2(0f, -5f);
    }

    private void BuildDropRow(RectTransform root)
    {
        var row = new GameObject("Drops", typeof(RectTransform));
        dropRow = (RectTransform)row.transform;
        dropRow.SetParent(root, false);

        for (int i = 0; i < 3; i++)
        {
            Image icon = NewImage("Drop" + i, dropRow);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var iconRect = (RectTransform)icon.transform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);

            TMP_Text label = NewText("DropLabel" + i, dropRow, 20f);
            label.color = labelColor;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);

            dropIcons.Add(icon);
            dropLabels.Add(label);

            int slot = i;
            HudTooltipTrigger.Attach(icon, () => DropTooltip(slot));
            HudTooltipTrigger.Attach(label, () => DropTooltip(slot));
        }

        // The Core Item piece sits at the right end of the same row: the loot on
        // the left is what the trip pays every time, the piece on the right is
        // the one that wins the run - and its art is the same box/key the HUD is
        // asking for.
        coreDropIcon = NewImage("CorePiece", dropRow);
        coreDropIcon.preserveAspect = true;
        coreDropIcon.raycastTarget = false;

        var coreRect = (RectTransform)coreDropIcon.transform;
        coreRect.anchorMin = coreRect.anchorMax = new Vector2(1f, 1f);
        coreRect.pivot = new Vector2(1f, 1f);

        HudTooltipTrigger.Attach(coreDropIcon, CoreTooltip);
    }

    /// <summary>Unique item types this dungeon can hand out, as icon + amount.</summary>
    private void BuildDrops(GatherZoneConfig config)
    {
        var seen = new List<InventoryItemType>();

        if (config != null)
        {
            CollectType(seen, config.mainLoot);
            if (config.hardSuccessBonusPool != null)
            {
                foreach (var loot in config.hardSuccessBonusPool) CollectType(seen, loot);
            }
        }

        for (int i = 0; i < dropIcons.Count; i++)
        {
            bool visible = i < seen.Count;
            dropIcons[i].gameObject.SetActive(visible);
            dropLabels[i].gameObject.SetActive(visible);
            if (!visible) continue;

            dropIcons[i].sprite = SpriteForItem(seen[i]);
            dropIcons[i].color = locked ? LockedArt : Color.white;
            dropLabels[i].text = AmountFor(config, seen[i]);
            dropLabels[i].color = locked ? LockedInk : labelColor;
        }

        PaintCorePiece(config != null ? config.zone : GatherZone.Easy);
    }

    /// <summary>
    /// Draws the piece this cave guarantees on a Hard Success: the key from
    /// Normal, the box from Hard, nothing from Easy. The art is taken from the
    /// HUD so the card is promising exactly the icon the player is collecting.
    /// </summary>
    private void PaintCorePiece(GatherZone zone)
    {
        if (coreDropIcon == null) return;

        var art = Hud();
        Sprite sprite = null;

        switch (zone)
        {
            case GatherZone.Normal:
                sprite = art != null ? art.CoreKeySprite : null;
                break;
            case GatherZone.Hard:
                sprite = art != null ? art.CoreBoxSprite : null;
                break;
        }

        coreDropIcon.sprite = sprite;
        coreDropIcon.enabled = sprite != null;
        coreDropIcon.color = locked ? LockedArt : Color.white;
    }

    /// <summary>
    /// Hover breakdowns for the card, one per readout instead of one popup for
    /// the whole card: the title explains the cave tier, the success value
    /// explains the roll, the core-drop line explains the Jackpot, and every drop
    /// explains where that item comes from. All of them read the live readout
    /// numbers, so they follow the units the player picks.
    /// </summary>
    private HudTooltipContent TitleTooltip()
    {
        var zone = Data != null ? Data.zone : GatherZone.Easy;
        var config = shownConfig;

        string tier = zone switch
        {
            GatherZone.Normal => "NORMAL",
            GatherZone.Hard => "HARD",
            _ => "EASY"
        };

        int baseValue = config != null ? config.baseSuccessValue : 0;
        int ap = config != null ? config.apCost : 0;

        return new HudTooltipContent($"{tier} CAVE",
            $"BASE SUCCESS VALUE {baseValue}. {ap} AP PER UNIT SENT.\n" +
            $"{tier} IS THIS CAVE'S TIER: A UNIT WHOSE APTITUDE MATCHES IT ADDS +{GatherResolver.AptitudeBonus}.");
    }

    private HudTooltipContent ChanceTooltip()
    {
        int hard = GatherResolver.HardSuccessThreshold(shownChance);

        return new HudTooltipContent("SUCCESS VALUE",
            $"YOU ROLL 1 TO 100 AND HAVE TO ROLL UNDER {Mathf.Max(0, shownChance)}.\n" +
            $"UNDER {hard}: HARD SUCCESS, FULL LOOT PLUS A BONUS PICK.\n" +
            $"UNDER {Mathf.Max(0, shownChance)}: REGULAR, MAIN LOOT ONLY. ABOVE IT: FAILURE.\n" +
            "THE NUMBER MOVES AS YOU PICK UNITS.");
    }

    private HudTooltipContent CoreTooltip()
    {
        var config = shownConfig;
        GatherZone zone = config != null ? config.zone : GatherZone.Easy;

        string drop;
        switch (zone)
        {
            case GatherZone.Normal:
                drop = "A HARD SUCCESS ALWAYS HANDS OVER THE KEY.";
                break;
            case GatherZone.Hard:
                drop = "A HARD SUCCESS ALWAYS HANDS OVER THE BOX.";
                break;
            default:
                drop = "EASY CAVES NEVER HOLD A CORE ITEM.";
                break;
        }

        string jackpot = shownJackpot
            ? $"\nJACKPOT ARMED: SV {GatherDispatchController.JackpotSuccessValue}+ MEANS A NATURAL " +
              $"{GatherDispatchController.JackpotRoll} ALSO GRABS WHATEVER PIECE YOU ARE STILL MISSING."
            : $"\nJACKPOT NEEDS SV {GatherDispatchController.JackpotSuccessValue}+ - THE FOG IS TOO THICK RIGHT NOW.";

        return new HudTooltipContent("CORE ITEM", drop + jackpot);
    }

    /// <summary>One drop slot's worth of explanation.</summary>
    private HudTooltipContent DropTooltip(int index)
    {
        var config = shownConfig;
        if (config == null) return null;

        var seen = new List<InventoryItemType>();
        CollectType(seen, config.mainLoot);
        if (config.hardSuccessBonusPool != null)
        {
            foreach (var loot in config.hardSuccessBonusPool) CollectType(seen, loot);
        }

        if (index >= seen.Count) return null;

        var type = seen[index];
        bool main = config.mainLoot.type == type;
        int amount = main ? config.mainLoot.amount : 0;

        return new HudTooltipContent(ItemName(type),
            main
                ? $"X{amount} FROM EVERY SUCCESSFUL TRIP. A HARD SUCCESS ADDS ONE BONUS PICK ON TOP."
                : "ONLY FROM THE HARD SUCCESS BONUS POOL - ONE OF THESE IS PICKED AT RANDOM.");
    }

    private static string ItemName(InventoryItemType type)
    {
        switch (type)
        {
            case InventoryItemType.CookingMaterial: return "COOKING MATERIAL";
            case InventoryItemType.Medicine: return "MEDICINE";
            case InventoryItemType.Materials: return "MATERIALS";
            default: return "LOOT";
        }
    }

    private static void CollectType(List<InventoryItemType> seen, LootAmount loot)
    {
        if (!seen.Contains(loot.type)) seen.Add(loot.type);
    }

    /// <summary>Main loot amount, or "+" when the type only comes from the Hard Success bonus pool.</summary>
    private static string AmountFor(GatherZoneConfig config, InventoryItemType type)
    {
        if (config != null && config.mainLoot.type == type) return $"x{config.mainLoot.amount}";
        return "+";
    }

    private Sprite SpriteForItem(InventoryItemType type)
    {
        // The card promises the same art the HUD is counting: the loot row and
        // the status bar must never disagree about what a plank or a pill is.
        var shared = Hud() != null ? Hud().IconFor(type) : null;
        if (shared != null) return shared;

        switch (type)
        {
            case InventoryItemType.CookingMaterial:
                return cookingMaterialSprite != null ? cookingMaterialSprite : PlaceholderIcons.Flask;
            case InventoryItemType.Medicine:
                return medicineSprite != null ? medicineSprite : PlaceholderIcons.Plus;
            case InventoryItemType.Materials:
                return materialsSprite != null ? materialsSprite : PlaceholderIcons.RoundedSquare;
            default:
                return PlaceholderIcons.RoundedSquare;
        }
    }

    /// <summary>The HUD, looked up once and re-tried until it exists (cards can build before it).</summary>
    private StatusBarView Hud()
    {
        if (hud == null) hud = FindAnyObjectByType<StatusBarView>();
        return hud;
    }

    private void BuildFrame(RectTransform root)
    {
        for (int i = 0; i < 4; i++)
        {
            Image strip = NewImage("Frame" + i, root);
            strip.raycastTarget = false;
            frame.Add(strip);
        }

        var top = (RectTransform)frame[0].transform;
        top.anchorMin = new Vector2(0f, 1f);
        top.anchorMax = new Vector2(1f, 1f);
        top.pivot = new Vector2(0.5f, 1f);
        top.anchoredPosition = Vector2.zero;

        var bottom = (RectTransform)frame[1].transform;
        bottom.anchorMin = new Vector2(0f, 0f);
        bottom.anchorMax = new Vector2(1f, 0f);
        bottom.pivot = new Vector2(0.5f, 0f);
        bottom.anchoredPosition = Vector2.zero;

        var left = (RectTransform)frame[2].transform;
        left.anchorMin = new Vector2(0f, 0f);
        left.anchorMax = new Vector2(0f, 1f);
        left.pivot = new Vector2(0f, 0.5f);
        left.anchoredPosition = Vector2.zero;

        var right = (RectTransform)frame[3].transform;
        right.anchorMin = new Vector2(1f, 0f);
        right.anchorMax = new Vector2(1f, 1f);
        right.pivot = new Vector2(1f, 0.5f);
        right.anchoredPosition = Vector2.zero;
    }

    private static Color ColorForZone(GatherZone zone)
    {
        switch (zone)
        {
            case GatherZone.Easy: return EasyColor;
            case GatherZone.Normal: return NormalColor;
            case GatherZone.Hard: return HardColor;
            default: return Color.white;
        }
    }

    private void HandleClick()
    {
        // The Button already refuses a locked card; the guard keeps a direct
        // programmatic click (tests, the right-click cancel path) honest too.
        if (locked) return;

        OnAnyNodeClicked?.Invoke(this);
    }

    // -------------------------------------------------------------- helpers

    private static Image NewImage(string objectName, Transform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.type = Image.Type.Simple;
        image.color = Color.white;
        return image;
    }

    private static TMP_Text NewText(string objectName, Transform parent, float fontSize)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        // Built inactive so TMP's Awake sees the font already applied instead of
        // logging "no Font Asset assigned" on the first frame.
        go.SetActive(false);
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        TmpFont.Apply(text);
        go.SetActive(true);
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}