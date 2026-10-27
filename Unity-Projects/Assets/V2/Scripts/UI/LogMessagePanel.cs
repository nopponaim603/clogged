using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Action history panel, bottom-left. Newest entry sits at the bottom and older
/// lines scroll up. Collapses to a single line when minimized.
///
/// Renders <see cref="RunLog"/> only - it never decides what counts as an
/// event, so gameplay code keeps writing through RunLog.Add.
///
/// Rows are pooled: the template is cloned at most <see cref="capacity"/> times
/// and then reused, so appending never grows the hierarchy or triggers a
/// rebuild of the whole list.
/// </summary>
public class LogMessagePanel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform entriesContainer;
    [SerializeField] private TextMeshProUGUI entryTemplate;
    [SerializeField] private GameObject expandedRoot;
    [SerializeField] private GameObject collapsedRoot;
    [SerializeField] private TextMeshProUGUI collapsedLine;
    [SerializeField] private Button minMaxButton;
    [SerializeField] private TextMeshProUGUI minMaxLabel;

    [Header("Layout")]
    [Tooltip("Maximum lines kept on screen. Older ones scroll out of view.")]
    [SerializeField] private int capacity = 16;
    [Tooltip("Panel height while expanded.")]
    [SerializeField] private float expandedHeight = 300f;
    [Tooltip("Panel height while collapsed - enough for the header plus one line.")]
    [SerializeField] private float collapsedHeight = 64f;

    [Header("Colours")]
    [SerializeField] private Color actionColor = new Color(0.78f, 0.82f, 0.88f);
    [SerializeField] private Color successColor = new Color(0.42f, 0.85f, 0.45f);
    [SerializeField] private Color failColor = new Color(0.91f, 0.38f, 0.38f);
    [SerializeField] private Color warningColor = new Color(1f, 0.72f, 0.25f);
    [SerializeField] private Color dayEndColor = new Color(0.55f, 0.75f, 1f);
    [SerializeField] private Color systemColor = new Color(0.60f, 0.63f, 0.68f);

    private readonly List<TextMeshProUGUI> rows = new List<TextMeshProUGUI>();
    private bool expanded = true;

    public int Capacity => capacity;

    private void Awake()
    {
        if (entryTemplate != null) entryTemplate.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        RunLog.OnEntryAdded += HandleEntryAdded;
        RunLog.OnCleared += Render;
        if (minMaxButton != null) minMaxButton.onClick.AddListener(ToggleExpanded);

        Render();
        ApplyExpanded();
    }

    private void OnDisable()
    {
        RunLog.OnEntryAdded -= HandleEntryAdded;
        RunLog.OnCleared -= Render;
        if (minMaxButton != null) minMaxButton.onClick.RemoveListener(ToggleExpanded);
    }

    private void HandleEntryAdded(LogEntry entry) => Render();

    /// <summary>Maps the newest <see cref="capacity"/> entries onto the pooled rows.</summary>
    private void Render()
    {
        if (entriesContainer == null || entryTemplate == null) return;

        var entries = RunLog.Entries;
        int visible = Mathf.Min(entries.Count, capacity);
        int first = entries.Count - visible;

        for (int i = 0; i < visible; i++)
        {
            var row = RowAt(i);
            if (row == null) break;

            var entry = entries[first + i];
            row.text = Format(entry);
            row.color = ColorFor(entry.Kind);
            row.gameObject.SetActive(true);
        }

        for (int i = visible; i < rows.Count; i++)
        {
            if (rows[i] != null) rows[i].gameObject.SetActive(false);
        }

        if (collapsedLine != null)
        {
            collapsedLine.text = entries.Count > 0 ? Format(entries[entries.Count - 1]) : "—";
        }

        // Only the last line matters while collapsed, so skip the layout work.
        if (expanded) ScrollToBottom();
    }

    private TextMeshProUGUI RowAt(int index)
    {
        while (rows.Count <= index)
        {
            if (rows.Count >= capacity) return null;

            var clone = Instantiate(entryTemplate.gameObject, entriesContainer);
            clone.name = $"Entry {rows.Count + 1}";
            clone.SetActive(false);
            rows.Add(clone.GetComponent<TextMeshProUGUI>());
        }
        return rows[index];
    }

    private static string Format(LogEntry entry)
    {
        string day = entry.Day > 0 ? $"<color=#6E7A8A>[D{entry.Day}]</color> " : "";
        return day + entry.Text;
    }

    private Color ColorFor(LogKind kind)
    {
        switch (kind)
        {
            case LogKind.Success: return successColor;
            case LogKind.Fail: return failColor;
            case LogKind.Warning: return warningColor;
            case LogKind.DayEnd: return dayEndColor;
            case LogKind.System: return systemColor;
            default: return actionColor;
        }
    }

    private void ToggleExpanded()
    {
        expanded = !expanded;
        ApplyExpanded();

        // Collapsed rows keep their text, so re-rendering here just re-syncs
        // whichever lines were evicted while the panel was shut.
        Render();
    }

    private void ApplyExpanded()
    {
        if (expandedRoot != null) expandedRoot.SetActive(expanded);
        if (collapsedRoot != null) collapsedRoot.SetActive(!expanded);
        if (minMaxLabel != null) minMaxLabel.text = expanded ? "–" : "+";

        var rect = panelRoot != null ? panelRoot : (RectTransform)transform;
        var size = rect.sizeDelta;
        size.y = expanded ? expandedHeight : collapsedHeight;
        rect.sizeDelta = size;
    }

    private void ScrollToBottom()
    {
        if (scrollRect == null) return;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
