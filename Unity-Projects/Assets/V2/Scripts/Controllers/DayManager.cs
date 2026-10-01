using System;
using UnityEngine;

public enum RunOutcome
{
    InProgress,
    Win,
    LoseDeadline,
    LoseAllUnitsLost
}

/// <summary>
/// Drives the End Day button: feeds eligible units in roster order (left to
/// right, skipping Away/Missing), applies starvation to anyone food ran out
/// on, advances the day counter, checks win/lose, then refreshes AP for the
/// next day.
/// </summary>
public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("Run Length")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private int deadlineDay = 7;

    public int CurrentDay => currentDay;
    public int DeadlineDay => deadlineDay;
    public RunOutcome Outcome { get; private set; } = RunOutcome.InProgress;

    /// <summary>Fired after a new day's AP refresh completes (run still in progress).</summary>
    public event Action<int> OnDayStarted;
    public event Action<RunOutcome> OnRunEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>Call this from the End Day button.</summary>
    public void EndDay()
    {
        if (Outcome != RunOutcome.InProgress) return; // run already decided, button should be disabled

        if (RunResources.Instance != null && RunResources.Instance.HasCoreItem)
        {
            DeclareOutcome(RunOutcome.Win);
            return;
        }

        FeedEligibleUnitsInOrder();

        currentDay++;

        if (UnitManager.Instance != null && UnitManager.Instance.AllUnitsLost())
        {
            DeclareOutcome(RunOutcome.LoseAllUnitsLost);
            return;
        }

        bool hasCoreItem = RunResources.Instance != null && RunResources.Instance.HasCoreItem;
        if (currentDay > deadlineDay && !hasCoreItem)
        {
            DeclareOutcome(RunOutcome.LoseDeadline);
            return;
        }

        RefreshEligibleUnitsForNewDay();
        OnDayStarted?.Invoke(currentDay);
    }

    /// <summary>
    /// Feeds units not in Away or Missing, in roster order, until food runs
    /// out. Anyone eligible who doesn't get fed takes a starvation stack.
    /// </summary>
    private void FeedEligibleUnitsInOrder()
    {
        if (UnitManager.Instance == null) return;

        foreach (var unit in UnitManager.Instance.Units)
        {
            if (unit.state == UnitState.Away || unit.state == UnitState.Missing) continue;

            bool fed = RunResources.Instance != null && RunResources.Instance.TryConsumeFood();
            if (!fed)
            {
                unit.ApplyStarvationStack();
            }
        }
    }

    /// <summary>Away units are out resolving their trip and aren't refreshed here - the send-to-gather system handles their return.</summary>
    private void RefreshEligibleUnitsForNewDay()
    {
        if (UnitManager.Instance == null) return;

        foreach (var unit in UnitManager.Instance.Units)
        {
            if (unit.state == UnitState.Missing || unit.state == UnitState.Away) continue;
            unit.RefreshForNewDay();
        }
    }

    /// <summary>Call right after obtaining the Core Item - wins immediately if still within the deadline, per the doc's "ชนะทันทีถ้ายังไม่เกินวันที่ 7" rule.</summary>
    public void CheckImmediateWin()
    {
        if (Outcome != RunOutcome.InProgress) return;
        if (RunResources.Instance != null && RunResources.Instance.HasCoreItem && currentDay <= deadlineDay)
        {
            DeclareOutcome(RunOutcome.Win);
        }
    }

    private void DeclareOutcome(RunOutcome outcome)
    {
        Outcome = outcome;
        OnRunEnded?.Invoke(outcome);
    }
}
