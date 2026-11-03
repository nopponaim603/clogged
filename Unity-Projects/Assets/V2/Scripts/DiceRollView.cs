using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Popup that reveals one gather roll with two tumbling percentile dice
/// (a "tens" die 00-90 and a "ones" die 0-9, like a real D100 set), then a
/// bar showing where the roll lands (Hard Success / Regular / Failure /
/// Fumble). Purely visual - GatherDispatchController has already applied the
/// outcome by the time Show() is called.
///
/// Builds its whole UI in code, including a generated rounded-square sprite
/// for the dice, so no prefab, sprite or layout setup is needed. Put this
/// component on an empty GameObject under your Canvas. Swap in real dice art
/// later by replacing the Images inside NewDie().
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DiceRollView : MonoBehaviour
{
    [Header("Timing (unscaled seconds)")]
    [SerializeField] private float tumbleDuration = 1.5f;
    [SerializeField] private float settleDuration = 0.25f;
    [SerializeField] private float markerDuration = 0.8f;

    [Header("Dice feel")]
    [SerializeField] private float bounceHeight = 70f;

    private const float BarWidth = 560f;
    private const float BarHeight = 36f;
    private const float DieSize = 116f;
    private const float DieSpacing = 90f; // each die sits this far left/right of centre

    private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.7f);
    private static readonly Color PanelColor = new Color(0.12f, 0.12f, 0.16f, 0.98f);
    private static readonly Color DieFaceColor = new Color(0.96f, 0.94f, 0.90f);
    private static readonly Color DieEdgeColor = new Color(0.25f, 0.25f, 0.30f);
    private static readonly Color DieTextColor = new Color(0.12f, 0.12f, 0.15f);
    private static readonly Color HardColor = new Color(0.35f, 0.80f, 0.40f);
    private static readonly Color RegularColor = new Color(0.85f, 0.75f, 0.30f);
    private static readonly Color FailureColor = new Color(0.80f, 0.45f, 0.30f);
    private static readonly Color FumbleColor = new Color(0.60f, 0.15f, 0.15f);

    private class Die
    {
        public RectTransform rect;
        public Image face;
        public TMP_Text text;
        public Vector2 basePos;
        public float angle;
        public float angularVelocity;
        public float bounceFrequency;
    }

    private GameObject overlay;
    private CanvasGroup overlayGroup;
    private TMP_Text titleText;
    private TMP_Text detailText;
    private TMP_Text legendText;
    private TMP_Text tierText;
    private TMP_Text summaryText;
    private RectTransform bar;
    private RectTransform marker;
    private Button continueButton;
    private Die tensDie;
    private Die onesDie;

    private readonly List<GameObject> segments = new List<GameObject>();
    private Action onClosed;
    private GatherDispatchResult current;
    private bool animating;
    private bool skipRequested;

    private static Sprite roundedSprite;

    /// <summary>The live popup, created on demand by <see cref="EnsureExists"/>.</summary>
    public static DiceRollView Instance { get; private set; }

    public bool IsShowing => overlayGroup != null && overlayGroup.alpha > 0.5f;

    /// <summary>
    /// True once the dice have stopped and the result is on screen. The tutorial
    /// uses it to hold its card back until the roll has actually played out.
    /// </summary>
    public bool Settled { get; private set; }

    /// <summary>
    /// Success value the bar is currently drawn for - the number its bands and
    /// legend are built from, so a test can compare them against the resolver.
    /// </summary>
    public int CurrentSuccessValue => current != null ? current.successValue : 0;

    /// <summary>
    /// Puts the popup under the root canvas, once per scene. Called from
    /// <see cref="RunBootstrap"/> so any controller can simply call
    /// <c>DiceRollView.Instance?.Show(...)</c> without a scene reference.
    /// </summary>
    public static DiceRollView EnsureExists()
    {
        if (Instance != null) return Instance;

        Canvas canvas = null;
        foreach (var candidate in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!candidate.isRootCanvas) continue;
            canvas = candidate;
            break;
        }
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        var go = new GameObject("DiceRollView", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster),
            typeof(DiceRollView));
        go.transform.SetParent(canvas.transform, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // The map lives on its own canvas, so a popup that merely sat in the HUD
        // canvas could still lose the draw order and the raycast to it. Own
        // canvas + a high sorting order makes this a true modal layer.
        var own = go.GetComponent<Canvas>();
        own.overrideSorting = true;
        own.sortingOrder = 500;

        go.transform.SetAsLastSibling();
        return Instance;
    }

    private void Awake()
    {
        Instance = this;

        var rect = (RectTransform)transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        BuildUi();
        SetVisible(false);
    }

    /// <summary>Plays the reveal for a resolved trip. closedCallback fires when the player presses Continue.</summary>
    public void Show(GatherDispatchResult result, Action closedCallback)
    {
        if (result == null)
        {
            closedCallback?.Invoke();
            return;
        }

        StopAllCoroutines();
        onClosed = closedCallback;
        current = result;
        Settled = false;

        transform.SetAsLastSibling();
        SetVisible(true);

        PrepareFor(result);
        StartCoroutine(PlayRoutine(result));
    }

    /// <summary>
    /// Fades the popup instead of deactivating it. A GameObject that is inactive
    /// while the canvas batches gets <c>graphic.depth == -1</c>, and Graphics
    /// Raycaster skips those, so re-activating it inside one frame left the
    /// popup visible but unclickable.
    /// </summary>
    private void SetVisible(bool visible)
    {
        if (overlayGroup == null) return;

        overlayGroup.alpha = visible ? 1f : 0f;
        overlayGroup.blocksRaycasts = visible;
        overlayGroup.interactable = visible;
    }

    // ---------------------------------------------------------------- flow

    private void PrepareFor(GatherDispatchResult r)
    {
        titleText.text = $"{r.zone} Zone";
        detailText.text = r.successValue >= 100
            ? $"Success value {r.successValue}  -  always a success unless the dice fumble"
            : $"Success value {r.successValue}  -  roll {r.successValue} or less to succeed";
        tierText.text = "";
        summaryText.text = "";
        continueButton.gameObject.SetActive(false);

        foreach (var die in new[] { tensDie, onesDie })
        {
            die.text.text = "?";
            die.face.color = DieFaceColor;
            SnapDie(die);
        }

        BuildBar(r.successValue);
        marker.anchoredPosition = Vector2.zero;
    }

    private IEnumerator PlayRoutine(GatherDispatchResult r)
    {
        animating = true;
        skipRequested = false;

        SplitRoll(r.rollValue, out int finalTens, out int finalOnes);

        // Each die gets its own spin direction/speed and bounce rhythm so they don't move in lockstep.
        foreach (var die in new[] { tensDie, onesDie })
        {
            die.angle = 0f;
            die.angularVelocity = UnityEngine.Random.Range(520f, 820f) * (UnityEngine.Random.value < 0.5f ? -1f : 1f);
            die.bounceFrequency = UnityEngine.Random.Range(4.5f, 6.5f);
        }

        // Phase 1: tumble. Spin and bounce both fade out over the duration; faces flicker, slowing down.
        float t = 0f;
        float flickerTimer = 0f;
        while (t < tumbleDuration && !skipRequested)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            float p = Mathf.Clamp01(t / tumbleDuration);
            float decay = 1f - p;

            foreach (var die in new[] { tensDie, onesDie })
            {
                die.angle += die.angularVelocity * decay * dt;
                float bounce = Mathf.Abs(Mathf.Sin(t * die.bounceFrequency * Mathf.PI)) * bounceHeight * decay;
                float squash = 1f + 0.08f * Mathf.Sin(t * 14f) * decay;

                die.rect.anchoredPosition = die.basePos + new Vector2(0f, bounce);
                die.rect.localRotation = Quaternion.Euler(0f, 0f, die.angle);
                die.rect.localScale = new Vector3(squash, squash, 1f);
            }

            flickerTimer -= dt;
            if (flickerTimer <= 0f)
            {
                tensDie.text.text = TensLabel(UnityEngine.Random.Range(0, 10) * 10);
                onesDie.text.text = UnityEngine.Random.Range(0, 10).ToString();
                flickerTimer = Mathf.Lerp(0.04f, 0.2f, p * p);
            }
            yield return null;
        }

        tensDie.text.text = TensLabel(finalTens);
        onesDie.text.text = finalOnes.ToString();

        // Phase 2: settle - ease each die to the nearest upright angle and onto the table.
        float startAngleTens = tensDie.angle;
        float startAngleOnes = onesDie.angle;
        float endAngleTens = Mathf.Round(startAngleTens / 360f) * 360f;
        float endAngleOnes = Mathf.Round(startAngleOnes / 360f) * 360f;
        Vector2 startPosTens = tensDie.rect.anchoredPosition;
        Vector2 startPosOnes = onesDie.rect.anchoredPosition;

        float s = 0f;
        while (s < settleDuration && !skipRequested)
        {
            s += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(s / settleDuration);
            float eased = p * p * (3f - 2f * p);

            tensDie.rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startAngleTens, endAngleTens, eased));
            onesDie.rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startAngleOnes, endAngleOnes, eased));
            tensDie.rect.anchoredPosition = Vector2.Lerp(startPosTens, tensDie.basePos, eased);
            onesDie.rect.anchoredPosition = Vector2.Lerp(startPosOnes, onesDie.basePos, eased);
            yield return null;
        }
        SnapDie(tensDie);
        SnapDie(onesDie);

        // Phase 3: the marker slides along the bar to where the roll landed.
        float targetX = (r.rollValue - 0.5f) / 100f * BarWidth;
        float m = 0f;
        while (m < markerDuration && !skipRequested)
        {
            m += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(m / markerDuration);
            float eased = 1f - (1f - p) * (1f - p); // ease out
            marker.anchoredPosition = new Vector2(targetX * eased, 0f);
            yield return null;
        }
        marker.anchoredPosition = new Vector2(targetX, 0f);

        ApplyFinalState(r);
    }

    /// <summary>
    /// Puts the popup straight on the result: dice showing the final numbers,
    /// marker parked, summary and CONTINUE visible. The tail of
    /// <see cref="PlayRoutine"/> uses it too, so skipping and finishing agree.
    /// </summary>
    private void ApplyFinalState(GatherDispatchResult r)
    {
        SplitRoll(r.rollValue, out int tens, out int ones);
        tensDie.text.text = TensLabel(tens);
        onesDie.text.text = ones.ToString();
        SnapDie(tensDie);
        SnapDie(onesDie);

        float targetX = (r.rollValue - 0.5f) / 100f * BarWidth;
        marker.anchoredPosition = new Vector2(targetX, 0f);

        Color tierColor = ColorForTier(r.tier);
        Color dieTint = Color.Lerp(DieFaceColor, tierColor, 0.55f);
        tensDie.face.color = dieTint;
        onesDie.face.color = dieTint;

        tierText.text = $"{r.rollValue}  -  {LabelForTier(r.tier)}";
        tierText.color = tierColor;
        summaryText.text = BuildSummary(r);

        continueButton.gameObject.SetActive(true);
        animating = false;
        Settled = true;

        // The roll is the game's one suspense beat, so the dice get their own
        // sting the moment they land: a good tier chirps, a bad one thuds.
        if (r.tier == GatherResultTier.Regular || r.tier == GatherResultTier.HardSuccess)
        {
            AudioDirector.Instance?.PlaySuccess();
        }
        else
        {
            AudioDirector.Instance?.PlayFail();
        }
    }

    /// <summary>
    /// Ends the animation early and shows the result. Used by the test suite,
    /// which runs in a single frame, and available to any caller that wants the
    /// numbers without watching the dice tumble.
    /// </summary>
    public void RevealImmediately()
    {
        if (!IsShowing || current == null) return;

        StopAllCoroutines();
        skipRequested = true;
        ApplyFinalState(current);
    }

    private static void SnapDie(Die die)
    {
        die.angle = 0f;
        die.rect.anchoredPosition = die.basePos;
        die.rect.localRotation = Quaternion.identity;
        die.rect.localScale = Vector3.one;
    }

    /// <summary>
    /// Splits a 1-100 roll the way a real percentile set reads: tens die
    /// 00-90 plus ones die 0-9, with 00 + 0 meaning 100.
    /// </summary>
    private static void SplitRoll(int roll, out int tens, out int ones)
    {
        if (roll >= 100)
        {
            tens = 0;
            ones = 0;
            return;
        }
        tens = (roll / 10) * 10;
        ones = roll % 10;
    }

    private static string TensLabel(int tens)
    {
        return tens == 0 ? "00" : tens.ToString();
    }

    private void OnSkipClicked()
    {
        if (animating) skipRequested = true;
    }

    private void OnContinueClicked()
    {
        if (animating) return;

        SetVisible(false);
        current = null;
        Settled = false;
        var callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }

    // ------------------------------------------------------------- content

    private static string LabelForTier(GatherResultTier tier)
    {
        switch (tier)
        {
            case GatherResultTier.HardSuccess: return "HARD SUCCESS!";
            case GatherResultTier.Regular: return "SUCCESS";
            case GatherResultTier.Failure: return "FAILURE";
            case GatherResultTier.Fumble: return "FUMBLE!";
            default: return "";
        }
    }

    private static Color ColorForTier(GatherResultTier tier)
    {
        switch (tier)
        {
            case GatherResultTier.HardSuccess: return HardColor;
            case GatherResultTier.Regular: return RegularColor;
            case GatherResultTier.Failure: return FailureColor;
            case GatherResultTier.Fumble: return FumbleColor;
            default: return Color.white;
        }
    }

    private static string BuildSummary(GatherDispatchResult r)
    {
        var lines = new List<string>();

        if (r.mainLootGranted.HasValue)
        {
            string loot = $"{r.mainLootGranted.Value.type} x{r.mainLootGranted.Value.amount}";
            if (r.bonusLootGranted.HasValue)
            {
                loot += $"  +  bonus {r.bonusLootGranted.Value.type} x{r.bonusLootGranted.Value.amount}";
            }
            lines.Add("Got " + loot);
        }
        else
        {
            lines.Add("Got nothing");
        }

        if (r.hurtUnits.Count == 1) lines.Add($"{r.hurtUnits[0].unitName} was hurt");
        else if (r.hurtUnits.Count > 1) lines.Add($"All {r.hurtUnits.Count} units were hurt");

        if (r.traitRecipients.Count > 1)
        {
            lines.Add($"Everyone gained a {r.traitPolarity} trait");
        }
        else if (r.traitAwarded && r.traitRecipient != null)
        {
            lines.Add($"{r.traitRecipient.unitName} gained a {r.traitPolarity} trait: {r.traitName}");
        }
        if (r.coreItemsObtained != null)
        {
            foreach (var kind in r.coreItemsObtained)
            {
                lines.Add(kind == CoreItemKind.A ? "Found the sealed box!" : "Found the key!");
            }
        }

        if (r.riskReduced > 0)
        {
            int risk = RunResources.Instance != null ? RunResources.Instance.Risk : 0;
            lines.Add($"The red fog thins: risk -{r.riskReduced} (now {risk}/{RunResources.MaxRisk})");
        }

        return string.Join("\n", lines);
    }

    // ----------------------------------------------------------- the bar

    /// <summary>
    /// Segments mirror GatherResolver.Roll exactly: fumble range is checked
    /// first (96-100 if success value &lt; 50, else only 100), then
    /// Hard Success (&lt;= <see cref="GatherResolver.HardSuccessThreshold"/>,
    /// the top third), Regular (&lt;= value), Failure.
    /// </summary>
    private void BuildBar(int successValue)
    {
        foreach (var seg in segments) Destroy(seg);
        segments.Clear();

        int fumbleStart = successValue < 50 ? 96 : 100;
        int hardEnd = Mathf.Clamp(GatherResolver.HardSuccessThreshold(successValue), 0, fumbleStart - 1);
        int regularEnd = Mathf.Clamp(successValue, hardEnd, fumbleStart - 1);
        int failureEnd = fumbleStart - 1;

        AddSegment(0, hardEnd, HardColor);
        AddSegment(hardEnd, regularEnd, RegularColor);
        AddSegment(regularEnd, failureEnd, FailureColor);
        AddSegment(failureEnd, 100, FumbleColor);

        marker.SetAsLastSibling();

        // A success value of 100 or more leaves no failure band, and short
        // bands drop out of the legend so it never repeats a dead range.
        var parts = new List<string>();
        if (hardEnd > 0)
        {
            parts.Add($"<color=#{ColorUtility.ToHtmlStringRGB(HardColor)}>HARD 1-{hardEnd}</color>");
        }
        if (regularEnd > hardEnd)
        {
            parts.Add($"<color=#{ColorUtility.ToHtmlStringRGB(RegularColor)}>REGULAR {hardEnd + 1}-{regularEnd}</color>");
        }
        if (failureEnd > regularEnd)
        {
            parts.Add($"<color=#{ColorUtility.ToHtmlStringRGB(FailureColor)}>FAILURE {regularEnd + 1}-{failureEnd}</color>");
        }
        parts.Add(fumbleStart >= 100
            ? $"<color=#{ColorUtility.ToHtmlStringRGB(FumbleColor)}>FUMBLE 100</color>"
            : $"<color=#{ColorUtility.ToHtmlStringRGB(FumbleColor)}>FUMBLE {fumbleStart}-100</color>");

        legendText.text = string.Join("   ", parts);
    }

    private void AddSegment(int fromRoll, int toRoll, Color color)
    {
        if (toRoll <= fromRoll) return; // empty range, e.g. no Hard Success when success value is 1

        var rect = NewRect("Segment", bar);
        rect.anchorMin = new Vector2(fromRoll / 100f, 0f);
        rect.anchorMax = new Vector2(toRoll / 100f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        segments.Add(rect.gameObject);
    }

    // --------------------------------------------------------- UI building

    private void BuildUi()
    {
        // Full-screen dim; clicking outside the panel skips the animation.
        var overlayRect = NewRect("Overlay", transform);
        Stretch(overlayRect);
        overlay = overlayRect.gameObject;
        overlayGroup = overlay.AddComponent<CanvasGroup>();
        overlayGroup.alpha = 0f;
        overlayGroup.blocksRaycasts = false;
        overlayGroup.interactable = false;

        var dim = overlay.AddComponent<Image>();
        dim.color = DimColor;
        var dimButton = overlay.AddComponent<Button>();
        dimButton.transition = Selectable.Transition.None;
        dimButton.onClick.AddListener(OnSkipClicked);

        var panel = NewRect("Panel", overlayRect);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(680f, 560f);
        panel.anchoredPosition = Vector2.zero;
        var panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.sprite = PlaceholderIcons.RoundedSquare;
        panelImage.type = Image.Type.Sliced;
        panelImage.color = PanelColor;
        var panelButton = panel.gameObject.AddComponent<Button>();
        panelButton.transition = Selectable.Transition.None;
        panelButton.onClick.AddListener(OnSkipClicked);

        titleText = NewText("Title", panel, 36, new Vector2(0f, 240f), new Vector2(640f, 50f));
        detailText = NewText("Detail", panel, 22, new Vector2(0f, 196f), new Vector2(640f, 36f));
        detailText.color = new Color(0.8f, 0.8f, 0.85f);

        // Created after the title/detail so the dice draw on top of them while bouncing.
        tensDie = NewDie("TensDie", panel, new Vector2(-DieSpacing, 100f));
        onesDie = NewDie("OnesDie", panel, new Vector2(DieSpacing, 100f));

        bar = NewRect("Bar", panel);
        bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0.5f);
        bar.sizeDelta = new Vector2(BarWidth, BarHeight);
        bar.anchoredPosition = new Vector2(0f, 20f);

        // The marker hangs off the bar's left edge; anchoredPosition.x is the distance along the bar.
        marker = NewRect("Marker", bar);
        marker.anchorMin = marker.anchorMax = new Vector2(0f, 0.5f);
        marker.pivot = new Vector2(0.5f, 0.5f);
        marker.sizeDelta = new Vector2(6f, BarHeight + 20f);
        var markerImage = marker.gameObject.AddComponent<Image>();
        markerImage.color = Color.white;
        markerImage.raycastTarget = false;

        legendText = NewText("Legend", panel, 16, new Vector2(0f, -25f), new Vector2(660f, 30f));
        legendText.textWrappingMode = TextWrappingModes.NoWrap;
        legendText.overflowMode = TextOverflowModes.Overflow;
        tierText = NewText("Tier", panel, 46, new Vector2(0f, -78f), new Vector2(640f, 60f));
        summaryText = NewText("Summary", panel, 22, new Vector2(0f, -148f), new Vector2(640f, 90f));

        var buttonRect = NewRect("ContinueButton", panel);
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(220f, 48f);
        buttonRect.anchoredPosition = new Vector2(0f, -238f);
        var buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.sprite = PlaceholderIcons.RoundedSquare;
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = new Color(0.30f, 0.50f, 0.80f);
        continueButton = buttonRect.gameObject.AddComponent<Button>();
        continueButton.targetGraphic = buttonImage;
        continueButton.onClick.AddListener(OnContinueClicked);
        UiClickSound.Attach(continueButton, UiClickKind.Plain);
        HoverHighlight.Attach(buttonRect.gameObject);
        var label = NewText("Label", buttonRect, 24, Vector2.zero, new Vector2(220f, 48f));
        label.text = "CONTINUE";
    }

    /// <summary>One die: a dark rounded square (edge) with a lighter rounded square on top (face) and the number on that.</summary>
    private Die NewDie(string name, Transform parent, Vector2 position)
    {
        var die = new Die { basePos = position };

        die.rect = NewRect(name, parent);
        die.rect.anchorMin = die.rect.anchorMax = new Vector2(0.5f, 0.5f);
        die.rect.sizeDelta = new Vector2(DieSize, DieSize);
        die.rect.anchoredPosition = position;

        var edge = die.rect.gameObject.AddComponent<Image>();
        edge.sprite = GetRoundedSprite();
        edge.color = DieEdgeColor;
        edge.raycastTarget = false;

        var faceRect = NewRect("Face", die.rect);
        Stretch(faceRect);
        faceRect.offsetMin = new Vector2(5f, 5f);
        faceRect.offsetMax = new Vector2(-5f, -5f);
        die.face = faceRect.gameObject.AddComponent<Image>();
        die.face.sprite = GetRoundedSprite();
        die.face.color = DieFaceColor;
        die.face.raycastTarget = false;

        die.text = NewText("Number", die.rect, 56, Vector2.zero, new Vector2(DieSize, DieSize));
        die.text.color = DieTextColor;
        die.text.fontStyle = FontStyles.Bold;

        return die;
    }

    /// <summary>Generated once and shared: a white rounded square with anti-aliased edges, tinted per use via Image.color.</summary>
    private static Sprite GetRoundedSprite()
    {
        if (roundedSprite != null) return roundedSprite;

        const int size = 64;
        const float radius = 14f;
        float half = size * 0.5f;

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Signed distance to a rounded square centred in the texture; inside is negative.
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - radius), 0f);
                float distance = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                float alpha = Mathf.Clamp01(0.5f - distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        roundedSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return roundedSprite;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static TMP_Text NewText(string name, Transform parent, float fontSize, Vector2 position, Vector2 size)
    {
        var rect = NewRect(name, parent);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        TmpFont.Apply(text);
        return text;
    }
}
