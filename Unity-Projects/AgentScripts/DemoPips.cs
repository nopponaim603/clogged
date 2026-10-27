using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Puts the hand into the "a cave is picked, so every card previews its AP cost"
/// state so the AP pips can be screenshotted.
/// Run: unity command script.run --file AgentScripts/DemoPips.cs --entry DemoPips.CaveSelected
/// </summary>
public static class DemoPips
{
    public static string CaveSelected()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 5f;
        }

        var node = Object.FindAnyObjectByType<MapNodeView>();
        if (node == null) return "no cave node";

        // Same path the player takes: the node's own button.
        node.GetComponent<Button>().onClick.Invoke();

        var roster = Object.FindAnyObjectByType<UnitRosterView>();
        string onCave = roster == null ? "no roster" : Describe(roster);

        // Pick one card so the selected frame reads next to the pips.
        if (roster != null)
        {
            foreach (var card in roster.Cards)
            {
                if (!card.CanBeSelected) continue;
                ExecuteEvents.Execute(card.gameObject,
                    new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left },
                    ExecuteEvents.pointerClickHandler);
                break;
            }
        }

        return $"cave selected -> {onCave}";
    }

    /// <summary>Reads the pip state after the roster's Update has pushed the preview.</summary>
    public static string Report()
    {
        var roster = Object.FindAnyObjectByType<UnitRosterView>();
        return roster == null ? "no roster" : Describe(roster);
    }

    static string Describe(UnitRosterView roster)    {
        var parts = new System.Collections.Generic.List<string>();
        foreach (var card in roster.Cards)
        {
            if (card.Unit == null) continue;
            var row = card.transform.Find("ApPips");
            var pips = new System.Collections.Generic.List<string>();
            if (row != null)
            {
                for (int i = 0; i < row.childCount; i++)
                {
                    var child = row.GetChild(i);
                    if (!child.gameObject.activeSelf) continue;
                    var image = child.GetComponent<Image>();
                    pips.Add(image != null && image.sprite != null ? image.sprite.name : "?");
                }
            }
            parts.Add($"{card.Unit.unitName} ap={card.Unit.currentActionPoints}/{card.Unit.maxActionPoints} " +
                      $"preview={card.ApCostPreview} pips=[{string.Join(",", pips)}]");
        }
        return string.Join(" | ", parts);
    }
}
