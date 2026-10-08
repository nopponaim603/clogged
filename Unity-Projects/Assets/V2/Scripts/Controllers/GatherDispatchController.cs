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

    public UnitCharacter hurtUnit;
    public UnitCharacter missingUnit;

    public bool traitAwarded;
    public TraitPolarity traitPolarity;
    public string traitName;
    public UnitCharacter traitRecipient;

    public bool unlockedCoreItemMaterial;
    public bool obtainedCoreItem;
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
                reasonIfNot = $"{unit.unitName} is not available to gather.";
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
        foreach (var unit in selectedUnits)
        {
            unit.TrySpendActionPoints(config.apCost);
            unit.MarkGatheredToday();
            unit.state = UnitState.Away;
        }

        int successValue = GatherResolver.ComputeSuccessValue(config, selectedUnits);
        GatherOutcome rollOutcome = GatherResolver.Roll(successValue);

        GatherDispatchResult result = ApplyOutcome(zone, config, selectedUnits, rollOutcome);

        // Anyone still Away (not reassigned to Hurt/Missing by the outcome) returns to Idle or Resting.
        foreach (var unit in selectedUnits)
        {
            if (unit.state == UnitState.Away)
            {
                unit.state = unit.currentActionPoints > 0 ? UnitState.Idle : UnitState.Resting;
            }
        }

        OnGatherResolved?.Invoke(result);
        return result;
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
                GrantMainLoot(config.mainLoot, 1f, result);
                GrantBonusLoot(config, result);
                MaybeAwardTrait(selectedUnits, 0.5f, TraitPolarity.Positive, result);
                HandleCoreItemProgress(zone, result);
                break;

            case GatherResultTier.Regular:
                GrantMainLoot(config.mainLoot, 1f, result);
                break;

            case GatherResultTier.Failure:
                GrantMainLoot(config.mainLoot, 0.5f, result);
                InjureRandomUnit(selectedUnits, result);
                MaybeAwardTrait(selectedUnits, 0.5f, TraitPolarity.Negative, result);
                break;

            case GatherResultTier.Fumble:
                if (zone == GatherZone.Hard)
                {
                    LoseRandomUnit(selectedUnits, result);
                }
                MaybeAwardTrait(selectedUnits, 1f, TraitPolarity.Negative, result);
                break;
        }

        return result;
    }

    private void GrantMainLoot(LootAmount mainLoot, float fraction, GatherDispatchResult result)
    {
        int amount = Mathf.FloorToInt(mainLoot.amount * fraction);
        if (amount <= 0) return;

        RunResources.Instance?.AddItem(mainLoot.type, amount);
        result.mainLootGranted = new LootAmount { type = mainLoot.type, amount = amount };
    }

    private void GrantBonusLoot(GatherZoneConfig config, GatherDispatchResult result)
    {
        if (config.hardSuccessBonusPool == null || config.hardSuccessBonusPool.Count == 0) return;

        LootAmount pick = config.hardSuccessBonusPool[UnityEngine.Random.Range(0, config.hardSuccessBonusPool.Count)];
        RunResources.Instance?.AddItem(pick.type, pick.amount);
        result.bonusLootGranted = pick;
    }

    private void MaybeAwardTrait(List<UnitCharacter> selectedUnits, float chance, TraitPolarity polarity, GatherDispatchResult result)
    {
        if (UnityEngine.Random.value > chance) return;

        UnitCharacter recipient = selectedUnits[UnityEngine.Random.Range(0, selectedUnits.Count)];
        UnitTrait trait = TraitLibrary.CreateRandom(polarity, recipient);
        recipient.traits.Add(trait);

        result.traitName = trait.traitName;
        result.traitRecipient = recipient;
        result.traitAwarded = true;
        result.traitPolarity = polarity;
    }

    private void InjureRandomUnit(List<UnitCharacter> selectedUnits, GatherDispatchResult result)
    {
        UnitCharacter victim = selectedUnits[UnityEngine.Random.Range(0, selectedUnits.Count)];
        victim.state = UnitState.Hurt;
        result.hurtUnit = victim;
    }

    private void LoseRandomUnit(List<UnitCharacter> selectedUnits, GatherDispatchResult result)
    {
        UnitCharacter victim = selectedUnits[UnityEngine.Random.Range(0, selectedUnits.Count)];
        victim.state = UnitState.Missing;
        result.missingUnit = victim;
    }

    private void HandleCoreItemProgress(GatherZone zone, GatherDispatchResult result)
    {
        if (RunResources.Instance == null) return;

        if (zone == GatherZone.Normal && !RunResources.Instance.HasCoreItemUnlock)
        {
            RunResources.Instance.UnlockCoreItemMaterial();
            result.unlockedCoreItemMaterial = true;
        }
        else if (zone == GatherZone.Hard && RunResources.Instance.HasCoreItemUnlock && !RunResources.Instance.HasCoreItem)
        {
            RunResources.Instance.ObtainCoreItem();
            result.obtainedCoreItem = true;
            DayManager.Instance?.CheckImmediateWin();
        }
    }
}
