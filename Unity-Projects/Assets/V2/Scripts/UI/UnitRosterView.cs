using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spawns one UnitIconView card per unit in UnitManager (same order as the
/// roster), keeps them up to date, and owns the gather selection: clicking a
/// card toggles it, max <see cref="maxSelection"/> at a time. This replaces the
/// old toggle checkboxes that lived on the map panel.
/// </summary>
public class UnitRosterView : MonoBehaviour
{
    [SerializeField] private RectTransform container;
    [SerializeField] private UnitIconView iconPrefab;
    [SerializeField] private int maxSelection = 2;
    [SerializeField] private float cardSpacing = 14f;
    [SerializeField] private Vector2 layoutOrigin = Vector2.zero;

    [Tooltip("Frame color per roster slot, in roster order (Ann, Boon, Cia, Dan).")]
    [SerializeField] private Color[] unitColors = new Color[0];

    private static readonly Color[] FallbackColors =
    {
        new Color(0.90f, 0.29f, 0.29f), // Ann  — red
        new Color(0.95f, 0.80f, 0.25f), // Boon — yellow
        new Color(0.36f, 0.80f, 0.42f), // Cia  — green
        new Color(0.33f, 0.61f, 0.95f), // Dan  — blue
    };

    private readonly List<UnitIconView> views = new List<UnitIconView>();
    private bool selectionEnabled = true;

    public event Action OnSelectionChanged;
    public event Action<string> OnSelectionRejected;

    public IReadOnlyList<UnitIconView> Cards => views;
    public int MaxSelection => maxSelection;
    public bool HasSelection => SelectedCount > 0;

    public int SelectedCount
    {
        get
        {
            int count = 0;
            foreach (var view in views)
            {
                if (view.IsSelected) count++;
            }
            return count;
        }
    }

    private void Awake()
    {
        if (container == null) container = (RectTransform)transform;

        // Cards are positioned by LayoutCard() instead, so their size and
        // spacing never depend on the container's rect.
        var layoutGroup = container.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup != null) layoutGroup.enabled = false;
    }

    private void Start()
    {
        BuildCards();
    }

    private void Update()
    {
        bool selectionChanged = false;
        foreach (var view in views)
        {
            view.Refresh();

            // A unit can become unavailable mid-day (AP spent on base work,
            // hurt, away) - drop it from the selection so dispatch never
            // sends an ineligible unit.
            if (view.IsSelected && view.Unit != null && !view.Unit.CanBeSentGathering)
            {
                view.SetSelected(false);
                selectionChanged = true;
            }
        }

        if (selectionChanged) OnSelectionChanged?.Invoke();
    }

    private void OnDestroy()
    {
        foreach (var view in views)
        {
            if (view != null) view.Clicked -= HandleCardClicked;
        }
    }

    private void BuildCards()
    {
        if (views.Count > 0) return;

        if (UnitManager.Instance == null)
        {
            Debug.LogWarning("[UnitRosterView] UnitManager.Instance is missing.");
            return;
        }
        if (iconPrefab == null)
        {
            Debug.LogWarning("[UnitRosterView] iconPrefab is not assigned.");
            return;
        }

        var units = UnitManager.Instance.Units;
        for (int i = 0; i < units.Count; i++)
        {
            UnitIconView view = Instantiate(iconPrefab, container);
            view.Bind(units[i], ColorForSlot(i), true);
            view.Clicked += HandleCardClicked;
            LayoutCard((RectTransform)view.transform, i);
            views.Add(view);
        }

        RefreshAvailability();
        OnSelectionChanged?.Invoke();
    }

    /// <summary>Lays the cards out in a row rising from the container's bottom-left corner.</summary>
    private void LayoutCard(RectTransform card, int index)
    {
        Vector2 size = card.sizeDelta;

        card.anchorMin = new Vector2(0f, 0f);
        card.anchorMax = new Vector2(0f, 0f);
        card.pivot = new Vector2(0f, 0f);
        card.anchoredPosition = layoutOrigin + new Vector2(index * (size.x + cardSpacing), 0f);
    }

    private Color ColorForSlot(int index)
    {
        if (unitColors != null && index < unitColors.Length) return unitColors[index];
        return FallbackColors[index % FallbackColors.Length];
    }

    private void HandleCardClicked(UnitIconView card)
    {
        if (!selectionEnabled || !card.CanBeSelected) return;

        if (!card.IsSelected && SelectedCount >= maxSelection)
        {
            OnSelectionRejected?.Invoke($"Select up to {maxSelection} units ({maxSelection} already picked).");
            return;
        }

        card.SetSelected(!card.IsSelected);
        OnSelectionChanged?.Invoke();
    }

    public List<UnitCharacter> GetSelectedUnits()
    {
        var selected = new List<UnitCharacter>();
        foreach (var view in views)
        {
            if (view.IsSelected && view.Unit != null) selected.Add(view.Unit);
        }
        return selected;
    }

    public void ClearSelection()
    {
        bool changed = false;
        foreach (var view in views)
        {
            if (!view.IsSelected) continue;
            view.SetSelected(false);
            changed = true;
        }
        if (changed) OnSelectionChanged?.Invoke();
    }

    /// <summary>Freezes/unfreezes clicking while a trip is travelling.</summary>
    public void SetSelectionEnabled(bool enabled)
    {
        selectionEnabled = enabled;
        foreach (var view in views)
        {
            view.SetSelectionEnabled(enabled);
        }
    }

    /// <summary>Re-evaluates which cards can be sent (idle, not already gathered today) and drops stale selections.</summary>
    public void RefreshAvailability()
    {
        bool changed = false;
        foreach (var view in views)
        {
            if (view.Unit == null) continue;

            if (view.IsSelected && !view.Unit.CanBeSentGathering)
            {
                view.SetSelected(false);
                changed = true;
            }
            view.SetSelectable(view.Unit.CanBeSentGathering);
            view.Refresh();
        }
        if (changed) OnSelectionChanged?.Invoke();
    }
}
