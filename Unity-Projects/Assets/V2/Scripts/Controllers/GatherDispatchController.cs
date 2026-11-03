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

    /// <summary>Risk spent by a successful trip (0 for Failure/Fumble).</summary>
    public int riskReduced;

    /// <summary>Whether the roll landed on the Jackpot (a natural 1 with SV 80 or more).</summary>
    public bool jackpot;
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

    /// <summary>A roll of exactly 1 with at least this success value also grabs the other Core Item piece.</summary>
    public const int JackpotSuccessValue = 80;

    /// <summary>Natural roll that triggers the Jackpot.</summary>
    public const int JackpotRoll = 1;

    /// <summary>
    /// Which piece a cleared dungeon is guaranteed to hand over: the Key from
    /// Normal, the Box from Hard. Easy has no Core Item.
    /// </summary>
    public static CoreItemKind? CoreItemForZone(GatherZone zone)
    {
        switch (zone)
        {
            case GatherZone.Normal: return CoreItemKind.B; // the key
            case GatherZone.Hard: return CoreItemKind.A;   // the box
            default: return null;
        }
    }

    /// <summary>The run's Risk, which every trip subtracts from its success value.</summary>
    private static int CurrentRisk => RunResources.Instance != null ? RunResources.Instance.Risk : 0;

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
                GrantCoreItems(zone, rollOutcome, result);
                ReduceRisk(zone, result);
                break;

            case GatherResultTier.Regular:
                GrantMainLoot(config.mainLoot, 1f, selectedUnits, result);
                ReduceRisk(zone, result);
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
    /// Spends Risk down after a cleared dungeon: Easy 5, Normal 10, Hard 15,
    /// floored at 0. Only a Regular or Hard Success counts as "cleared", so a
    /// Failure leaves the fog exactly where it was.
    /// </summary>
    private void ReduceRisk(GatherZone zone, GatherDispatchResult result)
    {
        if (RunResources.Instance == null) return;

        int before = RunResources.Instance.Risk;
        if (before <= 0) return;

        RunResources.Instance.AddRisk(-GatherResolver.RiskReduction(zone));
        result.riskReduced = before - RunResources.Instance.Risk;
    }

    /// <summary>
    /// Core Items on a Hard Success, by cave rather than by dice:
    ///   Normal -> the Key, Hard -> the Box, Easy -> nothing.
    /// The drop is guaranteed, not rolled - no pity counter, no drop chance.
    ///
    /// Jackpot: a natural 1 with a success value of <see cref="JackpotSuccessValue"/>
    /// or more completes the set from whatever is still missing. So a Normal
    /// trip holding only the Key also takes the Box, a Hard trip holding only
    /// the Box also takes the Key, and a trip that has neither gets both.
    /// </summary>
    private void GrantCoreItems(GatherZone zone, GatherOutcome rollOutcome, GatherDispatchResult result)
    {
        if (RunResources.Instance == null) return;

        CoreItemKind? own = CoreItemForZone(zone);
        if (own.HasValue) Give(result, own.Value);

        bool jackpot = rollOutcome.successValue >= JackpotSuccessValue && rollOutcome.rollValue == JackpotRoll;
        if (!jackpot) 
        {
            if (result.coreItemsObtained.Count > 0) DayManager.Instance?.CheckImmediateWin();
            return;
        }

        result.jackpot = true;
        foreach (CoreItemKind kind in new[] { CoreItemKind.A, CoreItemKind.B }) Give(result, kind);

        // One win check per trip: a Jackpot already completes the set.
        if (result.coreItemsObtained.Count > 0) DayManager.Instance?.CheckImmediateWin();
    }

    /// <summary>Grants a piece once and records it; a piece already in the bag is a no-op.</summary>
    private static void Give(GatherDispatchResult result, CoreItemKind kind)
    {
        if (RunResources.Instance.HasCoreItem(kind)) return;

        RunResources.Instance.ObtainCoreItem(kind);
        result.coreItemsObtained.Add(kind);
    }
}
