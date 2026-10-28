using System;
using System.Collections.Generic;

/// <summary>
/// Everything needed to put a run back exactly where the player left it.
///
/// Plain serialisable data with no Unity object references, because
/// <see cref="UnityEngine.JsonUtility"/> persists it straight to disk. Enums are
/// stored as ints so a reordered enum cannot silently reinterpret an old save.
/// </summary>
[Serializable]
public class SaveData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public string savedAtUtc = "";

    // DayManager
    public int currentDay = 1;
    public int deadlineDay = 7;

    // RunResources
    public int food;
    public int cookingMaterial;
    public int medicine;
    public int materials;
    public bool coreItemA;
    public bool coreItemB;
    public int variant;

    // UnitManager
    public List<UnitSnapshot> units = new List<UnitSnapshot>();

    // RunStats
    public RunStatsSnapshot stats = new RunStatsSnapshot();

    /// <summary>One roster entry. Units are matched by name - the roster is fixed for a run.</summary>
    [Serializable]
    public class UnitSnapshot
    {
        public string unitName;
        public int state;
        public int currentActionPoints;
        public int starvationStacks;
    }
}
