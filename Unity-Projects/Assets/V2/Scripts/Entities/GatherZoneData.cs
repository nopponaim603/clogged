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
    /// <summary>
    /// Base success value before any team modifier. Keep Easy ≥ 65, Normal ≥ 45 and Hard ≥ 15:
    /// below 15 a solo specialist can no longer clear the party's risk floor, so Hard stops
    /// being chosen at all (simulated: Hard trips fell 55% → 1%). Hard below 25 also drops the
    /// solo specialist under SV 50, which doubles-and-a-half the fumble band (1% → 5%).
    /// See Assets/V2/test/gather-balance-basedsv-matrix.md.
    /// </summary>
    public int baseSuccessValue;
    public LootAmount mainLoot;

    [Tooltip("On Hard Success, one of these is picked at random and added on top of the main loot.")]
    public List<LootAmount> hardSuccessBonusPool = new List<LootAmount>();

    /// <summary>Which unit specialty matches this zone (ZoneAptitude mirrors GatherZone 1:1).</summary>
    public ZoneAptitude RequiredAptitude => zone switch
    {
        GatherZone.Easy => ZoneAptitude.Easy,
        GatherZone.Normal => ZoneAptitude.Normal,
        GatherZone.Hard => ZoneAptitude.Hard,
        _ => ZoneAptitude.Easy
    };
}
