using System;
using System.Collections.Generic;
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
///   Cook / Craft -> left click (or the + on the label) queues one more round
///   Heal         -> left click arms Heal (no queue)
///   Right click  -> handled run-wide by RightClickCancel, which drops every pending pick
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
    [Tooltip("Normal colour while no character is picked: the column cannot be used yet.")]
    [SerializeField] private Color lockedTint = new Color(0.16f, 0.17f, 0.21f, 1f);

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

    [Header("Round Counter")]
    [Tooltip("Width of the queued-round number, which sits outside the button on its left.")]
    [SerializeField] private float countWidth = 34f;
    [SerializeField] private float counterSpacing = 4f;
    [Tooltip("Plate behind the number. Same face as a skill button, so the pair reads as \"[ 3 ] [ COOK + ]\".")]
    [SerializeField] private Color countSlotColor = new Color(0.145f, 0.196f, 0.263f, 1f);
    [Tooltip("Glyph painted inside the button's right gutter, so \"click me to add a round\" is visible without hovering.")]
    [SerializeField] private string plusLabel = "+";
    [Tooltip("Tint of that glyph - the accent the options page already uses for values.")]
    [SerializeField] private Color plusColor = new Color(1f, 0.88f, 0.55f, 1f);
    [Tooltip("Distance from the button's right edge to the + glyph.")]
    [SerializeField] private float plusPadding = 4f;
    [SerializeField] private float plusGlyphWidth = 14f;
    [Tooltip("Tint of the + glyph while the column is locked (nobody picked).")]
    [SerializeField] private Color lockedGlyphColor = new Color(0.42f, 0.44f, 0.48f, 1f);

    /// <summary>Skill art, so the confirm button can repeat the icon the player just picked.</summary>
    public Sprite CookIcon => cookIcon;
    public Sprite HealIcon => healIcon;
    public Sprite CraftIcon => craftIcon;

    public event Action CookClicked;
    public event Action CraftClicked;
    public event Action HealClicked;
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

    /// <summary>Which skill currently owns the confirm button, so tinting can be re-applied.</summary>
    private SkillKind armedKind = SkillKind.None;

    /// <summary>Heal is only offered while somebody is hurt.</summary>
    private bool healAvailable = true;

    /// <summary>
    /// True while no character is picked. A skill needs somebody to act with, so
    /// the whole column greys out instead of accepting a press that cannot lead
    /// anywhere.
    /// </summary>
    private bool locked;

    private UnitRosterView roster;

    /// <summary>The "+" glyphs painted in the button gutters, dimmed while locked.</summary>
    private readonly List<TMP_Text> plusGlyphs = new List<TMP_Text>();

    /// <summary>True when the column is greyed out because nobody is picked.</summary>
    public bool IsLocked => locked;

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

        Wire(cookButton, () => CookClicked?.Invoke());
        Wire(craftButton, () => CraftClicked?.Invoke());
        Wire(healButton, () => HealClicked?.Invoke());

        // Each skill names its own sound: the player hears which one they are
        // arming. The mode toggles beside them only switch a mode - plain.
        UiClickSound.Attach(cookButton, UiClickKind.Cook);
        UiClickSound.Attach(healButton, UiClickKind.Heal);
        UiClickSound.Attach(craftButton, UiClickKind.Craft);
        UiClickSound.Attach(healModeButton, UiClickKind.Plain);
        UiClickSound.Attach(craftModeButton, UiClickKind.Plain);

        if (healModeButton != null) healModeButton.onClick.AddListener(HandleHealModeClicked);

        if (craftModeButton != null) craftModeButton.onClick.AddListener(HandleCraftModeClicked);

        BuildCounters();
        WireTooltips();
        PaintSkillIcons();
    }

    /// <summary>
    /// Watches the roster: the whole column is dead until a character is picked,
    /// because every skill needs somebody to act with.
    /// </summary>
    private void Start()
    {
        roster = FindAnyObjectByType<UnitRosterView>();
        if (roster != null) roster.OnSelectionChanged += RefreshLock;

        RefreshLock();
    }

    /// <summary>Re-reads the roster and greys the column when nothing is picked.</summary>
    public void RefreshLock()
    {
        SetLocked(roster != null && roster.SelectedCount == 0);
    }

    /// <summary>Greys the column out and refuses presses. Public so the flow can force it.</summary>
    public void SetLocked(bool value)
    {
        locked = value;
        ApplyButtonStates();
    }

    // ------------------------------------------------------- round counters

    /// <summary>
    /// Lays the two queued skills out as "[count] [button +]". The number used to
    /// sit inside the button frame, which read as part of the label, so it moves
    /// outside to the left; the "this adds a round" hint is a "+" inside the
    /// button's right gutter, tinted so it reads as an affordance rather than text.
    ///
    /// The number is deliberately kept OUT of the row's layout
    /// (<see cref="LayoutElement.ignoreLayout"/>): a layout group re-aligns its
    /// children whenever one appears or disappears, so a count that showed up on
    /// the first queued round used to drag the whole skill button sideways. Pinned
    /// to the row's left edge instead, the button never moves and the number just
    /// fills in beside it.
    /// </summary>
    private void BuildCounters()
    {
        BuildCounterRow(cookButton, cookCountLabel, "CookRow");
        BuildCounterRow(craftButton, craftCountLabel, null);

        PaintAddAffordance(cookButton);
        PaintAddAffordance(craftButton);
    }

    /// <summary>Slot plates by label, so the number and its plate hide together.</summary>
    private readonly System.Collections.Generic.Dictionary<TextMeshProUGUI, Image> countSlots = new();

    private void BuildCounterRow(Button skillButton, TextMeshProUGUI countLabel, string rowName)
    {
        if (skillButton == null) return;

        var buttonRect = (RectTransform)skillButton.transform;
        var parent = buttonRect.parent as RectTransform;
        if (parent == null) return;

        var group = parent.GetComponent<HorizontalLayoutGroup>();
        RectTransform row = parent;

        // Cook has no row of its own; Craft already shares one with its mode
        // button, so it is reused instead of nesting another group.
        if (group == null)
        {
            var rowGo = new GameObject(rowName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row = (RectTransform)rowGo.transform;
            row.SetParent(parent, false);
            row.localScale = Vector3.one;
            row.sizeDelta = buttonRect.sizeDelta;
            row.SetSiblingIndex(buttonRect.GetSiblingIndex());

            buttonRect.SetParent(row, false);
            group = rowGo.GetComponent<HorizontalLayoutGroup>();
        }

        // Left-aligned, so the row never re-centres its children when one of
        // them is hidden; the count is excluded from the math entirely below.
        group.childAlignment = TextAnchor.MiddleLeft;
        group.spacing = counterSpacing;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;
        group.childControlWidth = false;
        group.childControlHeight = false;

        if (countLabel == null) return;

        var countRect = countLabel.rectTransform;
        countRect.SetParent(row, false);
        countRect.localScale = Vector3.one;

        var element = countRect.GetComponent<LayoutElement>();
        if (element == null) element = countRect.gameObject.AddComponent<LayoutElement>();
        element.ignoreLayout = true;

        // Sits just outside the button's left edge (the button is the row's
        // first laid-out child, so the row's left edge is its left edge).
        countRect.anchorMin = countRect.anchorMax = new Vector2(0f, 0.5f);
        countRect.pivot = new Vector2(1f, 0.5f);
        countRect.anchoredPosition = new Vector2(-counterSpacing, 0f);
        countRect.sizeDelta = new Vector2(countWidth, buttonRect.sizeDelta.y);

        countLabel.alignment = TextAlignmentOptions.Center;
        countLabel.enableAutoSizing = false;
        countLabel.raycastTarget = false;

        BuildCountSlot(row, countRect, buttonRect, countLabel);
    }

    /// <summary>
    /// Puts a plate under the queued-round number, so it reads as the "[ 2 ]" half
    /// of "[ 2 ] [ COOK + ]" instead of a digit floating on the map.
    ///
    /// The plate is a sibling placed BEFORE the label, not an Image on the label:
    /// two graphics on one GameObject draw in enable order, and the label's own
    /// text would be painted over by the plate added after it.
    /// </summary>
    private void BuildCountSlot(RectTransform row, RectTransform countRect, RectTransform buttonRect,
        TextMeshProUGUI countLabel)
    {
        var existing = row.Find("CountSlot");
        if (existing != null) return;

        var go = new GameObject("CountSlot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var slotRect = (RectTransform)go.transform;
        slotRect.SetParent(row, false);
        slotRect.localScale = Vector3.one;
        slotRect.SetSiblingIndex(countRect.GetSiblingIndex());

        slotRect.anchorMin = slotRect.anchorMax = new Vector2(0f, 0.5f);
        slotRect.pivot = new Vector2(1f, 0.5f);
        slotRect.anchoredPosition = new Vector2(-counterSpacing, 0f);
        slotRect.sizeDelta = new Vector2(countWidth, buttonRect.sizeDelta.y);

        var element = go.AddComponent<LayoutElement>();
        element.ignoreLayout = true;

        var image = go.GetComponent<Image>();
        image.color = countSlotColor;
        image.raycastTarget = false;

        go.SetActive(countLabel.gameObject.activeSelf);
        countSlots[countLabel] = image;
    }

    /// <summary>
    /// Paints a small tinted "+" in a skill button's right gutter. It is a label,
    /// not a second button: the whole button already adds a round, so this is the
    /// visible promise of that without a separate target to miss - and it stays
    /// out of the button's own text, so "CRAFT +" never wraps.
    /// </summary>
    private void PaintAddAffordance(Button skillButton)
    {
        if (skillButton == null) return;

        var existing = skillButton.transform.Find("Plus");
        if (existing != null) return;

        var buttonRect = (RectTransform)skillButton.transform;

        var go = new GameObject("Plus", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        var rect = (RectTransform)go.transform;
        rect.SetParent(skillButton.transform, false);
        rect.localScale = Vector3.one;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-plusPadding, 0f);
        rect.sizeDelta = new Vector2(plusGlyphWidth, buttonRect.sizeDelta.y);

        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = plusLabel;
        text.fontSize = 26f;
        text.color = locked ? lockedGlyphColor : plusColor;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        TmpFont.Apply(text);

        plusGlyphs.Add(text);

        MakeRoomForPlus(skillButton, buttonRect);
    }

    /// <summary>
    /// Slides the button's label left when its text would run into the new
    /// glyph. The craft label is wider than the cook one and fills its rect, so
    /// without this the + lands on the "T" - the label is measured, not guessed.
    /// </summary>
    private void MakeRoomForPlus(Button skillButton, RectTransform buttonRect)
    {
        var label = skillButton.transform.Find("Label") as RectTransform;
        if (label == null) return;

        var labelText = label.GetComponent<TextMeshProUGUI>();
        if (labelText == null) return;

        float textWidth = labelText.preferredWidth;
        bool leftAligned = labelText.horizontalAlignment == HorizontalAlignmentOptions.Left;

        float textRight = leftAligned
            ? label.anchoredPosition.x - label.rect.width * 0.5f + textWidth
            : label.anchoredPosition.x + textWidth * 0.5f;

        float plusLeft = buttonRect.rect.width * 0.5f - plusPadding - plusGlyphWidth;
        float overlap = textRight + counterSpacing - plusLeft;
        if (overlap <= 0f) return;

        label.anchoredPosition -= new Vector2(overlap, 0f);
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
    /// Right click is quoted as "cancel everything" because that is what it does
    /// now: the run-wide gesture, not a per-button queue wipe.
    ///
    /// The bodies are providers, not strings: every number (AP, materials, the
    /// heal cost that changes with medicine stock) is read on hover so a tooltip
    /// can never quote a stale cost.
    /// </summary>
    private void WireTooltips()
    {
        if (!showTooltips) return;

        AttachTooltip(cookButton, CookContentObject, "ADD ONE MORE ROUND", "CANCEL EVERYTHING");
        AttachTooltip(craftButton, CraftContentObject, "ADD ONE MORE ROUND", "CANCEL EVERYTHING");
        AttachTooltip(healButton, HealContentObject, "ARM HEAL", "CANCEL EVERYTHING");
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
        int buffMaterial = controller != null ? controller.WorkshopBaseMaterialCost(WorkshopMode.BuffPotion) : 5;

        return new HudTooltipContent("CRAFT RECIPE",
            $"MED: {medicineAp} AP + {medicineMaterial} MATERIALS MAKES 1 MEDICINE. " +
            $"BUFF: {buffAp} AP + {buffMaterial} MATERIALS MAKES 1 BUFF POTION.");
    }

    private void OnDestroy()
    {
        if (healModeButton != null) healModeButton.onClick.RemoveListener(HandleHealModeClicked);
        if (craftModeButton != null) craftModeButton.onClick.RemoveListener(HandleCraftModeClicked);
        if (roster != null) roster.OnSelectionChanged -= RefreshLock;
    }

    private void HandleHealModeClicked() => HealModeToggled?.Invoke();

    /// <summary>
    /// Left click only: a right click is the run-wide "cancel what I picked"
    /// gesture, handled by <see cref="RightClickCancel"/> so every control
    /// answers it the same way. The panel does not keep a second, quieter
    /// meaning for it.
    /// </summary>
    private static void Wire(Button button, Action leftClick)
    {
        if (button == null) return;

        button.onClick.AddListener(() => leftClick());
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

        if (countSlots.TryGetValue(label, out var slot) && slot != null)
            slot.gameObject.SetActive(visible);
    }

    /// <summary>Shows how many Cook rounds are queued (0 hides the number).</summary>
    public void SetCookCount(int count) => SetCount(cookCountLabel, count);

    /// <summary>Shows how many Craft rounds are queued (0 hides the number).</summary>
    public void SetCraftCount(int count) => SetCount(craftCountLabel, count);

    /// <summary>Highlights whichever skill currently owns the confirm button.</summary>
    public void SetArmed(SkillKind armed)
    {
        armedKind = armed;
        ApplyButtonStates();
    }

    /// <summary>Heal is disabled outright while nobody is hurt.</summary>
    public void SetHealAvailable(bool available)
    {
        healAvailable = available;
        ApplyButtonStates();
    }

    /// <summary>
    /// Paints the three skill buttons from the live state: locked (nobody picked)
    /// beats armed, which beats idle. Kept in one place because three separate
    /// callers write these colours, and the last one to run used to win.
    /// </summary>
    private void ApplyButtonStates()
    {
        PaintSkillButton(cookButton, SkillKind.Cook, cookIdleTint, true);
        PaintSkillButton(craftButton, SkillKind.Craft, craftIdleTint, true);
        PaintSkillButton(healButton, SkillKind.Heal, healIdleTint, healAvailable);

        // The mode toggles are part of the same dead column: no character, no
        // recipe to switch.
        if (healModeButton != null)
        {
            healModeButton.interactable = !locked;
            SetDisabledTint(healModeButton);
        }
        if (craftModeButton != null)
        {
            craftModeButton.interactable = !locked;
            SetDisabledTint(craftModeButton);
        }

        Color glyph = locked ? lockedGlyphColor : plusColor;
        foreach (var text in plusGlyphs)
        {
            if (text != null) text.color = glyph;
        }

        PaintSkillArtTint();
    }

    /// <summary>The stock Unity disabled plate, restored the moment the column unlocks.</summary>
    private static readonly Color DefaultDisabledTint = new Color(0.55f, 0.55f, 0.55f, 0.45f);

    private void PaintSkillButton(Button button, SkillKind kind, Color idle, bool usable)
    {
        if (button == null) return;

        button.interactable = usable && !locked;

        Color tint = locked ? lockedTint : armedKind == kind ? armedTint : idle;
        SetNormalTint(button, tint);

        // A Button falls back to `disabledColor` (45% alpha in this project) when
        // it is not interactable, which turned a locked column see-through and
        // let the map show through as olive. Locked plates stay opaque grey.
        SetDisabledTint(button);
    }

    private void SetDisabledTint(Button button)
    {
        var colors = button.colors;
        Color disabled = locked ? lockedTint : DefaultDisabledTint;
        if (colors.disabledColor == disabled) return;

        colors.disabledColor = disabled;
        button.colors = colors;
    }

    /// <summary>Dim the skill art while the column is dead, so the icons do not read as live.</summary>
    private void PaintSkillArtTint()
    {
        Color tint = locked ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
        if (cookIconImage != null) cookIconImage.color = tint;
        if (healIconImage != null) healIconImage.color = tint;
        if (craftIconImage != null) craftIconImage.color = tint;
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
