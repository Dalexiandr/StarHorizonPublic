using System.Numerics;
using Content.Server._White.Spawners.Components;
using Content.Server.Atmos.Components;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Log;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server._White.Spawners.Systems;

public sealed class AreaSpawnerSystem : EntitySystem
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("TEST.areaspawner");
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private readonly List<Vector2> _offsets = new List<Vector2>
    {
                           new Vector2(0, 1),
        new Vector2(-1, 0),                  new Vector2(1, 0),
                           new Vector2(0, -1)
    };

    public override void Initialize()
    {
        _sawmill.Debug("AreaSpawnerSystem initialized");
        SubscribeLocalEvent<AreaSpawnerComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(EntityUid uid, AreaSpawnerComponent component, ComponentShutdown args)
    {
        _sawmill.Debug($"OnShutdown: uid={uid}, spawnedCount={component.Spawneds.Count}");
        foreach (var spawned in component.Spawneds)
        {
            var despawnComponent = new TimedDespawnComponent
            {
                Lifetime = _random.NextFloat(component.MinTime, component.MaxTime)
            };
            AddComp(spawned, despawnComponent);
            _sawmill.Debug($"OnShutdown: added TimedDespawnComponent to spawned={spawned}, lifetime={despawnComponent.Lifetime}");
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<AreaSpawnerComponent>();
        while (query.MoveNext(out var uid, out var areaSpawner))
        {
            if (time < areaSpawner.SpawnAt)
                continue;

            _sawmill.Debug($"Update: spawning for uid={uid}, prototype={areaSpawner.SpawnPrototype}");
            areaSpawner.SpawnAt = time + areaSpawner.SpawnDelay;

            var validTiles = GetValidTilesInRadius(uid, areaSpawner);
            _sawmill.Debug($"Update: found {validTiles.Count} valid tiles");

            foreach (var tile in validTiles)
            {
                var spawnedUid = Spawn(areaSpawner.SpawnPrototype, Transform(uid).Coordinates.Offset(tile));
                areaSpawner.Spawneds.Add(spawnedUid);
                _sawmill.Debug($"Update: spawned entity={spawnedUid} at tile={tile}");
            }
        }
    }

    public List<Vector2> GetValidTilesInRadius(EntityUid uid, AreaSpawnerComponent component)
    {
        _sawmill.Debug($"GetValidTilesInRadius: uid={uid}, radius={component.Radius}");
        var validTiles = new List<Vector2>();
        for (var y = -component.Radius; y <= component.Radius; y++)
        {
            for (var x = -component.Radius; x <= component.Radius; x++)
            {
                var tile = new Vector2(x, y);
                if (IsTileValidForSpawn(uid, component, tile))
                    validTiles.Add(tile);
            }
        }

        _sawmill.Debug($"GetValidTilesInRadius: found {validTiles.Count} valid tiles");
        return validTiles;
    }

    public bool IsTileValidForSpawn(EntityUid uid, AreaSpawnerComponent component, Vector2 offset)
    {
        _sawmill.Debug($"IsTileValidForSpawn: uid={uid}, offset={offset}");
        var xform = Transform(uid);
        if (_transform.GetGrid((uid, xform)) is not { } gridUid
            || !TryComp<MapGridComponent>(gridUid, out var mapGridComponent))
        {
            _sawmill.Debug($"IsTileValidForSpawn: no grid found, returning false");
            return false;
        }

        var coords = xform.Coordinates.Offset(offset);
        var tile = _turf.GetTileRef(coords);

        if (!tile.HasValue || tile.Value.Tile.IsEmpty)
        {
            _sawmill.Debug($"IsTileValidForSpawn: tile empty, returning false");
            return false;
        }

        foreach (var entity in _map.GetAnchoredEntities((gridUid, mapGridComponent), coords))
        {
            if (TryComp<AirtightComponent>(entity, out var airtight) && airtight.AirBlocked
                || Prototype(entity) != null && Prototype(entity)! == component.SpawnPrototype
                || Prototype(entity) == Prototype(uid))
            {
                _sawmill.Debug($"IsTileValidForSpawn: entity blocking tile, returning false");
                return false;
            }
        }

        foreach (var checkOffset in _offsets)
        {
            var checkCoords = coords.Offset(checkOffset);
            foreach (var entity in _map.GetAnchoredEntities((gridUid, mapGridComponent), checkCoords))
            {
                if (component.Spawneds.Contains(entity) || entity == uid)
                {
                    _sawmill.Debug($"IsTileValidForSpawn: valid tile found, returning true");
                    return true;
                }
            }
        }

        _sawmill.Debug($"IsTileValidForSpawn: no valid neighbor found, returning false");
        return false;
    }
}
