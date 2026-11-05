using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The single confirm button, bottom-right. Hidden until the player has
/// something to confirm; its label follows the current action
/// ("Dispatch" / "Cook" / "Heal" / "Craft") and a second, smaller line spells
/// out what that action will cost in AP.
///
/// The panel never decides what confirming means - it raises
/// <see cref="Clicked"/> and whoever owns the current flow handles it.
/// </summary>
public class DispatchActionPanel : MonoBehaviour
{
    /// <summary>
    /// Whose plan the visible button belongs to. The gather flow and the skill
    /// flow both listen to <see cref="Clicked"/>, and the skill handler clears
    /// its state inside that same click - so "is a skill active?" is already
    /// false by the time the gather handler runs. The owner is stamped when the
    /// button is painted and is the only thing a handler may act on.
    /// </summary>
    public enum ConfirmOwner
    {
        None,
        Dispatch,
        Skill
    }

    [Header("Refs")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button actionButton;
    [SerializeField] private Image actionIcon;
    [SerializeField] private TextMeshProUGUI actionLabel;
    [Tooltip("Second line under the label: the AP the pending action will spend. Hidden when the cost is 0.")]
    [SerializeField] private TextMeshProUGUI costLabel;

    [Header("Behaviour")]
    [Tooltip("Hide the whole panel while there is nothing to confirm.")]
    [SerializeField] private bool hideWhenIdle = true;
    [Tooltip("Label used when the player is confirming a gather trip.")]
    [SerializeField] private string dispatchLabel = "DISPATCH";
    [Tooltip("Sword drawn on the button while a gather trip is being confirmed.")]
    [SerializeField] private Sprite dispatchIcon;

    public event Action Clicked;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;
    public string CurrentLabel { get; private set; } = "";

    /// <summary>Which flow painted the current action. <see cref="ConfirmOwner.None"/> while hidden.</summary>
    public ConfirmOwner Owner { get; private set; } = ConfirmOwner.None;

    /// <summary>AP the pending action will spend, as painted on the cost line.</summary>
    public int CurrentApCost { get; private set; }

    /// <summary>Cost line text exactly as painted - "2 AP", "2 AP + 1 SWORD".</summary>
    public string CurrentCostText { get; private set; } = "";

    /// <summary>One-line explanation of what confirming does, shown in the hover tooltip.</summary>
    public string CurrentTooltip { get; private set; } = "";

    /// <summary>Sprite currently painted on the button, so tests can assert the wiring.</summary>
    public Sprite CurrentIcon => actionIcon != null ? actionIcon.sprite : null;

    /// <summary>The sword art this button uses for a trip - the help page reuses it.</summary>
    public Sprite DispatchIcon => dispatchIcon;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (actionButton != null) actionButton.onClick.AddListener(HandleClicked);

        // The one button that runs the trip or the skill.
        UiClickSound.Attach(actionButton, UiClickKind.Select);

        // Nothing to confirm yet - the panel appears once the player picks something.
        if (hideWhenIdle && panelRoot != null) panelRoot.SetActive(false);

        SetCostText(0);

        HudTooltipTrigger.Attach(this, ConfirmationTooltip);
    }

    private HudTooltipContent ConfirmationTooltip()
    {
        string heading = string.IsNullOrEmpty(CurrentLabel) ? "CONFIRM" : CurrentLabel;
        string body = CurrentTooltip;

        if (CurrentApCost > 0)
        {
            string cost = $"THIS SPENDS {CurrentApCost} AP.";
            body = string.IsNullOrEmpty(body) ? cost : $"{body} {cost}";
        }

        if (string.IsNullOrEmpty(body)) body = "COMMIT THE ACTION YOU PICKED.";

        return new HudTooltipContent(heading, body);
    }

    private void OnDestroy()
    {
        if (actionButton != null) actionButton.onClick.RemoveListener(HandleClicked);
    }

    private void HandleClicked() => Clicked?.Invoke();

    /// <summary>Shows the panel for a gather trip.</summary>
    public void ShowDispatch(bool interactable = true, int apCost = 0, string tooltip = null, string costText = null) =>
        Show(dispatchLabel, dispatchIcon, interactable, apCost, tooltip, costText, ConfirmOwner.Dispatch);

    /// <summary>Shows the panel with an action-specific label, its skill icon and the AP it will spend.</summary>
    public void Show(string label, Sprite icon, bool interactable = true, int apCost = 0, string tooltip = null,
        string costText = null, ConfirmOwner owner = ConfirmOwner.Skill)
    {
        CurrentLabel = label;
        CurrentTooltip = tooltip ?? "";
        CurrentApCost = Mathf.Max(0, apCost);
        Owner = owner;

        if (actionLabel != null) actionLabel.text = label;
        if (actionIcon != null)
        {
            actionIcon.sprite = icon;
            actionIcon.enabled = icon != null;
        }

        SetCostText(CurrentApCost, costText);

        if (actionButton != null) actionButton.interactable = interactable;
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    /// <summary>Shows the panel with a label but no icon.</summary>
    public void Show(string label, bool interactable = true, int apCost = 0, string tooltip = null) =>
        Show(label, null, interactable, apCost, tooltip);

    /// <summary>
    /// Keeps the label but greys the button out (missing prerequisites).
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (actionButton != null) actionButton.interactable = interactable;
    }

    public void Hide()
    {
        CurrentLabel = "";
        CurrentTooltip = "";
        CurrentApCost = 0;
        Owner = ConfirmOwner.None;
        SetCostText(0);

        if (hideWhenIdle && panelRoot != null) panelRoot.SetActive(false);
    }

    /// <summary>
    /// Paints the cost line. A trip costs AP plus a sword, so callers can pass
    /// the whole string; without one it falls back to "N AP". A zero cost means
    /// nothing is pending, and the line disappears rather than reading "0 AP".
    /// </summary>
    private void SetCostText(int apCost, string costText = null)
    {
        if (costLabel == null) return;

        bool visible = apCost > 0 || !string.IsNullOrEmpty(costText);
        costLabel.text = visible ? (string.IsNullOrEmpty(costText) ? $"{apCost} AP" : costText) : string.Empty;
        CurrentCostText = costLabel.text;
        costLabel.gameObject.SetActive(visible);
    }
}
