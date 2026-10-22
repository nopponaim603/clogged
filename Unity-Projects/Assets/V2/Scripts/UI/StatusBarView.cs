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
    [Tooltip("Placeholder frame next to the Core Item label; tinted by its locked/unlocked/obtained state.")]
    [SerializeField] private Image coreItemIcon;
    [SerializeField] private Sprite coreItemSprite;

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

    private static readonly Color LockedColor = new Color(1f, 1f, 1f, 0.18f);
    private static readonly Color UnlockedColor = new Color(1f, 0.84f, 0.35f, 0.75f);
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
            if (RunResources.Instance.HasCoreItem)
            {
                coreItemStatusText.text = "Core Item\n<color=#FFE68A>Obtained</color>";
            }
            else if (RunResources.Instance.HasCoreItemUnlock)
            {
                coreItemStatusText.text = "Core Item\n<color=#FFD75E>Unlocked</color>";
            }
            else
            {
                coreItemStatusText.text = "Core Item\n<color=#9AA4B2>Locked</color>";
            }
        }
    }

    /// <summary>
    /// Keeps the icon frames in sync. A slot with no sprite stays a faint
    /// placeholder, so the layout is already prepared for real art later.
    /// </summary>
    private void RefreshIcons()
    {
        if (RunResources.Instance != null && coreItemIcon != null)
        {
            if (coreItemSprite != null) coreItemIcon.sprite = coreItemSprite;

            if (RunResources.Instance.HasCoreItem) coreItemIcon.color = ObtainedColor;
            else if (RunResources.Instance.HasCoreItemUnlock) coreItemIcon.color = UnlockedColor;
            else coreItemIcon.color = LockedColor;
        }

        ApplyResourceIcon(foodIcon, foodSprite, foodText);
        ApplyResourceIcon(cookingMaterialIcon, cookingMaterialSprite, cookingMaterialText);
        ApplyResourceIcon(medicineIcon, medicineSprite, medicineText);
        ApplyResourceIcon(materialsIcon, materialsSprite, materialsText);
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

