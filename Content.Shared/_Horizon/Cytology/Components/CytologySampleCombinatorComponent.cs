using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._Horizon.Cytology.Components;

[RegisterComponent]
public sealed partial class CytologySampleCombinatorComponent : Component
{
    [DataField]
    public ItemSlot PetriDishSlot = new();

    [DataField]
    public List<ItemSlot> DiskSlot = new();
}

