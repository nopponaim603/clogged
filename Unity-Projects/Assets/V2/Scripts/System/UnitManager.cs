using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds and initializes the fixed 4-unit roster. Other systems (Day System,
/// Base Work, Send-to-Gather) read/modify units through this singleton
/// rather than holding their own references.
/// </summary>
public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }

    [SerializeField] private List<UnitCharacter> units = new List<UnitCharacter>();

    public IReadOnlyList<UnitCharacter> Units => units;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (units.Count == 0)
        {
            SetupDefaultRoster();
        }

        foreach (var unit in units)
        {
            unit.currentActionPoints = unit.maxActionPoints;
            unit.currentSwords = unit.maxSwords;
        }
    }

    /// <summary>
    /// Fixed starting roster. Each unit has one main aptitude (the dungeon tier
    /// it is good at, giving the team +25) and one talent (its secondary job).
    /// </summary>
    private void SetupDefaultRoster()
    {
        units.Add(new UnitCharacter
        {
            unitName = "Ann",
            primaryAptitude = ZoneAptitude.Easy,
            talent = UnitTalent.Heal
        });
        units.Add(new UnitCharacter
        {
            unitName = "Boon",
            primaryAptitude = ZoneAptitude.Hard,
            talent = UnitTalent.Crafting
        });
        units.Add(new UnitCharacter
        {
            unitName = "Cia",
            primaryAptitude = ZoneAptitude.Normal,
            talent = UnitTalent.Cooking
        });
        units.Add(new UnitCharacter
        {
            unitName = "Dan",
            primaryAptitude = ZoneAptitude.Hard,
            talent = UnitTalent.Exploring
        });
    }

    public int AliveCount()
    {
        int count = 0;
        foreach (var unit in units)
        {
            if (unit.IsAlive) count++;
        }
        return count;
    }

    /// <summary>True when every unit is dead - one of the lose conditions.</summary>
    public bool AllUnitsLost()
    {
        return AliveCount() == 0;
    }

    /// <summary>
    /// True when nobody can act any more: every unit is Hurt or dead. Healing
    /// needs a Ready healer, so this state cannot be walked back - it is the
    /// soft-lock the run is ended on. A unit that is Away (mid-trip) or Resting
    /// (out of AP until tomorrow) still counts as able, so the check never fires
    /// in the middle of a trip or on an empty AP bar.
    /// </summary>
    public bool AllUnitsIncapacitated()
    {
        if (units.Count == 0) return false;

        foreach (var unit in units)
        {
            if (unit.state != UnitState.Hurt && unit.state != UnitState.Death) return false;
        }

        return true;
    }

    /// <summary>
    /// Puts roster state back from a save. Units are matched by name, and any
    /// entry the save does not mention is left at its scene default so an old
    /// save cannot half-initialise the roster.
    /// </summary>
    public void RestoreUnits(System.Collections.Generic.List<SaveData.UnitSnapshot> snapshots)
    {
        if (snapshots == null || snapshots.Count == 0) return;

        foreach (var snapshot in snapshots)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.unitName)) continue;

            UnitCharacter unit = units.Find(u => u.unitName == snapshot.unitName);
            if (unit == null) continue;

            unit.state = (UnitState)Mathf.Clamp(snapshot.state, 0, (int)UnitState.Death);
            unit.currentActionPoints = Mathf.Max(0, snapshot.currentActionPoints);
            unit.currentSwords = Mathf.Clamp(snapshot.currentSwords, 0, unit.maxSwords);
            unit.starvationStacks = Mathf.Clamp(snapshot.starvationStacks, 0, UnitCharacter.MaxStarvationStacks);
            unit.blessed = snapshot.blessed;
        }
    }
}
