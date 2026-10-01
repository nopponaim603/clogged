using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Real UI replacement for the test triggers: pick 1-2 units with toggles,
/// click a node on the map, then press Dispatch. Toggle order must match the
/// order of UnitManager.Units (Ann, Boon, Cia, Dan).
/// </summary>
public class GatherDispatchPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button dispatchButton;
    [SerializeField] private Toggle[] unitToggles;
    [SerializeField] private TMP_Text messageText;

    private bool tripInProgress;

    // Trait counts of the sent units at dispatch time, so we can tell who gained a trait afterwards.
    private readonly Dictionary<UnitCharacter, int> traitsBefore = new Dictionary<UnitCharacter, int>();

    private void Start()
    {
        dispatchButton.onClick.AddListener(OnDispatchClicked);

        foreach (var toggle in unitToggles)
        {
            Toggle captured = toggle;
            captured.onValueChanged.AddListener(isOn => OnToggleChanged(captured, isOn));
        }

        if (GatherMapController.Instance != null)
        {
            GatherMapController.Instance.OnTripResolved += HandleTripResolved;
        }
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayStarted += HandleDayStarted;
        }

        RefreshToggles();
        SetMessage("Pick a cave on the map and 1-2 units");
    }

    private void OnDestroy()
    {
        if (GatherMapController.Instance != null)
        {
            GatherMapController.Instance.OnTripResolved -= HandleTripResolved;
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
            return;
        }

        List<UnitCharacter> selected = GetSelectedUnits();

        // Validate first so we can show the reason in the UI, and so the
        // button never gets stuck disabled when ConfirmTrip bails out early.
        if (!GatherDispatchController.Instance.CanDispatch(map.SelectedNode.Data.zone, selected, out string reason))
        {
            SetMessage(reason);
            return;
        }

        traitsBefore.Clear();
        foreach (var unit in selected) traitsBefore[unit] = unit.traits.Count;

        tripInProgress = true;
        dispatchButton.interactable = false;
        SetMessage("Traveling...");
        map.ConfirmTrip(selected);
    }

    private void HandleTripResolved(GatherDispatchResult result)
    {
        tripInProgress = false;
        dispatchButton.interactable = true;

        foreach (var toggle in unitToggles) toggle.isOn = false;
        RefreshToggles();

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
        RefreshToggles();
    }

    /// <summary>Greys out units that can't be sent (Away/Hurt/Missing/Resting/already gathered today).</summary>
    private void RefreshToggles()
    {
        var units = UnitManager.Instance != null ? UnitManager.Instance.Units : null;
        if (units == null) return;

        for (int i = 0; i < unitToggles.Length; i++)
        {
            bool available = i < units.Count && units[i].CanBeSentGathering;
            unitToggles[i].interactable = available;
            if (!available) unitToggles[i].isOn = false;
        }
    }

    private void OnToggleChanged(Toggle changed, bool isOn)
    {
        if (!isOn) return;

        int count = 0;
        foreach (var toggle in unitToggles)
        {
            if (toggle.isOn) count++;
        }

        // Max 2 units per trip - undo the one that was just ticked.
        if (count > 2) changed.isOn = false;
    }

    private List<UnitCharacter> GetSelectedUnits()
    {
        var list = new List<UnitCharacter>();
        var units = UnitManager.Instance.Units;

        for (int i = 0; i < unitToggles.Length && i < units.Count; i++)
        {
            if (unitToggles[i].isOn) list.Add(units[i]);
        }
        return list;
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
