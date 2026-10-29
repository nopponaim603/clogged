using System;
using System.Collections.Generic;
using UnityEngine;

public enum LogKind
{
    /// <summary>Player-initiated action (dispatch, pick a skill, end day).</summary>
    Action,
    /// <summary>Something worked out.</summary>
    Success,
    /// <summary>Something was refused or came out badly.</summary>
    Fail,
    /// <summary>Blocking rule violation - also shown in the centre warning.</summary>
    Warning,
    /// <summary>Day rolled over.</summary>
    DayEnd,
    /// <summary>Anything else worth recording.</summary>
    System
}

public readonly struct LogEntry
{
    public readonly LogKind Kind;
    public readonly string Text;
    public readonly int Day;

    public LogEntry(LogKind kind, string text, int day)
    {
        Kind = kind;
        Text = text;
        Day = day;
    }
}

/// <summary>
/// Run-wide action history shown in the on-screen log panel: every player
/// action, its outcome, and every warning. Newest entry is last, so the panel
/// can append at the bottom and scroll off the top.
///
/// Static rather than a scene singleton so any system can write to it without
/// an inspector reference; statics are reset on every play-mode entry.
/// </summary>
public static class RunLog
{
    /// <summary>How many entries are kept. Older ones are dropped.</summary>
    public static int Capacity { get; set; } = 16;

    private static readonly List<LogEntry> entries = new List<LogEntry>();

    public static event Action<LogEntry> OnEntryAdded;
    public static event Action OnCleared;

    public static IReadOnlyList<LogEntry> Entries => entries;

    public static void Add(LogKind kind, string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        var entry = new LogEntry(kind, message, DayManager.Instance != null ? DayManager.Instance.CurrentDay : 0);
        entries.Add(entry);

        int overflow = entries.Count - Capacity;
        if (overflow > 0) entries.RemoveRange(0, overflow);

        OnEntryAdded?.Invoke(entry);
    }

    public static void Clear()
    {
        entries.Clear();
        OnCleared?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        entries.Clear();
        OnEntryAdded = null;
        OnCleared = null;
        Capacity = 16;
    }
}
