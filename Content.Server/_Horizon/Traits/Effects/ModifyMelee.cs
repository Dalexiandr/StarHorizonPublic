using Content.Shared._Horizon.Traits;
using Content.Shared.Weapons.Melee;

namespace Content.Server._Horizon.Traits;

public sealed partial class ModifyMelee : BaseTraitEffect
{
    [DataField]
    public float DamageModifier = 1f;

    [DataField]
    public float SpeedModifier = 1f;

    [DataField]
    public float RangeModifier = 1f;

    public override void DoEffect(EntityUid uid, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<MeleeWeaponComponent>(uid, out var comp))
            return;

        comp.Damage *= DamageModifier;
        comp.AttackRate *= SpeedModifier;
        comp.Range *= RangeModifier;
    }
}
