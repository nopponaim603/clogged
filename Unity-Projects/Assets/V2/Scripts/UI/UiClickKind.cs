/// <summary>Which click clip a control uses.</summary>
public enum UiClickKind
{
    /// <summary>Chrome: menu, options, help, close, mode toggles, CONTINUE, sliders.</summary>
    Plain,

    /// <summary>Choosing something in the run: a unit card, a dungeon card, the confirm button.</summary>
    Select,

    /// <summary>The cook skill - a bright food-ready chime.</summary>
    Cook,

    /// <summary>The heal skill.</summary>
    Heal,

    /// <summary>The craft skill.</summary>
    Craft,

    /// <summary>A page opening: options or help.</summary>
    Popup
}
