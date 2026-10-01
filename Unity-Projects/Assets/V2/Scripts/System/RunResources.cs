using UnityEngine;

/// <summary>Item types tracked in the shared inventory, other than food (which has its own dedicated fields since the Day System reads it every day).</summary>
public enum InventoryItemType
{
    CookingMaterial,
    Medicine,
    Materials
}

/// <summary>
/// Holds the run-wide resources: food supply, the Cooking Material/Medicine/
/// Materials inventory, and Core Item progress. Starting values match the
/// doc's "ตารางตัวเลขตั้งต้น" table.
/// </summary>
public class RunResources : MonoBehaviour
{
    public static RunResources Instance { get; private set; }

    [Header("Food")]
    [SerializeField] private int food = 8;
    public int Food => food;

    [Header("Inventory")]
    [SerializeField] private int cookingMaterial = 2;
    [SerializeField] private int medicine = 1;
    [SerializeField] private int materials = 0;

    public int CookingMaterial => cookingMaterial;
    public int Medicine => medicine;
    public int Materials => materials;

    [Header("Core Item Progress")]
    [Tooltip("Unlocked via a Hard Success at the Mid gather zone.")]
    [SerializeField] private bool hasCoreItemUnlock = false;
    [Tooltip("Obtained via a Hard Success at the Far gather zone, only once unlocked.")]
    [SerializeField] private bool hasCoreItem = false;

    public bool HasCoreItemUnlock => hasCoreItemUnlock;
    public bool HasCoreItem => hasCoreItem;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>Consumes 1 food if available. Returns false if there was none left - caller applies starvation in that case.</summary>
    public bool TryConsumeFood()
    {
        if (food <= 0) return false;
        food--;
        return true;
    }

    public void AddFood(int amount)
    {
        food = Mathf.Max(0, food + amount);
    }

    /// <summary>Adds to the named inventory item. Explicit switch (not a dictionary lookup) so an unhandled type logs an error instead of silently doing nothing.</summary>
    public void AddItem(InventoryItemType type, int amount)
    {
        switch (type)
        {
            case InventoryItemType.CookingMaterial:
                cookingMaterial = Mathf.Max(0, cookingMaterial + amount);
                break;
            case InventoryItemType.Medicine:
                medicine = Mathf.Max(0, medicine + amount);
                break;
            case InventoryItemType.Materials:
                materials = Mathf.Max(0, materials + amount);
                break;
            default:
                Debug.LogError($"[RunResources] AddItem called with unhandled type: {type}");
                break;
        }
    }

    /// <summary>Attempts to spend the given amount of an inventory item. Returns false (and spends nothing) if there isn't enough.</summary>
    public bool TryConsumeItem(InventoryItemType type, int amount)
    {
        int current = GetItemAmount(type);
        if (current < amount) return false;
        AddItem(type, -amount);
        return true;
    }

    public int GetItemAmount(InventoryItemType type)
    {
        switch (type)
        {
            case InventoryItemType.CookingMaterial: return cookingMaterial;
            case InventoryItemType.Medicine: return medicine;
            case InventoryItemType.Materials: return materials;
            default:
                Debug.LogError($"[RunResources] GetItemAmount called with unhandled type: {type}");
                return 0;
        }
    }

    public void UnlockCoreItemMaterial()
    {
        hasCoreItemUnlock = true;
    }

    /// <summary>Only takes effect if the unlock material has already been obtained, per the doc's rule.</summary>
    public void ObtainCoreItem()
    {
        if (hasCoreItemUnlock)
        {
            hasCoreItem = true;
        }
    }
}
