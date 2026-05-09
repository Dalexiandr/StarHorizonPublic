using Content.Shared.Body.Part;
using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Traits;

[RegisterComponent]
public sealed partial class TraitPendingBodyModificationComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public List<PartReplacement> Parts = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public List<OrganReplacement> Organs = new();
}

public sealed class PartReplacement
{
    public BodyPartType PartType;
    public BodyPartType? ParentPartType;
    public BodyPartSymmetry Symmetry;
    public string? ProtoId = null;
    public string? SlotId = null;

    public PartReplacement(BodyPartType partType, BodyPartType? parentPartType, BodyPartSymmetry symmetry, string? protoId = null, string? slotId = null)
    {
        PartType = partType;
        ParentPartType = parentPartType;
        Symmetry = symmetry;
        ProtoId = protoId;
        SlotId = slotId;
    }
};

public sealed class OrganReplacement
{
    public string OrganProto;
    public string OrganSlot;

    public OrganReplacement(string organSlot, string organProto)
    {
        OrganSlot = organSlot;
        OrganProto = organProto;
    }
}
