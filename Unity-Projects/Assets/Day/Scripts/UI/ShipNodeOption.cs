/// <summary>
/// What kind of Ship Phase node this is:
/// - Normal: nothing special, just heads into the Day Phase.
/// - Bonus: biases what resource type spawns more in the upcoming Day Phase.
/// - Recruit: opens an event to hire extra crew using resources, before the Day Phase.
/// </summary>
public enum ShipNodeType
{
    Normal,
    Bonus,
    Recruit
}

/// <summary>
/// Plain-data preview of one Ship Phase navigator card. No GameObject
/// involved — same "data first" pattern as CrewData.
/// </summary>
public class ShipNodeOption
{
    public ShipNodeType Type;
    public string Title;
    public string Description;

    /// <summary>Only meaningful when Type == Bonus. The resource key ("wood"/"food"/"relic") that will spawn more often today.</summary>
    public string BonusResourceType;
}
