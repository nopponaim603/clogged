using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Puts the UI into a representative mid-action state for a layout screenshot:
/// one card picked and two Cook rounds queued.
/// Run: unity command script.run --file AgentScripts/DemoState.cs --entry DemoState.CookQueued
/// </summary>
public static class DemoState
{
    public static string CookQueued()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 5f;
        }

        var roster = Object.FindAnyObjectByType<UnitRosterView>();
        var skillPanel = Object.FindAnyObjectByType<SkillTriggerPanel>(FindObjectsInactive.Include);
        if (roster == null || skillPanel == null) return "missing refs";

        RunResources.Instance.AddItem(InventoryItemType.CookingMaterial, 10);
        RunResources.Instance.AddItem(InventoryItemType.Materials, 10);
        RunResources.Instance.AddItem(InventoryItemType.Medicine, 3);

        foreach (var unit in UnitManager.Instance.Units)
        {
            unit.state = UnitState.Idle;
            unit.currentActionPoints = unit.maxActionPoints;
        }
        roster.RefreshAvailability();
        roster.ClearSelection();

        var card = roster.Cards.Count > 1 ? roster.Cards[1] : roster.Cards[0];
        var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(card.gameObject, data, ExecuteEvents.pointerClickHandler);

        var cook = FindButton(skillPanel, "CookButton");
        if (cook != null)
        {
            cook.onClick.Invoke();
            cook.onClick.Invoke();
        }

        return $"picked={roster.SelectedUnit?.unitName} armed={Object.FindAnyObjectByType<SkillFlowController>().Armed}";
    }

    static Button FindButton(SkillTriggerPanel panel, string childName)
    {
        foreach (var button in panel.GetComponentsInChildren<Button>(true))
        {
            if (button.name == childName) return button;
        }
        return null;
    }
}
