using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._Horizon.Shipyard;

[RegisterComponent]
public sealed partial class ShipyardEntityPriceMarkerComponent : Component
{
    [DataField]
    public float Radius = 4f;

    [DataField]
    public int Count = 1;

    [DataField(required: true)]
    public ProtoId<TagPrototype> Tag;

    [DataField(required: true)]
    public int PriceAdded;
}
