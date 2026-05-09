using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Systems;

namespace Content.Shared._Horizon.Traits;

public abstract class SharedQuirksSystem : EntitySystem
{
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TraitMoveSpeedModifierComponent, RefreshMovementSpeedModifiersEvent>(OnModifyMoveSpeed);
        SubscribeLocalEvent<TraitDamageSlowdownModifierComponent, ModifySlowOnDamageSpeedEvent>(OnModifyDamageSlowdown);
        SubscribeLocalEvent<LowPainToleranceComponent, DamageModifyEvent>(OnPainDamageModify);
    }

    private void OnModifyMoveSpeed(Entity<TraitMoveSpeedModifierComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var sortedModifiers = ent.Comp.Modifiers.OrderBy(x => x.Item1).ThenByDescending(x => x.Item2);
        foreach (var modifier in sortedModifiers)
        {
            args.ModifySpeed(modifier.Item1, modifier.Item2);
        }
    }

    private void OnModifyDamageSlowdown(Entity<TraitDamageSlowdownModifierComponent> ent, ref ModifySlowOnDamageSpeedEvent args)
    {
        var sortedModifiers = ent.Comp.Modifiers.OrderBy(x => x);
        foreach (var modifier in sortedModifiers)
        {
            var dif = 1 - args.Speed;
            if (dif <= 0)
                return;

            args.Speed = Math.Clamp(args.Speed + dif * modifier, 0.1f, 1);
        }
    }

    private void OnPainDamageModify(Entity<LowPainToleranceComponent> ent, ref DamageModifyEvent args)
    {
        if (!TryComp<StaminaComponent>(ent.Owner, out var stamina))
            return;

        var stam = _stamina.GetStaminaDamage(ent.Owner);
        var modifier = stam / stamina.CritThreshold * ent.Comp.DamageModifier;

        args.Damage *= 1 + modifier;
    }
}
