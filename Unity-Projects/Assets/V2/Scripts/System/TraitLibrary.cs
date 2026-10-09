using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pool of named traits. Per the doc, every good trait is +10 and every bad
/// trait is -10 to success value (counted once per trip, see GatherResolver),
/// so the names are flavor only for now. Edit these two lists freely - nothing
/// else depends on the specific names.
/// </summary>
public static class TraitLibrary
{
    private static readonly string[] GoodNames =
    {
        "Sharp-eyed",
        "Steady Hands",
        "Quick Feet",
        "Thick-skinned",
        "Lucky Streak"
    };

    private static readonly string[] BadNames =
    {
        "Clumsy",
        "Jumpy",
        "Shaky Nerves",
        "Sluggish",
        "Unlucky"
    };

    /// <summary>
    /// Picks a random trait of the given polarity, preferring one the owner
    /// doesn't have yet. If the owner already has every name of that polarity,
    /// repeats are allowed so the doc's "guaranteed bad trait on Fumble" still holds.
    /// </summary>
    public static UnitTrait CreateRandom(TraitPolarity polarity, UnitCharacter owner)
    {
        string[] pool = polarity == TraitPolarity.Positive ? GoodNames : BadNames;

        var unowned = new List<string>();
        foreach (string name in pool)
        {
            bool owned = false;
            foreach (var trait in owner.traits)
            {
                if (trait.traitName == name)
                {
                    owned = true;
                    break;
                }
            }
            if (!owned) unowned.Add(name);
        }

        string chosen = unowned.Count > 0
            ? unowned[Random.Range(0, unowned.Count)]
            : pool[Random.Range(0, pool.Length)];

        return new UnitTrait { traitName = chosen, polarity = polarity };
    }
}
