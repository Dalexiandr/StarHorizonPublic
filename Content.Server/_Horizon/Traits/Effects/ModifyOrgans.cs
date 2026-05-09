using Content.Shared._Horizon.Traits;

namespace Content.Server._Horizon.Traits;

public sealed partial class ModifyOrgans : BaseTraitEffect
{
    [DataField]
    public string ProtoId;

    [DataField]
    public string SlotId;

    public override void DoEffect(EntityUid uid, IEntityManager entMan)
    {
        var comp = entMan.EnsureComponent<TraitPendingBodyModificationComponent>(uid);
        var data = new OrganReplacement(SlotId, ProtoId);
        comp.Organs.Add(data);
    }
}
