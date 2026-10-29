using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Adds the Core Item open-box art and drops the crystal reveal overlay under
/// the canvas. Run outside play mode.
/// Run: unity command script.run --file AgentScripts/WireCrystalReveal.cs --entry WireCrystalReveal.Run
/// </summary>
public static class WireCrystalReveal
{
    const string IconFolder = "Assets/V2/Sprites/Icons";
    const string OverlayName = "CrystalReveal";

    public static string Run()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return "no Canvas in the open scene";

        var statusBar = Object.FindAnyObjectByType<StatusBarView>();
        if (statusBar == null) return "no StatusBarView";

        var so = new SerializedObject(statusBar);
        var orange = so.FindProperty("orangePair");
        var yellow = so.FindProperty("yellowPair");
        if (orange == null || yellow == null) return "CoreItemArt fields not found";

        Set(orange, "openBox", "coreitem_panel_orange_bar");
        Set(yellow, "openBox", "coreitem_panel_yellow_bar");
        so.ApplyModifiedPropertiesWithoutUndo();

        var overlay = canvas.transform.Find(OverlayName) as RectTransform;
        if (overlay == null)
        {
            var go = new GameObject(OverlayName, typeof(RectTransform), typeof(CanvasGroup), typeof(CrystalRevealView));
            overlay = (RectTransform)go.transform;
            overlay.SetParent(canvas.transform, false);
            // Last sibling: the reveal draws over every other panel.
            overlay.SetAsLastSibling();
        }
        else if (overlay.GetComponent<CrystalRevealView>() == null)
        {
            overlay.gameObject.AddComponent<CrystalRevealView>();
        }

        var view = overlay.GetComponent<CrystalRevealView>();
        var viewSo = new SerializedObject(view);
        viewSo.FindProperty("orangeCrystal").objectReferenceValue = Load("coreitem_crystal");
        viewSo.FindProperty("yellowCrystal").objectReferenceValue = Load("coreitem_crystal_b");
        viewSo.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(canvas.scene);
        EditorSceneManager.SaveScene(canvas.scene);
        AssetDatabase.SaveAssets();

        return $"orange open={Load("coreitem_panel_orange_bar").name} yellow open={Load("coreitem_panel_yellow_bar").name} " +
               $"overlay={OverlayName} on {canvas.scene.name}";
    }

    static void Set(SerializedProperty art, string field, string fileName)
    {
        var sprite = Load(fileName);
        if (sprite == null) return;

        var target = art.FindPropertyRelative(field);
        if (target == null)
        {
            Debug.LogError($"[WireCrystalReveal] field {field} not found");
            return;
        }
        target.objectReferenceValue = sprite;
    }

    static Sprite Load(string fileName)
    {
        string path = $"{IconFolder}/{fileName}.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogError($"[WireCrystalReveal] missing sprite {path}");
        return sprite;
    }
}
