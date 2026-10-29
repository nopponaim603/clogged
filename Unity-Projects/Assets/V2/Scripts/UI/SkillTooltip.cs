using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The hover popup shared by the skill triggers.
///
/// Built once in code under the root canvas and reused, so the popup cannot
/// drift out of sync with an extra prefab that nobody remembers to update. It
/// is layout-only: it never receives raycasts (the pointer must stay on the
/// button that opened it) and it fades out on the frame after the pointer
/// leaves.
///
/// Text is written in capitals because the whole HUD runs on the baked pixel
/// font, which only carries A-Z, 0-9 and the punctuation drawn procedurally.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SkillTooltip : MonoBehaviour
{
    private const float Width = 250f;
    private const float Padding = 10f;
    private const float Spacing = 6f;
    private const float IconSize = 16f;
    private const float IconGap = 6f;
    private const float ScreenMargin = 4f;
    private const float AnchorGap = 8f;

    private static readonly Color Backdrop = new Color(0.043f, 0.067f, 0.098f, 0.96f);
    private static readonly Color Border = new Color(0.26f, 0.29f, 0.37f, 1f);
    private static readonly Color TitleColor = new Color(1f, 0.88f, 0.55f, 1f);
    private static readonly Color BodyColor = new Color(0.78f, 0.82f, 0.88f, 1f);
    private static readonly Color HintColor = new Color(0.60f, 0.63f, 0.68f, 1f);

    private static SkillTooltip instance;

    private Canvas canvas;
    private RectTransform root;
    private CanvasGroup group;
    private TMP_Text titleLabel;
    private TMP_Text bodyLabel;
    private RectTransform leftRow;
    private RectTransform rightRow;
    private Image leftIcon;
    private Image rightIcon;
    private TMP_Text leftLabel;
    private TMP_Text rightLabel;

    public static void ShowNow(Canvas hostCanvas, RectTransform anchor,
        Sprite leftClick, Sprite rightClick,
        string title, string body, string leftHint, string rightHint)
    {
        if (hostCanvas == null || anchor == null) return;

        Ensure(hostCanvas.rootCanvas != null ? hostCanvas.rootCanvas : hostCanvas);
        instance.Present(anchor, leftClick, rightClick, title, body, leftHint, rightHint);
    }

    public static void Hide()
    {
        if (instance == null) return;
        instance.group.alpha = 0f;
    }

    private static void Ensure(Canvas hostCanvas)
    {
        if (instance != null && instance.canvas == hostCanvas) return;

        if (instance != null)
        {
            Destroy(instance.gameObject);
            instance = null;
        }

        // A domain reload clears the static while the old popup can survive in a
        // running play session, so adopt nothing and remove any orphans first.
        foreach (var orphan in hostCanvas.GetComponentsInChildren<SkillTooltip>(true))
        {
            Destroy(orphan.gameObject);
        }

        var host = new GameObject("SkillTooltip", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Outline));
        host.transform.SetParent(hostCanvas.transform, false);
        host.transform.SetAsLastSibling();

        instance = host.AddComponent<SkillTooltip>();
        instance.canvas = hostCanvas;
        instance.Build(host);
    }

    private void Build(GameObject host)
    {
        root = host.GetComponent<RectTransform>();
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(1f, 0.5f);
        root.sizeDelta = new Vector2(Width, 60f);

        var background = host.GetComponent<Image>();
        background.color = Backdrop;
        background.raycastTarget = false;

        var outline = host.GetComponent<Outline>();
        outline.effectColor = Border;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        group = host.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        titleLabel = CreateLabel("Title", 16, TitleColor);
        bodyLabel = CreateLabel("Body", 15, BodyColor);

        leftRow = CreateRow("LeftClickRow", out leftIcon, out leftLabel);
        rightRow = CreateRow("RightClickRow", out rightIcon, out rightLabel);
    }

    private TMP_Text CreateLabel(string name, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(root, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.overflowMode = TextOverflowModes.Overflow;

        return text;
    }

    private RectTransform CreateRow(string name, out Image icon, out TMP_Text label)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(root, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(Width - Padding * 2f, IconSize);

        var iconGo = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(rect, false);

        var iconRect = (RectTransform)iconGo.transform;
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.sizeDelta = new Vector2(IconSize, IconSize);

        icon = iconGo.AddComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;

        var labelGo = new GameObject("Hint", typeof(RectTransform));
        labelGo.transform.SetParent(rect, false);

        var labelRect = (RectTransform)labelGo.transform;
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = new Vector2(IconSize + IconGap, 0f);
        labelRect.sizeDelta = new Vector2(Width - Padding * 2f - IconSize - IconGap, 0f);

        label = labelGo.AddComponent<TextMeshProUGUI>();
        label.fontSize = 14;
        label.color = HintColor;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.overflowMode = TextOverflowModes.Overflow;

        return rect;
    }

    private void Present(RectTransform anchor, Sprite leftClick, Sprite rightClick,
        string title, string body, string leftHint, string rightHint)
    {
        titleLabel.text = Wrap(title);
        bodyLabel.text = Wrap(body);

        LayoutContent(anchor, leftClick, leftHint, rightClick, rightHint);

        group.alpha = 1f;
        transform.SetAsLastSibling();
    }

    private void LayoutContent(RectTransform anchor, Sprite leftClick, string leftHint,
        Sprite rightClick, string rightHint)
    {
        float contentWidth = Width - Padding * 2f;
        float y = -Padding;

        float titleHeight = Measure(titleLabel, contentWidth);
        titleLabel.rectTransform.anchoredPosition = new Vector2(Padding, y);
        titleLabel.rectTransform.sizeDelta = new Vector2(contentWidth, titleHeight);
        y -= titleHeight + Spacing;

        if (!string.IsNullOrEmpty(bodyLabel.text))
        {
            y -= Spacing;
            float bodyHeight = Measure(bodyLabel, contentWidth);
            bodyLabel.rectTransform.anchoredPosition = new Vector2(Padding, y);
            bodyLabel.rectTransform.sizeDelta = new Vector2(contentWidth, bodyHeight);
            y -= bodyHeight;
            bodyLabel.gameObject.SetActive(true);
        }
        else
        {
            bodyLabel.gameObject.SetActive(false);
        }

        y = PlaceRow(leftRow, leftIcon, leftLabel, leftClick, leftHint, y, contentWidth);
        y = PlaceRow(rightRow, rightIcon, rightLabel, rightClick, rightHint, y, contentWidth);

        float height = -y + Padding;
        root.sizeDelta = new Vector2(Width, Mathf.Max(height, 40f));

        Place(anchor);
    }

    private float PlaceRow(RectTransform row, Image icon, TMP_Text label, Sprite sprite, string hint,
        float y, float contentWidth)
    {
        bool visible = sprite != null && !string.IsNullOrEmpty(hint);
        row.gameObject.SetActive(visible);
        if (!visible) return y;

        // One gap above every row, so the stack keeps even spacing whether the
        // left row, the right row, or both are shown.
        y -= Spacing;

        float labelWidth = contentWidth - IconSize - IconGap;
        float labelHeight = Measure(label, labelWidth);
        float rowHeight = Mathf.Max(IconSize, labelHeight);

        row.anchoredPosition = new Vector2(Padding, y);
        row.sizeDelta = new Vector2(contentWidth, rowHeight);

        icon.sprite = sprite;
        icon.rectTransform.anchoredPosition = new Vector2(0f, -(rowHeight - IconSize) * 0.5f);
        icon.rectTransform.sizeDelta = new Vector2(IconSize, IconSize);

        label.text = Wrap(hint);
        label.rectTransform.sizeDelta = new Vector2(labelWidth, labelHeight);

        return y - rowHeight;
    }

    /// <summary>
    /// Places the popup beside the trigger, preferring its left side (the skill
    /// column sits on the right of the screen) and flipping to the right when
    /// there is no room. Both edges stay clamped inside the canvas.
    /// </summary>
    private void Place(RectTransform anchor)
    {
        var canvasRect = (RectTransform)canvas.transform;
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        var corners = new Vector3[4];
        anchor.GetWorldCorners(corners);

        Vector2 leftMidScreen = RectTransformUtility.WorldToScreenPoint(camera, (corners[0] + corners[1]) * 0.5f);
        Vector2 rightMidScreen = RectTransformUtility.WorldToScreenPoint(camera, (corners[2] + corners[3]) * 0.5f);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, leftMidScreen, camera, out Vector2 leftLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, rightMidScreen, camera, out Vector2 rightLocal);

        Rect bounds = canvasRect.rect;
        float halfHeight = root.sizeDelta.y * 0.5f;
        bool fitsLeft = leftLocal.x - AnchorGap - Width > bounds.xMin + ScreenMargin;

        float x = fitsLeft
            ? leftLocal.x - AnchorGap
            : rightLocal.x + AnchorGap + Width;

        root.pivot = new Vector2(fitsLeft ? 1f : 0f, 0.5f);

        float minY = bounds.yMin + halfHeight + ScreenMargin;
        float maxY = bounds.yMax - halfHeight - ScreenMargin;
        float y = Mathf.Clamp(leftLocal.y, minY, Mathf.Max(minY, maxY));

        root.anchoredPosition = new Vector2(x, y);
    }

    private static float Measure(TMP_Text label, float width)
    {
        if (string.IsNullOrEmpty(label.text)) return 0f;
        return Mathf.Ceil(label.GetPreferredValues(label.text, width, 0f).y);
    }

    private static string Wrap(string value) => string.IsNullOrEmpty(value) ? string.Empty : value.ToUpperInvariant();
}
