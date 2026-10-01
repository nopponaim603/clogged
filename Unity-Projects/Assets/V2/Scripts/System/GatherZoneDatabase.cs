using System.Collections.Generic;
using UnityEngine;

public class GatherZoneDatabase : MonoBehaviour
{
    public static GatherZoneDatabase Instance { get; private set; }

    [SerializeField] private List<GatherZoneConfig> zones = new List<GatherZoneConfig>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (zones.Count == 0)
        {
            SetupDefaults();
        }
    }

    private void SetupDefaults()
    {
        zones.Add(new GatherZoneConfig
        {
            zone = GatherZone.Near,
            apCost = 1,
            baseSuccessValue = 80,
            mainLoot = new LootAmount { type = InventoryItemType.CookingMaterial, amount = 2 },
            hardSuccessBonusPool = new List<LootAmount>
            {
                new LootAmount { type = InventoryItemType.CookingMaterial, amount = 2 },
                new LootAmount { type = InventoryItemType.Medicine, amount = 1 }
            }
        });
        zones.Add(new GatherZoneConfig
        {
            zone = GatherZone.Mid,
            apCost = 2,
            baseSuccessValue = 55,
            mainLoot = new LootAmount { type = InventoryItemType.Medicine, amount = 1 },
            hardSuccessBonusPool = new List<LootAmount>
            {
                new LootAmount { type = InventoryItemType.Medicine, amount = 1 },
                new LootAmount { type = InventoryItemType.CookingMaterial, amount = 1 }
            }
        });
        zones.Add(new GatherZoneConfig
        {
            zone = GatherZone.Far,
            apCost = 3,
            baseSuccessValue = 30,
            mainLoot = new LootAmount { type = InventoryItemType.Materials, amount = 2 },
            hardSuccessBonusPool = new List<LootAmount>
            {
                new LootAmount { type = InventoryItemType.Materials, amount = 2 },
                new LootAmount { type = InventoryItemType.Medicine, amount = 1 }
            }
        });
    }

    public GatherZoneConfig GetConfig(GatherZone zone)
    {
        foreach (var config in zones)
        {
            if (config.zone == zone) return config;
        }
        Debug.LogError($"[GatherZoneDatabase] No config found for zone: {zone}");
        return null;
    }
}
