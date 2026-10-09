using System.Collections.Generic;
using UnityEngine;

public enum GatherZone
{
    Easy,
    Normal,
    Hard
}

[System.Serializable]
public struct LootAmount
{
    public InventoryItemType type;
    public int amount;
}

[System.Serializable]
public class GatherZoneConfig
{
    public GatherZone zone;
    public int apCost;
    public int baseSuccessValue;
    public LootAmount mainLoot;

    [Tooltip("On Hard Success, one of these is picked at random and added on top of the main loot.")]
    public List<LootAmount> hardSuccessBonusPool = new List<LootAmount>();

    /// <summary>Matches this zone against a unit's AptitudeTag lists (AptitudeTag.Easy/Normal/Hard mirror GatherZone).</summary>
    public AptitudeTag ZoneTag => zone switch
    {
        GatherZone.Easy => AptitudeTag.Easy,
        GatherZone.Normal => AptitudeTag.Normal,
        GatherZone.Hard => AptitudeTag.Hard,
        _ => AptitudeTag.Easy
    };
}
