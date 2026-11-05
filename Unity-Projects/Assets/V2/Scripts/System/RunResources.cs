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

    /// <summary>
    /// The run starts at full risk and full red fog; a successful trip thins it.
    /// One number drives the mist, the gauge and the success-value penalty, so
    /// they can never disagree.
    /// </summary>
    public const int MaxRisk = 80;

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

    [Header("Party Buff")]
    [Tooltip("Permanent +success value from buff potions. Stacks and lasts for the rest of the run.")]
    [SerializeField] private int partySuccessBonus = 0;
    public int PartySuccessBonus => partySuccessBonus;

    [Header("Risk (red fog)")]
    [Tooltip("Starts at MaxRisk and is spent down by successful trips. Subtracted from every success value; at 0 the fog is gone entirely.")]
    [Range(0, MaxRisk)]
    [SerializeField] private int risk = MaxRisk;

    /// <summary>Current risk, 0..<see cref="MaxRisk"/>. Subtracted from every trip's success value.</summary>
    public int Risk => risk;

    /// <summary>Risk as 0..1 - what the fog overlay and the gauge tube both paint.</summary>
    public float RiskRatio => MaxRisk > 0 ? (float)risk / MaxRisk : 0f;

    /// <summary>Spends risk down to a floor of 0. A negative delta is the normal case (a cleared dungeon).</summary>
    public void AddRisk(int delta)
    {
        if (delta == 0) return;
        SetRisk(risk + delta);
    }

    /// <summary>Overwrites risk, clamped to 0..<see cref="MaxRisk"/>.</summary>
    public void SetRisk(int value)
    {
        int clamped = Mathf.Clamp(value, 0, MaxRisk);
        if (risk == clamped) return;

        risk = clamped;
        OnChanged?.Invoke();
    }

    /// <summary>Adds a buff-potion stack (+10 success value by default) for the whole team.</summary>
    public void AddPartySuccessBonus(int amount)
    {
        partySuccessBonus += amount;
        OnChanged?.Invoke();
    }

    /// <summary>Overwrites the party bonus - used when restoring a save or resetting a test run.</summary>
    public void SetPartySuccessBonus(int value)
    {
        partySuccessBonus = Mathf.Max(0, value);
        OnChanged?.Invoke();
    }

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

        // Loot from a trip arrives here too, but the dice already stung for that
        // roll; the item cue belongs to base work and core items instead, so this
        // stays silent and the callers that own a "you gained something" moment
        // play the clip themselves.
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

        // A core item is the loudest thing that can land in the bag, so it gets
        // the same Success sting as a good roll.
        AudioDirector.Instance?.PlaySuccess();

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
        partySuccessBonus = Mathf.Max(0, data.partySuccessBonus);

        // A save written before Risk existed has no value for it; those runs
        // start the fog at full rather than at zero.
        risk = data.version >= 2 ? Mathf.Clamp(data.risk, 0, MaxRisk) : MaxRisk;

        coreItemA = data.coreItemA;
        coreItemB = data.coreItemB;

        // A restored run keeps the pair it was saved with, even if this
        // particular start rolled the other one.
        variant = (CoreItemVariant)Mathf.Clamp(data.variant, 0, 1);
        randomiseVariant = false;
    }
}
