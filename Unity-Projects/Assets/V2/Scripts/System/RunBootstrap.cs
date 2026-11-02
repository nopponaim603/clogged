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
    [Header("HUD icons")]
    [Tooltip("Cog shown in the OPTIONS button. Assigned in the scene because the top bar is built in code.")]
    [SerializeField] private Sprite optionsIcon;
    [Tooltip("Question mark shown in the HELP button.")]
    [SerializeField] private Sprite helpIcon;

    private void Awake()
    {
        HudTopBar.EnsureExists(optionsIcon, helpIcon);
        DiceRollView.EnsureExists();

        var director = AudioDirector.EnsureExists();
        var data = SaveSystem.ConsumePendingLoad();
        if (data == null)
        {
            // Fresh run: NewGame already rolled the track, so this only has to
            // start it. A scene opened directly (never through the menu) falls
            // back to a roll of its own.
            if (AudioDirector.PendingRunTrack < 0) AudioDirector.PendingRunTrack = AudioDirector.RollRunTrack();
            director.BeginRun(AudioDirector.PendingRunTrack);
            return;
        }

        Apply(data);
        RunLog.Add(LogKind.System, $"Run resumed on day {data.currentDay}.");

        // Resuming must not restart the music mid-run, so the save names the
        // track. A save written before the track existed re-rolls once and from
        // then on keeps that choice.
        AudioDirector.PendingRunTrack = data.musicTrack >= 0 ? data.musicTrack : AudioDirector.RollRunTrack();
        director.BeginRun(AudioDirector.PendingRunTrack);
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

        var gather = GatherDispatchController.Instance;
        if (gather != null) gather.RestorePity(data.pityNormal, data.pityHard);

        RunStats.ApplySnapshot(data.stats);
    }
}
