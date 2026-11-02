using System.Collections.Generic;
using UnityEngine;

public enum UnitState
{
    Idle,       // available, can be dispatched or given base work
    Resting,    // AP spent for the day, unusable until next day
    Hurt,       // injured, needs healing before it can act
    Away,       // out on a gathering trip
    Death       // lost for the rest of the run
}

/// <summary>
/// Which dungeon tier a unit is naturally good at. One per unit, mirrors
/// <see cref="GatherZone"/>. Sending a unit to its own tier gives the team
/// bonus; every tier of distance costs success value instead.
/// </summary>
public enum ZoneAptitude
{
    Easy,
    Normal,
    Hard
}

/// <summary>
/// Secondary specialty, one per unit. Talents change what a unit produces
/// (Cooking/Crafting), how far it can push a trip (Exploring) or who it can
/// patched up (Heal).
/// </summary>
public enum UnitTalent
{
    None,
    /// <summary>Patients this unit heals gain Blessed (+10 success value on their next trip).</summary>
    Heal,
    /// <summary>Medicine crafted by this unit is doubled.</summary>
    Crafting,
    /// <summary>Food cooked by this unit is doubled.</summary>
    Cooking,
    /// <summary>Gather loot is doubled (Core Items excluded) and the Core Item chance is +25%.</summary>
    Exploring
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

    [Header("Swords (dispatch readiness — refreshes each day like AP)")]
    [Tooltip("One sword per day. Only a unit holding a sword may be sent into a cave.")]
    public int maxSwords = 1;
    public int currentSwords = 1;

    [Header("Aptitude (Easy/Normal/Hard — own tier gives the team bonus)")]
    public ZoneAptitude primaryAptitude = ZoneAptitude.Easy;

    [Header("Talent (secondary specialty)")]
    public UnitTalent talent = UnitTalent.None;

    [Header("Traits")]
    public List<UnitTrait> traits = new List<UnitTrait>();

    [Header("Buffs")]
    [Tooltip("Freshened up by a Heal-talent medic. +10 success value on the next trip, then cleared.")]
    public bool blessed;

    [Header("Starvation")]
    [Range(0, MaxStarvationStacks)]
    public int starvationStacks = 0;

    /// <summary>
    /// A unit can only be sent gathering once per day: the trip spends its
    /// sword. Kept as a property so the card and the tests keep reading the
    /// readiness flag rather than the raw counter.
    /// </summary>
    public bool HasGatheredToday => currentSwords <= 0;

    /// <summary>True while the unit still holds the sword a trip costs.</summary>
    public bool HasSword => currentSwords > 0;

    /// <summary>The manual "feed to reduce starvation" button can only be used once per day per unit - separate from the automatic start-of-day feeding.</summary>
    public bool HasBeenFedManuallyToday { get; private set; }

    public bool IsAlive => state != UnitState.Death;

    /// <summary>
    /// Can this unit take a base-work action right now? AP is part of the check,
    /// not just the state: an Idle unit with an empty AP bar has nothing left to
    /// spend, so its card must not look pickable.
    /// </summary>
    public bool CanBeDispatched => state == UnitState.Idle && currentActionPoints > 0;

    public bool CanBeSentGathering => state == UnitState.Idle && HasSword;

    /// <summary>
    /// Puts a unit back into play after something cleared its condition, without
    /// handing it a free turn: a unit with no AP left becomes Resting, not Idle.
    ///
    /// This is the one place that rule lives. Heal used to set Idle outright,
    /// which made a 0 AP patient - Dan after a 3 AP Hard trip - come back as a
    /// clickable card even though he could not afford anything.
    /// </summary>
    public void ReturnToPlay()
    {
        state = currentActionPoints > 0 ? UnitState.Idle : UnitState.Resting;
    }

    /// <summary>Spends the sword a trip costs. Returns false when there is none left.</summary>
    public bool TrySpendSword()
    {
        if (currentSwords <= 0) return false;

        currentSwords--;
        return true;
    }

    public void MarkGatheredToday() => TrySpendSword();
    public void MarkFedManuallyToday() => HasBeenFedManuallyToday = true;

    public bool HasAptitude(ZoneAptitude zone) => primaryAptitude == zone;
    public bool HasTalent(UnitTalent other) => talent == other;

    /// <summary>How far this unit's specialty is from the zone it is being sent to (0-2 tiers).</summary>
    public int AptitudeDistance(ZoneAptitude zone) => Mathf.Abs((int)primaryAptitude - (int)zone);

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
        currentSwords = maxSwords;
        HasBeenFedManuallyToday = false;
        if (state == UnitState.Resting)
        {
            state = UnitState.Idle;
        }
        // Hurt / Away / Death are left as-is here - they're cleared by
        // Heal, trip-return, and never respectively, not by the day refresh.
    }

    /// <summary>Called by the Day System when this unit doesn't get fed at the start of a day.</summary>
    public void ApplyStarvationStack()
    {
        starvationStacks = Mathf.Min(starvationStacks + 1, MaxStarvationStacks);
        if (starvationStacks >= MaxStarvationStacks)
        {
            state = UnitState.Death;
        }
    }

    /// <summary>Called by the "feed to reduce starvation" button - once per unit per day, handled by the caller.</summary>
    public void ReduceStarvationStack()
    {
        starvationStacks = Mathf.Max(starvationStacks - 1, 0);
    }
}
