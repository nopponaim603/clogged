using System.Collections.Generic;
using UnityEngine;

public enum GatherZone
{
    Near,
    Mid,
    Far
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

    /// <summary>Matches this zone against a unit's AptitudeTag lists (AptitudeTag.Near/Mid/Far mirror GatherZone).</summary>
    public AptitudeTag ZoneTag => zone switch
    {
        GatherZone.Near => AptitudeTag.Near,
        GatherZone.Mid => AptitudeTag.Mid,
        GatherZone.Far => AptitudeTag.Far,
        _ => AptitudeTag.Near
    };
}
