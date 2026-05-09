namespace Content.Server._Horizon.Shipyard;

[RegisterComponent]
public sealed partial class ShipyardTileAtmosPriceMarkerComponent : Component
{
    [DataField(required: true)]
    public int PriceAdded;
}
