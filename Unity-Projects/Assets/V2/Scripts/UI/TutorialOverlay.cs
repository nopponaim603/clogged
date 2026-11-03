using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The "coach mark" layer the tutorial talks through: everything except one
/// rectangle is dimmed, that rectangle is ringed in the accent colour, and a
/// card explains what the player is looking at.
///
/// It lives on its own canvas above the dice popup, so a step can point at the
/// dice and its success-value bar while they are on screen. Every graphic is
/// raycast-transparent except the optional full-screen catcher, which is only
/// enabled for "tap to continue" steps - a step that asks the player to press a
/// real control must let that click through untouched.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TutorialOverlay : MonoBehaviour
{
    private const float HolePadding = 8f;
    private const float CardGap = 22f;
    private const float CardWidth = 760f;
    private const float RingThickness = 3f;
    private const float MinHoleSize = 24f;

    private static readonly Color DimColor = new Color(0.01f, 0.015f, 0.03f, 0.78f);
    private static readonly Color RingColor = new Color(1f, 0.88f, 0.55f, 1f);
    private static readonly Color CardColor = new Color(0.06f, 0.08f, 0.12f, 0.98f);
    private static readonly Color CardBorder = new Color(1f, 0.88f, 0.55f, 0.55f);
    private static readonly Color PromptColor = new Color(0.42f, 0.85f, 0.45f, 1f);

    /// <summary>True while the overlay is showing a step.</summary>
    public bool IsShowing { get; private set; }

    /// <summary>Counts taps caught by the overlay - a test can read it to prove the catcher works.</summary>
    public int TapsSeen { get; private set; }

    /// <summary>Rect of the current hole in canvas space, for tests.</summary>
    public Rect Hole { get; private set; }

    private RectTransform canvasRect;
    private Image dimTop, dimBottom, dimLeft, dimRight;
    private Image ringTop, ringBottom, ringLeft, ringRight;
    private Image catcher;
    private RectTransform card;
    private TMP_Text message;
    private TMP_Text prompt;
    private RectTransform finale;
    private Image finaleDim;
    private TMP_Text finaleTitle;
    private Button finaleButton;
    private TMP_Text finaleLabel;
    private readonly List<Image> focusRings = new List<Image>();
    private RectTransform[] focusTargets;
    /// <summary>Earliest time a real click may advance the step; stops the click that opened the tour leaking in.</summary>
    private float tapArmedAt;

    private Action onTap;
    private Action finaleAction;

    /// <summary>
    /// Builds the layer on the root canvas. Sorting order 600 puts it above the
    /// dice popup (500), so a step can ring the dice panel and still be read.
    /// </summary>
    public static TutorialOverlay Create()
    {
        Canvas canvas = null;
        foreach (var candidate in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!candidate.isRootCanvas) continue;
            canvas = candidate;
            break;
        }
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        var go = new GameObject("TutorialOverlay", typeof(RectTransform), typeof(Canvas),
            typeof(GraphicRaycaster), typeof(CanvasGroup));
        go.transform.SetParent(canvas.transform, false);

        var own = go.GetComponent<Canvas>();
        own.overrideSorting = true;
        own.sortingOrder = 600;

        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        var overlay = go.AddComponent<TutorialOverlay>();
        overlay.Build();
        return overlay;
    }

    private void Build()
    {
        canvasRect = (RectTransform)transform.parent;

        catcher = NewImage("Catcher", transform, new Color(0f, 0f, 0f, 0f));
        UiFactory.Stretch(catcher.rectTransform);
        catcher.raycastTarget = false;

        // Four dim bands rather than a masked image: no shader, no extra mask
        // material, and the hole is exactly the rect the caller asked for.
        dimTop = NewBand("DimTop", DimColor);
        dimBottom = NewBand("DimBottom", DimColor);
        dimLeft = NewBand("DimLeft", DimColor);
        dimRight = NewBand("DimRight", DimColor);

        ringTop = NewBand("RingTop", RingColor);
        ringBottom = NewBand("RingBottom", RingColor);
        ringLeft = NewBand("RingLeft", RingColor);
        ringRight = NewBand("RingRight", RingColor);

        BuildCard();
        BuildFinale();

        Hole = Rect.zero;
        card.gameObject.SetActive(false);
        finale.gameObject.SetActive(false);
        IsShowing = false;
    }

    private void BuildCard()
    {
        var panel = NewImage("Card", transform, CardColor);
        card = panel.rectTransform;
        panel.raycastTarget = false;
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0f);
        card.sizeDelta = new Vector2(CardWidth, 120f);

        var border = panel.gameObject.AddComponent<Outline>();
        border.effectColor = CardBorder;
        border.effectDistance = new Vector2(2f, -2f);
        border.useGraphicAlpha = false;

        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(26, 26, 20, 18);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        message = UiFactory.CreateText("Message", card, "", 26f, Color.white);
        message.alignment = TextAlignmentOptions.Top;
        message.textWrappingMode = TextWrappingModes.Normal;
        message.enableAutoSizing = false;
        var messageLayout = message.gameObject.AddComponent<LayoutElement>();
        messageLayout.flexibleWidth = 1f;
        TmpFont.Apply(message);

        prompt = UiFactory.CreateText("Prompt", card, "CLICK TO CONTINUE", 20f, PromptColor);
        prompt.alignment = TextAlignmentOptions.Center;
        TmpFont.Apply(prompt);
    }

    private void BuildFinale()
    {
        finaleDim = NewImage("FinaleDim", transform, new Color(0.01f, 0.015f, 0.03f, 0.86f));
        UiFactory.Stretch(finaleDim.rectTransform);
        finaleDim.raycastTarget = true;

        var panel = NewImage("Finale", transform, CardColor);
        finale = panel.rectTransform;
        panel.raycastTarget = true;
        UiFactory.Center(finale, Vector2.zero, new Vector2(820f, 340f));

        var border = panel.gameObject.AddComponent<Outline>();
        border.effectColor = CardBorder;
        border.effectDistance = new Vector2(2f, -2f);
        border.useGraphicAlpha = false;

        finaleTitle = UiFactory.CreateText("FinaleTitle", finale, "", 38f, UiFactory.Accent);
        UiFactory.Center(finaleTitle.rectTransform, new Vector2(0f, 62f), new Vector2(740f, 130f));
        finaleTitle.alignment = TextAlignmentOptions.Center;
        finaleTitle.textWrappingMode = TextWrappingModes.Normal;
        finaleTitle.enableAutoSizing = false;
        TmpFont.Apply(finaleTitle);

        finaleButton = UiFactory.CreateButton(finale, "FinaleButton", "RETURN TO GAME",
            new Vector2(420f, 64f), HandleFinalePressed);
        UiFactory.Center((RectTransform)finaleButton.transform, new Vector2(0f, -100f), new Vector2(420f, 64f));
        finaleLabel = finaleButton.GetComponentInChildren<TMP_Text>();
    }

    // ------------------------------------------------------------- steps

    /// <summary>
    /// Shows one coach mark. <paramref name="target"/> may be null, in which case
    /// nothing is punched out and the card sits in the middle of the screen.
    /// <paramref name="catchTap"/> installs the full-screen catcher and calls
    /// <paramref name="onTapped"/> when the player clicks anywhere.
    /// </summary>
    public void ShowStep(string text, string promptText, RectTransform target, bool catchTap, Action onTapped,
        RectTransform[] focus = null)
    {
        IsShowing = true;
        TapsSeen = 0;
        onTap = catchTap ? onTapped : null;
        focusTargets = focus;

        // The press that opened the tutorial (or the previous card) can still be
        // in flight; a short grace keeps it from skipping the first step.
        tapArmedAt = Time.unscaledTime + 0.2f;

        catcher.raycastTarget = catchTap;

        card.gameObject.SetActive(true);
        card.SetAsLastSibling();
        finale.gameObject.SetActive(false);
        finaleDim.gameObject.SetActive(false);

        message.text = text ?? "";
        prompt.text = promptText ?? "";
        prompt.gameObject.SetActive(!string.IsNullOrEmpty(promptText));

        PlaceCard(target);
    }

    /// <summary>
    /// Moves the ring onto a different control without touching the card copy or
    /// the tap state. A step that points at something which only appears later
    /// (the dice popup's CONTINUE, the queued-round number) calls this as soon as
    /// the control it wants exists.
    /// </summary>
    public void Retarget(RectTransform target)
    {
        if (!IsShowing) return;

        PlaceCard(target);
    }

    /// <summary>Hides the coach mark but leaves the layer alive.</summary>
    public void HideStep()
    {
        IsShowing = false;
        onTap = null;
        catcher.raycastTarget = false;
        card.gameObject.SetActive(false);

        foreach (var band in new[] { dimTop, dimBottom, dimLeft, dimRight, ringTop, ringBottom, ringLeft, ringRight })
        {
            if (band != null) band.enabled = false;
        }

        foreach (var ring in focusRings) ring.enabled = false;
        focusTargets = null;
    }

    /// <summary>
    /// The closing card: a full-screen dim plus one button. Used when the script
    /// has run out of steps and the player needs a way out of the tutorial scene.
    /// </summary>
    public void ShowFinale(string title, string buttonLabel, Action onPressed)
    {
        HideStep();

        finaleDim.gameObject.SetActive(true);
        finaleDim.transform.SetAsLastSibling();

        finale.gameObject.SetActive(true);
        finale.SetAsLastSibling();

        finaleTitle.text = title ?? "";
        if (finaleLabel != null) finaleLabel.text = buttonLabel ?? "CONTINUE";
        finaleAction = onPressed;
    }

    public void HideFinale()
    {
        finale.gameObject.SetActive(false);
        finaleDim.gameObject.SetActive(false);
        finaleAction = null;
    }

    /// <summary>Presses the finale button from code (tests, and the click path itself).</summary>
    public void PressFinale()
    {
        var callback = finaleAction;
        finaleAction = null;
        callback?.Invoke();
    }

    private void HandleFinalePressed() => PressFinale();

    // ------------------------------------------------------------ clicks

    private void Update()
    {
        if (onTap == null) return;
        if (Time.unscaledTime < tapArmedAt) return;

        // The project is Input-System-only, so the legacy Input class never sees
        // the click - read the device directly, the same way RightClickCancel does.
        var mouse = Mouse.current;
        if (mouse == null) return;
        if (!mouse.leftButton.wasPressedThisFrame && !mouse.rightButton.wasPressedThisFrame) return;
        if (!PointerInsideCanvas()) return;

        TapsSeen++;
        var callback = onTap;
        onTap = null;
        catcher.raycastTarget = false;
        callback?.Invoke();
    }

    /// <summary>
    /// The catcher covers the whole screen, so a tap inside the canvas is a tap
    /// on it. Checking the rect (rather than the event system) keeps the gesture
    /// working while another overlay owns the raycast.
    /// </summary>
    private bool PointerInsideCanvas()
    {
        return RectTransformUtility.RectangleContainsScreenPoint(canvasRect, Input.mousePosition, null);
    }

    /// <summary>Feeds a tap in directly - the editor cannot synthesise mouse input for tests.</summary>
    public void Tap()
    {
        if (onTap == null) return;

        TapsSeen++;
        var callback = onTap;
        onTap = null;
        catcher.raycastTarget = false;
        callback?.Invoke();
    }

    // ------------------------------------------------------------ layout

    private Image NewImage(string objectName, Transform parent, Color color)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;

        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Image NewBand(string objectName, Color color)
    {
        var image = NewImage(objectName, transform, color);
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        image.rectTransform.pivot = new Vector2(0f, 0f);
        image.enabled = false;
        return image;
    }

    /// <summary>Punches the hole and rings it.</summary>
    private void SetHole(Rect hole, bool visible)
    {
        foreach (var band in new[] { dimTop, dimBottom, dimLeft, dimRight, ringTop, ringBottom, ringLeft, ringRight })
        {
            if (band != null) band.enabled = visible;
        }
        if (!visible) return;

        Rect area = canvasRect.rect;
        float left = Mathf.Clamp(hole.xMin, area.xMin, area.xMax);
        float right = Mathf.Clamp(hole.xMax, area.xMin, area.xMax);
        float bottom = Mathf.Clamp(hole.yMin, area.yMin, area.yMax);
        float top = Mathf.Clamp(hole.yMax, area.yMin, area.yMax);

        Hole = Rect.MinMaxRect(left, bottom, right, top);

        // Bands cover everything outside the hole; between them they read as one
        // continuous dim with a window in it.
        PlaceRect(dimTop, area.xMin, top, area.xMax, area.yMax);
        PlaceRect(dimBottom, area.xMin, area.yMin, area.xMax, bottom);
        PlaceRect(dimLeft, area.xMin, bottom, left, top);
        PlaceRect(dimRight, right, bottom, area.xMax, top);

        // Ring: four bars just outside the hole, so the hole itself stays clean.
        PlaceRect(ringTop, left, top, right, top + RingThickness);
        PlaceRect(ringBottom, left, bottom - RingThickness, right, bottom);
        PlaceRect(ringLeft, left - RingThickness, bottom, left, top);
        PlaceRect(ringRight, right, bottom, right + RingThickness, top);

        DrawFocusRings();
    }

    /// <summary>
    /// Rings a few controls INSIDE the open hole - the two badges on a card, say -
    /// so a step can keep a whole card readable and still point at the part that
    /// matters. Rings live in a pool: one step needs none, the next needs eight.
    /// </summary>
    private void DrawFocusRings()
    {
        int needed = focusTargets != null ? focusTargets.Length * 4 : 0;

        while (focusRings.Count < needed) focusRings.Add(NewBand("FocusRing", RingColor));
        for (int i = 0; i < focusRings.Count; i++) focusRings[i].enabled = i < needed;

        if (needed == 0) return;

        for (int i = 0; i < focusTargets.Length; i++)
        {
            var target = focusTargets[i];
            if (target == null) continue;

            Rect r = LocalRect(target);
            r = Rect.MinMaxRect(r.xMin - 4f, r.yMin - 4f, r.xMax + 4f, r.yMax + 4f);

            PlaceRect(focusRings[i * 4 + 0], r.xMin, r.yMax, r.xMax, r.yMax + RingThickness);
            PlaceRect(focusRings[i * 4 + 1], r.xMin, r.yMin - RingThickness, r.xMax, r.yMin);
            PlaceRect(focusRings[i * 4 + 2], r.xMin - RingThickness, r.yMin, r.xMin, r.yMax);
            PlaceRect(focusRings[i * 4 + 3], r.xMax, r.yMin, r.xMax + RingThickness, r.yMax);
        }
    }

    private static void PlaceRect(Image image, float xMin, float yMin, float xMax, float yMax)
    {
        var rect = image.rectTransform;
        rect.anchoredPosition = new Vector2(xMin, yMin);
        rect.sizeDelta = new Vector2(Mathf.Max(0f, xMax - xMin), Mathf.Max(0f, yMax - yMin));
    }

    /// <summary>Rect of a world-space UGUI control, in this canvas's local space.</summary>
    private Rect LocalRect(RectTransform target)
    {
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Vector2 min = canvasRect.InverseTransformPoint(corners[0]);
        Vector2 max = canvasRect.InverseTransformPoint(corners[2]);

        return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y),
            Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
    }

    /// <summary>
    /// Rings the target and places the card on whichever side of it has room,
    /// so a step about the status bar does not bury the bar in its own card.
    /// </summary>
    private void PlaceCard(RectTransform target)
    {
        Rect area = canvasRect.rect;

        bool hasTarget = target != null && target.gameObject.activeInHierarchy;
        Rect hole;

        if (hasTarget)
        {
            Rect raw = LocalRect(target);
            hole = Rect.MinMaxRect(
                raw.xMin - HolePadding, raw.yMin - HolePadding,
                raw.xMax + HolePadding, raw.yMax + HolePadding);

            if (hole.width < MinHoleSize || hole.height < MinHoleSize)
            {
                Vector2 mid = hole.center;
                hole = Rect.MinMaxRect(mid.x - MinHoleSize * 0.5f, mid.y - MinHoleSize * 0.5f,
                    mid.x + MinHoleSize * 0.5f, mid.y + MinHoleSize * 0.5f);
            }
        }
        else
        {
            hole = Rect.MinMaxRect(-1f, area.center.y - 1f, 1f, area.center.y + 1f);
        }

        SetHole(hole, hasTarget);

        // The card measures itself before it can be placed against an edge.
        LayoutRebuilder.ForceRebuildLayoutImmediate(card);
        float height = Mathf.Max(80f, card.rect.height);
        float width = card.rect.width;

        float xMin = area.xMin + width * 0.5f + 12f;
        float xMax = area.xMax - width * 0.5f - 12f;
        float x = Mathf.Clamp(hole.center.x, xMin, Mathf.Max(xMin, xMax));

        if (!hasTarget)
        {
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = new Vector2(x, area.center.y);
            return;
        }

        float aboveY = hole.yMax + CardGap;
        if (aboveY + height <= area.yMax - 10f)
        {
            card.pivot = new Vector2(0.5f, 0f);
            card.anchoredPosition = new Vector2(x, aboveY);
            return;
        }

        float belowY = hole.yMin - CardGap;
        if (belowY - height >= area.yMin + 10f)
        {
            card.pivot = new Vector2(0.5f, 1f);
            card.anchoredPosition = new Vector2(x, belowY);
            return;
        }

        // No room either side: centre vertically, which is only reachable for a
        // target that fills the screen.
        card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = new Vector2(x, area.center.y);
    }
}
