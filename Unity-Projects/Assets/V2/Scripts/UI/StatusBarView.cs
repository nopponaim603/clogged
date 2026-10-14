using TMPro;
using UnityEngine;

/// <summary>
/// Always-visible HUD strip: day/deadline counter and the shared resource
/// totals. Polls DayManager/RunResources every frame - same simple approach
/// as UnitRosterView, fine for a handful of text fields with no event wiring needed.
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

    private const string WarningHex = "#E65C5C";

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
    }

    private void RefreshDay()
    {
        if (DayManager.Instance == null || dayText == null) return;
        int day = DayManager.Instance.CurrentDay;
        int deadline = DayManager.Instance.DeadlineDay;
        int daysLeft = deadline - day + 1; // counting today

        string left = daysLeft <= 1
            ? $"<color={WarningHex}>last day!</color>"
            : $"{daysLeft} days left";
        dayText.text = $"Day {day} / {deadline} ({left})";
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
        if (cookingMaterialText != null) cookingMaterialText.text = $"Cooking Material: {RunResources.Instance.CookingMaterial}";
        if (medicineText != null) medicineText.text = $"Medicine: {RunResources.Instance.Medicine}";
        if (materialsText != null) materialsText.text = $"Materials: {RunResources.Instance.Materials}";

        if (coreItemStatusText != null)
        {
            if (RunResources.Instance.HasCoreItem)
            {
                coreItemStatusText.text = "Core Item: OBTAINED";
            }
            else if (RunResources.Instance.HasCoreItemUnlock)
            {
                coreItemStatusText.text = "Core Item: Unlocked - go Far";
            }
            else
            {
                coreItemStatusText.text = "Core Item: Locked - go Mid first";
            }
        }
    }
}
