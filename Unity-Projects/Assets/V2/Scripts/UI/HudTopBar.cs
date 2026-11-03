using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-left chrome the sprint review asked for: a Back-to-Menu button, a Help
/// button opening a how-to-play page, and the Core Item slot moved out of that
/// corner into the free space beside the Day frame.
///
/// Built in code and spawned once per run scene so the prefab/scene work stays
/// out of the way - <see cref="RunBootstrap"/> calls <see cref="EnsureExists"/>.
/// </summary>
public class HudTopBar : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("Space between the two top-left buttons.")]
    [SerializeField] private float buttonGap = 8f;
    [Tooltip("Vertical gap between the bottom of the Day frame and the Core Item slots.")]
    [SerializeField] private float coreItemTopGap = 10f;

    [Header("Icons")]
    [Tooltip("Cog drawn inside the OPTIONS button. Set by RunBootstrap from the scene, since this bar is built in code.")]
    [SerializeField] private Sprite optionsIcon;
    [Tooltip("Question mark drawn inside the HELP button.")]
    [SerializeField] private Sprite helpIcon;

    private static readonly Vector2 ButtonSize = new Vector2(48f, 48f);

    /// <summary>Icon size inside a text button - the label needs the rest of the width.</summary>
    private const float IconWithLabelSize = 20f;

    /// <summary>Icon size inside an icon-only button - it owns the whole square.</summary>
    private const float IconOnlySize = 28f;
    private static readonly Color MenuColor = new Color(0.24f, 0.28f, 0.38f, 0.95f);
    private static readonly Color HelpColor = new Color(0.20f, 0.42f, 0.55f, 0.95f);
    private static readonly Color PanelColor = new Color(0.08f, 0.09f, 0.13f, 0.98f);

    private GameObject helpOverlay;
    private GameObject optionsOverlay;
    private CanvasGroup helpGroup;
    private CanvasGroup optionsGroup;
    private OptionsPanelView optionsPage;
    private HelpPanelView helpPage;

    /// <summary>Creates the bar under the run canvas, once per scene.</summary>
    public static void EnsureExists(Sprite optionsIcon = null, Sprite helpIcon = null)
    {
        if (FindFirstObjectByType<HudTopBar>() != null) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("HudTopBar", typeof(RectTransform), typeof(HudTopBar));
        go.transform.SetParent(canvas.transform, false);

        var bar = go.GetComponent<HudTopBar>();
        bar.optionsIcon = optionsIcon;
        bar.helpIcon = helpIcon;

        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        go.transform.SetAsLastSibling();
    }

    private void Start()
    {
        BuildButtons();
        BuildOptionsOverlay();
        BuildHelpOverlay();
        RelocateCoreItemGroup();
    }

    // ---------------------------------------------------------- top-left

    private void BuildButtons()
    {
        // Icon-only squares: a cog and a question mark are unambiguous at this
        // size, and dropping the words keeps the corner from eating the map.
        Button options = BuildButton(transform, "OptionsButton", null, MenuColor, optionsIcon, out _);
        var optionsRect = (RectTransform)options.transform;
        optionsRect.anchorMin = optionsRect.anchorMax = new Vector2(0f, 1f);
        optionsRect.pivot = new Vector2(0f, 1f);
        optionsRect.sizeDelta = ButtonSize;
        optionsRect.anchoredPosition = new Vector2(16f, -16f);
        options.onClick.AddListener(ToggleOptions);

        Button help = BuildButton(transform, "HelpButton", null, HelpColor, helpIcon, out _);
        var helpRect = (RectTransform)help.transform;
        helpRect.anchorMin = helpRect.anchorMax = new Vector2(0f, 1f);
        helpRect.pivot = new Vector2(0f, 1f);
        helpRect.sizeDelta = ButtonSize;
        helpRect.anchoredPosition = new Vector2(16f + ButtonSize.x + buttonGap, -16f);
        help.onClick.AddListener(ToggleHelp);
    }

    /// <summary>
    /// Options page - the very same page the main menu shows, so the settings
    /// live in one place. The only difference is the footer: leaving a run goes
    /// through BACK TO GAME / RETURN TO MENU instead of BACK.
    /// </summary>
    private void BuildOptionsOverlay()
    {
        var overlayRect = NewRect("OptionsOverlay", transform);
        Stretch(overlayRect);
        optionsOverlay = overlayRect.gameObject;

        optionsPage = OptionsPanelView.Create(
            overlayRect,
            "BACK TO GAME", () => SetOverlayVisible(optionsGroup, false),
            "RETURN TO MENU", ReturnToMenu);

        optionsGroup = MakeOverlay(optionsOverlay);
    }

    private void ReturnToMenu()
    {
        SetOverlayVisible(optionsGroup, false);
        SceneFlow.ToMainMenu();
    }

    private void BuildHelpOverlay()
    {
        var overlayRect = NewRect("HelpOverlay", transform);
        Stretch(overlayRect);
        helpOverlay = overlayRect.gameObject;

        // The page owns its own dim backdrop, card, rail and scrolling pane -
        // the copy outgrew a single text block, so it lives in HelpPanelView.
        helpPage = HelpPanelView.Create(overlayRect, () => SetOverlayVisible(helpGroup, false));

        helpGroup = MakeOverlay(helpOverlay);
        helpGroup.alpha = 0f;
        helpPage.transform.SetParent(overlayRect, false);
        Stretch((RectTransform)helpPage.transform);
    }

    private void ToggleHelp()
    {
        bool visible = helpGroup != null && helpGroup.alpha < 0.5f;

        // Rebuilt on open: the roster's cards (and therefore the sword/lamp art
        // the copy quotes) may not have existed when the page was first built.
        if (visible) helpPage?.Rebuild();

        // The button itself already clicked; a page opening is its own sound.
        if (visible) AudioDirector.Instance?.PlayClick(UiClickKind.Popup);

        SetOverlayVisible(helpGroup, visible);
    }

    private void ToggleOptions()
    {
        if (optionsOverlay == null) return;

        bool visible = optionsGroup != null && optionsGroup.alpha < 0.5f;
        if (visible && optionsPage != null) optionsPage.CloseResolutionList();
        if (visible) AudioDirector.Instance?.PlayClick(UiClickKind.Popup);

        SetOverlayVisible(optionsGroup, visible);
    }

    // ------------------------------------------------------- core item slot

    /// <summary>
    /// The Core Item slots started in the top-left corner, which now belongs to
    /// Options/Help. They are stacked vertically (2x1) and parked directly under
    /// the Day frame, sharing its right edge, so the right side of the HUD reads
    /// as one column.
    /// </summary>
    private void RelocateCoreItemGroup()
    {
        Transform group = transform.root.Find("Canvas/StatusBar/CoreItemGroup");
        if (group == null) group = FindByName(transform.root, "CoreItemGroup");
        if (group == null) return;

        var groupRect = (RectTransform)group;
        RectTransform dayFrame = FindByName(transform.root, "DayFrame") as RectTransform;

        groupRect.anchorMin = new Vector2(1f, 1f);
        groupRect.anchorMax = new Vector2(1f, 1f);
        groupRect.pivot = new Vector2(1f, 1f);
        groupRect.localScale = Vector3.one;

        float dayBottom = 0f;
        float rightInset = 0f;

        if (dayFrame != null)
        {
            // DayFrame is top-anchored with a left pivot; its bottom edge is the
            // distance its rect hangs below the top of the canvas.
            dayBottom = -dayFrame.anchoredPosition.y + dayFrame.sizeDelta.y;

            // Line the two panels up on the same right edge.
            rightInset = dayFrame.anchoredPosition.x + dayFrame.sizeDelta.x;
        }

        groupRect.anchoredPosition = new Vector2(rightInset, -(dayBottom + coreItemTopGap));
    }

    private static Transform FindByName(Transform root, string name)
    {
        foreach (var candidate in root.GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name == name) return candidate;
        }
        return null;
    }

    // ------------------------------------------------------------ helpers

    /// <summary>
    /// Builds a plate button. Pass <paramref name="label"/> = null (or "") for an
    /// icon-only square: the icon then centres and owns the whole frame, and no
    /// Label object is created at all.
    /// </summary>
    private Button BuildButton(Transform parent, string objectName, string label, Color color, Sprite icon, out TMP_Text text)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.sprite = PlaceholderIcons.RoundedSquare;
        image.type = Image.Type.Sliced;
        image.color = color;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;

        // Built here rather than through UiFactory, so the chrome click has to be
        // attached explicitly or the top bar would be the one silent corner.
        UiClickSound.Attach(button, UiClickKind.Plain);
        HoverHighlight.Attach(go);

        text = null;
        bool hasLabel = !string.IsNullOrEmpty(label);
        float inset = 0f;

        if (icon != null)
        {
            const float gutter = 6f;
            float iconSize = hasLabel ? IconWithLabelSize : IconOnlySize;

            var iconGo = new GameObject(objectName + "Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.SetParent(rect, false);

            if (hasLabel)
            {
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(gutter, 0f);
                inset = gutter + iconSize;
            }
            else
            {
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = Vector2.zero;
            }

            iconRect.sizeDelta = new Vector2(iconSize, iconSize);

            var iconImage = iconGo.GetComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        if (!hasLabel) return button;

        text = NewText("Label", rect, 22f);
        text.text = label;
        text.fontStyle = FontStyles.Bold;

        // Shrink instead of clipping: an icon eats part of the button, and
        // "OPTIONS" was being cut to "OPT ION" without this.
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 22f;

        var labelRect = (RectTransform)text.transform;
        labelRect.offsetMin = new Vector2(inset, 0f);
        labelRect.offsetMax = new Vector2(-4f, 0f);

        return button;
    }

    private static RectTransform NewRect(string objectName, Transform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    /// <summary>
    /// Both overlays stay active and fade with a CanvasGroup. Toggling the
    /// GameObject instead would leave their graphics out of the canvas batch, so
    /// the map behind kept winning both the draw order and the raycast.
    /// </summary>
    private static CanvasGroup MakeOverlay(GameObject overlay)
    {
        var group = overlay.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        return group;
    }

    private static void SetOverlayVisible(CanvasGroup group, bool visible)
    {
        if (group == null) return;

        group.alpha = visible ? 1f : 0f;
        group.blocksRaycasts = visible;
        group.interactable = visible;
        if (visible) group.transform.SetAsLastSibling();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static TMP_Text NewText(string objectName, Transform parent, float fontSize)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        // Built inactive so TMP's Awake sees the font already applied.
        go.SetActive(false);
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        Stretch(rect);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        TmpFont.Apply(text);
        go.SetActive(true);
        return text;
    }
}
