using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Always-visible HUD strip: Core Item state on the left, the shared resource
/// totals in the middle, and the day counter on the right. Polls
/// DayManager/RunResources every frame - same simple approach as
/// UnitRosterView, fine for a handful of text fields with no event wiring needed.
/// </summary>
public class StatusBarView : MonoBehaviour
{
    [Header("Day")]
    [SerializeField] private TMP_Text dayText;

    [Header("Resources")]
    [SerializeField] private TMP_Text foodText;
    [SerializeField] private TMP_Text cookingMaterialText;
    [SerializeField] private TMP_Text medicineText;
    [SerializeField] private TMP_Text materialsText;
    [SerializeField] private TMP_Text coreItemStatusText;

    [Header("Icon Slots (optional - drop art in later)")]
    [Tooltip("Core Item slot 1: the sealed box, then the crystal once both pieces are held.")]
    [SerializeField] private Image coreItemIcon;
    [SerializeField] private Sprite coreItemSprite;

    [Tooltip("Core Item slot 2: the key.")]
    [SerializeField] private Image coreItemBIcon;
    [SerializeField] private Sprite coreItemBSprite;

    [Tooltip("Art for the orange pair: orange box, orange key, spiky crystal.")]
    [SerializeField] private CoreItemArt orangePair = new CoreItemArt();
    [Tooltip("Art for the yellow pair: yellow box, grey key, round crystal.")]
    [SerializeField] private CoreItemArt yellowPair = new CoreItemArt();

    [SerializeField] private Image foodIcon;
    [SerializeField] private Sprite foodSprite;
    [SerializeField] private Image cookingMaterialIcon;
    [SerializeField] private Sprite cookingMaterialSprite;
    [SerializeField] private Image medicineIcon;
    [SerializeField] private Sprite medicineSprite;
    [SerializeField] private Image materialsIcon;
    [SerializeField] private Sprite materialsSprite;

    [Tooltip("When an icon sprite is assigned, hide that resource's text so the HUD can become icon-only.")]
    [SerializeField] private bool hideResourceLabelWhenIconSet = true;

    [Header("Resource Row")]
    [Tooltip("Gap between a resource's icon and its number.")]
    [SerializeField] private float iconLabelGap = 6f;
    [Tooltip("Gap between one resource pair and the next.")]
    [SerializeField] private float pairSpacing = 30f;
    [Tooltip("Square size of every resource icon in the row.")]
    [SerializeField] private float resourceIconSize = 28f;

    /// <summary>Art the HUD is drawing right now, so the help page can repeat it.</summary>
    public Sprite FoodSprite => foodSprite;
    public Sprite MedicineSprite => medicineSprite;
    public Sprite CookingMaterialSprite => cookingMaterialSprite;
    public Sprite MaterialsSprite => materialsSprite;

    /// <summary>
    /// Icon this HUD uses for a resource, so any other screen promising that
    /// resource (the dungeon cards' loot row) shows the same art the player is
    /// counting at the top of the screen.
    /// </summary>
    public Sprite IconFor(InventoryItemType type)
    {
        switch (type)
        {
            case InventoryItemType.CookingMaterial: return cookingMaterialSprite;
            case InventoryItemType.Medicine: return medicineSprite;
            case InventoryItemType.Materials: return materialsSprite;
            default: return null;
        }
    }

    /// <summary>The sealed box this run is after, in whichever palette it rolled.</summary>
    public Sprite CoreBoxSprite => CurrentArt.closedBox != null ? CurrentArt.closedBox : coreItemSprite;

    /// <summary>The key that opens it - the piece a Normal cave hands over.</summary>
    public Sprite CoreKeySprite => CurrentArt.key != null ? CurrentArt.key : coreItemBSprite;

    private CoreItemArt CurrentArt =>
        RunResources.Instance != null && RunResources.Instance.Variant == CoreItemVariant.Yellow ? yellowPair : orangePair;

    private const string WarningHex = "#E65C5C";
    private const string ObtainedHex = "#FFE68A";
    private const string PendingHex = "#9AA4B2";

    private static readonly Color GoalPlateColor = new Color(0.09f, 0.10f, 0.15f, 0.96f);
    private static readonly Color GoalFrameColor = new Color(0.82f, 0.70f, 0.36f, 0.9f);
    private static readonly Color GoalLabelColor = new Color(0.82f, 0.70f, 0.36f, 0.85f);

    /// <summary>The frame's name plate, one letter per line down its left rail.</summary>
    private const string GoalLabelText = "G\nO\nA\nL";

    [Header("Goal Frame")]
    [Tooltip("Width of the lane the GOAL letters are stacked in, to the left of the two slots.")]
    [SerializeField] private float goalLaneWidth = 30f;
    [Tooltip("Padding between the slots and the frame's edge.")]
    [SerializeField] private float goalPadding = 6f;
    [SerializeField] private float goalLabelFontSize = 20f;

    /// <summary>One row of the Core Item art table: the box in both states, the key that opens it, and the crystal found inside.</summary>
    [System.Serializable]
    private class CoreItemArt
    {
        [Tooltip("Closed box - shown while the item is still sealed.")]
        public Sprite closedBox;
        [Tooltip("Same box with the lid open - shown once both pieces are held.")]
        public Sprite openBox;
        [Tooltip("The key that opens this box.")]
        public Sprite key;
        [Tooltip("What was inside. Shown by CrystalRevealView, not in the HUD slot.")]
        public Sprite crystal;
    }

    private static readonly Color LockedColor = new Color(1f, 1f, 1f, 0.18f);
    private static readonly Color ObtainedColor = new Color(1f, 0.9f, 0.45f, 1f);

    private static int CountMouthsTonight()
    {
        if (UnitManager.Instance == null) return 0;

        int count = 0;
        foreach (var unit in UnitManager.Instance.Units)
        {
            if (unit.state != UnitState.Away && unit.state != UnitState.Death) count++;
        }
        return count;
    }

    private void Update()
    {
        RefreshDay();
        RefreshResources();
        RefreshIcons();
    }

    private void Start()
    {
        BuildResourceRow();
        BuildGoalFrame();
        WireTooltips();
    }

    /// <summary>
    /// Rebuilds the four resource readouts as icon+label pairs that hug each
    /// other and sit as one centred block.
    ///
    /// The row was authored as eight absolutely-positioned pieces with wide fixed
    /// label rects, so the text started right after its icon but the *next* icon
    /// sat at the far end of a 240-320px box: the numbers drifted apart and the
    /// whole strip leaned off-centre. A horizontal layout group measures the
    /// labels instead, so the pairs pack tight and the block can be centred in
    /// the bar no matter how long "Food: 8 (need 4 tonight)" gets.
    /// </summary>
    private void BuildResourceRow()
    {
        if (foodIcon == null || foodText == null) return;

        var bar = foodIcon.rectTransform.parent as RectTransform;
        if (bar == null) return;
        if (bar.Find("ResourceRow") != null) return;

        var row = NewRow("ResourceRow", bar, pairSpacing);
        var fitter = row.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Pinned to the middle rather than stretched: a ContentSizeFitter owns
        // sizeDelta on the axes it fits, and a stretched rect with no height was
        // collapsing the whole row (and every icon in it) to zero.
        var rowRect = (RectTransform)row.transform;
        rowRect.anchorMin = rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = Vector2.zero;
        rowRect.sizeDelta = new Vector2(400f, resourceIconSize);

        AddPair(row.transform, "Food", foodIcon, foodText);
        AddPair(row.transform, "Cooking", cookingMaterialIcon, cookingMaterialText);
        AddPair(row.transform, "Materials", materialsIcon, materialsText);
        AddPair(row.transform, "Medicine", medicineIcon, medicineText);

        // The scene authored these on fixed offsets; the parent is an image with
        // no mask, so clearing the size is enough to let the group own the layout.
        foodText.textWrappingMode = TextWrappingModes.NoWrap;
        cookingMaterialText.textWrappingMode = TextWrappingModes.NoWrap;
        materialsText.textWrappingMode = TextWrappingModes.NoWrap;
        medicineText.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private HorizontalLayoutGroup NewRow(string name, RectTransform parent, float spacing)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;

        var group = go.GetComponent<HorizontalLayoutGroup>();
        group.childAlignment = TextAnchor.MiddleCenter;
        group.spacing = spacing;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;
        return group;
    }

    /// <summary>One "icon + number" cell, packed to its own preferred width.</summary>
    private void AddPair(Transform row, string name, Image icon, TMP_Text label)
    {
        if (icon == null || label == null) return;
        if (icon.transform.parent == null || label.transform.parent != icon.transform.parent) return;

        var pair = NewRow("Pair_" + name, (RectTransform)row, iconLabelGap);
        var pairRect = (RectTransform)pair.transform;
        pairRect.anchorMin = pairRect.anchorMax = new Vector2(0.5f, 0.5f);
        pairRect.sizeDelta = new Vector2(40f, resourceIconSize);

        var pairFitter = pair.gameObject.AddComponent<ContentSizeFitter>();
        pairFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        pairFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        icon.rectTransform.SetParent(pairRect, false);
        icon.rectTransform.localScale = Vector3.one;
        var element = icon.GetComponent<LayoutElement>();
        if (element == null) element = icon.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = resourceIconSize;
        element.preferredHeight = resourceIconSize;
        element.flexibleWidth = 0f;

        label.rectTransform.SetParent(pairRect, false);
        label.rectTransform.localScale = Vector3.one;
        label.alignment = TextAlignmentOptions.MidlineLeft;
    }

    /// <summary>
    /// Wraps the two Core Item slots in one plate with the word GOAL down its left
    /// rail, so the pair reads as the objective of the run instead of two loose
    /// icons on the edge of the screen.
    ///
    /// Built around the slots rather than around the group's own rect: the group
    /// is anchored to the top-right corner, and the frame has to grow leftwards to
    /// stay on screen - the run's own goal mark never pushes a slot off the edge.
    /// </summary>
    private void BuildGoalFrame()
    {
        // The slots are the icon frames, not the icons: the frame is drawn around
        // the 118px boxes so their own borders sit inside it.
        RectTransform box = coreItemIcon != null ? coreItemIcon.transform.parent as RectTransform : null;
        RectTransform keySlot = coreItemBIcon != null ? coreItemBIcon.transform.parent as RectTransform : null;
        if (box == null || keySlot == null || box.parent == null || box.parent != keySlot.parent) return;
        if (!IsTopLeftAnchored(box) || !IsTopLeftAnchored(keySlot)) return;

        var group = (RectTransform)box.parent;
        if (group.Find("GoalFrame") != null) return;

        // Bounds of the two slots in the group's own space (anchored top-left).
        float left = Mathf.Min(SlotLeft(box), SlotLeft(keySlot));
        float right = Mathf.Max(SlotRight(box), SlotRight(keySlot));
        float top = Mathf.Max(SlotTop(box), SlotTop(keySlot));
        float bottom = Mathf.Min(SlotBottom(box), SlotBottom(keySlot));

        float plateLeft = left - goalLaneWidth - goalPadding;
        float plateTop = top + goalPadding;

        var frame = UiFactory.CreateImage("GoalFrame", group, GoalPlateColor);
        frame.sprite = PlaceholderIcons.RoundedSquare;
        frame.type = Image.Type.Sliced;
        frame.raycastTarget = true;

        var plate = frame.rectTransform;
        plate.anchorMin = plate.anchorMax = new Vector2(0f, 1f);
        plate.pivot = new Vector2(0f, 1f);
        plate.anchoredPosition = new Vector2(plateLeft, plateTop);
        plate.sizeDelta = new Vector2(right - plateLeft + goalPadding, plateTop - bottom + goalPadding);
        plate.SetAsFirstSibling();

        BuildGoalEdges(plate);

        var label = UiFactory.CreateText("GoalLabel", plate, GoalLabelText, goalLabelFontSize, GoalLabelColor);
        label.alignment = TextAlignmentOptions.Center;
        label.lineSpacing = 4f;
        TmpFont.Apply(label);

        var labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.offsetMin = new Vector2(goalPadding - 3f, goalPadding);
        labelRect.offsetMax = new Vector2(goalPadding - 3f + goalLaneWidth, -goalPadding);

        HudTooltipTrigger.Attach(frame, CoreItemTip);
    }

    /// <summary>The bounds math below assumes a child pinned to one corner, not stretched.</summary>
    private static bool IsTopLeftAnchored(RectTransform rect)
    {
        return rect.anchorMin == new Vector2(0f, 1f) && rect.anchorMax == new Vector2(0f, 1f);
    }

    /// <summary>Four thin bars just inside the plate's edge, the same rim a dungeon card wears.</summary>
    private void BuildGoalEdges(RectTransform plate)
    {
        BuildEdge(plate, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 3f));
        BuildEdge(plate, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 3f));
        BuildEdge(plate, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(3f, 0f));
        BuildEdge(plate, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(3f, 0f));
    }

    private static void BuildEdge(RectTransform plate, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 thickness)
    {
        var image = UiFactory.CreateImage("Frame" + name, plate, GoalFrameColor);
        image.raycastTarget = false;

        var rect = image.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = thickness;
        rect.anchoredPosition = Vector2.zero;
    }

    // Extremes of a slot inside its parent, for a parent whose children anchor
    // top-left with a centre pivot - the shape the HUD's core slots use.
    private static float SlotLeft(RectTransform slot) => slot.anchoredPosition.x - slot.rect.width * slot.pivot.x;
    private static float SlotRight(RectTransform slot) => SlotLeft(slot) + slot.rect.width;
    private static float SlotTop(RectTransform slot) => slot.anchoredPosition.y + slot.rect.height * (1f - slot.pivot.y);
    private static float SlotBottom(RectTransform slot) => SlotTop(slot) - slot.rect.height;

    /// <summary>
    /// Hover popups for the readouts. The bodies are providers, so the numbers a
    /// popup quotes are the numbers on screen at hover time - no cache to
    /// invalidate after a trip or a cook.
    /// </summary>
    private void WireTooltips()
    {
        Attach(foodIcon, foodText, FoodTip);
        Attach(medicineIcon, medicineText, MedicineTip);
        Attach(cookingMaterialIcon, cookingMaterialText, CookingMaterialTip);
        Attach(materialsIcon, materialsText, MaterialsTip);

        HudTooltipTrigger.Attach(coreItemStatusText, CoreItemTip);

        if (coreItemIcon != null)
            HudTooltipTrigger.Attach(coreItemIcon, () => CorePieceTip(RunResources.Instance != null && RunResources.Instance.HasCoreItemA, "THE SEALED BOX"));
        if (coreItemBIcon != null)
            HudTooltipTrigger.Attach(coreItemBIcon, () => CorePieceTip(RunResources.Instance != null && RunResources.Instance.HasCoreItemB, "THE KEY"));
    }

    /// <summary>Both slots get a popup, whichever one the HUD is currently drawing.</summary>
    private static void Attach(Image icon, TMP_Text label, Func<HudTooltipContent> content)
    {
        if (icon != null) HudTooltipTrigger.Attach(icon, content);
        if (label != null) HudTooltipTrigger.Attach(label, content);
    }

    private static HudTooltipContent FoodTip()
    {
        var resources = RunResources.Instance;
        int food = resources != null ? resources.Food : 0;
        int need = CountMouthsTonight();

        string verdict = food >= need ? "ENOUGH FOR TONIGHT." : "NOT ENOUGH FOR TONIGHT - COOK BEFORE END DAY.";
        return new HudTooltipContent("FOOD",
            $"{food} IN STOCK. {need} EATEN AT END DAY, ONE PER CREW MEMBER AT CAMP.\n{verdict}");
    }

    private static HudTooltipContent MedicineTip()
    {
        var resources = RunResources.Instance;
        int stock = resources != null ? resources.Medicine : 0;
        var controller = BaseWorkController.Instance;

        int withMedicine = controller != null ? controller.HealBaseApCost(true) : 1;
        int without = controller != null ? controller.HealBaseApCost(false) : 3;

        return new HudTooltipContent("MEDICINE",
            $"{stock} IN STOCK.\nHEAL USES 1 MEDICINE AND {withMedicine} AP. WITHOUT IT, HEAL COSTS {without} AP.");
    }

    private static HudTooltipContent CookingMaterialTip()
    {
        var resources = RunResources.Instance;
        int stock = resources != null ? resources.CookingMaterial : 0;
        var controller = BaseWorkController.Instance;
        int perRound = controller != null ? controller.CookMaterialPerRound : 1;

        return new HudTooltipContent("COOKING MATERIAL",
            $"{stock} IN STOCK.\nCOOK SPENDS {perRound} PER ROUND AND MAKES FOOD.");
    }

    private static HudTooltipContent MaterialsTip()
    {
        var resources = RunResources.Instance;
        int stock = resources != null ? resources.Materials : 0;
        var controller = BaseWorkController.Instance;

        int medicineCost = controller != null ? controller.WorkshopBaseMaterialCost(WorkshopMode.Medicine) : 2;
        int buffCost = controller != null ? controller.WorkshopBaseMaterialCost(WorkshopMode.BuffPotion) : 4;

        return new HudTooltipContent("MATERIALS",
            $"{stock} IN STOCK.\nCRAFT: {medicineCost} FOR MEDICINE, {buffCost} FOR A BUFF POTION.");
    }

    private static HudTooltipContent CoreItemTip()
    {
        var resources = RunResources.Instance;
        bool box = resources != null && resources.HasCoreItemA;
        bool key = resources != null && resources.HasCoreItemB;

        string state = box && key ? "BOTH PIECES FOUND - THE WAY OUT IS OPEN." : "FIND BOTH PIECES TO LEAVE.";
        return new HudTooltipContent("CORE ITEM",
            $"BOX {(box ? "FOUND" : "MISSING")}, KEY {(key ? "FOUND" : "MISSING")}.\n{state}");
    }

    private static HudTooltipContent CorePieceTip(bool found, string what)
    {
        return new HudTooltipContent("CORE ITEM",
            found ? $"{what} IS ALREADY IN THE BAG." : $"{what} IS STILL OUT THERE. HARD SUCCESS IN THE DEEPER CAVES CAN DROP IT.");
    }

    private void RefreshDay()
    {
        if (DayManager.Instance == null || dayText == null) return;
        int day = DayManager.Instance.CurrentDay;
        int deadline = DayManager.Instance.DeadlineDay;
        int daysLeft = deadline - day + 1; // counting today

        string left = daysLeft <= 1
            ? $"<color={WarningHex}>last day!</color>"
            : $"{daysLeft} left";
        dayText.text = $"DAY\n{day} / {deadline}<size=70%>  ({left})</size>";
    }

    private void RefreshResources()
    {
        if (RunResources.Instance == null) return;

        if (foodText != null)
        {
            // Everyone who isn't Away/Missing eats at End Day. Show the bill so a shortfall is visible before it happens.
            int need = CountMouthsTonight();
            int food = RunResources.Instance.Food;
            foodText.text = food >= need
                ? $"Food: {food} (need {need} tonight)"
                : $"<color={WarningHex}>Food: {food} (need {need} tonight)</color>";
        }
        if (cookingMaterialText != null) cookingMaterialText.text = $"Cooking: {RunResources.Instance.CookingMaterial}";
        if (medicineText != null) medicineText.text = $"Medicine: {RunResources.Instance.Medicine}";
        if (materialsText != null) materialsText.text = $"Materials: {RunResources.Instance.Materials}";

        if (coreItemStatusText != null)
        {
            bool a = RunResources.Instance.HasCoreItemA;
            bool b = RunResources.Instance.HasCoreItemB;

            // ASCII marks only: the default TMP atlas has no tick glyph, and a
            // missing glyph turns the whole HUD slot into a box.
            coreItemStatusText.text =
                $"<color={(a ? ObtainedHex : PendingHex)}>BOX {(a ? "+" : "-")}</color>\n" +
                $"<color={(b ? ObtainedHex : PendingHex)}>KEY {(b ? "+" : "-")}</color>";
        }
    }

    /// <summary>
    /// Keeps the icon frames in sync. A slot with no art stays a faint
    /// placeholder, so the layout is already prepared for real sprites.
    /// </summary>
    private void RefreshIcons()
    {
        if (RunResources.Instance != null)
        {
            var art = CurrentArt;
            bool hasBox = RunResources.Instance.HasCoreItemA;
            bool hasKey = RunResources.Instance.HasCoreItemB;
            bool opened = RunResources.Instance.HasBothCoreItems;

            // The box stays a box, just open, and the crystal inside gets its own
            // reveal over the whole screen - so the HUD keeps reading as inventory.
            Sprite boxArt = opened && art.openBox != null ? art.openBox : art.closedBox;
            ApplyCoreItemIcon(coreItemIcon, coreItemSprite, boxArt, hasBox || opened);
            ApplyCoreItemIcon(coreItemBIcon, coreItemBSprite, art.key, hasKey);
        }

        ApplyResourceIcon(foodIcon, foodSprite, foodText);
        ApplyResourceIcon(cookingMaterialIcon, cookingMaterialSprite, cookingMaterialText);
        ApplyResourceIcon(medicineIcon, medicineSprite, medicineText);
        ApplyResourceIcon(materialsIcon, materialsSprite, materialsText);
    }

    /// <summary>
    /// Real art keeps its own colours and only dims while locked; a slot with no
    /// art falls back to the placeholder tint so the frame still reads.
    /// </summary>
    private void ApplyCoreItemIcon(Image icon, Sprite placeholder, Sprite art, bool obtained)
    {
        if (icon == null) return;

        Sprite sprite = art != null ? art : placeholder;
        if (sprite != null) icon.sprite = sprite;
        icon.color = obtained ? (art != null ? Color.white : ObtainedColor) : LockedColor;
    }

    private void ApplyResourceIcon(Image icon, Sprite sprite, TMP_Text label)
    {
        if (icon == null) return;

        bool hasSprite = sprite != null;
        if (hasSprite) icon.sprite = sprite;
        icon.color = hasSprite ? Color.white : LockedColor;

        if (label != null && hideResourceLabelWhenIconSet)
        {
            label.enabled = !hasSprite;
        }
    }
}

