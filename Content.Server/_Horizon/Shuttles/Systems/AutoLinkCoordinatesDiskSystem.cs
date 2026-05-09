using Content.Server.Station.Components;
using Content.Shared._Horizon.Shuttles.Components;
using Content.Shared.Shuttles.Components;

namespace Content.Server._Horizon.Shuttles.Systems;

/// <summary>
/// Links coordinate disks to a station map by BecomesStation ID when either the disk or the station appears.
/// </summary>
public sealed class AutoLinkCoordinatesDiskSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoLinkCoordinatesDiskComponent, ComponentStartup>(OnDiskStartup);
        SubscribeLocalEvent<BecomesStationComponent, ComponentStartup>(OnStationStartup);
    }

    private void OnDiskStartup(Entity<AutoLinkCoordinatesDiskComponent> ent, ref ComponentStartup args)
    {
        TryLinkDisk(ent);
    }

    private void OnStationStartup(Entity<BecomesStationComponent> ent, ref ComponentStartup args)
    {
        var query = EntityQueryEnumerator<AutoLinkCoordinatesDiskComponent>();
        while (query.MoveNext(out var uid, out var disk))
        {
            if (disk.TargetStationId != ent.Comp.Id)
                continue;

            TryLinkDisk((uid, disk));
        }
    }

    private void TryLinkDisk(Entity<AutoLinkCoordinatesDiskComponent> ent)
    {
        if (!TryComp<ShuttleDestinationCoordinatesComponent>(ent, out var coords) || coords.Destination != null)
            return;

        var query = EntityQueryEnumerator<BecomesStationComponent, TransformComponent>();

        while (query.MoveNext(out _, out var becomesStation, out var xform))
        {
            if (becomesStation.Id != ent.Comp.TargetStationId)
                continue;

            if (xform.MapUid == null)
                continue;

            coords = EnsureComp<ShuttleDestinationCoordinatesComponent>(ent);
            coords.Destination = xform.MapUid.Value;
            Dirty(ent, coords);
            return;
        }
    }
}
