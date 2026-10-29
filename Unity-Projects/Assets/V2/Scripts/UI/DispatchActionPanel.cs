using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The single confirm button, bottom-right. Hidden until the player has
/// something to confirm; its label follows the current action
/// ("Dispatch" / "Cook" / "Heal" / "Craft").
///
/// The panel never decides what confirming means - it raises
/// <see cref="Clicked"/> and whoever owns the current flow handles it.
/// </summary>
public class DispatchActionPanel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button actionButton;
    [SerializeField] private Image actionIcon;
    [SerializeField] private TextMeshProUGUI actionLabel;

    [Header("Behaviour")]
    [Tooltip("Hide the whole panel while there is nothing to confirm.")]
    [SerializeField] private bool hideWhenIdle = true;
    [Tooltip("Label used when the player is confirming a gather trip.")]
    [SerializeField] private string dispatchLabel = "DISPATCH";

    public event Action Clicked;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;
    public string CurrentLabel { get; private set; } = "";

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (actionButton != null) actionButton.onClick.AddListener(HandleClicked);

        // Nothing to confirm yet - the panel appears once the player picks something.
        if (hideWhenIdle && panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (actionButton != null) actionButton.onClick.RemoveListener(HandleClicked);
    }

    private void HandleClicked() => Clicked?.Invoke();

    /// <summary>Shows the panel for a gather trip.</summary>
    public void ShowDispatch(bool interactable = true) => Show(dispatchLabel, interactable);

    /// <summary>Shows the panel with an action-specific label (Cook / Heal / Craft).</summary>
    public void Show(string label, bool interactable = true)
    {
        CurrentLabel = label;

        if (actionLabel != null) actionLabel.text = label;
        if (actionButton != null) actionButton.interactable = interactable;
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    /// <summary>Keeps the label but greys the button out (missing prerequisites).</summary>
    public void SetInteractable(bool interactable)
    {
        if (actionButton != null) actionButton.interactable = interactable;
    }

    public void Hide()
    {
        CurrentLabel = "";
        if (hideWhenIdle && panelRoot != null) panelRoot.SetActive(false);
    }
}
