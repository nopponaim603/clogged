using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace V2.EditorTools
{
/// <summary>
/// Points every TextMeshPro component under the V2 UI at the pixel font.
///
/// Prefabs are edited through LoadPrefabContents so instances in the scene
/// inherit the new font instead of each keeping a stale override. The scene is
/// then saved so the hand-authored labels pick it up too.
///
/// Re-runnable: components already on the pixel font are skipped.
/// </summary>
public static class ApplyPixelFont
{
    private const string FontPath = "Assets/V2/Fonts/PixelTileset.asset";
    private const string ScenePath = "Assets/V2/Scenes/Core.unity";
    private const string PrefabFolder = "Assets/V2/Prefabs";
    private const string SettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

    [MenuItem("Tools/V2/Apply Pixel Font To V2 UI")]
    public static void Run()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) { Debug.LogError($"ApplyPixelFont: {FontPath} not found."); return; }

        var log = new StringBuilder();
        int prefabCount = 0;
        int sceneCount = 0;

        bool defaultChanged = SetTmpDefaultFont(font);
        log.AppendLine($"  TMP default font {(defaultChanged ? "-> " + font.name : "already " + font.name)}");

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;

            int changed = Apply(root, font);
            if (changed > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                prefabCount += changed;
                log.AppendLine($"  {path} : {changed}");
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        // Only touch the scene when it is the one already open, otherwise open
        // it, edit, save and put back what the user had.
        string previous = EditorSceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
        {
            sceneCount += Apply(root, font);
        }
        EditorSceneManager.SaveScene(scene);
        log.AppendLine($"  {ScenePath} : {sceneCount}");

        if (!string.IsNullOrEmpty(previous) && previous != ScenePath)
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"ApplyPixelFont: {prefabCount + sceneCount} components now use {font.name}\n{log}");
    }

    /// <summary>
    /// Points TMP's global default at the pixel font, so labels built in code
    /// (which never assign a font) do not silently come out in LiberationSans.
    /// </summary>
    private static bool SetTmpDefaultFont(TMP_FontAsset font)
    {
        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(SettingsPath);
        if (settings == null) { Debug.LogWarning($"ApplyPixelFont: {SettingsPath} not found."); return false; }

        var so = new SerializedObject(settings);
        var prop = so.FindProperty("m_defaultFontAsset");
        if (prop == null) { Debug.LogWarning("ApplyPixelFont: m_defaultFontAsset not found."); return false; }
        if (prop.objectReferenceValue == font) return false;

        prop.objectReferenceValue = font;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
        return true;
    }

    /// <summary>Rewrites every TMP component under root. Returns how many changed.</summary>
    private static int Apply(GameObject root, TMP_FontAsset font)
    {
        int changed = 0;
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            // Reading text.font throws when the stored reference is a destroyed
            // object, so both the check and the write go through the serialised
            // field - that path tolerates broken references.
            var so = new SerializedObject(text);
            var fontProp = so.FindProperty("m_fontAsset");
            if (fontProp == null) continue;

            var materialProp = so.FindProperty("m_sharedMaterial");
            bool fontOk = fontProp.objectReferenceValue == font;
            bool materialOk = materialProp == null || materialProp.objectReferenceValue == font.material;
            if (fontOk && materialOk) continue;

            fontProp.objectReferenceValue = font;
            if (materialProp != null) materialProp.objectReferenceValue = font.material;
            so.ApplyModifiedPropertiesWithoutUndo();

            text.SetAllDirty();
            EditorUtility.SetDirty(text);
            changed++;
        }
        return changed;
    }

    /// <summary>
    /// Reports characters the font cannot draw. Those fall back to the face in
    /// the fallback list, which is the one place two typefaces can still mix.
    /// Also lists any component still pointing at another font.
    /// </summary>
    [MenuItem("Tools/V2/Check Pixel Font Coverage")]
    public static void CheckCoverage()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) { Debug.LogError($"ApplyPixelFont: {FontPath} not found."); return; }

        // Rich-text tags are consumed by TMP, never rendered, so they are not
        // missing glyphs.
        var reported = new HashSet<char>();
        var detail = new StringBuilder();
        var stragglers = new StringBuilder();
        int total = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;
            total += Scan(root, font, path, reported, detail, stragglers);
            PrefabUtility.UnloadPrefabContents(root);
        }

        string previous = EditorSceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
            total += Scan(root, font, ScenePath, reported, detail, stragglers);
        if (!string.IsNullOrEmpty(previous) && previous != ScenePath)
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);

        Debug.Log($"CheckPixelFontCoverage: {total} TMP components in V2.\n" +
                  (stragglers.Length == 0 ? "all use PixelTileset\n" : $"NOT on PixelTileset:\n{stragglers}") +
                  (reported.Count == 0
                      ? "every stored character has a glyph."
                      : $"{reported.Count} chars fall back: {string.Join(" ", reported.Select(c => c.ToString()))}\n{detail}"));
    }

    private static int Scan(GameObject root, TMP_FontAsset font, string origin,
        HashSet<char> reported, StringBuilder detail, StringBuilder stragglers)
    {
        int total = 0;
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            total++;

            // Serialised read: a destroyed reference throws through the property.
            var so = new SerializedObject(text);
            var fontProp = so.FindProperty("m_fontAsset");
            var current = fontProp != null ? fontProp.objectReferenceValue as TMP_FontAsset : null;
            if (current != font)
            {
                stragglers.AppendLine($"  {origin} :: {Path(text.transform)} -> {(current != null ? current.name : "null")}");
                continue;
            }

            if (string.IsNullOrEmpty(text.text)) continue;

            // Strip <tag> markup: TMP parses it instead of rendering it.
            string plain = System.Text.RegularExpressions.Regex.Replace(text.text, "<[^>]*>", "");
            bool first = true;
            foreach (char c in plain)
            {
                if (c == '\n' || c == '\r') continue;
                if (font.HasCharacter(c)) continue;

                if (reported.Add(c) || first)
                {
                    detail.AppendLine($"  U+{(int)c:X4} '{c}' in {origin} :: {text.gameObject.name}");
                    first = false;
                }
            }
        }
        return total;
    }

    private static string Path(Transform t)
    {
        string path = t.name;
        while (t.parent != null && t.parent.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
}
