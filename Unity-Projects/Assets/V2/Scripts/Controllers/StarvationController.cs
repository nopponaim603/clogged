using UnityEngine;

/// <summary>Report of one feed-button use, for the UI to display.</summary>
public class FeedResult
{
    public UnitCharacter unit;
    public bool blocked;
    public int stacksBefore;
    public int stacksAfter;
}

/// <summary>
/// The manual "feed to reduce starvation" button (doc item 30). Separate
/// from the automatic food payment in DayManager's End Day flow - this is a
/// player action available any time during the day, costs 1 food, reduces
/// 1 stack, and can only be used once per unit per day.
/// </summary>
public class StarvationController : MonoBehaviour
{
    public static StarvationController Instance { get; private set; }

    [SerializeField] private int foodCostPerFeed = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public int FoodCostPerFeed => foodCostPerFeed;

    /// <summary>Why feeding this unit isn't possible right now, or null if it is. The UI shows this text next to the disabled button.</summary>
    public string GetBlockReason(UnitCharacter unit)
    {
        if (unit == null) return "No unit selected";
        if (unit.starvationStacks <= 0) return $"{unit.unitName} isn't hungry";
        if (unit.HasBeenFedManuallyToday) return $"{unit.unitName} was already fed today";
        if (RunResources.Instance == null || RunResources.Instance.Food < foodCostPerFeed) return "Not enough Food";
        return null;
    }

    public bool CanFeedUnit(UnitCharacter unit)
    {
        return GetBlockReason(unit) == null;
    }

    public FeedResult FeedUnit(UnitCharacter unit)
    {
        var result = new FeedResult { unit = unit };

        if (!CanFeedUnit(unit))
        {
            result.blocked = true;
            return result;
        }

        result.stacksBefore = unit.starvationStacks;

        RunResources.Instance.AddFood(-foodCostPerFeed);
        unit.ReduceStarvationStack();
        unit.MarkFedManuallyToday();

        result.stacksAfter = unit.starvationStacks;
        return result;
    }
}
