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

    private CanvasGroup group;
    private UnitCharacter unit;
    private Color accent = Color.white;
    private bool selectable;
    private bool selectionEnabled = true;
    private bool chromeBuilt;
    private float appliedThickness = -1f;

    // 0 = top, 1 = bottom, 2 = left, 3 = right
    private readonly Image[] frame = new Image[4];

    public event Action<UnitIconView> Clicked;

    public UnitCharacter Unit => unit;
    public bool IsSelected { get; private set; }
    public bool CanBeSelected => selectable && selectionEnabled && unit != null;

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

    /// <summary>Used to freeze selection (e.g. while a trip is travelling) without changing availability.</summary>
    public void SetSelectionEnabled(bool value)
    {
        if (selectionEnabled == value) return;

        selectionEnabled = value;
        ApplyFrame();
    }

    public void Refresh()
    {
        if (unit == null) return;

        if (statusText != null)
        {
            string hunger = unit.starvationStacks > 0 ? $"  Hungry x{unit.starvationStacks}" : "";
            string text = $"AP {unit.currentActionPoints}/{unit.maxActionPoints}\n{StateLabel(unit.state)}{hunger}";
            if (statusText.text != text) statusText.text = text;
        }

        if (iconImage != null)
        {
            Color tint = unit.state == UnitState.Hurt ? new Color(1f, 0.55f, 0.55f) : Color.white;
            if (iconImage.color != tint) iconImage.color = tint;
        }

        if (group != null)
        {
            float alpha;
            if (unit.state == UnitState.Missing) alpha = 0.25f;
            else if (!unit.CanBeSentGathering) alpha = 0.5f;
            else if (unit.state != UnitState.Idle) alpha = 0.55f;
            else alpha = 1f;

            if (!Mathf.Approximately(group.alpha, alpha)) group.alpha = alpha;
        }

        // Frame color follows availability; deselection is handled by UnitRosterView
        // so it can raise a single selection-changed event.
        selectable = unit.CanBeSentGathering;
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
        LayoutText(statusText, padding + portraitHeight + 42f, 54f, statusColor, 24f);

        for (int i = 0; i < frame.Length; i++)
        {
            frame[i] = CreateImage("Frame" + i);
            frame[i].raycastTarget = false;
        }
        ConfigureFrameRects();
    }

    private void LayoutText(TMP_Text text, float topOffset, float height, Color color, float maxFontSize)
    {
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

        float thickness = IsSelected ? selectedFrameThickness : frameThickness;
        if (!Mathf.Approximately(appliedThickness, thickness))
        {
            appliedThickness = thickness;
            ((RectTransform)frame[0].transform).sizeDelta = new Vector2(0f, thickness);
            ((RectTransform)frame[1].transform).sizeDelta = new Vector2(0f, thickness);
            ((RectTransform)frame[2].transform).sizeDelta = new Vector2(thickness, 0f);
            ((RectTransform)frame[3].transform).sizeDelta = new Vector2(thickness, 0f);
        }

        Color color = accent;
        if (!CanBeSelected) color.a = unavailableFrameAlpha;
        else color.a = IsSelected ? 1f : unselectedFrameAlpha;

        for (int i = 0; i < frame.Length; i++) frame[i].color = color;
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
