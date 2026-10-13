using UnityEngine;

/// <summary>
/// The 3 base-work actions (doc sections 12-15, numbers table "งานที่ฐาน").
/// Unlike Send-to-Gather, base work has no once-per-day limit - the same or
/// different units can do base work any number of times, before or after
/// a gather trip.
/// </summary>
public class BaseWorkController : MonoBehaviour
{
    public static BaseWorkController Instance { get; private set; }

    [Header("Cooking")]
    [SerializeField] private int cookApCost = 1;
    [SerializeField] private int cookMaterialCostPerRound = 1;
    [SerializeField] private int cookFoodOutputBase = 1;
    [SerializeField] private int cookFoodOutputWithAptitude = 2;

    [Header("Healing")]
    [SerializeField] private int healApCostWithMedicine = 1;
    [SerializeField] private int healApCostWithoutMedicine = 2;

    [Header("Workshop")]
    [SerializeField] private int workshopApCost = 1;
    [SerializeField] private int workshopMaterialsCost = 2;
    [SerializeField] private int workshopMedicineOutput = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // --- Planning helpers (the UI reads these so the numbers live in one place) ---

    public int CookApPerRound(UnitCharacter worker) => worker.GetActualApCost(cookApCost);
    public int CookMaterialPerRound => cookMaterialCostPerRound;

    public int CookFoodPerRound(UnitCharacter worker)
    {
        return worker.HasAptitude(AptitudeTag.Cooking) ? cookFoodOutputWithAptitude : cookFoodOutputBase;
    }

    /// <summary>How many cooking rounds this unit can afford right now (limited by AP and Cooking Material).</summary>
    public int GetMaxCookRounds(UnitCharacter worker)
    {
        if (RunResources.Instance == null) return 0;

        int byAp = worker.currentActionPoints / Mathf.Max(1, CookApPerRound(worker));
        int byMaterial = RunResources.Instance.CookingMaterial / Mathf.Max(1, cookMaterialCostPerRound);
        return Mathf.Min(byAp, byMaterial);
    }

    /// <summary>Which healing method would be used right now (Medicine if any is in stock) and its real AP cost for this healer.</summary>
    public void GetHealPlan(UnitCharacter healer, out bool useMedicine, out int apCost)
    {
        useMedicine = RunResources.Instance != null && RunResources.Instance.Medicine >= 1;
        apCost = healer.GetActualApCost(useMedicine ? healApCostWithMedicine : healApCostWithoutMedicine);
    }

    public int WorkshopApPerRound(UnitCharacter worker) => worker.GetActualApCost(workshopApCost);
    public int WorkshopMaterialsPerRound => workshopMaterialsCost;
    public int WorkshopMedicinePerRound => workshopMedicineOutput;

    /// <summary>How many workshop rounds this unit can afford right now (limited by AP and Materials).</summary>
    public int GetMaxWorkshopRounds(UnitCharacter worker)
    {
        if (RunResources.Instance == null) return 0;

        int byAp = worker.currentActionPoints / Mathf.Max(1, WorkshopApPerRound(worker));
        int byMaterials = RunResources.Instance.Materials / Mathf.Max(1, workshopMaterialsCost);
        return Mathf.Min(byAp, byMaterials);
    }

    // --- Cooking ---

    public bool CanCook(UnitCharacter worker)
    {
        if (!worker.CanBeDispatched) return false;
        if (worker.currentActionPoints < worker.GetActualApCost(cookApCost)) return false;
        if (RunResources.Instance == null || RunResources.Instance.CookingMaterial < cookMaterialCostPerRound) return false;
        return true;
    }

    /// <summary>
    /// Cooks up to desiredRepeats rounds in one confirm, per the doc's "เลือกได้ว่าจะใช้ AP เท่าไหร่".
    /// Stops early - without failing the whole action - if AP or cooking material runs out mid-way.
    /// </summary>
    public BaseWorkResult Cook(UnitCharacter worker, int desiredRepeats)
    {
        var result = new BaseWorkResult { workType = BaseWorkType.Cooking, worker = worker };

        if (!worker.CanBeDispatched || RunResources.Instance == null)
        {
            result.blocked = true;
            return result;
        }

        int foodPerRound = worker.HasAptitude(AptitudeTag.Cooking) ? cookFoodOutputWithAptitude : cookFoodOutputBase;

        for (int i = 0; i < desiredRepeats; i++)
        {
            int apCost = worker.GetActualApCost(cookApCost);
            if (worker.currentActionPoints < apCost) break;
            if (!RunResources.Instance.TryConsumeItem(InventoryItemType.CookingMaterial, cookMaterialCostPerRound)) break;

            worker.TrySpendActionPoints(cookApCost);
            RunResources.Instance.AddFood(foodPerRound);

            result.repeatsPerformed++;
            result.foodProduced += foodPerRound;
            result.cookingMaterialConsumed += cookMaterialCostPerRound;
        }

        return result;
    }

    // --- Healing ---

    public bool CanHeal(UnitCharacter healer, UnitCharacter patient)
    {
        if (healer == null || patient == null || healer == patient) return false;
        if (!healer.CanBeDispatched) return false;
        if (patient.state != UnitState.Hurt) return false;

        bool hasMedicine = RunResources.Instance != null && RunResources.Instance.Medicine >= 1;
        int apCost = hasMedicine ? healApCostWithMedicine : healApCostWithoutMedicine;
        return healer.currentActionPoints >= healer.GetActualApCost(apCost);
    }

    public BaseWorkResult Heal(UnitCharacter healer, UnitCharacter patient)
    {
        var result = new BaseWorkResult { workType = BaseWorkType.Healing, worker = healer };

        if (!CanHeal(healer, patient))
        {
            result.blocked = true;
            return result;
        }

        bool useMedicine = RunResources.Instance.Medicine >= 1;
        int apCost = useMedicine ? healApCostWithMedicine : healApCostWithoutMedicine;

        healer.TrySpendActionPoints(apCost);
        if (useMedicine)
        {
            RunResources.Instance.TryConsumeItem(InventoryItemType.Medicine, 1);
        }

        patient.state = UnitState.Idle;

        result.healedUnit = patient;
        result.usedMedicine = useMedicine;
        return result;
    }

    // --- Workshop ---

    public bool CanUseWorkshop(UnitCharacter worker)
    {
        if (!worker.CanBeDispatched) return false;
        if (worker.currentActionPoints < worker.GetActualApCost(workshopApCost)) return false;
        if (RunResources.Instance == null || RunResources.Instance.Materials < workshopMaterialsCost) return false;
        return true;
    }

    public BaseWorkResult UseWorkshop(UnitCharacter worker) => UseWorkshop(worker, 1);

    /// <summary>
    /// Crafts up to desiredRepeats rounds in one confirm (the doc's "จำนวนสูงสุดที่สร้างได้").
    /// Stops early if AP or Materials run out; blocked only if not even one round was possible.
    /// </summary>
    public BaseWorkResult UseWorkshop(UnitCharacter worker, int desiredRepeats)
    {
        var result = new BaseWorkResult { workType = BaseWorkType.Workshop, worker = worker };

        if (!worker.CanBeDispatched || RunResources.Instance == null)
        {
            result.blocked = true;
            return result;
        }

        for (int i = 0; i < desiredRepeats; i++)
        {
            if (worker.currentActionPoints < worker.GetActualApCost(workshopApCost)) break;
            if (!RunResources.Instance.TryConsumeItem(InventoryItemType.Materials, workshopMaterialsCost)) break;

            worker.TrySpendActionPoints(workshopApCost);
            RunResources.Instance.AddItem(InventoryItemType.Medicine, workshopMedicineOutput);

            result.repeatsPerformed++;
            result.medicineProduced += workshopMedicineOutput;
        }

        if (result.repeatsPerformed == 0)
        {
            result.blocked = true;
        }
        return result;
    }
}
