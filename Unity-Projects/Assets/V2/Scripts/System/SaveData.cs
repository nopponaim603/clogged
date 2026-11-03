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
    public const int CurrentVersion = 2;

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
    public int partySuccessBonus;

    // RunResources risk (red fog). Version 2 added it; older saves default to
    // full risk on load.
    public int risk = RunResources.MaxRisk;

    // AudioDirector - which BGM track this run is looping, so Continue resumes
    // the same song instead of rolling a new one mid-run.
    public int musicTrack = -1;

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
        public int currentSwords = 1;
        public int starvationStacks;
        public bool blessed;
    }
}
