using System;

/// <summary>
/// Per-run tallies shown on the win/lose screen.
///
/// Static for the same reason <see cref="RunLog"/> is: the systems that produce
/// these numbers (base work, day rollover, gather dispatch) already sit behind
/// singletons, but a second inspector wiring just to count is not worth it.
/// <see cref="Reset"/> runs on every play-mode entry and whenever the player
/// starts a fresh run, and the snapshot pair lets a save file carry the counts.
/// </summary>
public static class RunStats
{
    public static int DaysPlayed { get; private set; }
    public static int HealsPerformed { get; private set; }
    public static int CookRounds { get; private set; }
    public static int FoodCooked { get; private set; }
    public static int CraftRounds { get; private set; }
    public static int MedicineCrafted { get; private set; }
    public static int FoodConsumed { get; private set; }
    public static int StarvationTicks { get; private set; }
    public static int GatherTrips { get; private set; }
    public static int UnitsLost { get; private set; }

    public static void Reset()
    {
        DaysPlayed = 0;
        HealsPerformed = 0;
        CookRounds = 0;
        FoodCooked = 0;
        CraftRounds = 0;
        MedicineCrafted = 0;
        FoodConsumed = 0;
        StarvationTicks = 0;
        GatherTrips = 0;
        UnitsLost = 0;
    }

    /// <summary>Called from <see cref="BaseWorkController"/>'s single emit funnel.</summary>
    public static void CountBaseWork(BaseWorkResult result)
    {
        if (result == null) return;

        switch (result.workType)
        {
            case BaseWorkType.Healing:
                if (!result.blocked && result.healedUnit != null) HealsPerformed++;
                break;
            case BaseWorkType.Cooking:
                CookRounds += result.repeatsPerformed;
                FoodCooked += result.foodProduced;
                break;
            case BaseWorkType.Workshop:
                CraftRounds += result.repeatsPerformed;
                MedicineCrafted += result.medicineProduced;
                break;
        }
    }

    /// <summary>Called from the day rollover with what it actually did.</summary>
    public static void CountDayEnd(DayEndReport report)
    {
        if (report == null) return;

        DaysPlayed = Math.Max(DaysPlayed, report.dayAfter - 1);
        FoodConsumed += report.fed.Count;
        StarvationTicks += report.starved.Count;
        UnitsLost += report.lostSignal.Count;
    }

    /// <summary>Called from the gather roll with the resolved trip.</summary>
    public static void CountGather(GatherDispatchResult result)
    {
        if (result == null) return;

        GatherTrips++;
        if (result.missingUnit != null) UnitsLost++;
    }

    public static RunStatsSnapshot TakeSnapshot()
    {
        return new RunStatsSnapshot
        {
            daysPlayed = DaysPlayed,
            heals = HealsPerformed,
            cookRounds = CookRounds,
            foodCooked = FoodCooked,
            craftRounds = CraftRounds,
            medicineCrafted = MedicineCrafted,
            foodConsumed = FoodConsumed,
            starvationTicks = StarvationTicks,
            gatherTrips = GatherTrips,
            unitsLost = UnitsLost
        };
    }

    public static void ApplySnapshot(RunStatsSnapshot snapshot)
    {
        if (snapshot == null) return;

        DaysPlayed = snapshot.daysPlayed;
        HealsPerformed = snapshot.heals;
        CookRounds = snapshot.cookRounds;
        FoodCooked = snapshot.foodCooked;
        CraftRounds = snapshot.craftRounds;
        MedicineCrafted = snapshot.medicineCrafted;
        FoodConsumed = snapshot.foodConsumed;
        StarvationTicks = snapshot.starvationTicks;
        GatherTrips = snapshot.gatherTrips;
        UnitsLost = snapshot.unitsLost;
    }

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLoad() => Reset();
}

/// <summary>Serialisable copy of <see cref="RunStats"/> for the save file.</summary>
[Serializable]
public class RunStatsSnapshot
{
    public int daysPlayed;
    public int heals;
    public int cookRounds;
    public int foodCooked;
    public int craftRounds;
    public int medicineCrafted;
    public int foodConsumed;
    public int starvationTicks;
    public int gatherTrips;
    public int unitsLost;
}
