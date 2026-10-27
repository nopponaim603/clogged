using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spawns one UnitIconView card per unit in UnitManager (same order as the
/// roster), keeps them up to date, and owns the gather selection: clicking a
/// card toggles it, max <see cref="maxSelection"/> at a time.
///
/// When a <see cref="CharacterHandPanel"/> is wired in, the hand owns the
/// container, card size and spacing, and decides which units are pickable
/// (dispatch vs skill mode). Without one, the legacy bottom-left layout and
/// the "can be sent gathering" rule are used.
/// </summary>
public class UnitRosterView : MonoBehaviour
{
    [SerializeField] private RectTransform container;
    [SerializeField] private UnitIconView iconPrefab;
    [SerializeField] private int maxSelection = 2;
    [SerializeField] private float cardSpacing = 14f;
    [SerializeField] private Vector2 layoutOrigin = Vector2.zero;

    [Header("Hand (optional)")]
    [Tooltip("Bottom-centre character hand. When set, it drives container/layout and selectability.")]
    [SerializeField] private CharacterHandPanel handPanel;

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
    public int MaxSelection => handPanel != null ? handPanel.MaxSelection : maxSelection;
    public bool HasSelection => SelectedCount > 0;
    public CharacterHandPanel HandPanel => handPanel;

    /// <summary>Switches the hand between the dispatch rule, the skill rule and heal targeting.</summary>
    public void SetMode(CharacterHandPanel.SelectionMode mode)
    {
        if (handPanel == null) return;

        handPanel.SetMode(mode);
        TrimSelectionToMax();
        RefreshAvailability();
    }

    /// <summary>Drops the oldest picks when a narrower mode allows fewer cards.</summary>
    private void TrimSelectionToMax()
    {
        int max = MaxSelection;
        int kept = 0;
        bool changed = false;

        foreach (var view in views)
        {
            if (!view.IsSelected) continue;

            kept++;
            if (kept <= max) continue;

            view.SetSelected(false);
            changed = true;
        }

        if (changed) OnSelectionChanged?.Invoke();
    }

    /// <summary>Whether this unit may be picked right now (mode-aware when a hand is wired).</summary>
    private bool CanPick(UnitCharacter unit)
    {
        if (unit == null) return false;
        return handPanel != null ? handPanel.CanSelect(unit) : unit.CanBeSentGathering;
    }

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
        if (handPanel != null && handPanel.CardsContainer != null) container = handPanel.CardsContainer;
        if (container == null) container = (RectTransform)transform;

        // With a hand panel the layout group lays the cards out (centred row);
        // without one, LayoutCard positions them manually.
        var layoutGroup = container.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup != null) layoutGroup.enabled = handPanel != null;
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
            // AP, wounds and "out today" all move without the roster being told,
            // so selectability is re-derived instead of cached on events.
            bool pickable = CanPick(view.Unit);
            view.SetSelectable(pickable);
            view.Refresh();

            // A unit can become unavailable mid-day (AP spent on base work,
            // hurt, away) - drop it from the selection so the confirm action
            // never uses an ineligible unit.
            if (view.IsSelected && !pickable)
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

            if (handPanel != null)
            {
                var rect = (RectTransform)view.transform;
                rect.sizeDelta = handPanel.CardSize;

                var element = view.GetComponent<LayoutElement>();
                if (element == null) element = view.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = handPanel.CardSize.x;
                element.preferredHeight = handPanel.CardSize.y;
            }
            else
            {
                LayoutCard((RectTransform)view.transform, i);
            }

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

        if (!card.IsSelected && SelectedCount >= MaxSelection)
        {
            OnSelectionRejected?.Invoke($"Select up to {MaxSelection} units ({MaxSelection} already picked).");
            return;
        }

        // Skill mode is single-select: picking another card replaces the current one.
        if (!card.IsSelected && MaxSelection == 1)
        {
            foreach (var view in views)
            {
                if (view != card && view.IsSelected) view.SetSelected(false);
            }
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

    /// <summary>The first picked unit in roster order, or null when nothing is picked.</summary>
    public UnitCharacter SelectedUnit
    {
        get
        {
            foreach (var view in views)
            {
                if (view.IsSelected && view.Unit != null) return view.Unit;
            }
            return null;
        }
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

    /// <summary>
    /// Re-evaluates which cards are pickable under the current mode and drops
    /// stale selections.
    /// </summary>
    public void RefreshAvailability()
    {
        bool changed = false;
        foreach (var view in views)
        {
            if (view.Unit == null) continue;

            bool pickable = CanPick(view.Unit);

            if (view.IsSelected && !pickable)
            {
                view.SetSelected(false);
                changed = true;
            }
            view.SetSelectable(pickable);
            view.Refresh();
        }
        if (changed) OnSelectionChanged?.Invoke();
    }
}
