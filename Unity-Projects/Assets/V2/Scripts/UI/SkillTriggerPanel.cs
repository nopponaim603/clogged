using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The three base-work skills the right-column triggers can start.</summary>
public enum SkillKind
{
    None = 0,
    Cook = 1,
    Heal = 2,
    Craft = 3
}

/// <summary>
/// The right-column skill triggers that replace the old BaseWorkPanel dropdown
/// flow. Purely presentational: it raises input events and paints counts, and
/// <see cref="SkillFlowController"/> decides what each press means.
///
///   Cook / Craft -> left click queues one more round, right click clears the queue
///   Heal         -> left click arms Heal (no queue), right click cancels it
///   HealMode     -> toggles "1 Medicine + 1 AP" against "2 AP"
/// </summary>
public class SkillTriggerPanel : MonoBehaviour
{
    [Header("Cook")]
    [SerializeField] private Button cookButton;
    [SerializeField] private TextMeshProUGUI cookCountLabel;

    [Header("Heal")]
    [SerializeField] private Button healButton;
    [SerializeField] private Button healModeButton;
    [SerializeField] private TextMeshProUGUI healModeLabel;

    [Header("Craft")]
    [SerializeField] private Button craftButton;
    [SerializeField] private TextMeshProUGUI craftCountLabel;

    [Header("Look")]
    [Tooltip("Counts below this stay hidden so an idle panel reads as plain buttons.")]
    [SerializeField] private int hideCountBelow = 1;
    [Tooltip("Normal colour of a skill button while it owns the pending action.")]
    [SerializeField] private Color armedTint = new Color(0.24f, 0.52f, 0.85f, 1f);

    public event Action CookClicked;
    public event Action CookCleared;
    public event Action CraftClicked;
    public event Action CraftCleared;
    public event Action HealClicked;
    public event Action HealCancelled;
    public event Action HealModeToggled;

    // "MED" = spends 1 Medicine + 1 AP, "AP" = spends 2 AP only. Latin text
    // keeps both labels inside the default TMP atlas.
    private const string MedicineModeLabel = "MED";
    private const string ApOnlyModeLabel = "AP";

    private Color cookIdleTint;
    private Color craftIdleTint;
    private Color healIdleTint;

    private void Awake()
    {
        // One idle tint for all three so the column reads as a set; taking it
        // from Cook avoids inheriting odd per-prefab colour blocks.
        cookIdleTint = NormalTint(cookButton);
        craftIdleTint = cookIdleTint;
        healIdleTint = cookIdleTint;

        SetNormalTint(craftButton, craftIdleTint);
        SetNormalTint(healButton, healIdleTint);

        Wire(cookButton, () => CookClicked?.Invoke(), () => CookCleared?.Invoke());
        Wire(craftButton, () => CraftClicked?.Invoke(), () => CraftCleared?.Invoke());
        Wire(healButton, () => HealClicked?.Invoke(), () => HealCancelled?.Invoke());

        if (healModeButton != null) healModeButton.onClick.AddListener(HandleHealModeClicked);
    }

    private void OnDestroy()
    {
        if (healModeButton != null) healModeButton.onClick.RemoveListener(HandleHealModeClicked);
    }

    private void HandleHealModeClicked() => HealModeToggled?.Invoke();

    private static void Wire(Button button, Action leftClick, Action rightClick)
    {
        if (button == null) return;

        button.onClick.AddListener(() => leftClick());

        var right = button.GetComponent<RightClickHandler>();
        if (right == null) right = button.gameObject.AddComponent<RightClickHandler>();
        right.Clicked += () => rightClick();
    }

    private static Color NormalTint(Button button)
    {
        return button != null ? button.colors.normalColor : Color.white;
    }

    private static void SetNormalTint(Button button, Color color)
    {
        if (button == null) return;

        var colors = button.colors;
        if (colors.normalColor == color) return;

        colors.normalColor = color;
        colors.selectedColor = color;
        button.colors = colors;
    }

    private void SetCount(TextMeshProUGUI label, int count)
    {
        if (label == null) return;

        bool visible = count >= hideCountBelow;
        label.text = visible ? count.ToString() : "";
        label.gameObject.SetActive(visible);
    }

    /// <summary>Shows how many Cook rounds are queued (0 hides the number).</summary>
    public void SetCookCount(int count) => SetCount(cookCountLabel, count);

    /// <summary>Shows how many Craft rounds are queued (0 hides the number).</summary>
    public void SetCraftCount(int count) => SetCount(craftCountLabel, count);

    /// <summary>Highlights whichever skill currently owns the confirm button.</summary>
    public void SetArmed(SkillKind armed)
    {
        SetNormalTint(cookButton, armed == SkillKind.Cook ? armedTint : cookIdleTint);
        SetNormalTint(craftButton, armed == SkillKind.Craft ? armedTint : craftIdleTint);
        SetNormalTint(healButton, armed == SkillKind.Heal ? armedTint : healIdleTint);
    }

    /// <summary>Heal is disabled outright while nobody is hurt.</summary>
    public void SetHealAvailable(bool available)
    {
        if (healButton != null) healButton.interactable = available;
    }

    /// <summary>Paints the medicine/AP toggle. The toggle is dead without medicine in stock.</summary>
    public void SetHealMode(bool useMedicine, bool medicineAvailable)
    {
        bool canUseMedicine = useMedicine && medicineAvailable;

        if (healModeLabel != null)
        {
            string label = canUseMedicine ? MedicineModeLabel : ApOnlyModeLabel;
            if (healModeLabel.text != label) healModeLabel.text = label;
        }
        if (healModeButton != null) healModeButton.interactable = medicineAvailable;
    }
}
