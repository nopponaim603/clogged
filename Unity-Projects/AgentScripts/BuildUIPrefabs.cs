using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// P1 builder: creates the new-UI prefab skeletons under Assets/V2/Prefabs/UI.
/// Layout only - no behaviour is wired yet, and Core.unity is not touched.
/// Run with: unity command script.run --file AgentScripts/BuildUIPrefabs.cs --entry BuildUIPrefabs.All
/// </summary>
public static class BuildUIPrefabs
{
    const string Folder = "Assets/V2/Prefabs/UI";

    static readonly Color Bg = new Color(0.063f, 0.086f, 0.122f, 0.94f);
    static readonly Color Header = new Color(0.106f, 0.145f, 0.196f, 1f);
    static readonly Color Slot = new Color(0.145f, 0.196f, 0.263f, 1f);
    static readonly Color Text = new Color(0.902f, 0.929f, 0.969f, 1f);
    static readonly Color Dim = new Color(0.604f, 0.643f, 0.698f, 1f);
    static readonly Color Accent = new Color(1f, 0.843f, 0.369f, 1f);

    public static string All()
    {
        EnsureFolder();

        string log = BuildLogPanel();
        string hand = BuildCharacterHand();
        string dispatch = BuildDispatchPanel();
        string skill = BuildSkillPanel();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return string.Join("\n", new[] { log, hand, dispatch, skill });
    }

    // ---------------------------------------------------------------- LogPanel

    static string BuildLogPanel()
    {
        var root = NewRect("LogPanel", null);
        Anchor(root, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(24f, 24f), new Vector2(700f, 300f));
        AddImage(root, Bg);

        // Header strip
        var header = NewRect("Header", root);
        Anchor(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 34f));
        AddImage(header, Header);

        var title = NewRect("Title", header);
        Anchor(title, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(12f, 0f), new Vector2(-56f, 0f));
        title.offsetMin = new Vector2(14f, 0f);
        title.offsetMax = new Vector2(-56f, 0f);
        var titleText = AddText(title, "LOG", 20f, TextAlignmentOptions.MidlineLeft, Text);

        var minMax = NewRect("MinMaxButton", header);
        Anchor(minMax, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(34f, 26f));
        AddImage(minMax, Slot);
        var minMaxButton = minMax.gameObject.AddComponent<Button>();
        minMaxButton.targetGraphic = minMax.GetComponent<Image>();
        var minMaxLabelRect = NewRect("Label", minMax);
        Stretch(minMaxLabelRect, 0f);
        var minMaxLabel = AddText(minMaxLabelRect, "–", 20f, TextAlignmentOptions.Center, Text);

        // Expanded body: scroll view
        var expanded = NewRect("Expanded", root);
        Stretch(expanded, 0f);
        expanded.offsetMin = new Vector2(8f, 8f);
        expanded.offsetMax = new Vector2(-8f, -42f);

        var scrollRect = expanded.gameObject.AddComponent<ScrollRect>();
        var viewport = NewRect("Viewport", expanded);
        Stretch(viewport, 0f);
        viewport.gameObject.AddComponent<RectMask2D>();
        AddImage(viewport, new Color(0f, 0f, 0f, 0.001f));

        var content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        // Row template (kept inactive; the panel clones it per entry)
        var row = NewRect("EntryTemplate", content);
        row.sizeDelta = new Vector2(0f, 24f);
        var rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 24f;
        var rowText = AddText(row, "[Day 1] sample log entry", 17f, TextAlignmentOptions.MidlineLeft, Text);
        rowText.textWrappingMode = TextWrappingModes.Normal;
        row.gameObject.SetActive(false);

        // Collapsed body: single line
        var collapsed = NewRect("Collapsed", root);
        Stretch(collapsed, 0f);
        collapsed.offsetMin = new Vector2(10f, 8f);
        collapsed.offsetMax = new Vector2(-10f, -40f);
        var collapsedLine = AddText(collapsed, "…", 17f, TextAlignmentOptions.MidlineLeft, Dim);
        collapsed.gameObject.SetActive(false);

        var panel = root.gameObject.AddComponent<LogMessagePanel>();
        SetObj(panel, "panelRoot", root);
        SetObj(panel, "scrollRect", scrollRect);
        SetObj(panel, "entriesContainer", content);
        SetObj(panel, "entryTemplate", rowText);
        SetObj(panel, "expandedRoot", expanded.gameObject);
        SetObj(panel, "collapsedRoot", collapsed.gameObject);
        SetObj(panel, "collapsedLine", collapsedLine);
        SetObj(panel, "minMaxButton", minMaxButton);
        SetObj(panel, "minMaxLabel", minMaxLabel);
        SetInt(panel, "capacity", 16);
        SetFloat(panel, "expandedHeight", 300f);
        SetFloat(panel, "collapsedHeight", 64f);

        return Save(root, "LogPanel");
    }

    // ------------------------------------------------------- CharacterHandPanel

    static string BuildCharacterHand()
    {
        // Sits in the gap between the log panel (left, 24..724) and the dispatch
        // panel (right, 1596..1896), so its centre is shifted right of screen centre.
        var root = NewRect("CharacterHandPanel", null);
        Anchor(root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(190f, 20f), new Vector2(830f, 300f));

        var cards = NewRect("Cards", root);
        Stretch(cards, 0f);

        var hlg = cards.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 12f;
        hlg.padding = new RectOffset(6, 6, 6, 6);
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Cards are spawned at runtime by UnitRosterView, which uses this hand
        // for the container, card size and the dispatch/skill pick rule.
        var panel = root.gameObject.AddComponent<CharacterHandPanel>();
        SetObj(panel, "handRoot", root);
        SetObj(panel, "cardsContainer", cards);
        SetFloat(panel, "cardSpacing", 12f);
        SetFloat(panel, "cardWidth", 195f);
        SetFloat(panel, "cardHeight", 265f);
        SetInt(panel, "maxDispatchSelection", 2);
        SetInt(panel, "maxSkillSelection", 1);

        var roster = root.gameObject.AddComponent<UnitRosterView>();
        SetObj(roster, "container", cards);
        SetObj(roster, "iconPrefab", LoadScoutIcon());
        SetObj(roster, "handPanel", panel);
        SetInt(roster, "maxSelection", 2);
        SetFloat(roster, "cardSpacing", 12f);

        return Save(root, "CharacterHandPanel");
    }

    // ------------------------------------------------------ DispatchActionPanel

    static string BuildDispatchPanel()
    {
        var root = NewRect("DispatchActionPanel", null);
        Anchor(root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-24f, 24f), new Vector2(300f, 200f));

        var bg = AddImage(root, new Color(0.161f, 0.220f, 0.294f, 1f));
        var button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;

        var icon = NewRect("Icon", root);
        Anchor(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 16f), new Vector2(120f, 120f));
        var iconImage = AddImage(icon, new Color(1f, 1f, 1f, 0.10f));

        var label = NewRect("Label", root);
        Anchor(label, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 8f), new Vector2(-16f, 44f));
        var labelText = AddText(label, "DISPATCH", 24f, TextAlignmentOptions.Center, Text);

        var panel = root.gameObject.AddComponent<DispatchActionPanel>();
        SetObj(panel, "panelRoot", root);
        SetObj(panel, "actionButton", button);
        SetObj(panel, "actionIcon", iconImage);
        SetObj(panel, "actionLabel", labelText);
        SetBool(panel, "hideWhenIdle", true);

        return Save(root, "DispatchActionPanel");
    }

    // ------------------------------------------------------- SkillTriggerPanel

    static string BuildSkillPanel()
    {
        var root = NewRect("SkillTriggerPanel", null);
        Anchor(root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-24f, 244f), new Vector2(236f, 300f));

        var vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperRight;
        vlg.spacing = 10f;
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        var cookButton = MakeSkillButton(root, "CookButton", "Cook", out var cookCount, 220f);
        var healRow = NewRect("HealRow", root);
        healRow.sizeDelta = new Vector2(220f, 64f);
        var healLayout = healRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        healLayout.childAlignment = TextAnchor.MiddleRight;
        healLayout.spacing = 8f;
        healLayout.childControlWidth = false;
        healLayout.childControlHeight = false;
        healLayout.childForceExpandWidth = false;
        healLayout.childForceExpandHeight = false;

        var healButton = MakeSkillButton(healRow, "HealButton", "Heal", out _, 156f);
        if (healButton.GetComponent<LayoutElement>() == null) healButton.gameObject.AddComponent<LayoutElement>();
        healButton.GetComponent<LayoutElement>().preferredWidth = 156f;

        var modeButtonRect = NewRect("HealModeButton", healRow);
        modeButtonRect.sizeDelta = new Vector2(56f, 64f);
        var modeBg = AddImage(modeButtonRect, Slot);
        var modeButton = modeButtonRect.gameObject.AddComponent<Button>();
        modeButton.targetGraphic = modeBg;
        var modeLabelRect = NewRect("Label", modeButtonRect);
        Stretch(modeLabelRect, 0f);
        // "MED" = consumes 1 Medicine + 1 AP, "AP" = spends 2 AP only.
        // Latin text keeps the glyphs inside the default TMP atlas.
        var modeLabel = AddText(modeLabelRect, "MED", 18f, TextAlignmentOptions.Center, Accent);

        var craftButton = MakeSkillButton(root, "CraftButton", "Craft", out var craftCount, 220f);

        var panel = root.gameObject.AddComponent<SkillTriggerPanel>();
        SetObj(panel, "cookButton", cookButton);
        SetObj(panel, "cookCountLabel", cookCount);
        SetObj(panel, "healButton", healButton);
        SetObj(panel, "healModeButton", modeButton);
        SetObj(panel, "healModeLabel", modeLabel);
        SetObj(panel, "craftButton", craftButton);
        SetObj(panel, "craftCountLabel", craftCount);

        return Save(root, "SkillTriggerPanel");
    }

    static Button MakeSkillButton(RectTransform parent, string name, string label, out TextMeshProUGUI count, float width)
    {
        var rect = NewRect(name, parent);
        rect.sizeDelta = new Vector2(width, 64f);
        var le = rect.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = 64f;

        var bg = AddImage(rect, Slot);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;

        var labelRect = NewRect("Label", rect);
        Stretch(labelRect, 0f);
        labelRect.offsetMin = new Vector2(14f, 0f);
        labelRect.offsetMax = new Vector2(-52f, 0f);
        AddText(labelRect, label, 22f, TextAlignmentOptions.MidlineLeft, Text);

        var countRect = NewRect("Count", rect);
        Anchor(countRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-12f, 0f), new Vector2(40f, 32f));
        count = AddText(countRect, "", 22f, TextAlignmentOptions.Center, Accent);

        return button;
    }

    // ----------------------------------------------------------------- helpers

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/V2/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets/V2", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets/V2/Prefabs", "UI");
        }
    }

    static UnitIconView LoadScoutIcon()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/V2/Prefabs/ScoutIcon.prefab");
        if (prefab == null)
        {
            Debug.LogWarning("[BuildUIPrefabs] ScoutIcon.prefab not found - cards will not spawn.");
            return null;
        }

        var view = prefab.GetComponent<UnitIconView>();
        if (view == null)
        {
            Debug.LogWarning("[BuildUIPrefabs] ScoutIcon.prefab has no UnitIconView on its root.");
        }
        return view;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    static Image AddImage(RectTransform rt, Color color)
    {
        var image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    static TextMeshProUGUI AddText(RectTransform rt, string text, float size, TextAlignmentOptions align, Color color)
    {
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    static string Save(RectTransform root, string fileName)
    {
        string path = $"{Folder}/{fileName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
        Object.DestroyImmediate(root.gameObject);
        bool ok = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        return (ok ? "OK   " : "FAIL ") + path;
    }

    static void SetObj(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogWarning($"[BuildUIPrefabs] {target.GetType().Name}.{field} not found");
            return;
        }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetInt(Object target, string field, int value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop != null) prop.intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetFloat(Object target, string field, float value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop != null) prop.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetBool(Object target, string field, bool value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop != null) prop.boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
