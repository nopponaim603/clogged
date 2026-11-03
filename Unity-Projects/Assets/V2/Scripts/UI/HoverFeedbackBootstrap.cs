using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puts <see cref="HoverHighlight"/> on every uGUI Button in the scene, so the
/// authored HUD picks up the hover wash without each prefab being reopened.
///
/// Buttons built later by <see cref="UiFactory"/> get the component as they are
/// made, so this only has to run once per scene. Controls that paint their own
/// hover (the dungeon cards, the unit cards) are skipped - a mask laid over
/// their code-built art would sit under the art and never be seen.
/// </summary>
public class HoverFeedbackBootstrap : MonoBehaviour
{
    private void Start()
    {
        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var button in buttons)
        {
            if (button == null) continue;
            if (button.GetComponent<MapNodeView>() != null) continue;
            if (button.GetComponent<UnitIconView>() != null) continue;

            HoverHighlight.Attach(button.gameObject);
        }
    }
}
