using Content.Shared._White.Xenomorphs.Plasma;
using Content.Shared._White.Xenomorphs.Plasma.Components;
using Content.Shared._White.Xenomorphs.Stealth;
using Content.Shared._White.Xenomorphs.Xenomorph;
using Content.Shared.Placeable;
using Robust.Server.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Content.Server._White.Xenomorphs.Plasma;

public sealed class PlasmaSystem : SharedPlasmaSystem
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("TEST.plasma");
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly PhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill.Debug("PlasmaSystem initialized");
        SubscribeLocalEvent<PlasmaGainModifierComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<PlasmaGainModifierComponent, ItemRemovedEvent>(OnItemRemoved);
    }

    private void OnItemPlaced(EntityUid uid, PlasmaGainModifierComponent component, ItemPlacedEvent args)
    {
        _sawmill.Debug($"OnItemPlaced: uid={uid}, otherEntity={args.OtherEntity}");
        if (!TryComp<XenomorphComponent>(args.OtherEntity, out var xenomorph) || xenomorph.OnWeed)
            return;

        xenomorph.OnWeed = true;
        _sawmill.Debug($"OnItemPlaced: set OnWeed=true for {args.OtherEntity}");
    }

    private void OnItemRemoved(EntityUid uid, PlasmaGainModifierComponent component, ItemRemovedEvent args)
    {
        _sawmill.Debug($"OnItemRemoved: uid={uid}, otherEntity={args.OtherEntity}");
        if (!TryComp<XenomorphComponent>(args.OtherEntity, out var xenomorph) || !xenomorph.OnWeed)
            return;

        foreach (var contact in _physics.GetContactingEntities(args.OtherEntity))
        {
            if (contact == uid || !HasComp<PlasmaGainModifierComponent>(contact))
                continue;

            _sawmill.Debug($"OnItemRemoved: found other contact, keeping OnWeed=true");
            return;
        }

        xenomorph.OnWeed = false;
        _sawmill.Debug($"OnItemRemoved: set OnWeed=false for {args.OtherEntity}");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<PlasmaVesselComponent>();
        var count = 0;
        while (query.MoveNext(out var uid, out var plasmaVessel))
        {
            count++;
            if (time < plasmaVessel.NextPointsAt)
                continue;

            plasmaVessel.NextPointsAt = time + TimeSpan.FromSeconds(1);

            var plasma = plasmaVessel.PlasmaPerSecondOffWeed;
            if (TryComp<XenomorphComponent>(uid, out var xenomorph) && xenomorph.OnWeed)
                plasma = plasmaVessel.PlasmaPerSecondOnWeed;

            if (TryComp<StealthOnWalkComponent>(uid, out var stealthOnWalk) && stealthOnWalk.Stealth)
                plasma -= stealthOnWalk.PlasmaCost;

            _sawmill.Debug($"Update: changing plasma for uid={uid}, amount={plasma}");
            ChangePlasmaAmount(uid, plasma, plasmaVessel);
        }
        if (count > 0)
            _sawmill.Debug($"Update: processed {count} plasma vessels");
    }
}
