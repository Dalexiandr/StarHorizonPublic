using Content.Server.Procedural;
using Content.Shared._Horizon.Procedural;
using Content.Shared.Procedural;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Horizon.Procedural;

public sealed class AnCoDungeonMarkerSystem : EntitySystem
{
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnCoDungeonMarkerComponent, MapInitEvent>(OnDungeonSpawnMapInit);
    }

    private void OnDungeonSpawnMapInit(EntityUid uid, AnCoDungeonMarkerComponent component, MapInitEvent args)
    {
        var xform = Transform(uid);

        if (xform.GridUid == null)
        {
            Log.Error($"Dungeon spawn marker {ToPrettyString(uid)} is not on a grid");
            QueueDel(uid);
            return;
        }

        if (!_prototype.TryIndex(component.DungeonPreset, out var dungeonConfig))
        {
            Log.Error($"Dungeon config '{component.DungeonPreset}' not found for {ToPrettyString(uid)}");
            QueueDel(uid);
            return;
        }

        var gridUid = xform.GridUid.Value;
        var mapGrid = Comp<MapGridComponent>(gridUid);
        var seed = component.Seed ?? _random.Next();
        var tilePosition = _maps.LocalToTile(gridUid, mapGrid, xform.Coordinates);

        _dungeon.GenerateDungeon(
            dungeonConfig,
            component.DungeonPreset,
            gridUid,
            mapGrid,
            tilePosition,
            seed);

        QueueDel(uid);
    }
}