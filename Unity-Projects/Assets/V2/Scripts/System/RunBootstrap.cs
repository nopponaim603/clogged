using UnityEngine;

/// <summary>
/// Applies a queued save once the game scene has woken up.
///
/// It lives on its own object so the restoring code is not buried in a gameplay
/// script. The execution order is late on purpose: every other Awake (roster AP,
/// the randomised box colour) has already run by then, so the values written
/// here are the ones the scene reads on its first frame. Awake rather than Start
/// because an unfocused editor can stall before Start ever runs, and Continue
/// must not depend on the frame loop.
/// </summary>
[DefaultExecutionOrder(1000)]
public class RunBootstrap : MonoBehaviour
{
    private void Awake()
    {
        var data = SaveSystem.ConsumePendingLoad();
        if (data == null) return;

        Apply(data);
        RunLog.Add(LogKind.System, $"Run resumed on day {data.currentDay}.");
    }

    /// <summary>Restores a save onto the live scene. Public so tests and tools can replay one.</summary>
    public static void Apply(SaveData data)
    {
        if (data == null) return;

        var days = DayManager.Instance;
        if (days != null) days.RestoreDay(data.currentDay, data.deadlineDay);

        var res = RunResources.Instance;
        if (res != null) res.RestoreState(data);

        var units = UnitManager.Instance;
        if (units != null) units.RestoreUnits(data.units);

        RunStats.ApplySnapshot(data.stats);
    }
}
