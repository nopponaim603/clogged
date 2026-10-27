using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puts the HUD into a chosen Core Item state so the icons can be screenshotted.
/// Run: unity command script.run --file AgentScripts/DemoCoreItems.cs --entry DemoCoreItems.BoxOnly
/// </summary>
public static class DemoCoreItems
{
    /// <summary>Nothing found yet - both slots show a dimmed silhouette.</summary>
    public static string Empty() => Paint(false, false, CoreItemVariant.Orange);

    /// <summary>Box found, key still missing: the box is closed.</summary>
    public static string BoxOnly() => Paint(true, false, CoreItemVariant.Orange);

    /// <summary>Box and key: the box is open and the crystal shows.</summary>
    public static string Won() => Paint(true, true, CoreItemVariant.Orange);

    /// <summary>Same win state on the yellow pair.</summary>
    public static string WonYellow() => Paint(true, true, CoreItemVariant.Yellow);

    static string Paint(bool box, bool key, CoreItemVariant variant)
    {
        // Game-view screenshots only pick up the UI when the canvas renders
        // through the camera instead of straight to the overlay.
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 5f;
        }

        var res = RunResources.Instance;
        if (res == null) return "no RunResources";

        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        foreach (string name in new[] { "coreItemA", "coreItemB" })
        {
            typeof(RunResources).GetField(name, flags)?.SetValue(res, false);
        }

        res.SetVariant(variant);
        if (box) res.ObtainCoreItem(CoreItemKind.A);
        if (key) res.ObtainCoreItem(CoreItemKind.B);

        var view = Object.FindAnyObjectByType<StatusBarView>();
        var method = typeof(StatusBarView).GetMethod("Update", flags);
        method?.Invoke(view, null);

        return $"{variant} box={box} key={key} -> {Describe()}";
    }

    static string Describe()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return "no canvas";

        var parts = new System.Collections.Generic.List<string>();
        foreach (string path in new[] { "StatusBar/CoreItemGroup/CoreItemFrame/CoreItemIcon",
                                        "StatusBar/CoreItemGroup/CoreItemFrameB/CoreItemBIcon" })
        {
            var icon = canvas.transform.Find(path)?.GetComponent<Image>();
            parts.Add(icon == null || icon.sprite == null ? "null" : icon.sprite.name);
        }
        return string.Join(" | ", parts);
    }
}
