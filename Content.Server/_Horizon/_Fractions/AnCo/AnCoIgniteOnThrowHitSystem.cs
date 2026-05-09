using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared._Horizon._Fractions.AnCo;
using Content.Shared.Throwing;

namespace Content.Server._Horizon._Fractions.AnCo;

public sealed class AnCoIgniteOnThrowHitSystem : EntitySystem
{
    [Dependency] private readonly FlammableSystem _flammable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnCoIgniteOnThrowHitComponent, ThrowDoHitEvent>(OnThrowHit);
    }

    private void OnThrowHit(EntityUid uid, AnCoIgniteOnThrowHitComponent component, ThrowDoHitEvent args)
    {
        if (!TryComp<FlammableComponent>(args.Target, out var flammable))
            return;

        _flammable.AdjustFireStacks(args.Target, component.FireStacks, flammable);
        _flammable.Ignite(args.Target, uid, flammable);
    }
}
