using Content.Shared.Containers.ItemSlots;

namespace Content.Server._Horizon.Cytology.Components;

[RegisterComponent]
public sealed partial class CytologyMicroscopeComponent : Component
{
    [DataField]
    public ItemSlot PetriDishSlot = new();
}
