using System;
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

    private static readonly Color NearColor = new Color(0.45f, 0.75f, 0.45f);
    private static readonly Color MidColor = new Color(0.85f, 0.75f, 0.3f);
    private static readonly Color FarColor = new Color(0.8f, 0.3f, 0.3f);

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

        SetSelected(false);
    }

    private static Color ColorForZone(GatherZone zone)
    {
        switch (zone)
        {
            case GatherZone.Near: return NearColor;
            case GatherZone.Mid: return MidColor;
            case GatherZone.Far: return FarColor;
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
