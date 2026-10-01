using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Test helper: attach to any empty GameObject, press Play, then
/// right-click this component in the Inspector and pick "Dispatch Near - Ann".
/// </summary>
public class TestDispatchTrigger : MonoBehaviour
{
    [ContextMenu("Dispatch Near - Ann")]
    private void DispatchNearAnn()
    {
        if (UnitManager.Instance == null || UnitManager.Instance.Units.Count == 0)
        {
            Debug.LogWarning("[TestDispatchTrigger] UnitManager missing or has no units. Press Play first.");
            return;
        }

        if (GatherDispatchController.Instance == null)
        {
            Debug.LogWarning("[TestDispatchTrigger] GatherDispatchController.Instance is null.");
            return;
        }

        UnitCharacter ann = UnitManager.Instance.Units[0];
        var list = new List<UnitCharacter> { ann };
        
        Debug.Log($"[Test] {ann.unitName} AP {ann.currentActionPoints}/{ann.maxActionPoints}, starvation {ann.starvationStacks}, state {ann.state}");

        int apBefore = ann.currentActionPoints;
        GatherDispatchResult result = GatherDispatchController.Instance.Dispatch(GatherZone.Near, list);

        if (result == null)
        {
            Debug.LogWarning("[TestDispatchTrigger] Dispatch returned null (blocked). Check the warning above.");
            return;
        }

        Debug.Log(
            $"[TestDispatchTrigger] {ann.unitName} -> {result.zone}\n" +
            $"Tier: {result.tier} (roll {result.rollValue} vs success {result.successValue})\n" +
            $"Main loot: {FormatLoot(result.mainLootGranted)}\n" +
            $"Bonus loot: {FormatLoot(result.bonusLootGranted)}\n" +
            $"Hurt: {(result.hurtUnit != null ? result.hurtUnit.unitName : "-")} | " +
            $"Missing: {(result.missingUnit != null ? result.missingUnit.unitName : "-")}\n" +
            $"Trait awarded: {result.traitAwarded} ({result.traitPolarity})\n" +
            $"AP: {apBefore} -> {ann.currentActionPoints} | State: {ann.state}");
    }

    private static string FormatLoot(LootAmount? loot)
    {
        return loot.HasValue ? $"{loot.Value.type} x{loot.Value.amount}" : "-";
    }
}
