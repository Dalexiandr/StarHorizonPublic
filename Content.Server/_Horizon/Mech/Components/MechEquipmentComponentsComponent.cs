using Robust.Shared.Prototypes;

namespace Content.Server._Horizon.Mech.Components;

[RegisterComponent]
public sealed partial class MechEquipmentComponentsComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();
}
