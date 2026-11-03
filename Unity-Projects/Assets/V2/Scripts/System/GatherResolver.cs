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

    /// <summary>
    /// Success value a cleared dungeon takes off the run's Risk. Only back at
    /// camp - a Failure or a Fumble leaves the fog exactly where it was.
    /// </summary>
    public static int RiskReduction(GatherZone zone)
    {
        switch (zone)
        {
            case GatherZone.Easy: return 5;
            case GatherZone.Normal: return 10;
            case GatherZone.Hard: return 15;
            default: return 0;
        }
    }

    /// <summary>
    /// How much of the fog a cave shrugs off. Easy 30, Normal 20, Hard 0 - the
    /// shallower the cave, the more Risk it tolerates for free.
    /// </summary>
    public static int ZoneHandicap(GatherZone zone)
    {
        switch (zone)
        {
            case GatherZone.Easy: return 30;
            case GatherZone.Normal: return 20;
            case GatherZone.Hard: return 0;
            default: return 0;
        }
    }

    /// <summary>
    /// What a trip at this cave actually pays: Risk − Handicap, floored at 0.
    ///
    /// The handicap is a flat allowance, so once Risk has burned down to it the
    /// cave stops being penalised at all - Easy frees up at Risk 30, Normal at 20,
    /// and only Hard still cares all the way down to 0. That is what lets the
    /// shallow caves come back before the deep ones do, without ever letting a
    /// bonus push a success value up.
    /// </summary>
    public static int RiskPenalty(GatherZone zone, int risk)
    {
        return Mathf.Max(0, risk - ZoneHandicap(zone));
    }

    /// <summary>Negative modifier for one unit: -10 per tier away from its specialty (same tier = 0).</summary>
    public static int AptitudeMismatchPenalty(ZoneAptitude unitAptitude, ZoneAptitude zone)
    {
        return -AptitudeMismatchPerTier * Mathf.Abs((int)unitAptitude - (int)zone);
    }

    /// <summary>
    /// Success value = zone base + aptitude bonus/penalty (per unit) + duo bonus
    /// + trait bonus/penalty (each counted once) + Blessed per blessed unit
    /// + the party-wide buff-potion bonus - Risk + this cave's Handicap
    /// (<see cref="RiskPenalty"/>, floored at 0).
    ///
    /// There is no solo-Hard penalty any more.
    /// </summary>
    public static int ComputeSuccessValue(GatherZoneConfig config, List<UnitCharacter> sentUnits)
    {
        return ComputeSuccessValue(config, sentUnits,
            RunResources.Instance != null ? RunResources.Instance.PartySuccessBonus : 0,
            RunResources.Instance != null
                ? RiskPenalty(config.zone, RunResources.Instance.Risk)
                : 0);
    }

    /// <summary>Overload for tests/tools that must not touch the live singletons.</summary>
    public static int ComputeSuccessValue(GatherZoneConfig config, List<UnitCharacter> sentUnits, int partySuccessBonus,
        int riskPenalty = 0)
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

        // The red fog: −Risk + the cave's Handicap, floored at 0 by the caller.
        value -= Mathf.Max(0, riskPenalty);

        return value;
    }

    /// <summary>
    /// Hard Success = the top third of the success value (floor), maximum 100.
    /// Exposed so the HUD and the tests can quote the same number the roll uses.
    /// </summary>
    public static int HardSuccessThreshold(int successValue) => Mathf.Clamp(successValue / 3, 0, 100);

    /// <summary>
    /// When set (1-100) the next <see cref="Roll"/> uses this number instead of
    /// rolling, then clears itself. The tutorial uses it to land a scripted Hard
    /// Success; nothing else in the game touches it, and one use is one roll, so a
    /// forced value can never leak into the next trip.
    /// </summary>
    public static int ForcedRoll { get; set; } = -1;

    /// <summary>
    /// Rolls 1D100 and resolves it against successValue. Hard Success is the top
    /// third of the success value (SV / 3). Fumble range depends on successValue
    /// (96-100 if under 50, exactly 100 otherwise) and is checked before
    /// Hard Success/Regular/Failure.
    /// </summary>
    public static GatherOutcome Roll(int successValue)
    {
        int rollValue = ForcedRoll >= 1 && ForcedRoll <= 100
            ? ForcedRoll
            : Random.Range(1, 101); // 1-100 inclusive

        ForcedRoll = -1;

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
