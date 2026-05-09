using System.Linq;
using Content.Server._NF.GameTicking.Events;
using Content.Server._NF.PublicTransit.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.GameTicking.Events;
using Content.Server.Parallax;
using Content.Server.Screens.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Shared._Horizon.CCVar;
using Content.Shared._Horizon.Planet;
using Content.Shared._NF.Shipyard.Prototypes;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Shuttles.Components;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Horizon.Planet;

public sealed class PlanetSystem : EntitySystem
{

    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly ShuttleSystem _shuttles = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private List<(Vector2i, Tile)> _setTiles = new();
    public Dictionary<ProtoId<PlanetPrototype>, EntityUid> LoadedPlanets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarted);
        SubscribeLocalEvent<StationsGeneratedEvent>(OnStationsGenerated);
        SubscribeLocalEvent<PlanetTaxiComponent, FTLCompletedEvent>(OnDock);
    }

    private void OnRoundStarted(RoundStartingEvent ev)
    {
        if (!_cfg.GetCVar(HorizonCCVars.SpawnPlanets))
            return;

        foreach (var proto in _proto.EnumeratePrototypes<PlanetPrototype>())
        {
            if (!proto.SpawnRoundstart)
                continue;

            if (proto.MapPath is { } path)
            {
                LoadPlanetWithMap(proto.ID, path.CanonPath);
            }
            else
                SpawnPlanet(proto.ID);
        }
    }

    private void OnStationsGenerated(StationsGeneratedEvent args)
    {
        if (!_cfg.GetCVar(HorizonCCVars.SpawnPlanets))
            return;

        var vessel = _proto.Index<VesselPrototype>("Cheetah");
        var dummyMapUid = _map.CreateMap(out var dummyMap);

        if (!_mapLoader.TryLoadGrid(dummyMap, vessel.ShuttlePath, out var grid))
            return;

        // Добавляем нужные компоненты
        var taxi = EnsureComp<PlanetTaxiComponent>(grid.Value);
        var shuttle = EnsureComp<ShuttleComponent>(grid.Value);
        EnsureComp<PreventPilotComponent>(grid.Value);

        // Выставляем текст на экранах
        var netComp = EnsureComp<DeviceNetworkComponent>(grid.Value);
        _deviceNetwork.SetTransmitFrequency(grid.Value, 10000, netComp);
        var payload = new NetworkPayload
        {
            [ScreenMasks.Text] = Loc.GetString("planet-taxi-text"),
            [ScreenMasks.LocalGrid] = grid.Value.Owner,
        };
        _deviceNetwork.QueuePacket(grid.Value, null, payload, 10000, device: netComp);

        // Удаляем лишнее без удаления энтити
        var children = Transform(grid.Value.Owner).ChildEnumerator;
        while (children.MoveNext(out var uid))
            RemComp<BusScheduleComponent>(uid);

        var targets = EntityManager.AllEntities<TagComponent>().Where(x => _tag.HasTag(x.Owner, "PlanetTaxiAirlock")).Select(x => Transform(x.Owner).ParentUid);
        if (targets.Count() <= 0)
            return;

        _shuttles.FTLToDock(grid.Value, shuttle, targets.First(), hyperspaceTime: (float)taxi.FTLTime.TotalSeconds, priorityTag: "PlanetTaxiAirlock");
    }

    private void OnDock(Entity<PlanetTaxiComponent> ent, ref FTLCompletedEvent args)
    {
        ent.Comp.NextLaunch = _timing.CurTime + ent.Comp.StopDuration;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PlanetTaxiComponent, ShuttleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var shuttle))
        {
            if (comp.NextLaunch > _timing.CurTime)
                continue;

            var targets = EntityManager.AllEntities<TagComponent>().Where(x => _tag.HasTag(x.Owner, "PlanetTaxiAirlock")).Select(x => Transform(x.Owner).ParentUid);

            if (targets.Count() <= 0)
                continue;

            comp.CurIdx++;
            if (comp.CurIdx >= targets.Count())
                comp.CurIdx = 0;

            comp.NextLaunch = _timing.CurTime + TimeSpan.FromMinutes(30);

            _shuttles.FTLToDock(uid, shuttle, targets.ElementAt(comp.CurIdx), hyperspaceTime: (float)comp.FTLTime.TotalSeconds, priorityTag: "PlanetTaxiAirlock");
        }
    }

    /// <summary>
    /// Создаёт планету из прототипа
    /// </summary>
    public EntityUid SpawnPlanet(ProtoId<PlanetPrototype> id, bool runMapInit = true)
    {
        var planet = _proto.Index(id);

        var map = _map.CreateMap(out _, runMapInit: runMapInit);
        _biome.EnsurePlanet(map, _proto.Index(planet.Biome), mapLight: planet.MapLight);

        // add each marker layer
        var biome = Comp<BiomeComponent>(map);
        foreach (var layer in planet.BiomeMarkerLayers)
        {
            _biome.AddMarkerLayer(map, biome, layer);
        }

        if (planet.AddedComponents is { } added)
            EntityManager.AddComponents(map, added);

        _atmos.SetMapAtmosphere(map, false, planet.Atmosphere);

        _meta.SetEntityName(map, Loc.GetString(planet.MapName));

        LoadedPlanets[id] = map;
        return map;
    }

    /// <summary>
    /// Спавнит планету с загрузкой определённой карты
    /// </summary>
    public EntityUid? LoadPlanetWithMap(ProtoId<PlanetPrototype> id, string path)
    {
        var map = SpawnPlanet(id, runMapInit: false);
        var mapId = Comp<MapComponent>(map).MapId;

        if (!_mapLoader.TryLoadGrid(mapId, new ResPath(path), out var grids))
        {
            Log.Error($"Failed to load planet grid {path} for planet {id}!");
            return null;
        }

        if (grids.HasValue)
        {
            var gridUid = grids.Value;
            _setTiles.Clear();
            var aabb = Comp<MapGridComponent>(gridUid).LocalAABB;
            _biome.ReserveTiles(map, aabb.Enlarged(0.2f), _setTiles);
        }
        else
        {
            Log.Error("Grid not found for this map.");
        }

        _map.InitializeMap(map);

        LoadedPlanets[id] = map;
        return map;
    }
}
