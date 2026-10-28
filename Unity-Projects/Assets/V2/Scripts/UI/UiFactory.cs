using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared construction helpers for the menus that build their UI in code.
///
/// The in-game HUD was authored in the scene, but the menu and result screens
/// are new and self-contained, so building them here keeps the scene file small
/// and makes the layout reviewable. Colours match the HUD palette.
/// </summary>
public static class UiFactory
{
    public static readonly Color Window = new Color(0.043f, 0.067f, 0.098f, 1f);
    public static readonly Color Panel = new Color(0.075f, 0.098f, 0.137f, 0.98f);
    public static readonly Color ButtonIdle = new Color(0.106f, 0.141f, 0.208f, 1f);
    public static readonly Color Border = new Color(0.26f, 0.29f, 0.37f, 1f);
    public static readonly Color TextColor = new Color(0.78f, 0.82f, 0.88f, 1f);
    public static readonly Color DimText = new Color(0.55f, 0.58f, 0.64f, 1f);
    public static readonly Color Accent = new Color(1f, 0.88f, 0.55f, 1f);
    public static readonly Color Success = new Color(0.42f, 0.85f, 0.45f, 1f);
    public static readonly Color Fail = new Color(0.91f, 0.38f, 0.38f, 1f);
    /// <summary>Fill for the option that is currently picked (window mode, selected list row).</summary>
    public static readonly Color ChoiceSelected = new Color(0.24f, 0.52f, 0.85f, 1f);
    public static readonly Color SliderTrack = new Color(0.13f, 0.16f, 0.22f, 1f);

    public static RectTransform CreateRect(string objectName, Transform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    public static Image CreateImage(string objectName, Transform parent, Color color)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;

        var image = go.GetComponent<Image>();
        image.color = color;
        image.type = Image.Type.Simple;
        return image;
    }

    public static TextMeshProUGUI CreateText(string objectName, Transform parent, string content, float fontSize, Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;

        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    /// <summary>
    /// A flat button with the darker panel fill and a lavender outline, matching
    /// how the skill column reads.
    /// </summary>
    public static Button CreateButton(Transform parent, string objectName, string label, Vector2 size, Action onClick,
        float fontSize = 24f)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(Button), typeof(Outline));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.sizeDelta = size;

        var image = go.GetComponent<Image>();
        image.color = ButtonIdle;

        var outline = go.GetComponent<Outline>();
        outline.effectColor = Border;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
        colors.pressedColor = new Color(0.8f, 0.85f, 0.95f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.45f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        if (onClick != null) button.onClick.AddListener(() => onClick());

        var text = CreateText("Label", rect, label, fontSize, TextColor);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(12f, 4f);
        text.rectTransform.offsetMax = new Vector2(-12f, -4f);
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = fontSize;

        return button;
    }

    /// <summary>
    /// A horizontal slider with an outlined track, a filled left portion and a
    /// square handle. Built here because the project has no prefab for one, and
    /// Slider drives its own anchors on the fill/handle at runtime.
    /// </summary>
    public static Slider CreateSlider(Transform parent, string objectName, float width, float height)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Slider));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(width, height);

        var slider = go.GetComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;

        var track = CreateImage("Track", rect, SliderTrack);
        UiFactoryCenterVertically((RectTransform)track.transform, height, 10f);

        var outline = track.gameObject.AddComponent<Outline>();
        outline.effectColor = Border;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        var fillArea = CreateRect("Fill Area", rect);
        UiFactoryCenterVertically(fillArea, height, 10f);
        fillArea.offsetMin = new Vector2(2f, fillArea.offsetMin.y);
        fillArea.offsetMax = new Vector2(-2f, fillArea.offsetMax.y);

        var fill = CreateImage("Fill", fillArea, ChoiceSelected);
        var fillRect = (RectTransform)fill.transform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        var handleArea = CreateRect("Handle Slide Area", rect);
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(10f, 0f);
        handleArea.offsetMax = new Vector2(-10f, 0f);

        var handle = CreateImage("Handle", handleArea, Accent);
        var handleRect = (RectTransform)handle.transform;
        handleRect.anchorMin = new Vector2(0f, 0f);
        handleRect.anchorMax = new Vector2(0f, 1f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(14f, 0f);

        var handleOutline = handle.gameObject.AddComponent<Outline>();
        handleOutline.effectColor = Border;
        handleOutline.effectDistance = new Vector2(2f, -2f);
        handleOutline.useGraphicAlpha = false;

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle;

        return slider;
    }

    private static void UiFactoryCenterVertically(RectTransform rect, float parentHeight, float ownHeight)
    {
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, ownHeight);
    }

    public static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(-inset * 2f, -inset * 2f);
    }

    /// <summary>Anchors to the parent's centre and places the rect at an offset from it.</summary>
    public static void Center(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    /// <summary>Full-width band anchored to a normalised vertical position (0 = bottom, 1 = top).</summary>
    public static void Band(RectTransform rect, float anchorY, float height, float horizontalInset = 0f)
    {
        rect.anchorMin = new Vector2(0f, anchorY);
        rect.anchorMax = new Vector2(1f, anchorY);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(-horizontalInset * 2f, height);
    }
}
