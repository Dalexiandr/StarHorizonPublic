using Content.Shared._Horizon.Traits;
using Content.Shared.Damage.Components;

namespace Content.Server._Horizon.Traits;

public sealed partial class ModifyStamina : BaseTraitEffect
{
    [DataField]
    public float AfterCritDecayMultiplier = 1f;

    [DataField]
    public float CooldownModifier = 1f;

    [DataField]
    public float CritThresholdModifier = 1f;

    [DataField]
    public float StunModifier = 1f;

    public override void DoEffect(EntityUid uid, IEntityManager entMan)
    {
        var comp = entMan.EnsureComponent<StaminaComponent>(uid);
        comp.AfterCritDecayMultiplier *= AfterCritDecayMultiplier;
        comp.Cooldown *= CooldownModifier;
        comp.CritThreshold *= CritThresholdModifier;
        comp.StunTime *= StunModifier;

        entMan.Dirty(uid, comp);
    }
}
