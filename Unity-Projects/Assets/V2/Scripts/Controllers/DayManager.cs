using System;
using System.Collections.Generic;
using UnityEngine;

public enum RunOutcome
{
    InProgress,
    Win,
    LoseDeadline,
    LoseAllUnitsLost
}

/// <summary>What the day rollover did: who ate, who went hungry, and how the day moved.</summary>
public class DayEndReport
{
    public int dayBefore;
    public int dayAfter;
    public readonly List<UnitCharacter> fed = new List<UnitCharacter>();
    public readonly List<UnitCharacter> starved = new List<UnitCharacter>();
    /// <summary>Units that hit max starvation this rollover and went Missing.</summary>
    public readonly List<UnitCharacter> lostSignal = new List<UnitCharacter>();
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

    /// <summary>Fired right after feeding and the day counter advance, before the run-end checks resolve.</summary>
    public event Action<DayEndReport> OnDayEnded;

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

        if (RunResources.Instance != null && RunResources.Instance.HasBothCoreItems)
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

        bool hasBothCoreItems = RunResources.Instance != null && RunResources.Instance.HasBothCoreItems;
        if (currentDay > deadlineDay && !hasBothCoreItems)
        {
            DeclareOutcome(RunOutcome.LoseDeadline);
            return;
        }

        RefreshEligibleUnitsForNewDay();
        OnDayStarted?.Invoke(currentDay);
    }

    /// <summary>
    /// Dry-runs the feeding step so the UI can warn about a food shortfall
    /// before the player commits. Changes nothing.
    /// </summary>
    public DayEndReport PreviewFeeding()
    {
        var report = new DayEndReport { dayBefore = currentDay, dayAfter = currentDay + 1 };
        if (UnitManager.Instance == null) return report;

        int foodLeft = RunResources.Instance != null ? RunResources.Instance.Food : 0;

        foreach (var unit in UnitManager.Instance.Units)
        {
            if (unit.state == UnitState.Away || unit.state == UnitState.Missing) continue;

            if (foodLeft > 0)
            {
                foodLeft--;
                report.fed.Add(unit);
            }
            else
            {
                report.starved.Add(unit);
                if (unit.starvationStacks + 1 >= UnitCharacter.MaxStarvationStacks)
                {
                    report.lostSignal.Add(unit);
                }
            }
        }

        return report;
    }

    /// <summary>
    /// Feeds units not in Away or Missing, in roster order, until food runs
    /// out. Anyone eligible who doesn't get fed takes a starvation stack.
    /// </summary>
    private void FeedEligibleUnitsInOrder()
    {
        var report = new DayEndReport { dayBefore = currentDay, dayAfter = currentDay + 1 };
        if (UnitManager.Instance == null) return;

        foreach (var unit in UnitManager.Instance.Units)
        {
            if (unit.state == UnitState.Away || unit.state == UnitState.Missing) continue;

            bool fed = RunResources.Instance != null && RunResources.Instance.TryConsumeFood();
            if (!fed)
            {
                unit.ApplyStarvationStack();
                report.starved.Add(unit);
                if (unit.state == UnitState.Missing) report.lostSignal.Add(unit);
            }
            else
            {
                report.fed.Add(unit);
            }
        }

        OnDayEnded?.Invoke(report);
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

    /// <summary>Call right after obtaining a Core Item piece - wins immediately once both pieces are held, per the doc's "ชนะทันทีถ้ายังไม่เกินวันที่ 7" rule.</summary>
    public void CheckImmediateWin()
    {
        if (Outcome != RunOutcome.InProgress) return;
        if (RunResources.Instance != null && RunResources.Instance.HasBothCoreItems && currentDay <= deadlineDay)
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
