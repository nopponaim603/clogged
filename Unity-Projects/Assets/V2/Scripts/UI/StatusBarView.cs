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

    /// <summary>Art the HUD is drawing right now, so the help page can repeat it.</summary>
    public Sprite FoodSprite => foodSprite;
    public Sprite MedicineSprite => medicineSprite;
    public Sprite CookingMaterialSprite => cookingMaterialSprite;
    public Sprite MaterialsSprite => materialsSprite;

    private const string WarningHex = "#E65C5C";
    private const string ObtainedHex = "#FFE68A";
    private const string PendingHex = "#9AA4B2";

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
        WireTooltips();
    }

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
            var art = RunResources.Instance.Variant == CoreItemVariant.Yellow ? yellowPair : orangePair;
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

