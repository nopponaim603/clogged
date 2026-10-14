/// <summary>Report of one base-work action, for the results UI to display.</summary>
public class BaseWorkResult
{
    public BaseWorkType workType;
    public UnitCharacter worker;
    public bool blocked;

    // Cooking
    public int repeatsPerformed;
    public int foodProduced;
    public int cookingMaterialConsumed;

    // Healing
    public UnitCharacter healedUnit;
    public bool usedMedicine;

    // Workshop
    public int medicineProduced;
}
