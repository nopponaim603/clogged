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
            aptitudes = new List<AptitudeTag> { AptitudeTag.Speed, AptitudeTag.Near },
            nonAptitudes = new List<AptitudeTag> { AptitudeTag.Far }
        });
        units.Add(new UnitCharacter
        {
            unitName = "Boon",
            aptitudes = new List<AptitudeTag> { AptitudeTag.Endurance, AptitudeTag.Far },
            nonAptitudes = new List<AptitudeTag> { AptitudeTag.Cooking }
        });
        units.Add(new UnitCharacter
        {
            unitName = "Cia",
            aptitudes = new List<AptitudeTag> { AptitudeTag.Gathering, AptitudeTag.Mid, AptitudeTag.Cooking },
            nonAptitudes = new List<AptitudeTag> { AptitudeTag.Combat }
        });
        units.Add(new UnitCharacter
        {
            unitName = "Dan",
            aptitudes = new List<AptitudeTag> { AptitudeTag.Combat, AptitudeTag.Far },
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
}
