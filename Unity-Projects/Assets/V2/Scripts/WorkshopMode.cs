/// <summary>
/// What the Workshop is brewing. The skill panel toggles between the two,
/// the same way heal toggles medicine / no medicine.
/// </summary>
public enum WorkshopMode
{
    /// <summary>1 AP + 2 Materials -> 1 Medicine (doubled by the Crafting talent).</summary>
    Medicine,
    /// <summary>2 AP + 4 Materials -> +10 success value for the whole party, for the rest of the run.</summary>
    BuffPotion
}
