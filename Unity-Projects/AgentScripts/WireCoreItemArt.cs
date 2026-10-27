using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Fills the CoreItemArt table on the scene's StatusBarView: closed box + key +
/// crystal per variant. Run outside play mode.
/// Run: unity command script.run --file AgentScripts/WireCoreItemArt.cs --entry WireCoreItemArt.Run
/// </summary>
public static class WireCoreItemArt
{
    const string IconFolder = "Assets/V2/Sprites/Icons";

    public static string Run()
    {
        var view = Object.FindAnyObjectByType<StatusBarView>();
        if (view == null) return "no StatusBarView in the open scene";

        var so = new SerializedObject(view);
        var orange = so.FindProperty("orangePair");
        var yellow = so.FindProperty("yellowPair");
        if (orange == null || yellow == null) return "CoreItemArt fields not found on StatusBarView";

        // Orange box -> orange key. Yellow box -> grey key.
        Set(orange, "closedBox", "coreitem_panel_orange");
        Set(orange, "key", "coreitem_key_lit");
        Set(orange, "crystal", "coreitem_crystal");

        Set(yellow, "closedBox", "coreitem_panel_yellow");
        Set(yellow, "key", "coreitem_key_plain");
        Set(yellow, "crystal", "coreitem_crystal_b");

        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
        EditorSceneManager.SaveScene(view.gameObject.scene);
        AssetDatabase.SaveAssets();

        return $"wired orange={{panel_orange, key_lit, crystal}} yellow={{panel_yellow, key_plain, crystal_b}} " +
               $"on {view.gameObject.scene.name}";
    }

    static void Set(SerializedProperty art, string field, string fileName)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{IconFolder}/{fileName}.png");
        if (sprite == null)
        {
            Debug.LogError($"[WireCoreItemArt] missing sprite {fileName}");
            return;
        }

        var target = art.FindPropertyRelative(field);
        if (target == null)
        {
            Debug.LogError($"[WireCoreItemArt] field {field} not found");
            return;
        }
        target.objectReferenceValue = sprite;
    }
}
