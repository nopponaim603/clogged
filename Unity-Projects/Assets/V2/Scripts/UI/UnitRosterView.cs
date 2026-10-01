using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns one UnitIconView per unit in UnitManager (same order as the roster)
/// and keeps them up to date. Polling in Update is fine for 4 icons and means
/// no event wiring is needed - any change to AP/state shows up automatically.
/// </summary>
public class UnitRosterView : MonoBehaviour
{
    [SerializeField] private RectTransform container;
    [SerializeField] private UnitIconView iconPrefab;

    private readonly List<UnitIconView> views = new List<UnitIconView>();

    private void Start()
    {
        if (UnitManager.Instance == null)
        {
            Debug.LogWarning("[UnitRosterView] UnitManager.Instance is missing.");
            return;
        }

        foreach (var unit in UnitManager.Instance.Units)
        {
            UnitIconView view = Instantiate(iconPrefab, container);
            view.Bind(unit);
            views.Add(view);
        }
    }

    private void Update()
    {
        foreach (var view in views)
        {
            view.Refresh();
        }
    }
}
