using Content.Shared._Horizon.Traits;

namespace Content.Server._Horizon.Traits;

public sealed partial class ModifyDamageSlowdown : BaseTraitEffect
{
    [DataField]
    public float Modifier = 1f;

    public override void DoEffect(EntityUid uid, IEntityManager entMan)
    {
        var comp = entMan.EnsureComponent<TraitDamageSlowdownModifierComponent>(uid);
        comp.Modifiers.Add(Modifier);
        entMan.Dirty(uid, comp);
    }
}
