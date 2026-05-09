using Content.Shared._Horizon._Fractions.AnCo;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using System.Numerics;

namespace Content.Server._Horizon._Fractions.AnCo;

public sealed class AnCoHammerRepulseSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnCoHammerRepulseComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<AnCoHammerRepulseComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMeleeHit(EntityUid uid, AnCoHammerRepulseComponent component, MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        component.CurrentCharge = Math.Min(component.CurrentCharge + component.ChargePerHit, component.MaxCharge);
        Dirty(uid, component);

        if (component.CurrentCharge < component.MaxCharge)
            return;

        component.CurrentCharge = 0;
        Dirty(uid, component);

        var userPos = _transform.GetWorldPosition(args.User);

        foreach (var target in args.HitEntities)
        {
            var targetPos = _transform.GetWorldPosition(target);
            var direction = args.Direction ?? targetPos - userPos;

            if (direction == Vector2.Zero)
                continue;

            _throwing.TryThrow(target, direction.Normalized() * component.Distance, component.ThrowStrength, args.User);

            if (TryComp<StaminaComponent>(target, out var stamina))
            {
                var damage = stamina.CritThreshold * component.StaminaDamagePercent;
                _stamina.TakeStaminaDamage(target, damage, stamina);
            }
        }

        _popup.PopupEntity(Loc.GetString("anco-hammer-repulse"), args.User, args.User);
    }

    private void OnExamined(EntityUid uid, AnCoHammerRepulseComponent component, ExaminedEvent args)
    {
        var chargePercent = (int)((float)component.CurrentCharge / component.MaxCharge * 100);
        args.PushMarkup(Loc.GetString("anco-hammer-charge", ("charge", chargePercent)));
    }
}
