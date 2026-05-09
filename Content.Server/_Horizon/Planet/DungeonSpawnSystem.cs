using System.Linq;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Procedural;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Physics;
using Content.Shared.Procedural;
using Content.Shared.Random;
using Content.Shared.Salvage.Expeditions;
using Robust.Server.GameObjects;
using Robust.Shared.Collections;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._Horizon.Planet;

public sealed class DungeonSpawnSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AnchorableSystem _anchorable = default!;
    [Dependency] private readonly RandomSystem _randomSys = default!;
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly MapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DungeonSpawnComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<DungeonSpawnComponent> ent, ref MapInitEvent args)
    {
        var xform = Transform(ent.Owner);

        if (xform.GridUid is not { Valid: true } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        var dungeon = _proto.Index(_random.Pick(ent.Comp.Dungeons));
        var faction = _proto.Index(_random.Pick(ent.Comp.MobFactions));

        Generate(dungeon, faction, grid, gridComp, xform.Coordinates.Position.Floored(), ent.Comp.MobBudget);
    }

    private async void Generate(DungeonConfigPrototype dungeonProto, SalvageFactionPrototype faction, EntityUid grid, MapGridComponent gridComp, Vector2i position, int budget)
    {
        var dungeons = await _dungeon.GenerateDungeonAsync(dungeonProto, dungeonProto.ID, grid, gridComp, position, _random.Next());

        if (dungeons.Count <= 0 || dungeons.First() is not { } dungeon || dungeon.Rooms.Count <= 0)
            return;

        var budgetEntries = new List<IBudgetEntry>();
        foreach (var entry in faction.MobGroups)
            budgetEntries.Add(entry);

        float mobBudget = budget;
        var probSum = budgetEntries.Sum(x => x.Prob);
        var random = new Random(_random.Next());

        while (mobBudget > 0f)
        {
            var entry = _randomSys.GetBudgetEntry(ref mobBudget, ref probSum, budgetEntries, random);
            if (entry == null)
                break;

            SpawnEntry((grid, gridComp), entry, dungeon, random);
        }

    }

    private void SpawnEntry(Entity<MapGridComponent> grid, IBudgetEntry entry, Dungeon dungeon, Random random)
    {
        var availableRooms = new ValueList<DungeonRoom>(dungeon.Rooms);
        var availableTiles = new List<Vector2i>();

        while (availableRooms.Count > 0)
        {
            availableTiles.Clear();
            var roomIndex = random.Next(availableRooms.Count);
            var room = availableRooms.RemoveSwap(roomIndex);
            availableTiles.AddRange(room.Tiles);

            while (availableTiles.Count > 0)
            {
                var tile = availableTiles.RemoveSwap(random.Next(availableTiles.Count));

                if (!_anchorable.TileFree(grid, tile, (int)CollisionGroup.MachineLayer,
                        (int)CollisionGroup.MachineLayer))
                {
                    continue;
                }

                var uid = SpawnAtPosition(entry.Proto, _map.GridTileToLocal(grid, grid, tile));
                RemComp<GhostRoleComponent>(uid);
                RemComp<GhostTakeoverAvailableComponent>(uid);
                return;
            }
        }
    }
}
