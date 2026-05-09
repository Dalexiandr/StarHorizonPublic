using Content.Shared._Horizon.Traits;

namespace Content.Server._Horizon.Traits;

public sealed partial class ModifySpeed : BaseTraitEffect
{
    [DataField]
    public float WalkModifier = 1f;

    [DataField]
    public float SprintModifier = 1f;

    public override void DoEffect(EntityUid uid, IEntityManager entMan)
    {
        var comp = entMan.EnsureComponent<TraitMoveSpeedModifierComponent>(uid);
        comp.Modifiers.Add((WalkModifier, SprintModifier));
        entMan.Dirty(uid, comp);
    }
}
