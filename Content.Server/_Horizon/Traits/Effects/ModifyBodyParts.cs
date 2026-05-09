using Content.Shared._Horizon.Traits;
using Content.Shared.Body.Part;

namespace Content.Server._Horizon.Traits;

public sealed partial class ModifyBodyParts : BaseTraitEffect
{
    [DataField]
    public BodyPartType PartType;

    [DataField]
    public BodyPartSymmetry Symmetry;

    [DataField]
    public string? ProtoId = null;

    [DataField]
    public string? SlotId = null;

    [DataField]
    public BodyPartType? ParentPartType;


    public override void DoEffect(EntityUid uid, IEntityManager entMan)
    {
        var comp = entMan.EnsureComponent<TraitPendingBodyModificationComponent>(uid);
        var data = new PartReplacement(PartType, ParentPartType, Symmetry, ProtoId, SlotId);
        comp.Parts.Add(data);
    }
}
