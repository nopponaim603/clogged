using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public float DayLength = 120f;
    public float CurrentTime;
    public bool RoundActive = true;

    /// <summary>
    /// True while one or more missions are actively running. This is a
    /// reference count under the hood (via BeginMission/EndMission) instead
    /// of a plain bool, because multiple crew can be dispatched at once
    /// (DispatchAll) — a plain bool gets clobbered when one mission's finally
    /// block sets it false while another mission is still running.
    /// </summary>
    public bool IsMissionRunning => _activeMissionCount > 0;

    private int _activeMissionCount = 0;

    /// <summary>Fired exactly once, whenever the day ends — whether the clock ran out or EndDay() was called early.</summary>
    public event Action OnDayEnded;

    private void Start()
    {
        StartDay();
    }

    public void StartDay()
    {
        CurrentTime = DayLength;
        RoundActive = true;
        _activeMissionCount = 0;
    }

    /// <summary>Call when a mission coroutine starts.</summary>
    public void BeginMission()
    {
        _activeMissionCount++;
    }

    /// <summary>
    /// Call when a mission coroutine finishes (in its finally block). The
    /// moment the LAST active mission returns — the dispatched queue is
    /// empty — the day ends automatically, even if CurrentTime has time
    /// left. The clock is kept running purely as a visual gauge / fallback
    /// (see AdvanceTime) in case no missions ever get dispatched at all.
    /// </summary>
    public void EndMission()
    {
        _activeMissionCount = Mathf.Max(0, _activeMissionCount - 1);

        if (_activeMissionCount == 0 && RoundActive)
        {
            EndDay();
        }
    }

    public void AdvanceTime(float deltaTime)
    {
        if (!RoundActive) return;
        if (!IsMissionRunning) return;

        CurrentTime -= deltaTime;

        if (CurrentTime <= 0f)
        {
            CurrentTime = 0f;
            EndDay();
        }
    }

    /// <summary>
    /// Ends the day immediately, regardless of how much CurrentTime is left.
    /// Used both when the clock naturally hits zero (see AdvanceTime) and
    /// when the player chooses to end the day early — e.g. once the mission
    /// queue is empty and nothing is left running (see GameManager.EndDayEarly()).
    /// </summary>
    public void EndDay()
    {
        if (!RoundActive) return;

        RoundActive = false;
        OnDayEnded?.Invoke();
    }
}
