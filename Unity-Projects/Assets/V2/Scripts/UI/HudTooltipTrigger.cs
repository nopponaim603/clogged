using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Opens the shared <see cref="HudTooltip"/> when the pointer rests on a HUD
/// element.
///
/// The delay matters: without it every pass of the cursor across the screen
/// flashes a popup. Leaving cancels a pending show as well as an open one, so
/// a fast sweep never leaves a tooltip behind.
///
/// Content is pulled from a provider on every hover instead of being frozen at
/// build time, so a tooltip that quotes live numbers (AP cost, stock, success
/// value) never goes stale.
/// </summary>
public class HudTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string title;
    [SerializeField] private string body;
    [SerializeField] private string leftHint;
    [SerializeField] private string rightHint;
    [SerializeField] private Sprite leftIcon;
    [SerializeField] private Sprite rightIcon;
    [SerializeField] private float showDelay = 0.35f;

    private Func<HudTooltipContent> provider;
    private Coroutine pending;

    /// <summary>Fills in the content once. Called by the panel that owns the element.</summary>
    public void Configure(string tipTitle, string tipBody, string leftClickHint = null, string rightClickHint = null,
        Sprite leftClickIcon = null, Sprite rightClickIcon = null)
    {
        title = tipTitle;
        body = tipBody;
        leftHint = leftClickHint;
        rightHint = rightClickHint;
        leftIcon = leftClickIcon;
        rightIcon = rightClickIcon;
        provider = null;
    }

    /// <summary>Rebuilds the content on every hover - use this for anything with live numbers.</summary>
    public void Configure(Func<HudTooltipContent> contentProvider)
    {
        provider = contentProvider;
    }

    /// <summary>
    /// Attaches a tooltip to any HUD element. When the element has nothing the
    /// pointer can hit, a transparent <see cref="Image"/> is added so hovering
    /// works - labels and icons in this HUD are built without raycast targets.
    /// </summary>
    public static HudTooltipTrigger Attach(Component target, Func<HudTooltipContent> contentProvider)
    {
        if (target == null) return null;

        EnsureHitArea(target.gameObject);

        var trigger = target.GetComponent<HudTooltipTrigger>();
        if (trigger == null) trigger = target.gameObject.AddComponent<HudTooltipTrigger>();

        trigger.Configure(contentProvider);
        return trigger;
    }

    /// <summary>Attaches a fixed tooltip to any HUD element.</summary>
    public static HudTooltipTrigger Attach(Component target, string tipTitle, string tipBody)
    {
        if (target == null) return null;
        return Attach(target, () => new HudTooltipContent(tipTitle, tipBody));
    }

    /// <summary>
    /// Builds the content this trigger would show right now - also what the
    /// verification pass reads to prove a popup quotes live numbers.
    /// </summary>
    public HudTooltipContent Preview()
    {
        if (provider != null) return provider();

        return new HudTooltipContent(title, body)
        {
            leftHint = leftHint,
            rightHint = rightHint,
            leftIcon = leftIcon,
            rightIcon = rightIcon
        };
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Cancel();
        if (!isActiveAndEnabled) return;
        pending = StartCoroutine(ShowAfterDelay());
    }

    public void OnPointerExit(PointerEventData eventData) => Cancel();

    private void OnDisable() => Cancel();

    private void Cancel()
    {
        if (pending != null)
        {
            StopCoroutine(pending);
            pending = null;
        }

        HudTooltip.Hide();
    }

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, showDelay));
        pending = null;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) yield break;

        HudTooltipContent content = Preview();
        if (content == null) yield break;

        HudTooltip.ShowNow(canvas, (RectTransform)transform, content);
    }

    private static void EnsureHitArea(GameObject host)
    {
        foreach (var graphic in host.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.raycastTarget) return;
        }

        // Labels and icons in this HUD are built without raycast targets (so the
        // button behind them owns the click). Turning the host's own graphic into
        // the hover target keeps clicks bubbling to the parent button.
        var own = host.GetComponent<Graphic>();
        if (own != null)
        {
            own.raycastTarget = true;
            return;
        }

        var image = host.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;
    }
}
