using UnityEngine;

/// <summary>
/// The single colour table for run feedback.
///
/// The log row and the warning banner both read from here, so the same message
/// cannot look amber in the log and red on the banner. Keyed by <see cref="LogKind"/>
/// rather than by a second UI-only enum: two palettes and two enums is exactly
/// how the banner drifted from the log in the first place.
///
/// Colours are tuned for the dark panel behind both of them
/// (log #0B1119, banner backdrop #0A0F17 at 85%).
/// </summary>
public static class UiPalette
{
    public static readonly Color Action = new Color(0.78f, 0.82f, 0.88f);
    public static readonly Color Success = new Color(0.42f, 0.85f, 0.45f);
    public static readonly Color Fail = new Color(0.91f, 0.38f, 0.38f);
    public static readonly Color Warning = new Color(1f, 0.72f, 0.25f);
    public static readonly Color DayEnd = new Color(0.55f, 0.75f, 1f);
    public static readonly Color System = new Color(0.60f, 0.63f, 0.68f);

    public static Color For(LogKind kind)
    {
        switch (kind)
        {
            case LogKind.Success: return Success;
            case LogKind.Fail: return Fail;
            case LogKind.Warning: return Warning;
            case LogKind.DayEnd: return DayEnd;
            case LogKind.System: return System;
            default: return Action;
        }
    }

    public static string ToHex(Color color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }
}
