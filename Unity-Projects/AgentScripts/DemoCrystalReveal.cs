using UnityEngine;

/// <summary>
/// Drives the Core Item states for screenshots: the HUD's open box and the
/// centre-screen crystal reveal.
/// Run: unity command script.run --file AgentScripts/DemoCrystalReveal.cs --entry DemoCrystalReveal.OpenOrange
/// </summary>
public static class DemoCrystalReveal
{
    /// <summary>Box + key on the orange pair: HUD shows the open box, reveal shows the crystal.</summary>
    public static string OpenOrange() => Paint(CoreItemVariant.Orange);

    /// <summary>Same on the yellow pair.</summary>
    public static string OpenYellow() => Paint(CoreItemVariant.Yellow);

    static string Paint(CoreItemVariant variant)
    {
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

        var reveal = Object.FindAnyObjectByType<CrystalRevealView>(FindObjectsInactive.Include);
        reveal?.Hide(true);

        var statusBar = Object.FindAnyObjectByType<StatusBarView>();
        // Step the reveal like real frames do: the win is a false->true edge, so
        // clearing the flags has to be seen by one Update before the pair lands.
        Tick(reveal);
        Tick(statusBar);

        res.SetVariant(variant);
        res.ObtainCoreItem(CoreItemKind.A);
        Tick(reveal);
        Tick(statusBar);

        res.ObtainCoreItem(CoreItemKind.B);
        Tick(reveal);
        Tick(statusBar);

        return $"{variant} -> hud={HudSprite()} reveal={Name(reveal?.CurrentCrystal)} showing={reveal?.IsShowing}";
    }

    static void Tick(MonoBehaviour view)
    {
        if (view == null) return;

        var method = view.GetType().GetMethod("Update",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        method?.Invoke(view, null);
    }

    static string HudSprite()
    {
        var canvas = GameObject.Find("Canvas");
        var icon = canvas != null
            ? canvas.transform.Find("StatusBar/CoreItemGroup/CoreItemFrame/CoreItemIcon")?.GetComponent<UnityEngine.UI.Image>()
            : null;
        return icon == null || icon.sprite == null ? "null" : icon.sprite.name;
    }

    static string Name(Sprite sprite) => sprite != null ? sprite.name : "null";
}
