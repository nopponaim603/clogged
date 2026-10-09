using System.Collections.Generic;
using UnityEngine;

public enum GatherResultTier
{
    HardSuccess,
    Regular,
    Failure,
    Fumble
}

/// <summary>Report of one resolved gather roll - what tier it landed on and the raw numbers behind it. GatherDispatchController turns this into actual loot/state changes.</summary>
public class GatherOutcome
{
    public GatherResultTier tier;
    public int successValue;
    public int rollValue;
}

/// <summary>
/// Pure math for the D100 roll-under system: success value formula and
/// tier resolution. No side effects, no state changes - GatherDispatchController
/// applies whatever this produces.
/// </summary>
public static class GatherResolver
{
    /// <summary>
    /// Success value = zone base + aptitude bonus/penalty (each counted once,
    /// regardless of how many of the 1-2 sent units qualify) + duo bonus +
    /// solo-far penalty + trait bonus/penalty (each counted once).
    /// </summary>
    public static int ComputeSuccessValue(GatherZoneConfig config, List<UnitCharacter> sentUnits)
    {
        int value = config.baseSuccessValue;

        bool hasApt = false;
        bool hasNonApt = false;
        bool hasPositiveTrait = false;
        bool hasNegativeTrait = false;

        foreach (var unit in sentUnits)
        {
            if (unit.HasAptitude(config.ZoneTag)) hasApt = true;
            if (unit.HasNonAptitude(config.ZoneTag)) hasNonApt = true;

            foreach (var trait in unit.traits)
            {
                if (trait.polarity == TraitPolarity.Positive) hasPositiveTrait = true;
                if (trait.polarity == TraitPolarity.Negative) hasNegativeTrait = true;
            }
        }

        if (hasApt) value += 20;
        if (hasNonApt) value -= 15;

        if (sentUnits.Count == 2) value += 15;
        if (config.zone == GatherZone.Hard && sentUnits.Count == 1) value -= 20;

        if (hasPositiveTrait) value += 10;
        if (hasNegativeTrait) value -= 10;

        return value;
    }

    /// <summary>
    /// Rolls 1D100 and resolves it against successValue. Fumble range depends
    /// on successValue (96-100 if under 50, exactly 100 otherwise) and is
    /// checked before Hard Success/Regular/Failure.
    /// </summary>
    public static GatherOutcome Roll(int successValue)
    {
        int rollValue = Random.Range(1, 101); // 1-100 inclusive

        bool isFumble = successValue < 50
            ? rollValue >= 96
            : rollValue == 100;

        GatherResultTier tier;
        if (isFumble)
        {
            tier = GatherResultTier.Fumble;
        }
        else
        {
            int hardThreshold = successValue / 2; // floor, per the doc's example (75 -> 37)
            if (rollValue <= hardThreshold) tier = GatherResultTier.HardSuccess;
            else if (rollValue <= successValue) tier = GatherResultTier.Regular;
            else tier = GatherResultTier.Failure;
        }

        return new GatherOutcome
        {
            tier = tier,
            successValue = successValue,
            rollValue = rollValue
        };
    }
}
