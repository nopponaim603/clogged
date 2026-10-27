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

    private const string WarningHex = "#E65C5C";
    private const string ObtainedHex = "#FFE68A";
    private const string PendingHex = "#9AA4B2";

    /// <summary>One row of the Core Item art table: the box, the key that opens it, and what is inside.</summary>
    [System.Serializable]
    private class CoreItemArt
    {
        [Tooltip("Closed box - the state the item is in while it is still sealed.")]
        public Sprite closedBox;
        [Tooltip("The key that opens this box.")]
        public Sprite key;
        [Tooltip("Shown in the box slot once both pieces are held: the box is open and this is inside.")]
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
            if (unit.state != UnitState.Away && unit.state != UnitState.Missing) count++;
        }
        return count;
    }

    private void Update()
    {
        RefreshDay();
        RefreshResources();
        RefreshIcons();
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

            // The box slot turns into the crystal the moment both pieces are
            // held: that is the win condition, i.e. the box is open.
            Sprite boxArt = opened && art.crystal != null ? art.crystal : art.closedBox;
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

