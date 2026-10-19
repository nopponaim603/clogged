using TMPro;
using UnityEngine;

/// <summary>
/// TMP-specific text styling. UGUI's Outline/Shadow components do not show up
/// on TextMeshPro text (TMP rebuilds its own mesh), so outlines have to be set
/// on a per-text material instance instead.
/// </summary>
public static class TmpOutlineUtil
{
    /// <summary>
    /// Gives this text its own material instance with the SDF outline enabled.
    /// <paramref name="width"/> is the TMP outline thickness (0-1; ~0.2 reads well at UI sizes).
    /// Overlaps look thicker than the shader value suggests, so 0.15-0.25 is plenty.
    /// </summary>
    public static bool ApplyOutline(TMP_Text text, Color color, float width, float softness = 0f)
    {
        Material material = CreateOutlineMaterial(text, color, width, softness);
        if (material == null) return false;

        text.fontMaterial = material;
        return true;
    }

    /// <summary>
    /// Builds (or reuses) the text's outline material instance. Reusing it lets
    /// callers recolour the outline per state without leaking a material each time.
    /// </summary>
    public static Material CreateOutlineMaterial(TMP_Text text, Color color, float width, float softness = 0f)
    {
        if (text == null || text.fontSharedMaterial == null) return null;

        Material material = text.fontMaterial;
        if (material == null || material == text.fontSharedMaterial)
        {
            material = new Material(text.fontSharedMaterial);
        }

        material.EnableKeyword(ShaderUtilities.Keyword_Outline);
        material.SetColor(ShaderUtilities.ID_OutlineColor, color);
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, Mathf.Clamp01(width));
        material.SetFloat(ShaderUtilities.ID_OutlineSoftness, Mathf.Clamp01(softness));
        return material;
    }

    public static void SetOutlineColor(Material material, Color color)
    {
        if (material == null) return;
        material.SetColor(ShaderUtilities.ID_OutlineColor, color);
    }

    /// <summary>Convenience: create a TMP label under <paramref name="parent"/> with sensible UI defaults.</summary>
    public static TextMeshProUGUI CreateLabel(Transform parent, string objectName, float width, float height)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(width, height);

        var label = go.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 10f;
        label.fontSizeMax = 22f;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }
}
