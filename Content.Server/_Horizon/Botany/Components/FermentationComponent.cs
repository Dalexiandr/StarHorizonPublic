using Content.Shared.FixedPoint;

namespace Content.Server._Horizon.Botany.Components;

[RegisterComponent]
public sealed partial class FermentationComponent : Component
{
    [DataField("fermentate")]
    public string Fermentate = "Beer";

    [DataField("quantity")]
    public FixedPoint2 Quantity = new();
}
