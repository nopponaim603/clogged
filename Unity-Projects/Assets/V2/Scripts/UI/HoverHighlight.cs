using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Says "this area is clickable" before the player commits to a click: a warm
/// wash fades in while the pointer is over the control, and a quiet tick plays
/// as the pointer arrives.
///
/// Two ways to paint it, because controls differ:
///   * no <see cref="target"/> - a mask is laid over the control's own graphic
///     (this is the uGUI Button case),
///   * with a <see cref="target"/> - that graphic is lightened in place, which
///     is what a card whose art is drawn in code needs.
///
/// Cards that drive their own colours from game state (the unit cards, the
/// dungeon cards) do this themselves rather than layering a mask on top.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class HoverHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Wash laid over the control while hovered. Alpha is the resting strength.")]
    [SerializeField] private Color tint = new Color(1f, 0.94f, 0.70f, 0.16f);

    [Tooltip("How fast the wash fades in and out. Higher is snappier.")]
    [SerializeField] private float fadeSpeed = 12f;

    [SerializeField] private bool playSound = true;

    [Tooltip("Lighten this graphic instead of drawing a mask over the control.")]
    [SerializeField] private Graphic target;

    private const string MaskName = "HoverMask";

    // Above this a control is a whole panel rather than a click target, and a
    // wash would read as the screen flickering.
    private const float MaxMaskWidth = 1400f;
    private const float MaxMaskHeight = 700f;

    private Image mask;
    private Graphic tinted;
    private Color tintedBase;
    private Selectable gate;
    private bool hovered;
    private float amount;

    /// <summary>Current wash strength, 0 resting to 1 fully hovered. Exposed so tests can wait on it.</summary>
    public float Amount => amount;

    /// <summary>
    /// Adds the component unless it is already there. Safe to call on every
    /// button a factory builds.
    /// </summary>
    public static HoverHighlight Attach(GameObject owner)
    {
        if (owner == null) return null;

        var hover = owner.GetComponent<HoverHighlight>();
        if (hover == null) hover = owner.AddComponent<HoverHighlight>();
        return hover;
    }

    /// <summary>Asks for the lighten-in-place mode and names the graphic to lighten.</summary>
    public HoverHighlight SetTarget(Graphic graphic)
    {
        target = graphic;
        return this;
    }

    private void Awake()
    {
        gate = GetComponent<Selectable>();

        if (target != null)
        {
            tinted = target;
            tintedBase = target.color;
            return;
        }

        var rect = (RectTransform)transform;
        if (rect.rect.width > MaxMaskWidth || rect.rect.height > MaxMaskHeight) return;

        BuildMask();
    }

    private void OnEnable()
    {
        // A control that was hovered when it disappeared must not stay lit.
        hovered = false;
        if (amount <= 0f) return;

        amount = 0f;
        Apply();
    }

    private void OnDisable()
    {
        hovered = false;
        amount = 0f;
        if (mask != null) mask.enabled = false;
    }

    private void BuildMask()
    {
        if (mask != null) return;

        var go = new GameObject(MaskName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        mask = go.GetComponent<Image>();
        mask.raycastTarget = false;
        mask.enabled = false;

        // Behind every authored child so the label and art stay clean, but above
        // the control's own graphic (a child always draws over its parent).
        rect.SetAsFirstSibling();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (gate != null && !gate.IsInteractable()) return;
        if (gate == null && !isActiveAndEnabled) return;

        hovered = true;
        if (playSound) AudioDirector.Instance?.PlayHover();
    }

    public void OnPointerExit(PointerEventData eventData) => hovered = false;

    private void Update()
    {
        float wanted = hovered ? 1f : 0f;
        if (Mathf.Approximately(amount, wanted)) return;

        amount = Mathf.MoveTowards(amount, wanted, fadeSpeed * Time.unscaledDeltaTime);
        Apply();
    }

    private void Apply()
    {
        if (tinted != null)
        {
            // The source colour can change underneath us (a card repainted red
            // because the unit got hurt), so it is re-read while at rest.
            if (amount <= 0f) tintedBase = tinted.color;

            Color lifted = Color.Lerp(tintedBase, Color.white, 0.22f);

            // A target that is already white (the day plate) has no room to
            // lighten, so the wash also lifts its alpha.
            lifted.a = Mathf.Min(1f, tintedBase.a + 0.22f);

            tinted.color = Color.Lerp(tintedBase, lifted, amount);
            return;
        }

        if (mask == null) return;

        Color color = tint;
        color.a *= amount;
        mask.color = color;
        mask.enabled = amount > 0.001f;
    }
}
