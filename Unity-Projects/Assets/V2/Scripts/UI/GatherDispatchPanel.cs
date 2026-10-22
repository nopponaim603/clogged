using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Send-to-gather UI: click one or two unit cards in the bottom-left roster
/// (the cards are the selection control now, not the old toggles), click a
/// cave on the map, then press Dispatch.
/// </summary>
public class GatherDispatchPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button dispatchButton;
    [SerializeField] private UnitRosterView rosterView;
    [SerializeField] private WarningView warningView;
    [SerializeField] private TMP_Text messageText;

    private bool tripInProgress;

    // Trait counts of the sent units at dispatch time, so we can tell who gained a trait afterwards.
    private readonly Dictionary<UnitCharacter, int> traitsBefore = new Dictionary<UnitCharacter, int>();

    private void Start()
    {
        dispatchButton.onClick.AddListener(OnDispatchClicked);

        if (rosterView == null) rosterView = FindAnyObjectByType<UnitRosterView>();
        if (warningView == null) warningView = FindAnyObjectByType<WarningView>();
        if (rosterView != null)
        {
            rosterView.OnSelectionChanged += HandleSelectionChanged;
            rosterView.OnSelectionRejected += HandleSelectionRejected;
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

        RefreshAvailability();
        RefreshMessage();
    }

    private void OnDestroy()
    {
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
    }

    private void OnDispatchClicked()
    {
        if (tripInProgress) return;

        var map = GatherMapController.Instance;
        if (map == null || map.SelectedNode == null)
        {
            SetMessage("No cave selected");
            ShowWarning("Pick a cave on the map first.", WarningKind.Warning);
            return;
        }

        List<UnitCharacter> selected = rosterView != null
            ? rosterView.GetSelectedUnits()
            : new List<UnitCharacter>();

        // Validate first so we can show the reason in the UI, and so the
        // button never gets stuck disabled when ConfirmTrip bails out early.
        if (!GatherDispatchController.Instance.CanDispatch(map.SelectedNode.Data.zone, selected, out string reason))
        {
            SetMessage(reason);
            ShowWarning(reason, WarningKind.Warning);
            return;
        }

        traitsBefore.Clear();
        foreach (var unit in selected) traitsBefore[unit] = unit.traits.Count;

        tripInProgress = true;
        dispatchButton.interactable = false;
        if (rosterView != null) rosterView.SetSelectionEnabled(false);
        SetMessage("Traveling...");
        map.ConfirmTrip(selected);
    }

    private void HandleTripResolved(GatherDispatchResult result)
    {
        tripInProgress = false;
        dispatchButton.interactable = true;

        if (rosterView != null)
        {
            rosterView.SetSelectionEnabled(true);
            rosterView.ClearSelection();
            rosterView.RefreshAvailability();
        }

        if (result == null)
        {
            SetMessage("Trip cancelled");
            return;
        }

        string text =
            $"{result.zone}: {result.tier} (roll {result.rollValue}/{result.successValue})\n" +
            $"Got {FormatLoot(result.mainLootGranted)}" +
            (result.bonusLootGranted.HasValue ? $" + bonus {FormatLoot(result.bonusLootGranted)}" : "");

        // One line per unit that was sent.
        foreach (var unit in result.sentUnits)
        {
            text += $"\n{unit.unitName}: {StatusWord(unit)} (AP {unit.currentActionPoints}/{unit.maxActionPoints})";

            if (traitsBefore.TryGetValue(unit, out int before) && unit.traits.Count > before)
            {
                text += $", gained a {unit.traits[unit.traits.Count - 1].polarity} trait";
            }
        }

        if (result.unlockedCoreItemMaterial) text += "\nUnlocked the Core Item material!";
        if (result.obtainedCoreItem) text += "\nGot the Core Item!";

        SetMessage(text);
        Debug.Log("[GatherDispatchPanel] " + text.Replace("\n", " | "));
    }

    private void HandleDayStarted(int day)
    {
        RefreshAvailability();
        RefreshMessage();
    }

    private void HandleCaveSelected(MapNodeView node)
    {
        if (tripInProgress) return;
        RefreshMessage();
    }

    private void HandleSelectionChanged()
    {
        if (tripInProgress) return;
        RefreshMessage();
    }

    /// <summary>
    /// Builds the MessageUi text from the current cave + unit selection, so
    /// clicking a cave always tells the player what it costs.
    /// </summary>
    private void RefreshMessage()
    {
        var map = GatherMapController.Instance;
        MapNodeView node = map != null ? map.SelectedNode : null;
        List<UnitCharacter> selected = rosterView != null
            ? rosterView.GetSelectedUnits()
            : new List<UnitCharacter>();

        if (node == null && selected.Count == 0)
        {
            SetMessage("Pick a cave on the map, then click 1-2 unit cards");
            return;
        }

        string text = "";

        if (node != null)
        {
            GatherZone zone = node.Data.zone;
            GatherZoneConfig config = GatherZoneDatabase.Instance != null
                ? GatherZoneDatabase.Instance.GetConfig(zone)
                : null;

            if (config != null)
            {
                text += $"{zone} cave — {config.apCost} AP per unit, base success {config.baseSuccessValue}, loot {config.mainLoot.type} x{config.mainLoot.amount}";
            }
            else
            {
                text += $"{zone} cave";
            }
        }
        else
        {
            text += "No cave selected (click a cave, click again to clear)";
        }

        if (selected.Count > 0)
        {
            var names = new List<string>(selected.Count);
            foreach (var unit in selected) names.Add(unit.unitName);
            text += $"\nUnits {selected.Count}/{rosterView.MaxSelection}: {string.Join(", ", names)}";
        }
        else
        {
            text += "\nUnits: none selected";
        }

        SetMessage(text);
    }

    private void HandleSelectionRejected(string reason)
    {
        ShowWarning(reason, WarningKind.Error);
    }

    private void ShowWarning(string text, WarningKind kind)
    {
        if (warningView != null) warningView.Show(text, kind);
    }

    /// <summary>Greys out cards that can't be sent (Away/Hurt/Missing/Resting/already gathered today).</summary>
    private void RefreshAvailability()
    {
        if (rosterView != null) rosterView.RefreshAvailability();
    }

    private static string StatusWord(UnitCharacter unit)
    {
        switch (unit.state)
        {
            case UnitState.Hurt: return "HURT";
            case UnitState.Missing: return "MISSING";
            case UnitState.Resting: return "safe, resting";
            default: return "safe";
        }
    }

    private static string FormatLoot(LootAmount? loot)
    {
        return loot.HasValue ? $"{loot.Value.type} x{loot.Value.amount}" : "nothing";
    }

    private void SetMessage(string text)
    {
        if (messageText != null) messageText.text = text;
    }
}
