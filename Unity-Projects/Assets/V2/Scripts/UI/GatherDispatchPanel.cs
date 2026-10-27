using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Send-to-gather UI: click a cave on the map, pick one or two cards in the
/// character hand, then press the shared confirm button.
///
/// The trip resolves the moment the button is pressed, so nothing here locks the
/// UI afterwards - the player can immediately pick base work or end the day.
///
/// There is no dedicated message surface any more: the cave stats go to the log
/// when a cave is picked, and anything that would block a dispatch becomes a
/// centre warning.
/// </summary>
public class GatherDispatchPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private DispatchActionPanel actionPanel;
    [SerializeField] private UnitRosterView rosterView;
    [SerializeField] private WarningView warningView;

    // Trait counts of the sent units at dispatch time, so we can tell who gained a trait afterwards.
    private readonly Dictionary<UnitCharacter, int> traitsBefore = new Dictionary<UnitCharacter, int>();

    private void Start()
    {
        if (actionPanel != null) actionPanel.Clicked += OnDispatchClicked;
        else Debug.LogWarning("[GatherDispatchPanel] DispatchActionPanel is not assigned - the trip cannot be confirmed.");

        if (rosterView == null) rosterView = FindAnyObjectByType<UnitRosterView>();
        if (warningView == null) warningView = FindAnyObjectByType<WarningView>();

        if (rosterView != null)
        {
            rosterView.OnSelectionChanged += HandleSelectionChanged;
            rosterView.OnSelectionRejected += HandleSelectionRejected;
            rosterView.SetMode(CharacterHandPanel.SelectionMode.Dispatch);
        }
        else
        {
            Debug.LogWarning("[GatherDispatchPanel] UnitRosterView is missing - units cannot be selected.");
        }

        if (GatherMapController.Instance != null)
        {
            GatherMapController.Instance.OnTripResolved += HandleTripResolved;
            GatherMapController.Instance.OnSelectionChanged += HandleCaveSelected;
        }
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayStarted += HandleDayStarted;
        }
        if (SkillFlowController.Instance != null)
        {
            SkillFlowController.Instance.StateChanged += HandleSkillStateChanged;
        }

        RefreshAvailability();
        RefreshActionPanel();
    }

    private void OnDestroy()
    {
        if (actionPanel != null) actionPanel.Clicked -= OnDispatchClicked;

        if (rosterView != null)
        {
            rosterView.OnSelectionChanged -= HandleSelectionChanged;
            rosterView.OnSelectionRejected -= HandleSelectionRejected;
        }
        if (GatherMapController.Instance != null)
        {
            GatherMapController.Instance.OnTripResolved -= HandleTripResolved;
            GatherMapController.Instance.OnSelectionChanged -= HandleCaveSelected;
        }
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayStarted -= HandleDayStarted;
        }
        if (SkillFlowController.Instance != null)
        {
            SkillFlowController.Instance.StateChanged -= HandleSkillStateChanged;
        }
    }

    private void OnDispatchClicked()
    {
        // A pending Cook / Heal / Craft owns the button; this flow stays quiet.
        if (IsSkillActive()) return;

        var map = GatherMapController.Instance;
        if (map == null || map.SelectedNode == null)
        {
            Warn("Pick a cave on the map first.");
            return;
        }

        List<UnitCharacter> selected = rosterView != null
            ? rosterView.GetSelectedUnits()
            : new List<UnitCharacter>();

        if (selected.Count == 0)
        {
            Warn("Pick 1 or 2 character cards first.");
            return;
        }

        // Validate first so we can show the reason in the UI.
        if (!GatherDispatchController.Instance.CanDispatch(map.SelectedNode.Data.zone, selected, out string reason))
        {
            Warn(reason);
            return;
        }

        traitsBefore.Clear();
        foreach (var unit in selected) traitsBefore[unit] = unit.traits.Count;

        RunLog.Add(LogKind.Action, $"Sent {JoinNames(selected)} to the {map.SelectedNode.Data.zone} cave.");

        // Resolves synchronously and raises OnTripResolved inside this call.
        map.ConfirmTrip(selected);
    }

    private void HandleTripResolved(GatherDispatchResult result)
    {
        if (rosterView != null)
        {
            rosterView.ClearSelection();
            rosterView.RefreshAvailability();
        }

        if (result == null) return;

        // Loot/tier/hurt lines come from RunLogNarrator; this only adds the
        // detail the narrator cannot know - who gained a trait on the way home.
        foreach (var unit in result.sentUnits)
        {
            if (!traitsBefore.TryGetValue(unit, out int before)) continue;
            if (unit.traits.Count <= before) continue;

            var gained = unit.traits[unit.traits.Count - 1];
            RunLog.Add(LogKind.Success, $"{unit.unitName} gained a {gained.polarity} trait.");
        }

        traitsBefore.Clear();
    }

    private void HandleDayStarted(int day)
    {
        RefreshAvailability();
    }

    /// <summary>Logs what the cave costs as soon as it is picked, so the numbers are never hidden.</summary>
    private void HandleCaveSelected(MapNodeView node)
    {
        if (node != null)
        {
            GatherZoneConfig config = GatherZoneDatabase.Instance != null
                ? GatherZoneDatabase.Instance.GetConfig(node.Data.zone)
                : null;

            if (config != null)
            {
                RunLog.Add(LogKind.Action,
                    $"{node.Data.zone} cave — {config.apCost} AP per unit, success value {config.baseSuccessValue}, " +
                    $"loot {config.mainLoot.type} x{config.mainLoot.amount}.");
            }
            else
            {
                RunLog.Add(LogKind.Action, $"{node.Data.zone} cave selected.");
            }
        }

        RefreshActionPanel();
    }

    private void HandleSelectionChanged()
    {
        RefreshActionPanel();
    }

    private void HandleSelectionRejected(string reason)
    {
        Warn(reason);
    }

    private void Warn(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (warningView != null) warningView.Show(text, LogKind.Warning);
        RunLog.Add(LogKind.Warning, text);
    }

    /// <summary>
    /// The shared confirm button only exists while the player is mid-decision:
    /// hidden with nothing picked, enabled once both a cave and units are chosen.
    /// </summary>
    private void RefreshActionPanel()
    {
        if (actionPanel == null) return;

        // The skill flow is using the button right now - leave it alone.
        if (IsSkillActive()) return;

        var map = GatherMapController.Instance;
        bool hasCave = map != null && map.SelectedNode != null;
        int picked = rosterView != null ? rosterView.SelectedCount : 0;

        // The button exists only while the player is actually choosing a crew.
        if (picked == 0)
        {
            actionPanel.Hide();
            return;
        }

        // Grey the button out rather than blocking the card click: a unit that
        // already gathered today can still do base work.
        bool ready = false;

        if (hasCave)
        {
            var dispatcher = GatherDispatchController.Instance;
            ready = dispatcher != null &&
                    dispatcher.CanDispatch(map.SelectedNode.Data.zone, rosterView.GetSelectedUnits(), out _);
        }

        actionPanel.ShowDispatch(interactable: ready);
    }

    private static bool IsSkillActive()
    {
        return SkillFlowController.Instance != null && SkillFlowController.Instance.IsSkillActive;
    }

    /// <summary>Hands the confirm button back once a pending skill finishes or is cancelled.</summary>
    private void HandleSkillStateChanged()
    {
        if (IsSkillActive()) return;

        if (rosterView != null) rosterView.SetMode(CharacterHandPanel.SelectionMode.Dispatch);
        RefreshActionPanel();
    }

    /// <summary>Greys out cards that can't be picked right now.</summary>
    private void RefreshAvailability()
    {
        if (rosterView != null) rosterView.RefreshAvailability();
        RefreshActionPanel();
    }

    private static string JoinNames(List<UnitCharacter> units)
    {
        var names = new List<string>(units.Count);
        foreach (var unit in units) names.Add(unit.unitName);
        return string.Join(" & ", names);
    }
}