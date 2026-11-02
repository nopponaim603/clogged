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
    /// <summary>Team bonus when at least one sent unit is a natural at the zone's tier (counted once).</summary>
    public const int AptitudeBonus = 25;

    /// <summary>Bonus for sending two units instead of one.</summary>
    public const int DuoBonus = 25;

    /// <summary>Cost per tier of distance between a unit's specialty and the zone.</summary>
    public const int AptitudeMismatchPerTier = 10;

    public const int TraitBonus = 10;
    public const int BlessedBonus = 10;

    /// <summary>Negative modifier for one unit: -10 per tier away from its specialty (same tier = 0).</summary>
    public static int AptitudeMismatchPenalty(ZoneAptitude unitAptitude, ZoneAptitude zone)
    {
        return -AptitudeMismatchPerTier * Mathf.Abs((int)unitAptitude - (int)zone);
    }

    /// <summary>
    /// Success value = zone base + aptitude bonus/penalty (per unit) + duo bonus
    /// + trait bonus/penalty (each counted once) + Blessed per blessed unit
    /// + the party-wide buff-potion bonus.
    ///
    /// There is no solo-Hard penalty any more.
    /// </summary>
    public static int ComputeSuccessValue(GatherZoneConfig config, List<UnitCharacter> sentUnits)
    {
        return ComputeSuccessValue(config, sentUnits, RunResources.Instance != null ? RunResources.Instance.PartySuccessBonus : 0);
    }

    /// <summary>Overload for tests/tools that must not touch the live singletons.</summary>
    public static int ComputeSuccessValue(GatherZoneConfig config, List<UnitCharacter> sentUnits, int partySuccessBonus)
    {
        int value = config.baseSuccessValue;

        bool hasApt = false;
        bool hasPositiveTrait = false;
        bool hasNegativeTrait = false;
        int blessedUnits = 0;

        foreach (var unit in sentUnits)
        {
            if (unit.HasAptitude(config.RequiredAptitude)) hasApt = true;

            value += AptitudeMismatchPenalty(unit.primaryAptitude, config.RequiredAptitude);

            if (unit.blessed) blessedUnits++;

            foreach (var trait in unit.traits)
            {
                if (trait.polarity == TraitPolarity.Positive) hasPositiveTrait = true;
                if (trait.polarity == TraitPolarity.Negative) hasNegativeTrait = true;
            }
        }

        if (hasApt) value += AptitudeBonus;
        if (sentUnits.Count == 2) value += DuoBonus;

        if (hasPositiveTrait) value += TraitBonus;
        if (hasNegativeTrait) value -= TraitBonus;

        value += blessedUnits * BlessedBonus;
        value += partySuccessBonus;

        return value;
    }

    /// <summary>
    /// Hard Success is the top third of the success value (floor). Exposed so the
    /// HUD and the tests can quote the same number the roll uses.
    /// </summary>
    public static int HardSuccessThreshold(int successValue) => Mathf.Max(0, successValue / 3);

    /// <summary>
    /// Rolls 1D100 and resolves it against successValue. Hard Success is the top
    /// third of the success value (SV / 3). Fumble range depends on successValue
    /// (96-100 if under 50, exactly 100 otherwise) and is checked before
    /// Hard Success/Regular/Failure.
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
            int hardThreshold = HardSuccessThreshold(successValue);
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
