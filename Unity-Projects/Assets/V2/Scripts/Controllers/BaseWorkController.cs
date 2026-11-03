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

    /// <summary>Fired after every base-work attempt, including blocked ones - the log/feedback layer listens to this.</summary>
    public event System.Action<BaseWorkResult> OnBaseWorkCompleted;

    [Header("Cooking")]
    [SerializeField] private int cookApCost = 1;
    [SerializeField] private int cookMaterialCostPerRound = 1;
    [SerializeField] private int cookFoodOutputBase = 1;
    [Tooltip("Output for a unit with the Cooking talent (Cia).")]
    [SerializeField] private int cookFoodOutputWithAptitude = 2;

    [Header("Healing")]
    [SerializeField] private int healApCostWithMedicine = 1;
    [SerializeField] private int healApCostWithoutMedicine = 3;

    [Header("Workshop — Medicine")]
    [SerializeField] private int workshopApCost = 1;
    [SerializeField] private int workshopMaterialsCost = 2;
    [SerializeField] private int workshopMedicineOutput = 1;

    [Header("Workshop — Buff Potion")]
    [Tooltip("Brews a potion that gives the whole party a permanent success-value bonus (stacks).")]
    [SerializeField] private int buffPotionApCost = 2;
    [SerializeField] private int buffPotionMaterialsCost = 5;
    [SerializeField] private int buffPotionSuccessBonus = 10;

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

    // Base costs, before each unit's starvation surcharge - callers that paint
    // more than one unit's card need the unmodified number.
    public int CookBaseApCost => cookApCost;
    public int HealBaseApCost(bool useMedicine) => useMedicine ? healApCostWithMedicine : healApCostWithoutMedicine;

    public int CookApPerRound(UnitCharacter worker) => worker.GetActualApCost(cookApCost);
    public int CookMaterialPerRound => cookMaterialCostPerRound;

    /// <summary>Cia's Cooking talent doubles what a round produces.</summary>
    public int CookFoodPerRound(UnitCharacter worker)
    {
        return worker.HasTalent(UnitTalent.Cooking) ? cookFoodOutputWithAptitude : cookFoodOutputBase;
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
        GetHealPlan(healer, useMedicine, out apCost);
    }

    /// <summary>AP cost of the requested healing method for this healer (starvation included).</summary>
    public void GetHealPlan(UnitCharacter healer, bool useMedicine, out int apCost)
    {
        apCost = healer.GetActualApCost(useMedicine ? healApCostWithMedicine : healApCostWithoutMedicine);
    }

    public int WorkshopApPerRound(UnitCharacter worker) => worker.GetActualApCost(workshopApCost);
    public int WorkshopMaterialsPerRound => workshopMaterialsCost;
    public int WorkshopMedicinePerRound => workshopMedicineOutput;

    // Base crafting costs per mode, before the worker's starvation surcharge.
    public int WorkshopBaseApCost(WorkshopMode mode) => mode == WorkshopMode.BuffPotion ? buffPotionApCost : workshopApCost;
    public int WorkshopBaseMaterialCost(WorkshopMode mode) => mode == WorkshopMode.BuffPotion ? buffPotionMaterialsCost : workshopMaterialsCost;
    public int BuffPotionBonusPerRound => buffPotionSuccessBonus;

    public int WorkshopApPerRound(UnitCharacter worker, WorkshopMode mode) => worker.GetActualApCost(WorkshopBaseApCost(mode));

    /// <summary>How many workshop rounds this unit can afford right now (limited by AP and Materials).</summary>
    public int GetMaxWorkshopRounds(UnitCharacter worker) => GetMaxWorkshopRounds(worker, WorkshopMode.Medicine);

    public int GetMaxWorkshopRounds(UnitCharacter worker, WorkshopMode mode)
    {
        if (RunResources.Instance == null) return 0;

        int apPerRound = Mathf.Max(1, WorkshopApPerRound(worker, mode));
        int materialsPerRound = Mathf.Max(1, WorkshopBaseMaterialCost(mode));

        int byAp = worker.currentActionPoints / apPerRound;
        int byMaterials = RunResources.Instance.Materials / materialsPerRound;
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
            return Emit(result);
        }

        int foodPerRound = worker.HasTalent(UnitTalent.Cooking) ? cookFoodOutputWithAptitude : cookFoodOutputBase;

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

        if (result.repeatsPerformed == 0) result.blocked = true;
        return Emit(result);
    }

    // --- Healing ---

    public bool CanHeal(UnitCharacter healer, UnitCharacter patient) => CanHeal(healer, patient, null);

    /// <summary>
    /// Validates a heal. Pass <paramref name="useMedicine"/> to honour the UI's
    /// mode toggle; null keeps the legacy behaviour (medicine whenever it's in stock).
    /// </summary>
    public bool CanHeal(UnitCharacter healer, UnitCharacter patient, bool? useMedicine)
    {
        if (healer == null || patient == null || healer == patient) return false;
        if (!healer.CanBeDispatched) return false;
        if (patient.state != UnitState.Hurt) return false;

        bool wantsMedicine = useMedicine ?? (RunResources.Instance != null && RunResources.Instance.Medicine >= 1);
        if (wantsMedicine && (RunResources.Instance == null || RunResources.Instance.Medicine < 1)) return false;

        int apCost = wantsMedicine ? healApCostWithMedicine : healApCostWithoutMedicine;
        return healer.currentActionPoints >= healer.GetActualApCost(apCost);
    }

    public BaseWorkResult Heal(UnitCharacter healer, UnitCharacter patient) => Heal(healer, patient, null);

    /// <summary>Heals one patient. <paramref name="useMedicine"/> null = pick automatically.</summary>
    public BaseWorkResult Heal(UnitCharacter healer, UnitCharacter patient, bool? useMedicine)
    {
        var result = new BaseWorkResult { workType = BaseWorkType.Healing, worker = healer };

        if (!CanHeal(healer, patient, useMedicine))
        {
            result.blocked = true;
            return Emit(result);
        }

        bool useMed = useMedicine ?? (RunResources.Instance.Medicine >= 1);
        int apCost = useMed ? healApCostWithMedicine : healApCostWithoutMedicine;

        healer.TrySpendActionPoints(apCost);
        if (useMed)
        {
            RunResources.Instance.TryConsumeItem(InventoryItemType.Medicine, 1);
        }

        // Healed, but only back to play if the AP is still there - a patient who
        // spent everything on the trip that hurt them goes to Resting instead.
        patient.ReturnToPlay();

        // Ann's Heal talent: the patient is patched up properly, so their next
        // trip gets +10 success value and then the buff is spent.
        bool blessedByTalent = healer.HasTalent(UnitTalent.Heal);
        if (blessedByTalent) patient.blessed = true;

        result.healedUnit = patient;
        result.usedMedicine = useMed;
        result.blessedApplied = blessedByTalent;
        return Emit(result);
    }

    // --- Workshop ---

    public bool CanUseWorkshop(UnitCharacter worker) => CanUseWorkshop(worker, WorkshopMode.Medicine);

    public bool CanUseWorkshop(UnitCharacter worker, WorkshopMode mode)
    {
        if (!worker.CanBeDispatched) return false;
        if (worker.currentActionPoints < worker.GetActualApCost(WorkshopBaseApCost(mode))) return false;
        if (RunResources.Instance == null || RunResources.Instance.Materials < WorkshopBaseMaterialCost(mode)) return false;
        return true;
    }

    public BaseWorkResult UseWorkshop(UnitCharacter worker) => UseWorkshop(worker, 1);

    public BaseWorkResult UseWorkshop(UnitCharacter worker, int desiredRepeats) => UseWorkshop(worker, desiredRepeats, WorkshopMode.Medicine);

    /// <summary>
    /// Crafts up to desiredRepeats rounds in one confirm (the doc's "จำนวนสูงสุดที่สร้างได้").
    /// Stops early if AP or Materials run out; blocked only if not even one round was possible.
    ///
    /// Medicine mode brews healing stock (Boon's Crafting talent doubles it).
    /// BuffPotion mode brews a permanent party-wide success-value buff that stacks.
    /// </summary>
    public BaseWorkResult UseWorkshop(UnitCharacter worker, int desiredRepeats, WorkshopMode mode)
    {
        var result = new BaseWorkResult { workType = BaseWorkType.Workshop, worker = worker, workshopMode = mode };

        if (!worker.CanBeDispatched || RunResources.Instance == null)
        {
            result.blocked = true;
            return Emit(result);
        }

        int apCost = WorkshopBaseApCost(mode);
        int materialCost = WorkshopBaseMaterialCost(mode);
        int medicinePerRound = worker.HasTalent(UnitTalent.Crafting) ? workshopMedicineOutput * 2 : workshopMedicineOutput;

        for (int i = 0; i < desiredRepeats; i++)
        {
            if (worker.currentActionPoints < worker.GetActualApCost(apCost)) break;
            if (!RunResources.Instance.TryConsumeItem(InventoryItemType.Materials, materialCost)) break;

            worker.TrySpendActionPoints(apCost);

            if (mode == WorkshopMode.BuffPotion)
            {
                RunResources.Instance.AddPartySuccessBonus(buffPotionSuccessBonus);
                result.buffPotionsProduced++;
                result.successBonusGranted += buffPotionSuccessBonus;
            }
            else
            {
                RunResources.Instance.AddItem(InventoryItemType.Medicine, medicinePerRound);
                result.medicineProduced += medicinePerRound;
            }

            result.repeatsPerformed++;
        }

        if (result.repeatsPerformed == 0)
        {
            result.blocked = true;
        }
        return Emit(result);
    }

    /// <summary>Single funnel for the completion event so no exit path forgets to fire it.</summary>
    private BaseWorkResult Emit(BaseWorkResult result)
    {
        RunStats.CountBaseWork(result);
        OnBaseWorkCompleted?.Invoke(result);

        // Cook, heal and craft all end here, so this is the one place that can
        // tell "something came out of it" from "nothing happened". The success
        // sting is the game's generic "you gained something" cue.
        if (ProducedSomething(result)) AudioDirector.Instance?.PlaySuccess();

        return result;
    }

    private static bool ProducedSomething(BaseWorkResult result)
    {
        if (result == null || result.blocked) return false;

        return result.foodProduced > 0
            || result.healedUnit != null
            || result.medicineProduced > 0
            || result.buffPotionsProduced > 0;
    }
}
