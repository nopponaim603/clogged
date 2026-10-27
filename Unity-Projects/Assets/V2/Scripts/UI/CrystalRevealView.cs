using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Centre-screen reveal for the moment the run's Core Items pair up: the box
/// opens and the crystal inside is shown. The HUD keeps showing the box (now
/// drawn open), so this overlay is the only place the crystal appears.
///
/// Everything is built in code so the scene only needs one empty GameObject
/// under the canvas. It polls <see cref="RunResources"/> for the closed-to-open
/// edge, the same way the rest of the HUD polls its sources.
/// </summary>
public class CrystalRevealView : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Crystal art per Core Item pair. The one matching RunResources.Variant is shown.")]
    [SerializeField] private Sprite orangeCrystal;
    [SerializeField] private Sprite yellowCrystal;

    [SerializeField] private string caption = "THE BOX IS OPEN";
    [SerializeField] private Vector2 crystalSize = new Vector2(192f, 192f);
    [SerializeField] private Color backdropColor = new Color(0.04f, 0.05f, 0.08f, 0.82f);
    [SerializeField] private Color captionColor = new Color(1f, 0.9f, 0.55f, 1f);
    [SerializeField] private float fadeSeconds = 0.35f;

    private CanvasGroup group;
    private Image crystalImage;
    private TMP_Text captionLabel;
    private bool wasOpen;
    private bool built;
    private float targetAlpha;

    /// <summary>True while the reveal is on screen (after the fade-in starts).</summary>
    public bool IsShowing { get; private set; }

    /// <summary>Crystal currently painted, for tests and debugging.</summary>
    public Sprite CurrentCrystal => crystalImage != null ? crystalImage.sprite : null;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();

        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        IsShowing = false;

        // A run that already holds both pieces (reload during the win screen)
        // must not pop the reveal again.
        wasOpen = RunResources.Instance != null && RunResources.Instance.HasBothCoreItems;
    }

    private void Update()
    {
        var res = RunResources.Instance;
        if (res != null)
        {
            bool open = res.HasBothCoreItems;
            if (open && !wasOpen) Show();
            wasOpen = open;
        }

        if (!built || Mathf.Approximately(group.alpha, targetAlpha)) return;

        // The reveal has no coroutine on purpose: a paused editor (or a test in
        // a single frame) still settles, and nothing else has to be stopped.
        float step = fadeSeconds > 0f ? Time.unscaledDeltaTime / fadeSeconds : 1f;
        group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, step);
    }

    /// <summary>Shows the crystal for the current pair. Idempotent while already open.</summary>
    public void Show()
    {
        Build();

        Sprite art = RunResources.Instance != null && RunResources.Instance.Variant == CoreItemVariant.Yellow
            ? yellowCrystal
            : orangeCrystal;
        if (crystalImage != null) crystalImage.sprite = art;
        if (captionLabel != null) captionLabel.text = caption;

        IsShowing = true;
        targetAlpha = 1f;
        group.blocksRaycasts = true;
    }

    /// <summary>Fades the reveal out; the run stays won, only the art hides.</summary>
    public void Hide(bool instant = false)
    {
        IsShowing = false;
        targetAlpha = 0f;
        if (group == null) return;

        group.blocksRaycasts = false;
        if (instant) group.alpha = 0f;
    }

    /// <summary>Any click dismisses it - the win is already logged.</summary>
    public void OnPointerClick(PointerEventData eventData) => Hide();

    private void Build()
    {
        if (built) return;
        built = true;

        var root = (RectTransform)transform;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = Vector2.zero;

        // Dim the whole screen so the crystal reads on its own, and swallow
        // clicks so the board behind cannot be used while it is up.
        var backdrop = NewImage("Backdrop", root);
        backdrop.color = backdropColor;
        backdrop.raycastTarget = true;
        Stretch((RectTransform)backdrop.transform, 0f);

        crystalImage = NewImage("Crystal", root);
        crystalImage.preserveAspect = true;
        crystalImage.raycastTarget = false;

        var crystalRect = (RectTransform)crystalImage.transform;
        crystalRect.anchorMin = new Vector2(0.5f, 0.5f);
        crystalRect.anchorMax = new Vector2(0.5f, 0.5f);
        crystalRect.pivot = new Vector2(0.5f, 0.5f);
        crystalRect.anchoredPosition = new Vector2(0f, 24f);
        crystalRect.sizeDelta = crystalSize;

        captionLabel = TmpOutlineUtil.CreateLabel(root, "Caption", 640f, 40f);
        captionLabel.text = caption;
        captionLabel.color = captionColor;
        captionLabel.fontSizeMax = 26f;

        var captionRect = (RectTransform)captionLabel.transform;
        captionRect.anchorMin = new Vector2(0.5f, 0.5f);
        captionRect.anchorMax = new Vector2(0.5f, 0.5f);
        captionRect.pivot = new Vector2(0.5f, 1f);
        captionRect.anchoredPosition = new Vector2(0f, -24f - crystalSize.y * 0.5f);
        captionRect.sizeDelta = new Vector2(640f, 40f);
    }

    private static Image NewImage(string objectName, RectTransform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;

        var image = go.GetComponent<Image>();
        image.type = Image.Type.Simple;
        image.color = Color.white;
        return image;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(-inset * 2f, -inset * 2f);
    }
}
