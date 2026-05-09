using System.Linq;
using Content.Server.Botany.Components;
using Content.Server.Stack;
using Content.Server.Wires;
using Content.Shared._Horizon.Materials;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Storage;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._Horizon.Materials;

public sealed class LumberMillSystem : SharedLumberMillSystem
{
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LumberMillComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<LumberMillComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<ActiveLumberMillComponent, PowerChangedEvent>(OnActivePowerChanged);
    }

    private void OnPowerChanged(Entity<LumberMillComponent> entity, ref PowerChangedEvent args)
    {
        AmbientSound.SetAmbience(entity.Owner, entity.Comp.Enabled && args.Powered);
        entity.Comp.Powered = args.Powered;
        Dirty(entity);
    }

    private void OnInteractUsing(Entity<LumberMillComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = TryStartProcessItem(entity.Owner, args.Used, entity.Comp, args.User, predictSound: false);
    }

    private void OnActivePowerChanged(Entity<ActiveLumberMillComponent> entity, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            TryFinishProcessItem(entity.Owner, null, entity.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<LumberMillComponent, StorageComponent>();
        while (query.MoveNext(out var uid, out var mill, out var storage))
        {
            if (HasComp<ActiveLumberMillComponent>(uid))
                continue;
            if (!CanStart(uid, mill))
                continue;
            if (storage.Container.ContainedEntities.Count == 0)
                continue;
            var item = storage.Container.ContainedEntities.FirstOrDefault();
            if (item == EntityUid.Invalid)
                continue;
            if (!Container.Remove(item, storage.Container))
                continue;
            TryStartProcessItem(uid, item, mill, user: null, predictSound: false);
        }
    }

    public override void FinishProcessAndSpawnOutput(EntityUid uid, EntityUid item, float completion, LumberMillComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        base.FinishProcessAndSpawnOutput(uid, item, completion, component);

        if (!TryComp<LogComponent>(item, out var log))
        {
            QueueDel(item);
            return;
        }

        var spawnCount = (int)Math.Max(1, Math.Round(log.SpawnCount * completion));
        if (spawnCount <= 0)
        {
            QueueDel(item);
            return;
        }

        var coords = _transform.GetMoverCoordinates(uid);
        for (var i = 0; i < spawnCount; i++)
        {
            var spawned = Spawn(log.SpawnedPrototype, coords);
            _stack.TryMergeToContacts(spawned);
        }

        QueueDel(item);
    }
}
