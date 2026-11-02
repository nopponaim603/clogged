using TMPro;
using UnityEngine;

/// <summary>
/// Resolves the font for labels that are created in code.
///
/// <see cref="TMP_Settings.defaultFontAsset"/> comes back null at runtime in
/// this project, so runtime labels would otherwise log "Please assign a Font
/// Asset" and render nothing. The scene's authored labels all carry the pixel
/// font, so the first one found is used as the fallback and cached.
/// </summary>
public static class TmpFont
{
    private static TMP_FontAsset cached;
    private static bool searched;

    public static TMP_FontAsset Resolve()
    {
        if (cached != null) return cached;
        if (searched) return cached;

        searched = true;

        cached = TMP_Settings.defaultFontAsset;
        if (cached != null) return cached;

        var labels = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var label in labels)
        {
            if (label == null || label.font == null) continue;
            cached = label.font;
            return cached;
        }

        return cached;
    }

    /// <summary>Applies the resolved font, leaving the label untouched when none can be found.</summary>
    public static void Apply(TMP_Text text)
    {
        if (text == null) return;

        var font = Resolve();
        if (font != null && text.font != font) text.font = font;
    }
}
