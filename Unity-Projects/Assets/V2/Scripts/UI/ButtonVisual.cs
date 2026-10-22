using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes a button artwork-ready: it can show a text label only (current
/// state), or swap to a sprite icon. Nothing else in the game needs to know
/// which mode is active, so the two bottom-right buttons can grow real art
/// later without rewiring their onClick handlers.
/// </summary>
public class ButtonVisual : MonoBehaviour
{
    public enum VisualMode
    {
        Label,
        Icon,
    }

    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text label;
    [SerializeField] private VisualMode mode = VisualMode.Label;
    [SerializeField] private Sprite icon;
    [SerializeField] private string labelText;

    private void Awake()
    {
        Apply();
    }

    public void SetIcon(Sprite sprite, bool switchToIcon = true)
    {
        icon = sprite;
        if (switchToIcon) mode = VisualMode.Icon;
        Apply();
    }

    public void SetLabel(string text, bool switchToLabel = true)
    {
        labelText = text;
        if (switchToLabel) mode = VisualMode.Label;
        Apply();
    }

    public void SetMode(VisualMode newMode)
    {
        mode = newMode;
        Apply();
    }

    public void Apply()
    {
        bool useIcon = mode == VisualMode.Icon && icon != null;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = useIcon;
            iconImage.preserveAspect = true;
        }

        if (label != null)
        {
            if (!string.IsNullOrEmpty(labelText)) label.text = labelText;
            label.gameObject.SetActive(!useIcon);
        }
    }
}
