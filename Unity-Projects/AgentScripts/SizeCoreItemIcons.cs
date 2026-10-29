using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sizes the Core Item art to an integer multiple of the source pixels (96x96
/// box holding 16x14 / 16x10 / 12x12 sprites) so the pixel art stays crisp.
/// Run: unity command script.run --file AgentScripts/SizeCoreItemIcons.cs --entry SizeCoreItemIcons.Run
/// </summary>
public static class SizeCoreItemIcons
{
    public static string Run()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return "no canvas";

        string report = "";
        foreach (string path in new[]
                 {
                     "StatusBar/CoreItemGroup/CoreItemFrame/CoreItemIcon",
                     "StatusBar/CoreItemGroup/CoreItemFrameB/CoreItemBIcon",
                 })
        {
            var t = canvas.transform.Find(path) as RectTransform;
            if (t == null) { report += $"{path} MISSING\n"; continue; }

            t.anchorMin = new Vector2(0.5f, 0.5f);
            t.anchorMax = new Vector2(0.5f, 0.5f);
            t.pivot = new Vector2(0.5f, 0.5f);
            t.anchoredPosition = Vector2.zero;
            t.sizeDelta = new Vector2(96f, 96f);

            var image = t.GetComponent<Image>();
            if (image != null)
            {
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }

            report += $"{path} -> 96x96\n";
        }

        EditorSceneManager.MarkSceneDirty(canvas.scene);
        EditorSceneManager.SaveScene(canvas.scene);
        return report.TrimEnd();
    }
}
