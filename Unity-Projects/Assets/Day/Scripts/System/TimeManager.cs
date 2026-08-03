using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public float DayLength = 120f;
    public float CurrentTime;
    public bool RoundActive = true;
    public bool IsMissionRunning = false;

    /// <summary>Fired exactly once, the moment the day's timer reaches zero.</summary>
    public event Action OnDayEnded;

    private void Start()
    {
        StartDay();
    }

    public void StartDay()
    {
        CurrentTime = DayLength;
        RoundActive = true;
        IsMissionRunning = false;
    }

    public void AdvanceTime(float deltaTime)
    {
        if (!RoundActive) return;
        if (!IsMissionRunning) return;

        CurrentTime -= deltaTime;

        if (CurrentTime <= 0f)
        {
            CurrentTime = 0f;
            RoundActive = false;
            IsMissionRunning = false;
            OnDayEnded?.Invoke();
        }
    }
}
