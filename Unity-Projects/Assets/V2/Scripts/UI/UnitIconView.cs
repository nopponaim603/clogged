using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One unit card at the base: portrait, name, AP/state, and a colored frame
/// that identifies the unit. The card is also the selection control for the
/// gather dispatch (replaces the old standalone toggles that sat on the map).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UnitIconView : MonoBehaviour, IPointerClickHandler
{
    [Header("Refs (from prefab)")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;

    [Header("Card Layout")]
    [SerializeField] private Vector2 cardSize = new Vector2(195f, 265f);
    [SerializeField] private Color cardBackground = new Color(0.07f, 0.09f, 0.14f, 0.9f);
    [SerializeField] private float padding = 10f;
    [SerializeField] private float portraitHeight = 155f;
    [SerializeField] private Color statusColor = new Color(0.82f, 0.87f, 1f);

    [Header("Frame")]
    [SerializeField] private float frameThickness = 4f;
    [SerializeField] private float selectedFrameThickness = 7f;
    [SerializeField] private float unselectedFrameAlpha = 0.6f;
    [SerializeField] private float unavailableFrameAlpha = 0.2f;
    [SerializeField] private float selectedScale = 1.06f;

    [Header("Heal Target")]
    [Tooltip("Colour of a card that can be picked as a heal patient. Mint so it reads against every unit accent.")]
    [SerializeField] private Color targetableColor = new Color(0.55f, 1f, 0.75f, 1f);
    [SerializeField] private float targetableFrameThickness = 7f;
    [Tooltip("Slow pulse so the eligible patients catch the eye even before the player looks at the hand.")]
    [SerializeField] private float targetablePulseSpeed = 3.2f;
    [SerializeField] private float targetablePulseMinAlpha = 0.55f;

    [Header("AP Pips")]
    [Tooltip("One pip per action point. Available = still unspent, pending = the action about to be confirmed will eat it, spent = already gone.")]
    [SerializeField] private Sprite apPipAvailable;
    [SerializeField] private Sprite apPipPending;
    [SerializeField] private Sprite apPipSpent;
    [SerializeField] private float apPipWidth = 18f;
    [SerializeField] private float apPipHeight = 22f;
    [SerializeField] private float apPipSpacing = 4f;
    [SerializeField] private float apPipRowHeight = 22f;

    private CanvasGroup group;
    private UnitCharacter unit;
    private Color accent = Color.white;
    private bool selectable;
    private bool selectionEnabled = true;
    private bool chromeBuilt;
    private bool targetable;
    private float appliedThickness = -1f;
    private int pendingApCost;

    // 0 = top, 1 = bottom, 2 = left, 3 = right
    private readonly Image[] frame = new Image[4];
    private readonly System.Collections.Generic.List<Image> apPips =
        new System.Collections.Generic.List<Image>();
    private RectTransform apPipRow;

    public event Action<UnitIconView> Clicked;

    public UnitCharacter Unit => unit;
    public bool IsSelected { get; private set; }
    public bool CanBeSelected => selectable && selectionEnabled && unit != null;

    /// <summary>Base AP cost of the action being set up right now (0 when nothing is pending).</summary>
    public int ApCostPreview => pendingApCost;

    /// <summary>
    /// How many action points the action the player is currently setting up will
    /// spend, before this unit's starvation surcharge. 0 clears the preview.
    /// </summary>
    public void SetApCostPreview(int baseApCost)
    {
        pendingApCost = Mathf.Max(0, baseApCost);
    }

    /// <summary>True while this card is a legal target for the action being aimed (e.g. a heal patient).</summary>
    public bool IsTargetable => targetable;

    private void Update()
    {
        // Only the eligible targets animate, so idle cards cost nothing.
        if (!targetable || !chromeBuilt) return;

        float pulse = Mathf.Lerp(targetablePulseMinAlpha, 1f,
            (Mathf.Sin(Time.unscaledTime * targetablePulseSpeed) + 1f) * 0.5f);

        Color color = targetableColor;
        color.a = IsSelected ? 1f : pulse;
        for (int i = 0; i < frame.Length; i++) frame[i].color = color;
    }

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    /// <summary>Binds this card to a unit and paints its frame in the unit's accent color.</summary>
    public void Bind(UnitCharacter boundUnit, Color accentColor, bool makeSelectable)
    {
        unit = boundUnit;
        accent = accentColor;
        selectable = makeSelectable;

        BuildCardChrome();
        if (nameText != null) nameText.text = unit.unitName;
        ApplySelectionScale();
        ApplyFrame();
        Refresh();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!CanBeSelected) return;
        Clicked?.Invoke(this);
    }

    public void SetSelected(bool selected)
    {
        if (IsSelected == selected) return;

        IsSelected = selected;
        ApplySelectionScale();
        ApplyFrame();
    }

    public void SetSelectable(bool value)
    {
        if (selectable == value) return;

        selectable = value;
        ApplyFrame();
    }

    /// <summary>
    /// Marks this card as a legal target for the action being aimed (a heal
    /// patient). Eligible cards get a pulsing mint frame and full opacity -
    /// without it, a Hurt card would sit at the normal "not idle" dimming and
    /// read as unavailable, which is the opposite of what the player needs.
    /// </summary>
    public void SetTargetable(bool value)
    {
        if (targetable == value) return;

        targetable = value;
        if (group != null)
        {
            float alpha = value ? 1f : AlphaForState();
            if (!Mathf.Approximately(group.alpha, alpha)) group.alpha = alpha;
        }
        ApplyFrame();
    }

    /// <summary>Used to freeze selection (e.g. while a trip is travelling) without changing availability.</summary>
    public void SetSelectionEnabled(bool value)
    {
        if (selectionEnabled == value) return;

        selectionEnabled = value;
        if (!value) targetable = false;
        if (group != null && !value) group.alpha = AlphaForState();
        ApplyFrame();
    }

    public void Refresh()
    {
        if (unit == null) return;

        if (statusText != null)
        {
            string hunger = unit.starvationStacks > 0 ? $"  Hungry x{unit.starvationStacks}" : "";
            // Busy reads as its own line so a unit that already gathered today is
            // still visibly usable for base work (it keeps its remaining AP).
            string busy = unit.HasGatheredToday ? "\nOut today" : "";
            // Pips carry the AP count once they are wired, so the text keeps only
            // the state lines and the two never say the same thing twice.
            string text = apPipRow != null
                ? $"{StateLabel(unit.state)}{hunger}{busy}"
                : $"AP {unit.currentActionPoints}/{unit.maxActionPoints}\n{StateLabel(unit.state)}{hunger}{busy}";
            if (statusText.text != text) statusText.text = text;
        }

        RefreshApPips();

        if (iconImage != null)
        {
            Color tint = unit.state == UnitState.Hurt ? new Color(1f, 0.55f, 0.55f) : Color.white;
            if (iconImage.color != tint) iconImage.color = tint;
        }

        if (group != null)
        {
            float alpha = targetable ? 1f : AlphaForState();
            if (!Mathf.Approximately(group.alpha, alpha)) group.alpha = alpha;
        }

        // Selectability is owned by whoever drives the panel (dispatch vs skill
        // mode pick different rules), so it is never recomputed here.
        ApplyFrame();
    }

    /// <summary>
    /// Turns the prefab's root image into the card background and builds the
    /// portrait child, the text layout and the four frame strips in code, so
    /// ScoutIcon.prefab can keep being the walking icon as well.
    /// </summary>
    private void BuildCardChrome()
    {
        if (chromeBuilt) return;
        chromeBuilt = true;

        var root = (RectTransform)transform;
        root.sizeDelta = cardSize;

        Sprite portraitSprite = iconImage != null ? iconImage.sprite : null;
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = false;
            iconImage.color = cardBackground;
            iconImage.raycastTarget = true;
        }

        if (portraitSprite != null)
        {
            Image portrait = CreateImage("Portrait");
            portrait.sprite = portraitSprite;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portrait.color = Color.white;

            var rect = (RectTransform)portrait.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -padding);
            rect.sizeDelta = new Vector2(-padding * 2f, portraitHeight);

            // Portrait sits above the card background but below the texts.
            rect.SetSiblingIndex(0);
            iconImage = portrait;
        }

        LayoutText(nameText, padding + portraitHeight + 6f, 34f, Color.white, 30f);

        // The pips take over the "AP x/y" line, so the state text starts one row lower.
        float apTop = padding + portraitHeight + 42f;
        BuildApPips(apTop);
        LayoutText(statusText, apTop + (apPipRow != null ? apPipRowHeight + 2f : 0f), 32f, statusColor, 24f);

        for (int i = 0; i < frame.Length; i++)
        {
            frame[i] = CreateImage("Frame" + i);
            frame[i].raycastTarget = false;
        }
        ConfigureFrameRects();
    }

    /// <summary>
    /// Row that holds the AP pips. Nothing is built when the three sprites are
    /// missing, so a card prefab without them keeps the old "AP x/y" text.
    /// </summary>
    private void BuildApPips(float topOffset)
    {
        if (apPipAvailable == null || apPipPending == null || apPipSpent == null) return;

        var go = new GameObject("ApPips", typeof(RectTransform));
        apPipRow = (RectTransform)go.transform;
        apPipRow.SetParent(transform, false);
        apPipRow.anchorMin = new Vector2(0f, 1f);
        apPipRow.anchorMax = new Vector2(1f, 1f);
        apPipRow.pivot = new Vector2(0.5f, 1f);
        apPipRow.anchoredPosition = new Vector2(0f, -topOffset);
        apPipRow.sizeDelta = new Vector2(-padding * 2f, apPipRowHeight);
    }

    /// <summary>One pip per action point, painted from the unit's live AP plus the pending action's cost.</summary>
    private void RefreshApPips()
    {
        if (apPipRow == null || unit == null) return;

        int total = Mathf.Max(0, unit.maxActionPoints);
        int left = Mathf.Clamp(unit.currentActionPoints, 0, total);
        int spending = pendingApCost > 0 ? Mathf.Min(unit.GetActualApCost(pendingApCost), left) : 0;

        while (apPips.Count < total)
        {
            var go = new GameObject("ApPip" + apPips.Count, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(apPipRow, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            var image = go.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
            apPips.Add(image);
        }

        for (int i = 0; i < apPips.Count; i++)
        {
            bool visible = i < total;
            var pip = apPips[i];
            if (pip.gameObject.activeSelf != visible) pip.gameObject.SetActive(visible);
            if (!visible) continue;

            var rect = (RectTransform)pip.transform;
            rect.sizeDelta = new Vector2(apPipWidth, apPipHeight);
            rect.anchoredPosition = new Vector2(i * (apPipWidth + apPipSpacing), 0f);

            // AP drains from the left, so the pips just before the boundary are
            // the ones the pending action will eat.
            Sprite sprite = i >= left ? apPipSpent
                : i >= left - spending ? apPipPending
                : apPipAvailable;
            if (pip.sprite != sprite) pip.sprite = sprite;
        }
    }

    private void LayoutText(TMP_Text text, float topOffset, float height, Color color, float maxFontSize)    {
        if (text == null) return;

        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableAutoSizing = true;
        text.fontSizeMin = 11f;
        text.fontSizeMax = maxFontSize;
        text.color = color;
        text.raycastTarget = false;

        var rect = (RectTransform)text.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -topOffset);
        rect.sizeDelta = new Vector2(-padding * 2f, height);
    }

    private Image CreateImage(string objectName)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);

        var image = go.GetComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        return image;
    }

    private void ConfigureFrameRects()
    {
        var top = (RectTransform)frame[0].transform;
        top.anchorMin = new Vector2(0f, 1f);
        top.anchorMax = new Vector2(1f, 1f);
        top.pivot = new Vector2(0.5f, 1f);
        top.anchoredPosition = Vector2.zero;

        var bottom = (RectTransform)frame[1].transform;
        bottom.anchorMin = new Vector2(0f, 0f);
        bottom.anchorMax = new Vector2(1f, 0f);
        bottom.pivot = new Vector2(0.5f, 0f);
        bottom.anchoredPosition = Vector2.zero;

        var left = (RectTransform)frame[2].transform;
        left.anchorMin = new Vector2(0f, 0f);
        left.anchorMax = new Vector2(0f, 1f);
        left.pivot = new Vector2(0f, 0.5f);
        left.anchoredPosition = Vector2.zero;

        var right = (RectTransform)frame[3].transform;
        right.anchorMin = new Vector2(1f, 0f);
        right.anchorMax = new Vector2(1f, 1f);
        right.pivot = new Vector2(1f, 0.5f);
        right.anchoredPosition = Vector2.zero;
    }

    private void ApplySelectionScale()
    {
        transform.localScale = Vector3.one * (IsSelected ? selectedScale : 1f);
    }

    private void ApplyFrame()
    {
        if (!chromeBuilt) return;

        float thickness = IsSelected || targetable ? selectedFrameThickness : frameThickness;
        if (targetable) thickness = targetableFrameThickness;

        if (!Mathf.Approximately(appliedThickness, thickness))
        {
            appliedThickness = thickness;
            ((RectTransform)frame[0].transform).sizeDelta = new Vector2(0f, thickness);
            ((RectTransform)frame[1].transform).sizeDelta = new Vector2(0f, thickness);
            ((RectTransform)frame[2].transform).sizeDelta = new Vector2(thickness, 0f);
            ((RectTransform)frame[3].transform).sizeDelta = new Vector2(thickness, 0f);
        }

        // The pulse in Update() owns the colour while targetable.
        if (targetable) return;

        Color color = accent;
        if (!CanBeSelected) color.a = unavailableFrameAlpha;
        else color.a = IsSelected ? 1f : unselectedFrameAlpha;

        for (int i = 0; i < frame.Length; i++) frame[i].color = color;
    }

    /// <summary>Card opacity from the unit's state alone; the targetable flag overrides it.</summary>
    private float AlphaForState()
    {
        if (unit == null) return 1f;
        if (unit.state == UnitState.Missing) return 0.25f;
        if (!selectable) return 0.5f;
        if (unit.state != UnitState.Idle) return 0.55f;
        return 1f;
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
