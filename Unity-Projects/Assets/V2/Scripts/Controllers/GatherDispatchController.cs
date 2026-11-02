using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Full report of one resolved gather trip, for the results-window UI to display.</summary>
public class GatherDispatchResult
{
    public GatherZone zone;
    public List<UnitCharacter> sentUnits;
    public GatherResultTier tier;
    public int rollValue;
    public int successValue;

    public LootAmount? mainLootGranted;
    public LootAmount? bonusLootGranted;

    /// <summary>Everyone hurt by this trip. A Failure injures one unit, a Fumble injures the whole party.</summary>
    public readonly List<UnitCharacter> hurtUnits = new List<UnitCharacter>();

    /// <summary>Units that picked up a trait here. A Fumble hands one to every unit that went.</summary>
    public readonly List<UnitCharacter> traitRecipients = new List<UnitCharacter>();

    /// <summary>First injured unit - the single victim a Failure produces.</summary>
    public UnitCharacter hurtUnit => hurtUnits.Count > 0 ? hurtUnits[0] : null;

    public bool traitAwarded;
    public TraitPolarity traitPolarity;
    public string traitName;
    public UnitCharacter traitRecipient;

    /// <summary>Core Item pieces found on this trip (0-2).</summary>
    public List<CoreItemKind> coreItemsObtained = new List<CoreItemKind>();
}

/// <summary>
/// Entry point for the send-to-gather flow (doc sections 16-25). Call
/// CanDispatch() to validate a selection before showing the confirm window,
/// then Dispatch() to actually spend AP, roll, and apply the outcome.
/// </summary>
public class GatherDispatchController : MonoBehaviour
{
    public static GatherDispatchController Instance { get; private set; }

    public event Action<GatherDispatchResult> OnGatherResolved;

    /// <summary>Success value added to a zone's Core Item chance when an Exploring talent is on the trip.</summary>
    public const int ExploringCoreItemChanceBonus = 25;

    // Consecutive Hard Success trips that dropped no Core Item, per pity zone.
    private int pityNormal;
    private int pityHard;

    public int PityNormal => pityNormal;
    public int PityHard => pityHard;

    /// <summary>How many Hard Success trips in a row before the next one is guaranteed (0 = zone has no pity).</summary>
    public int PityThreshold(GatherZone zone)
    {
        var config = GatherZoneDatabase.Instance != null ? GatherZoneDatabase.Instance.GetConfig(zone) : null;
        return config != null ? config.coreItemPityTrips : 0;
    }

    /// <summary>Current pity progress for the zone, for the map card to display.</summary>
    public int PityCounter(GatherZone zone)
    {
        switch (zone)
        {
            case GatherZone.Normal: return pityNormal;
            case GatherZone.Hard: return pityHard;
            default: return 0;
        }
    }

    public void RestorePity(int normal, int hard)
    {
        pityNormal = Mathf.Max(0, normal);
        pityHard = Mathf.Max(0, hard);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool CanDispatch(GatherZone zone, List<UnitCharacter> selectedUnits, out string reasonIfNot)
    {
        reasonIfNot = null;

        if (selectedUnits == null || selectedUnits.Count == 0 || selectedUnits.Count > 2)
        {
            reasonIfNot = "Select 1 or 2 units.";
            return false;
        }

        var config = GatherZoneDatabase.Instance != null ? GatherZoneDatabase.Instance.GetConfig(zone) : null;
        if (config == null)
        {
            reasonIfNot = "Zone config missing.";
            return false;
        }

        foreach (var unit in selectedUnits)
        {
            if (!unit.CanBeSentGathering)
            {
                reasonIfNot = unit.state == UnitState.Idle
                    ? $"{unit.unitName} has no sword left today."
                    : $"{unit.unitName} is not available to gather.";
                return false;
            }

            int actualCost = unit.GetActualApCost(config.apCost);
            if (unit.currentActionPoints < actualCost)
            {
                reasonIfNot = $"{unit.unitName} doesn't have enough AP.";
                return false;
            }
        }

        return true;
    }

    /// <summary>Call after the player confirms the send. Returns null (and logs a warning) if the selection isn't actually valid - callers should already have checked with CanDispatch.</summary>
    public GatherDispatchResult Dispatch(GatherZone zone, List<UnitCharacter> selectedUnits)
    {
        if (!CanDispatch(zone, selectedUnits, out string reason))
        {
            Debug.LogWarning($"[GatherDispatchController] Dispatch blocked: {reason}");
            return null;
        }

        var config = GatherZoneDatabase.Instance.GetConfig(zone);

        // Each unit pays the full zone AP cost - duo doesn't split it, per the doc.
        // The sword is the dispatch ticket: one per unit, refreshed with AP.
        foreach (var unit in selectedUnits)
        {
            unit.TrySpendActionPoints(config.apCost);
            unit.TrySpendSword();
            unit.state = UnitState.Away;
        }

        int successValue = GatherResolver.ComputeSuccessValue(config, selectedUnits);
        GatherOutcome rollOutcome = GatherResolver.Roll(successValue);

        GatherDispatchResult result = ApplyOutcome(zone, config, selectedUnits, rollOutcome);

        // Blessed only covers the trip right after the heal, so it burns off here.
        foreach (var unit in selectedUnits) unit.blessed = false;

        // Anyone still Away (not reassigned to Hurt by the outcome) returns to Idle or Resting.
        foreach (var unit in selectedUnits)
        {
            if (unit.state == UnitState.Away) unit.ReturnToPlay();
        }

        OnGatherResolved?.Invoke(result);
        RunStats.CountGather(result);

        // Checked last, after everyone who was still Away is back in a real
        // state. A Fumble that wounds an already broken crew leaves nobody to
        // heal anyone, and that is a loss rather than a game that limps on.
        DayManager.Instance?.CheckImmediateLoss();

        return result;
    }

    /// <summary>
    /// Applies a known outcome without rolling. <see cref="Dispatch"/> is the
    /// normal entry point; this exists so the verification pass can force a
    /// tier (a Fumble cannot be waited for) and is safe to call for one trip.
    /// </summary>
    public GatherDispatchResult ApplyKnownOutcome(GatherZone zone, List<UnitCharacter> selectedUnits,
        GatherResultTier tier, int rollValue = 1, int successValue = 50)
    {
        var config = GatherZoneDatabase.Instance != null ? GatherZoneDatabase.Instance.GetConfig(zone) : null;
        if (config == null || selectedUnits == null || selectedUnits.Count == 0) return null;

        return ApplyOutcome(zone, config, selectedUnits,
            new GatherOutcome { tier = tier, rollValue = rollValue, successValue = successValue });
    }

    private GatherDispatchResult ApplyOutcome(GatherZone zone, GatherZoneConfig config, List<UnitCharacter> selectedUnits, GatherOutcome rollOutcome)
    {
        var result = new GatherDispatchResult
        {
            zone = zone,
            sentUnits = selectedUnits,
            tier = rollOutcome.tier,
            rollValue = rollOutcome.rollValue,
            successValue = rollOutcome.successValue
        };

        switch (rollOutcome.tier)
        {
            case GatherResultTier.HardSuccess:
                GrantMainLoot(config.mainLoot, 1f, selectedUnits, result);
                GrantBonusLoot(config, selectedUnits, result);
                MaybeAwardTrait(selectedUnits, 0.5f, TraitPolarity.Positive, result);
                TryRollCoreItems(config, selectedUnits, result);
                break;

            case GatherResultTier.Regular:
                GrantMainLoot(config.mainLoot, 1f, selectedUnits, result);
                break;

            case GatherResultTier.Failure:
                GrantMainLoot(config.mainLoot, 0.5f, selectedUnits, result);
                InjureRandomUnit(selectedUnits, result);
                MaybeAwardTrait(selectedUnits, 0.5f, TraitPolarity.Negative, result);
                break;

            case GatherResultTier.Fumble:
                // Spec (2026-11-02): a fumble costs the whole party - no loot,
                // every unit that went is Hurt, and every one of them picks up a
                // bad trait. Nobody dies from a roll; Death only comes from
                // starvation reaching its third stack.
                foreach (var unit in selectedUnits)
                {
                    HurtUnit(unit, result);
                    AwardTrait(unit, TraitPolarity.Negative, result);
                }
                break;
        }

        return result;
    }

    /// <summary>An Exploring talent on the trip doubles the loot taken out of the dungeon.</summary>
    private static int LootMultiplier(List<UnitCharacter> selectedUnits)
    {
        foreach (var unit in selectedUnits)
        {
            if (unit.HasTalent(UnitTalent.Exploring)) return 2;
        }
        return 1;
    }

    private static bool HasExploringTalent(List<UnitCharacter> selectedUnits)
    {
        return LootMultiplier(selectedUnits) > 1;
    }

    private void GrantMainLoot(LootAmount mainLoot, float fraction, List<UnitCharacter> selectedUnits, GatherDispatchResult result)
    {
        int amount = Mathf.FloorToInt(mainLoot.amount * fraction) * LootMultiplier(selectedUnits);
        if (amount <= 0) return;

        RunResources.Instance?.AddItem(mainLoot.type, amount);
        result.mainLootGranted = new LootAmount { type = mainLoot.type, amount = amount };
    }

    private void GrantBonusLoot(GatherZoneConfig config, List<UnitCharacter> selectedUnits, GatherDispatchResult result)
    {
        if (config.hardSuccessBonusPool == null || config.hardSuccessBonusPool.Count == 0) return;

        LootAmount pick = config.hardSuccessBonusPool[UnityEngine.Random.Range(0, config.hardSuccessBonusPool.Count)];
        pick.amount *= LootMultiplier(selectedUnits);

        RunResources.Instance?.AddItem(pick.type, pick.amount);
        result.bonusLootGranted = pick;
    }

    private void MaybeAwardTrait(List<UnitCharacter> selectedUnits, float chance, TraitPolarity polarity, GatherDispatchResult result)
    {
        if (UnityEngine.Random.value > chance) return;

        UnitCharacter recipient = selectedUnits[UnityEngine.Random.Range(0, selectedUnits.Count)];
        AwardTrait(recipient, polarity, result);
    }

    /// <summary>Gives one trait to one unit and records it for the popup/log.</summary>
    private static void AwardTrait(UnitCharacter recipient, TraitPolarity polarity, GatherDispatchResult result)
    {
        UnitTrait trait = TraitLibrary.CreateRandom(polarity, recipient);
        recipient.traits.Add(trait);

        result.traitAwarded = true;
        result.traitPolarity = polarity;
        result.traitRecipients.Add(recipient);

        // The single-value fields describe the first award - that is what the
        // dice popup prints for a one-victim outcome.
        if (string.IsNullOrEmpty(result.traitName))
        {
            result.traitName = trait.traitName;
            result.traitRecipient = recipient;
        }
    }

    private static void HurtUnit(UnitCharacter unit, GatherDispatchResult result)
    {
        unit.state = UnitState.Hurt;
        if (!result.hurtUnits.Contains(unit)) result.hurtUnits.Add(unit);
    }

    private void InjureRandomUnit(List<UnitCharacter> selectedUnits, GatherDispatchResult result)
    {
        HurtUnit(selectedUnits[UnityEngine.Random.Range(0, selectedUnits.Count)], result);
    }

    /// <summary>
    /// Rolls each Core Item piece the run is still missing against the zone's
    /// drop chance. Pieces are rolled independently, so one trip can yield both.
    /// Only reachable from Hard Success - Regular/Failure/Fumble never drop one.
    ///
    /// Pity: <see cref="GatherZoneConfig.coreItemPityTrips"/> Hard Success trips
    /// in a row without a piece guarantee one on the following trip. A missing
    /// piece is picked at random; obtaining anything resets that zone's counter.
    /// An Exploring talent adds <see cref="ExploringCoreItemChanceBonus"/> to the chance.
    /// </summary>
    private void TryRollCoreItems(GatherZoneConfig config, List<UnitCharacter> selectedUnits, GatherDispatchResult result)
    {
        if (RunResources.Instance == null) return;

        int threshold = config.coreItemPityTrips;
        int counter = PityCounter(config.zone);
        bool pityReady = threshold > 0 && counter + 1 >= threshold;

        int chance = Mathf.Clamp(config.coreItemChancePercent + (HasExploringTalent(selectedUnits) ? ExploringCoreItemChanceBonus : 0), 0, 100);

        var missing = new List<CoreItemKind>();
        foreach (CoreItemKind kind in new[] { CoreItemKind.A, CoreItemKind.B })
        {
            if (!RunResources.Instance.HasCoreItem(kind)) missing.Add(kind);
        }

        if (missing.Count > 0)
        {
            if (pityReady)
            {
                // Guarantee exactly one piece so one trip cannot also win off pity.
                CoreItemKind pick = missing[UnityEngine.Random.Range(0, missing.Count)];
                RunResources.Instance.ObtainCoreItem(pick);
                result.coreItemsObtained.Add(pick);
            }
            else
            {
                foreach (CoreItemKind kind in missing)
                {
                    if (UnityEngine.Random.Range(0, 100) >= chance) continue;

                    RunResources.Instance.ObtainCoreItem(kind);
                    result.coreItemsObtained.Add(kind);
                }
            }
        }

        // Drop success clears the counter; a dry Hard Success nudges it up.
        bool gotSomething = result.coreItemsObtained.Count > 0;
        AddPity(config.zone, gotSomething ? -counter : 1);

        // Win check once per trip, not once per piece - a trip can complete the set.
        if (gotSomething)
        {
            DayManager.Instance?.CheckImmediateWin();
        }
    }

    private void AddPity(GatherZone zone, int delta)
    {
        switch (zone)
        {
            case GatherZone.Normal:
                pityNormal = Mathf.Max(0, pityNormal + delta);
                break;
            case GatherZone.Hard:
                pityHard = Mathf.Max(0, pityHard + delta);
                break;
        }
    }
}
