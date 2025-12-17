namespace Content.Shared._Horizon.WorldItem;

[RegisterComponent]
public sealed partial class WorldItemComponent : Component
{
    public Dictionary<int, string> DefaultSpriteStates = new();

    [DataField]
    public bool Continue;

    [DataField]
    public string Prefix = "_world";

    [DataField]
    public string? OverridePrefix;
}
