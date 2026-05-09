namespace Content.Server._Horizon.Shipyard;

[RegisterComponent]
public sealed partial class SpipyardGridRequiresRustClearingComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public int StartingRustWalls = 0;

    [DataField]
    public int PriceAdded = 10000;
}
