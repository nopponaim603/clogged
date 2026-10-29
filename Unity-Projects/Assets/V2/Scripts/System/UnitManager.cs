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
        }
    }

    /// <summary>
    /// Default starting roster from Clogged A 0.2's "ยูนิตตั้งต้น" table.
    /// Aptitude = +20 to success value, Non-aptitude = -15.
    /// </summary>
    private void SetupDefaultRoster()
    {
        units.Add(new UnitCharacter
        {
            unitName = "Ann",
            aptitudes = new List<AptitudeTag> { AptitudeTag.Speed, AptitudeTag.Easy },
            nonAptitudes = new List<AptitudeTag> { AptitudeTag.Hard }
        });
        units.Add(new UnitCharacter
        {
            unitName = "Boon",
            aptitudes = new List<AptitudeTag> { AptitudeTag.Endurance, AptitudeTag.Hard },
            nonAptitudes = new List<AptitudeTag> { AptitudeTag.Cooking }
        });
        units.Add(new UnitCharacter
        {
            unitName = "Cia",
            aptitudes = new List<AptitudeTag> { AptitudeTag.Gathering, AptitudeTag.Normal, AptitudeTag.Cooking },
            nonAptitudes = new List<AptitudeTag> { AptitudeTag.Combat }
        });
        units.Add(new UnitCharacter
        {
            unitName = "Dan",
            aptitudes = new List<AptitudeTag> { AptitudeTag.Combat, AptitudeTag.Hard },
            nonAptitudes = new List<AptitudeTag> { AptitudeTag.Cooking }
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

    /// <summary>True when every unit is Missing (Lost Signal) - one of the lose conditions.</summary>
    public bool AllUnitsLost()
    {
        return AliveCount() == 0;
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

            unit.state = (UnitState)Mathf.Clamp(snapshot.state, 0, (int)UnitState.Missing);
            unit.currentActionPoints = Mathf.Max(0, snapshot.currentActionPoints);
            unit.starvationStacks = Mathf.Clamp(snapshot.starvationStacks, 0, UnitCharacter.MaxStarvationStacks);
        }
    }
}
