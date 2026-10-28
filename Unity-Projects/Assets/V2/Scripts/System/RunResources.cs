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

    /// <summary>Fired whenever any tracked value changes - use it instead of polling.</summary>
    public event System.Action OnChanged;

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

    [Header("Core Items")]
    [Tooltip("The sealed box. Drops independently of the key.")]
    [SerializeField] private bool coreItemA = false;
    [Tooltip("The key. Drops independently of the box; its colour always fits the box.")]
    [SerializeField] private bool coreItemB = false;

    [Tooltip("Which box/key pair this run uses. Randomised on start when the toggle below is on.")]
    [SerializeField] private CoreItemVariant variant = CoreItemVariant.Orange;
    [Tooltip("Roll a fresh box colour each run so the pair is not always orange.")]
    [SerializeField] private bool randomiseVariant = true;

    public bool HasCoreItemA => coreItemA;
    public bool HasCoreItemB => coreItemB;

    /// <summary>The box/key pair this run uses - drives which art the HUD shows.</summary>
    public CoreItemVariant Variant => variant;

    /// <summary>Forces a pair, e.g. from a test or the Inspector. Writes through even when randomising.</summary>
    public void SetVariant(CoreItemVariant newVariant)
    {
        variant = newVariant;
        randomiseVariant = false;
        OnChanged?.Invoke();
    }

    /// <summary>Both pieces are needed to escape - this is the win condition.</summary>
    public bool HasBothCoreItems => coreItemA && coreItemB;

    public bool HasCoreItem(CoreItemKind kind) =>
        kind == CoreItemKind.A ? coreItemA : coreItemB;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (randomiseVariant)
        {
            variant = Random.value < 0.5f ? CoreItemVariant.Orange : CoreItemVariant.Yellow;
        }
    }

    /// <summary>Consumes 1 food if available. Returns false if there was none left - caller applies starvation in that case.</summary>
    public bool TryConsumeFood()
    {
        if (food <= 0) return false;
        food--;
        OnChanged?.Invoke();
        return true;
    }

    public void AddFood(int amount)
    {
        food = Mathf.Max(0, food + amount);
        OnChanged?.Invoke();
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
                return;
        }

        OnChanged?.Invoke();
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

    /// <summary>Grants a Core Item piece. Idempotent - obtaining one that's already held does nothing.</summary>
    public void ObtainCoreItem(CoreItemKind kind)
    {
        if (HasCoreItem(kind)) return;

        if (kind == CoreItemKind.A) coreItemA = true;
        else coreItemB = true;

        OnChanged?.Invoke();
    }

    /// <summary>
    /// Restores every tracked value from a save in one shot. Silent - no
    /// <see cref="OnChanged"/> - because the scene is still waking up and the HUD
    /// polls these values on its first frame anyway.
    /// </summary>
    public void RestoreState(SaveData data)
    {
        if (data == null) return;

        food = Mathf.Max(0, data.food);
        cookingMaterial = Mathf.Max(0, data.cookingMaterial);
        medicine = Mathf.Max(0, data.medicine);
        materials = Mathf.Max(0, data.materials);
        coreItemA = data.coreItemA;
        coreItemB = data.coreItemB;

        // A restored run keeps the pair it was saved with, even if this
        // particular start rolled the other one.
        variant = (CoreItemVariant)Mathf.Clamp(data.variant, 0, 1);
        randomiseVariant = false;
    }
}
