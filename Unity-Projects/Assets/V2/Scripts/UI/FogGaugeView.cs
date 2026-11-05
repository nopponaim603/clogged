using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The run's Risk as a vertical gauge down the left edge of the map, built to
/// look like a piece of equipment rather than a stray bar: a plate with the same
/// rounded panel and four-edge frame as a dungeon card, a narrow measuring tube
/// standing in the middle of it, and RISK spelled down the left rail.
///
/// The tube reads Risk, not a cosmetic fog setting: full at 80, empty at 0, and
/// the red fog over the map follows the same number.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class FogGaugeView : MonoBehaviour
{
    private static readonly Color PlateColor = new Color(0.09f, 0.10f, 0.15f, 0.96f);
    private static readonly Color FrameColor = new Color(0.72f, 0.16f, 0.17f, 0.9f);
    private static readonly Color TubeColor = new Color(0.04f, 0.04f, 0.06f, 0.95f);
    private static readonly Color FillColor = new Color(0.74f, 0.10f, 0.11f, 1f);
    private static readonly Color TickColor = new Color(0.78f, 0.82f, 0.88f, 0.20f);
    private static readonly Color LabelColor = new Color(0.78f, 0.82f, 0.88f, 0.62f);

    /// <summary>The name plate, one letter per line down the plate's left rail.</summary>
    private const string LabelText = "R\nI\nS\nK";

    [Tooltip("Width of the measuring tube, in pixels. The plate around it is set by the map controller.")]
    [SerializeField] private float tubeWidth = 22f;
    [Tooltip("Gap between the plate's edge and the tube.")]
    [SerializeField] private float tubeInset = 10f;
    [Tooltip("Gap between the tube's own edge and the red column.")]
    [SerializeField] private float fillInset = 2f;
    [Tooltip("How fast the column chases a new Risk level, in fraction-of-tube per second.")]
    [SerializeField] private float followSpeed = 2.4f;
    [Tooltip("Point size of the RISK name plate.")]
    [SerializeField] private float labelFontSize = 20f;

    private RectTransform tube;
    private RectTransform fill;
    private TMP_Text riskLabel;
    private float shown;
    private float target = -1f;

    /// <summary>What the tube currently shows, 0-1. Exposed so the checks can read the gauge.</summary>
    public float Shown => shown;

    /// <summary>Height of the red column as a fraction of the tube's inner height.</summary>
    public float BarFraction
    {
        get
        {
            if (fill == null) return 0f;

            float inner = InnerHeight();
            return inner > 0f ? fill.sizeDelta.y / inner : 0f;
        }
    }

    /// <summary>Builds the gauge under <paramref name="parent"/>; the caller positions it.</summary>
    public static FogGaugeView Create(Transform parent)
    {
        var go = new GameObject("FogGauge", typeof(RectTransform), typeof(FogGaugeView));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;

        var view = go.GetComponent<FogGaugeView>();
        view.Build();
        return view;
    }

    /// <summary>Places and sizes the plate. Called by the map controller once the cards are laid out.</summary>
    public void SetGeometry(Vector2 anchoredPosition, Vector2 size)
    {
        var rect = (RectTransform)transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        LayoutTube();
        LayoutLabel();

        shown = CurrentRiskRatio();
        target = shown;
        Apply();
    }

    /// <summary>
    /// Jumps the column to the live Risk without animating. A test frame does
    /// not tick Update, so the follow would otherwise still be at its start.
    /// </summary>
    public void SnapToRisk()
    {
        shown = CurrentRiskRatio();
        target = shown;
        Apply();
    }

    private void Build()
    {
        // Plate first, then the four-edge frame, so the gauge reads as a card
        // back with a rim - the same shape the dungeon cards use.
        var plate = UiFactory.CreateImage("Plate", transform, PlateColor);
        plate.sprite = PlaceholderIcons.RoundedSquare;
        plate.type = Image.Type.Sliced;
        plate.raycastTarget = true;
        UiFactory.Stretch(plate.rectTransform);

        BuildFrame();

        tube = UiFactory.CreateImage("Tube", transform, TubeColor).rectTransform;
        var tubeImage = tube.GetComponent<Image>();
        tubeImage.raycastTarget = false;
        LayoutTube();

        var fillImage = UiFactory.CreateImage("Fill", tube, FillColor);
        fill = fillImage.rectTransform;
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(1f, 0f);
        fill.pivot = new Vector2(0.5f, 0f);
        fill.anchoredPosition = new Vector2(0f, fillInset);
        fill.sizeDelta = new Vector2(-2f * fillInset, 0f);
        fillImage.raycastTarget = false;

        BuildTick(0.25f);
        BuildTick(0.5f);
        BuildTick(0.75f);

        BuildLabel();

        HudTooltipTrigger.Attach(this, Describe);
    }

    /// <summary>
    /// The name plate: R/I/S/K stacked down the plate's left rail, so the column
    /// of red reads as a risk gauge and not as an unlabelled bar. One letter per
    /// line keeps it inside the narrow gutter the tube leaves over.
    /// </summary>
    private void BuildLabel()
    {
        riskLabel = UiFactory.CreateText("RiskLabel", transform, LabelText, labelFontSize, LabelColor);
        riskLabel.alignment = TextAlignmentOptions.Center;
        riskLabel.lineSpacing = 6f;
        TmpFont.Apply(riskLabel);
        LayoutLabel();
    }

    /// <summary>Pins the label to the gutter between the plate's edge and the tube.</summary>
    private void LayoutLabel()
    {
        if (riskLabel == null) return;

        var rect = riskLabel.rectTransform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.offsetMin = new Vector2(2f, tubeInset);
        rect.offsetMax = new Vector2(Mathf.Max(10f, LabelGutter() - 2f), -(tubeInset + 8f));
    }

    /// <summary>Width available to the left of the tube, inside the frame.</summary>
    private float LabelGutter()
    {
        var plate = (RectTransform)transform;
        return (plate.rect.width - tube.rect.width) * 0.5f - 1f;
    }

    /// <summary>Four thin bars just inside the plate's edge, mirroring MapNodeView's frame.</summary>
    private void BuildFrame()
    {
        BuildEdge("FrameTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 3f));
        BuildEdge("FrameBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 3f));
        BuildEdge("FrameLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(3f, 0f));
        BuildEdge("FrameRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(3f, 0f));
    }

    private void BuildEdge(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 thickness)
    {
        var image = UiFactory.CreateImage(name, transform, FrameColor);
        image.raycastTarget = false;

        var rect = image.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = thickness;
        rect.anchoredPosition = Vector2.zero;
    }

    private void BuildTick(float height)
    {
        var tick = UiFactory.CreateImage("Tick", tube, TickColor);
        tick.raycastTarget = false;

        var rect = tick.rectTransform;
        rect.anchorMin = new Vector2(0f, height);
        rect.anchorMax = new Vector2(1f, height);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(fillInset, -1f);
        rect.offsetMax = new Vector2(-fillInset, 1f);
    }

    /// <summary>Centres the tube in the plate and sizes it from the plate's own height.</summary>
    private void LayoutTube()
    {
        if (tube == null) return;

        var plate = (RectTransform)transform;
        tube.anchorMin = tube.anchorMax = new Vector2(0.5f, 0.5f);
        tube.pivot = new Vector2(0.5f, 0.5f);
        tube.anchoredPosition = Vector2.zero;
        tube.sizeDelta = new Vector2(Mathf.Min(tubeWidth, Mathf.Max(4f, plate.rect.width - tubeInset * 2f)),
            Mathf.Max(0f, plate.rect.height - tubeInset * 2f));
    }

    private HudTooltipContent Describe()
    {
        int risk = RunResources.Instance != null ? RunResources.Instance.Risk : 0;
        int max = RunResources.MaxRisk;

        return new HudTooltipContent("RISK",
            $"THE RED FOG IS AT {risk}/{max} - THE GAUGE, THE MIST AND THE PENALTY ARE ALL THIS ONE NUMBER.\n" +
            "EACH CAVE SHRUGS OFF ITS OWN HANDICAP BEFORE RISK COSTS IT ANYTHING: " +
            $"EASY {GatherResolver.ZoneHandicap(GatherZone.Easy)}, " +
            $"NORMAL {GatherResolver.ZoneHandicap(GatherZone.Normal)}, " +
            $"HARD {GatherResolver.ZoneHandicap(GatherZone.Hard)}.\n" +
            "SO EASY IS FREE OF FOG AT RISK " + GatherResolver.ZoneHandicap(GatherZone.Easy) +
            ", NORMAL AT " + GatherResolver.ZoneHandicap(GatherZone.Normal) +
            ", AND HARD ONLY AT 0.\n" +
            $"A CLEARED CAVE BURNS IT DOWN: EASY {GatherResolver.RiskReduction(GatherZone.Easy)}, " +
            $"NORMAL {GatherResolver.RiskReduction(GatherZone.Normal)}, HARD {GatherResolver.RiskReduction(GatherZone.Hard)}.\n" +
            "AT 0 THE FOG IS GONE AND NOTHING IS PENALISED.");
    }

    private void Update()
    {
        float wanted = CurrentRiskRatio();
        if (Mathf.Approximately(wanted, target) && Mathf.Approximately(shown, target)) return;

        target = wanted;
        shown = Mathf.MoveTowards(shown, target, followSpeed * Time.unscaledDeltaTime);
        Apply();
    }

    private void Apply()
    {
        if (fill == null) return;

        fill.sizeDelta = new Vector2(-2f * fillInset, InnerHeight() * Mathf.Clamp01(shown));
    }

    private float InnerHeight()
    {
        return tube != null ? Mathf.Max(0f, tube.rect.height - fillInset * 2f) : 0f;
    }

    private static float CurrentRiskRatio()
    {
        return RunResources.Instance != null ? RunResources.Instance.RiskRatio : 0f;
    }
}
