using System.Collections.Generic;
using UnityEngine;

public enum UnitState
{
    Idle,       // available, can be dispatched or given base work
    Resting,    // AP spent for the day, unusable until next day
    Hurt,       // injured, needs healing before it can act
    Away,       // out on a gathering trip
    Missing     // Lost Signal - out of the run permanently
}

/// <summary>
/// Tags used for both the "ที่ใกล้/กลาง/ไกล" gather zones and base-work job
/// types. A unit's Aptitude/NonAptitude lists are checked against these when
/// computing roll success value or base-work output.
/// </summary>
public enum AptitudeTag
{
    Near,
    Mid,
    Far,
    Cooking,
    Combat,
    Gathering,
    Speed,
    Endurance
}

public enum TraitPolarity
{
    Positive,
    Negative
}

[System.Serializable]
public class UnitTrait
{
    public string traitName;
    public TraitPolarity polarity;
}

/// <summary>
/// One of the 4 fixed units. Plain data class (not a MonoBehaviour) - the
/// roster is small and fixed for a run, held and iterated by UnitManager.
/// </summary>
[System.Serializable]
public class UnitCharacter
{
    public const int MaxStarvationStacks = 3;

    public string unitName;
    public UnitState state = UnitState.Idle;

    [Header("Action Points")]
    public int maxActionPoints = 3;
    public int currentActionPoints;

    [Header("Aptitude (+20 success value / relevant base-work output)")]
    public List<AptitudeTag> aptitudes = new List<AptitudeTag>();

    [Header("Non-aptitude (-15 success value)")]
    public List<AptitudeTag> nonAptitudes = new List<AptitudeTag>();

    [Header("Traits")]
    public List<UnitTrait> traits = new List<UnitTrait>();

    [Header("Starvation")]
    [Range(0, MaxStarvationStacks)]
    public int starvationStacks = 0;

    /// <summary>A unit can only be sent gathering once per day, even though it may return to Idle with AP left over.</summary>
    public bool HasGatheredToday { get; private set; }

    public bool IsAlive => state != UnitState.Missing;
    public bool CanBeDispatched => state == UnitState.Idle;
    public bool CanBeSentGathering => state == UnitState.Idle && !HasGatheredToday;

    public void MarkGatheredToday() => HasGatheredToday = true;

    public bool HasAptitude(AptitudeTag tag) => aptitudes.Contains(tag);
    public bool HasNonAptitude(AptitudeTag tag) => nonAptitudes.Contains(tag);

    /// <summary>Starvation stacks add +1 AP cost each, per the starvation system.</summary>
    public int GetActualApCost(int baseApCost) => baseApCost + starvationStacks;

    /// <summary>
    /// Attempts to spend AP for an action (base work, gathering, healing, crafting).
    /// Returns false if the unit doesn't have enough AP - caller should not
    /// proceed with the action in that case. Drops the unit to Resting once
    /// AP hits 0, matching the doc's state rules.
    /// </summary>
    public bool TrySpendActionPoints(int baseCost)
    {
        int actualCost = GetActualApCost(baseCost);
        if (currentActionPoints < actualCost) return false;

        currentActionPoints -= actualCost;
        if (currentActionPoints <= 0)
        {
            currentActionPoints = 0;
            if (state == UnitState.Idle)
            {
                state = UnitState.Resting;
            }
        }
        return true;
    }

    /// <summary>Called by the Day System at the start of a new day, after food has been paid.</summary>
    public void RefreshForNewDay()
    {
        currentActionPoints = maxActionPoints;
        HasGatheredToday = false;
        if (state == UnitState.Resting)
        {
            state = UnitState.Idle;
        }
        // Hurt / Away / Missing are left as-is here - they're cleared by
        // Heal, trip-return, and never respectively, not by the day refresh.
    }

    /// <summary>Called by the Day System when this unit doesn't get fed at the start of a day.</summary>
    public void ApplyStarvationStack()
    {
        starvationStacks = Mathf.Min(starvationStacks + 1, MaxStarvationStacks);
        if (starvationStacks >= MaxStarvationStacks)
        {
            state = UnitState.Missing;
        }
    }

    /// <summary>Called by the "feed to reduce starvation" button - once per unit per day, handled by the caller.</summary>
    public void ReduceStarvationStack()
    {
        starvationStacks = Mathf.Max(starvationStacks - 1, 0);
    }
}
