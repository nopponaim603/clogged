using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Placeholder visual for one map node: a colored circle (color = zone),
/// with an optional selected-highlight image. Swap iconImage's sprite for
/// real art later - the color-by-zone logic can stay as a fallback/overlay.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MapNodeView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image selectedHighlight;

    [Header("Zone Label")]
    [Tooltip("Optional pre-authored label. If empty, one is created at runtime so the prefab needs no extra setup.")]
    [SerializeField] private TMP_Text zoneLabel;
    [SerializeField] private float labelWidth = 130f;
    [SerializeField] private float labelHeight = 70f;
    [SerializeField] private Color labelOutlineColor = new Color(0f, 0f, 0f, 1f);
    [SerializeField] private float labelOutlineWidth = 0.25f;

    private static readonly Color EasyColor = new Color(0.45f, 0.75f, 0.45f);
    private static readonly Color NormalColor = new Color(0.85f, 0.75f, 0.3f);
    private static readonly Color HardColor = new Color(0.8f, 0.3f, 0.3f);

    public MapNodeData Data { get; private set; }

    /// <summary>Fired whenever any node is clicked, passing the node that was clicked. GatherMapController subscribes to this.</summary>
    public static event Action<MapNodeView> OnAnyNodeClicked;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    public void Initialize(MapNodeData data)
    {
        Data = data;

        var rect = (RectTransform)transform;
        rect.anchoredPosition = data.anchoredPosition;

        if (iconImage != null)
        {
            iconImage.color = ColorForZone(data.zone);
        }

        BuildZoneLabel();
        SetSelected(false);
    }

    /// <summary>
    /// Shows the zone name and its AP cost inside the cave marker, so
    /// difficulty is no longer conveyed by color alone. The prefab is
    /// non-uniformly scaled, so the label gets the inverse scale to stay
    /// undistorted.
    /// </summary>
    private void BuildZoneLabel()
    {
        GatherZoneConfig config = GatherZoneDatabase.Instance != null
            ? GatherZoneDatabase.Instance.GetConfig(Data.zone)
            : null;
        int apCost = config != null ? config.apCost : 1;

        if (zoneLabel == null)
        {
            zoneLabel = TmpOutlineUtil.CreateLabel(transform, "ZoneLabel", labelWidth, labelHeight);
            bool outlined = TmpOutlineUtil.ApplyOutline(zoneLabel, labelOutlineColor, labelOutlineWidth);

            // Yellow/green circles need dark text if the outline could not be
            // applied (e.g. a font asset without an SDF shader).
            zoneLabel.color = outlined ? Color.white : new Color(0.08f, 0.08f, 0.1f);
        }

        zoneLabel.text = $"{Data.zone}\n{apCost} AP";

        var labelRect = (RectTransform)zoneLabel.transform;
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;

        // Counteract the prefab's stretched scale so the text renders at its
        // authored pixel size no matter how the node itself is scaled.
        Vector3 parentScale = transform.localScale;
        labelRect.localScale = new Vector3(
            parentScale.x != 0f ? 1f / parentScale.x : 1f,
            parentScale.y != 0f ? 1f / parentScale.y : 1f,
            1f);
    }

    private static Color ColorForZone(GatherZone zone)
    {
        switch (zone)
        {
            case GatherZone.Easy: return EasyColor;
            case GatherZone.Normal: return NormalColor;
            case GatherZone.Hard: return HardColor;
            default: return Color.white;
        }
    }

    private void HandleClick()
    {
        OnAnyNodeClicked?.Invoke(this);
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null)
        {
            selectedHighlight.enabled = selected;
        }
    }
}
