using Content.Shared.Blocking;
using Content.Shared.Wieldable;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared._Horizon._Fractions.AnCo;

public sealed class AnCoBlockingOnWieldSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnCoBlockingOnWieldComponent, ItemWieldedEvent>(OnWielded);
        SubscribeLocalEvent<AnCoBlockingOnWieldComponent, ItemUnwieldedEvent>(OnUnwielded);
    }

    private void OnWielded(EntityUid uid, AnCoBlockingOnWieldComponent component, ref ItemWieldedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        component.IsWielded = true;
        Dirty(uid, component);

        if (!TryComp<BlockingComponent>(uid, out var blocking))
            return;

        blocking.User = args.User;
        Dirty(uid, blocking);

        if (TryComp<PhysicsComponent>(args.User, out var physics) &&
            physics.BodyType != BodyType.Static &&
            !HasComp<BlockingUserComponent>(args.User))
        {
            var userComp = EnsureComp<BlockingUserComponent>(args.User);
            userComp.BlockingItem = uid;
            userComp.OriginalBodyType = physics.BodyType;
        }
    }

    private void OnUnwielded(EntityUid uid, AnCoBlockingOnWieldComponent component, ref ItemUnwieldedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        component.IsWielded = false;
        Dirty(uid, component);

        if (TryComp<BlockingComponent>(uid, out var blocking))
        {
            blocking.User = null;
            Dirty(uid, blocking);
        }

        if (HasComp<BlockingUserComponent>(args.User))
        {
            RemCompDeferred<BlockingUserComponent>(args.User);
        }
    }
}
