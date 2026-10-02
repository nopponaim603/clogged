using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One unit's icon at the base. Shows name, AP and state, and dims itself
/// when the unit can't act (Resting/Hurt/Away/Missing).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UnitIconView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;

    private CanvasGroup group;
    private UnitCharacter unit;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    public void Bind(UnitCharacter boundUnit)
    {
        unit = boundUnit;
        if (nameText != null) nameText.text = unit.unitName;
        Refresh();
    }

    public void Refresh()
    {
        if (unit == null) return;

        if (statusText != null)
        {
            string hunger = unit.starvationStacks > 0 ? $" Hungry x{unit.starvationStacks}" : "";
            statusText.text = $"AP {unit.currentActionPoints}/{unit.maxActionPoints}\n{StateLabel(unit.state)}{hunger}";
        }

        switch (unit.state)
        {
            case UnitState.Idle:
                group.alpha = 1f;
                break;
            case UnitState.Missing:
                group.alpha = 0.2f;
                break;
            default:
                group.alpha = 0.5f;
                break;
        }

        if (iconImage != null)
        {
            iconImage.color = unit.state == UnitState.Hurt ? new Color(1f, 0.5f, 0.5f) : Color.white;
        }
    }

    private static string StateLabel(UnitState state)
    {
        switch (state)
        {
            case UnitState.Idle: return "Ready";
            case UnitState.Resting: return "Resting";
            case UnitState.Hurt: return "Hurt";
            case UnitState.Away: return "Away";
            case UnitState.Missing: return "Missing";
            default: return "";
        }
    }
}
