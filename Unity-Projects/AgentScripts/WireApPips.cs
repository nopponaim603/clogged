using UnityEditor;
using UnityEngine;

/// <summary>
/// Binds the three AP state sprites to the unit card prefab.
/// Run: unity command script.run --file AgentScripts/WireApPips.cs --entry WireApPips.Run
/// </summary>
public static class WireApPips
{
    const string PrefabPath = "Assets/V2/Prefabs/ScoutIcon.prefab";
    const string IconFolder = "Assets/V2/Sprites/Icons";

    public static string Run()
    {
        var available = Load("ap_45");
        var pending = Load("ap_30");
        var spent = Load("ap_15");
        if (available == null || pending == null || spent == null)
            return "missing sprites - check the import settings";

        var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var view = contents.GetComponentInChildren<UnitIconView>(true);
            if (view == null) return "no UnitIconView on " + PrefabPath;

            var so = new SerializedObject(view);
            so.FindProperty("apPipAvailable").objectReferenceValue = available;
            so.FindProperty("apPipPending").objectReferenceValue = pending;
            so.FindProperty("apPipSpent").objectReferenceValue = spent;
            // 10x12 source art, drawn larger while keeping its aspect.
            so.FindProperty("apPipWidth").floatValue = 18f;
            so.FindProperty("apPipHeight").floatValue = 22f;
            so.FindProperty("apPipSpacing").floatValue = 4f;
            so.FindProperty("apPipRowHeight").floatValue = 22f;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.SaveAssets();
        return $"wired {available.name} / {pending.name} / {spent.name} onto {PrefabPath}";
    }

    static Sprite Load(string fileName)
    {
        string path = $"{IconFolder}/{fileName}.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogError($"[WireApPips] no Sprite at {path}");
        return sprite;
    }
}
