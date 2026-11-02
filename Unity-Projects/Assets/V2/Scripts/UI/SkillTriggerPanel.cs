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
///   CraftMode    -> toggles the workshop recipe (medicine / buff potion)
///
/// Heal and Craft share one layout: a wide action button plus a narrow mode
/// button, both authored as a row in the prefab.
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
    [SerializeField] private Button craftModeButton;
    [SerializeField] private TextMeshProUGUI craftModeLabel;

    [Header("Look")]
    [Tooltip("Counts below this stay hidden so an idle panel reads as plain buttons.")]
    [SerializeField] private int hideCountBelow = 1;
    [Tooltip("Normal colour of a skill button while it owns the pending action.")]
    [SerializeField] private Color armedTint = new Color(0.24f, 0.52f, 0.85f, 1f);

    [Header("Hover Tooltips")]
    [SerializeField] private bool showTooltips = true;
    [Tooltip("Mouse art shown next to each hint. Leave empty to drop the icon row.")]
    [SerializeField] private Sprite leftClickIcon;
    [SerializeField] private Sprite rightClickIcon;

    [Header("Skill Art")]
    [Tooltip("Painted into the button's left gutter. The label is already inset to make room.")]
    [SerializeField] private Sprite cookIcon;
    [SerializeField] private Sprite healIcon;
    [SerializeField] private Sprite craftIcon;
    [SerializeField] private Image cookIconImage;
    [SerializeField] private Image healIconImage;
    [SerializeField] private Image craftIconImage;

    /// <summary>Skill art, so the confirm button can repeat the icon the player just picked.</summary>
    public Sprite CookIcon => cookIcon;
    public Sprite HealIcon => healIcon;
    public Sprite CraftIcon => craftIcon;

    public event Action CookClicked;
    public event Action CookCleared;
    public event Action CraftClicked;
    public event Action CraftCleared;
    public event Action HealClicked;
    public event Action HealCancelled;
    public event Action HealModeToggled;
    public event Action CraftModeToggled;

    // "MED" = spends 1 Medicine + 1 AP, "AP" = spends 2 AP only. Latin text
    // keeps both labels inside the default TMP atlas.
    private const string MedicineModeLabel = "MED";
    private const string ApOnlyModeLabel = "AP";

    // Craft modes: brew healing stock, or brew the party buff potion.
    private const string CraftMedicineLabel = "MED";
    private const string CraftBuffLabel = "BUFF";

    private Color cookIdleTint;
    private Color craftIdleTint;
    private Color healIdleTint;

    /// <summary>Last recipe painted by <see cref="SetCraftMode"/>, so the tooltip quotes the live cost.</summary>
    private WorkshopMode currentWorkshopMode = WorkshopMode.Medicine;

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

        // Each skill names its own sound: the player hears which one they are
        // arming. The mode toggles beside them only switch a mode - plain.
        UiClickSound.Attach(cookButton, UiClickKind.Cook);
        UiClickSound.Attach(healButton, UiClickKind.Heal);
        UiClickSound.Attach(craftButton, UiClickKind.Craft);
        UiClickSound.Attach(healModeButton, UiClickKind.Plain);
        UiClickSound.Attach(craftModeButton, UiClickKind.Plain);

        if (healModeButton != null) healModeButton.onClick.AddListener(HandleHealModeClicked);

        if (craftModeButton != null) craftModeButton.onClick.AddListener(HandleCraftModeClicked);

        WireTooltips();
        PaintSkillIcons();
    }

    private void HandleCraftModeClicked() => CraftModeToggled?.Invoke();

    /// <summary>
    /// Drops the skill art into the button gutter. A slot with no sprite is
    /// hidden rather than left as a white box, so the panel still reads as plain
    /// text while the art is being picked.
    /// </summary>
    private void PaintSkillIcons()
    {
        PaintIcon(cookIconImage, cookIcon);
        PaintIcon(healIconImage, healIcon);
        PaintIcon(craftIconImage, craftIcon);
    }

    private static void PaintIcon(Image image, Sprite sprite)
    {
        if (image == null) return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    /// <summary>
    /// Attaches the hover popups. Cook and Craft queue rounds, Heal is a single
    /// arming step, and the mode toggles have no right click - so the two rows of
    /// each popup differ and the unused one is dropped by the tooltip itself.
    ///
    /// The bodies are providers, not strings: every number (AP, materials, the
    /// heal cost that changes with medicine stock) is read on hover so a tooltip
    /// can never quote a stale cost.
    /// </summary>
    private void WireTooltips()
    {
        if (!showTooltips) return;

        AttachTooltip(cookButton, CookContentObject, "ADD ONE MORE ROUND", "CLEAR THE QUEUE");
        AttachTooltip(craftButton, CraftContentObject, "ADD ONE MORE ROUND", "CLEAR THE QUEUE");
        AttachTooltip(healButton, HealContentObject, "ARM HEAL", "CANCEL");
        AttachTooltip(healModeButton, HealModeContentObject, "SWITCH THE COST", null);
        AttachTooltip(craftModeButton, CraftModeContentObject, "SWITCH THE RECIPE", null);
    }

    private void AttachTooltip(Button button, Func<HudTooltipContent> content, string leftHint, string rightHint)
    {
        if (button == null) return;

        HudTooltipTrigger.Attach(button, () =>
        {
            var tip = content();
            tip.leftHint = leftHint;
            tip.rightHint = rightHint;
            tip.leftIcon = leftClickIcon;
            tip.rightIcon = rightClickIcon;
            return tip;
        });
    }

    private HudTooltipContent CookContentObject()
    {
        var controller = BaseWorkController.Instance;
        int ap = controller != null ? controller.CookBaseApCost : 1;
        int material = controller != null ? controller.CookMaterialPerRound : 1;

        return new HudTooltipContent("COOK",
            $"1 ROUND SPENDS {ap} AP + {material} COOKING MATERIAL AND MAKES FOOD. THE COOKING TALENT DOUBLES IT.");
    }

    private HudTooltipContent CraftContentObject()
    {
        var controller = BaseWorkController.Instance;
        int ap = controller != null ? controller.WorkshopBaseApCost(currentWorkshopMode) : 1;
        int material = controller != null ? controller.WorkshopBaseMaterialCost(currentWorkshopMode) : 2;
        int buffBonus = controller != null ? controller.BuffPotionBonusPerRound : 10;

        string effect = currentWorkshopMode == WorkshopMode.BuffPotion
            ? $"+{buffBonus} SUCCESS VALUE FOR THE WHOLE CREW, FOR THE REST OF THE RUN."
            : "1 MEDICINE. THE CRAFTING TALENT DOUBLES IT.";

        return new HudTooltipContent("CRAFT",
            $"1 ROUND SPENDS {ap} AP + {material} MATERIALS AND MAKES {effect}");
    }

    private static HudTooltipContent HealContentObject()
    {
        var controller = BaseWorkController.Instance;
        int withMedicine = controller != null ? controller.HealBaseApCost(true) : 1;
        int without = controller != null ? controller.HealBaseApCost(false) : 3;

        return new HudTooltipContent("HEAL",
            $"TREAT ONE HURT CREW MEMBER. MED: 1 MEDICINE + {withMedicine} AP. AP ONLY: {without} AP.");
    }

    private static HudTooltipContent HealModeContentObject()
    {
        var controller = BaseWorkController.Instance;
        int withMedicine = controller != null ? controller.HealBaseApCost(true) : 1;
        int without = controller != null ? controller.HealBaseApCost(false) : 3;

        return new HudTooltipContent("HEAL COST",
            $"MED USES 1 MEDICINE AND {withMedicine} AP. AP USES {without} AP.");
    }

    private static HudTooltipContent CraftModeContentObject()
    {
        var controller = BaseWorkController.Instance;
        int medicineAp = controller != null ? controller.WorkshopBaseApCost(WorkshopMode.Medicine) : 1;
        int medicineMaterial = controller != null ? controller.WorkshopBaseMaterialCost(WorkshopMode.Medicine) : 2;
        int buffAp = controller != null ? controller.WorkshopBaseApCost(WorkshopMode.BuffPotion) : 2;
        int buffMaterial = controller != null ? controller.WorkshopBaseMaterialCost(WorkshopMode.BuffPotion) : 4;

        return new HudTooltipContent("CRAFT RECIPE",
            $"MED: {medicineAp} AP + {medicineMaterial} MATERIALS MAKES 1 MEDICINE. " +
            $"BUFF: {buffAp} AP + {buffMaterial} MATERIALS MAKES 1 BUFF POTION.");
    }

    private void OnDestroy()
    {
        if (healModeButton != null) healModeButton.onClick.RemoveListener(HandleHealModeClicked);
        if (craftModeButton != null) craftModeButton.onClick.RemoveListener(HandleCraftModeClicked);
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

    /// <summary>Shows which craft the confirm button will run.</summary>
    public void SetCraftMode(WorkshopMode mode)
    {
        currentWorkshopMode = mode;

        if (craftModeLabel == null) return;

        string label = mode == WorkshopMode.BuffPotion ? CraftBuffLabel : CraftMedicineLabel;
        if (craftModeLabel.text != label) craftModeLabel.text = label;
    }
}
