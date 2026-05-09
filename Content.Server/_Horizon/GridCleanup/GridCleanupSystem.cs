using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;
using Robust.Shared.Maths;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Server.Player;
using Robust.Shared.Player;
using Content.Server.Salvage.Expeditions;
using Content.Server.Gateway.Components;
using Content.Server.Mind;
using Content.Shared.Ghost;
using Content.Shared.Parallax.Biomes;
using Content.Shared._Horizon.CCVar;

namespace Content.Server._Horizon.GridCleanup;

/// <summary>
/// Автоматически удаляет мелкие гриды (менее 10 тайлов) после задержки в 600 секунд.
/// </summary>
public sealed class GridCleanupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("gridcleanup");

    // The minimum number of tiles a grid needs to avoid being cleaned up
    private const int MinimumTiles = 10;

    // The delay before cleaning up a small grid (in seconds)
    private const float CleanupDelay = 600f;

    // Interval for rechecking grids that were skipped due to players (in seconds)
    private const float RecheckInterval = 60f;

    // Dictionary to track grids scheduled for deletion
    private readonly Dictionary<EntityUid, TimeSpan> _pendingCleanup = new();

    // Set of grids that were skipped due to players - need to be rechecked periodically
    private readonly HashSet<EntityUid> _skippedDueToPlayers = new();

    // Last time we rechecked skipped grids
    private TimeSpan _lastRecheckTime = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to grid events
        SubscribeLocalEvent<GridStartupEvent>(OnGridStartup);
        SubscribeLocalEvent<MapGridComponent, TileChangedEvent>(OnTileChanged);
        SubscribeLocalEvent<SalvageExpeditionComponent, ComponentStartup>(OnExpeditionStartup);
    }

    private bool IsCleanupEnabled()
    {
        return _cfg.GetCVar(HorizonCCVars.AutoGridCleanupEnabled);
    }

    private void OnGridStartup(GridStartupEvent ev)
    {
        // Check newly created grids
        if (TryComp<MapGridComponent>(ev.EntityUid, out var grid))
            CheckGrid((ev.EntityUid, grid));
    }

    private void OnTileChanged(Entity<MapGridComponent> ent, ref TileChangedEvent args)
    {
        // When a grid is modified (tiles added/removed), check if it needs cleanup
        CheckGrid(ent);
    }

    private void OnExpeditionStartup(EntityUid uid, SalvageExpeditionComponent component, ComponentStartup args)
    {
        // Make sure any grid that gets the expedition component is removed from cleanup
        if (_pendingCleanup.ContainsKey(uid))
        {
            _sawmill.Debug($"Expedition startup: Removing grid {uid} from cleanup queue");
            _pendingCleanup.Remove(uid);
        }

        // Check if this entity also has a grid component and ensure it's not marked for cleanup
        if (TryComp<MapGridComponent>(uid, out var grid))
        {
            // Make sure we don't clean up very small expedition grids
            var tileCount = CountTiles((uid, grid));
            _sawmill.Debug($"Expedition grid {uid} has {tileCount} tiles");
        }
    }

    private void CheckGrid(Entity<MapGridComponent> ent)
    {
        if (!IsCleanupEnabled())
            return;

        var gridUid = ent.Owner;
        var grid = ent.Comp;

        // Skip gateway destination grids
        if (HasComp<GatewayGeneratorDestinationComponent>(gridUid))
            return;

        // Skip if already scheduled for deletion
        if (_pendingCleanup.ContainsKey(gridUid))
            return;

        // Skip if this is a planet expedition grid
        if (HasComp<SalvageExpeditionComponent>(gridUid))
        {
            //Logger.DebugS("salvage", $"CheckGrid: Skipping grid {gridUid} with SalvageExpeditionComponent");
            return;
        }

        // Skip if the parent map has a SalvageExpeditionComponent
        var transform = Transform(gridUid);
        var mapId = transform.MapID;
        var mapUid = _mapSystem.GetMapOrInvalid(mapId);

        if (mapUid != EntityUid.Invalid)
        {
            // Skip if map has SalvageExpeditionComponent
            if (HasComp<SalvageExpeditionComponent>(mapUid))
            {
                //_sawmill.Debug($"CheckGrid: Skipping grid {gridUid} on expedition map {mapUid}");
                return;
            }

            // Skip if map has BiomeComponent - biomes generate tiles dynamically
            // and LocalAABB may be inaccurate until tiles are generated
            if (HasComp<BiomeComponent>(mapUid))
            {
                _sawmill.Debug($"CheckGrid: Skipping grid {gridUid} on biome map {mapUid} - tiles generated dynamically");
                return;
            }
        }

        // Check if there are players on the grid
        if (HasPlayersOnGrid(gridUid))
        {
            _sawmill.Debug($"CheckGrid: Skipping grid {gridUid} - players present");
            // Add to recheck list so we can check again when players leave
            _skippedDueToPlayers.Add(gridUid);
            return;
        }

        // Remove from recheck list if it was there (players left)
        _skippedDueToPlayers.Remove(gridUid);

        // Count tiles
        var tileCount = CountTiles((gridUid, grid));

        // If the tile count is below our threshold, schedule it for deletion
        if (tileCount < MinimumTiles)
        {
            _sawmill.Debug($"CheckGrid: Scheduling grid {gridUid} for cleanup with {tileCount} tiles");
            ScheduleGridCleanup(gridUid);
        }
    }

    private void ScheduleGridCleanup(EntityUid gridUid)
    {
        // Skip if already scheduled
        if (_pendingCleanup.ContainsKey(gridUid))
            return;

        // Remove from recheck list if it was there
        _skippedDueToPlayers.Remove(gridUid);

        var targetTime = _timing.CurTime + TimeSpan.FromSeconds(CleanupDelay);
        _pendingCleanup[gridUid] = targetTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!IsCleanupEnabled())
            return;

        var currentTime = _timing.CurTime;

        // Periodically recheck grids that were skipped due to players
        if (currentTime - _lastRecheckTime >= TimeSpan.FromSeconds(RecheckInterval))
        {
            _lastRecheckTime = currentTime;
            RecheckSkippedGrids();
        }

        if (_pendingCleanup.Count == 0)
            return;

        // Check if any grids need to be cleaned up
        var toRemove = new List<EntityUid>();

        foreach (var (gridUid, targetTime) in _pendingCleanup)
        {
            // Skip gateway destination grids
            if (HasComp<GatewayGeneratorDestinationComponent>(gridUid))
            {
                toRemove.Add(gridUid);
                continue;
            }

            // Skip if the time hasn't elapsed yet
            if (currentTime < targetTime)
                continue;

            // Check if the entity still exists
            if (!EntityManager.EntityExists(gridUid))
            {
                toRemove.Add(gridUid);
                continue;
            }

            // Skip if this is a planet expedition grid
            if (HasComp<SalvageExpeditionComponent>(gridUid))
            {
                _sawmill.Debug($"Update: Removing expedition grid {gridUid} from cleanup queue");
                toRemove.Add(gridUid);
                continue;
            }

            // Skip if the parent map has an expedition component or biome component
            var xform = Transform(gridUid);
            var mapId = xform.MapID;
            var mapUid = _mapSystem.GetMapOrInvalid(mapId);

            if (mapUid != EntityUid.Invalid)
            {
                if (HasComp<SalvageExpeditionComponent>(mapUid))
                {
                    _sawmill.Debug($"Update: Removing grid {gridUid} on expedition map {mapUid} from cleanup queue");
                    toRemove.Add(gridUid);
                    continue;
                }

                // Skip if map has BiomeComponent - biomes generate tiles dynamically
                if (HasComp<BiomeComponent>(mapUid))
                {
                    _sawmill.Debug($"Update: Removing grid {gridUid} on biome map {mapUid} from cleanup queue - tiles generated dynamically");
                    toRemove.Add(gridUid);
                    continue;
                }
            }

            // Verify it still has a grid component
            if (!TryComp<MapGridComponent>(gridUid, out var grid))
            {
                toRemove.Add(gridUid);
                continue;
            }

            // Check if there are players on the grid before deletion
            if (HasPlayersOnGrid(gridUid))
            {
                _sawmill.Debug($"Update: Removing grid {gridUid} from cleanup queue - players present");
                // Add to recheck list so we can check again when players leave
                _skippedDueToPlayers.Add(gridUid);
                toRemove.Add(gridUid);
                continue;
            }

            // Check tile count again to make sure it still needs to be deleted
            var tileCount = CountTiles((gridUid, grid));
            if (tileCount >= MinimumTiles)
            {
                toRemove.Add(gridUid);
                continue;
            }

            // Queue the grid for deletion
            QueueDel(gridUid);
            _sawmill.Debug($"Update: Queuing grid {gridUid} for deletion with {CountTiles((gridUid, grid))} tiles");
            toRemove.Add(gridUid);
        }

        // Remove processed grids from the pending list
        foreach (var gridUid in toRemove)
        {
            _pendingCleanup.Remove(gridUid);
        }
    }

    private int CountTiles(Entity<MapGridComponent> ent)
    {
        var gridUid = ent.Owner;
        var grid = ent.Comp;
        var tileCount = 0;

        // Get AABB of the grid
        var aabb = grid.LocalAABB;

        // Convert to grid coordinates
        var localTL = new Vector2i((int) Math.Floor(aabb.Left), (int) Math.Floor(aabb.Bottom));
        var localBR = new Vector2i((int) Math.Ceiling(aabb.Right), (int) Math.Ceiling(aabb.Top));

        // Iterate through all tiles in the grid's area
        for (var x = localTL.X; x < localBR.X; x++)
        {
            for (var y = localTL.Y; y < localBR.Y; y++)
            {
                var position = new Vector2i(x, y);

                // Check if tile exists at position and is not empty
                var tile = _mapSystem.GetTileRef(gridUid, grid, position);
                if (!tile.Tile.IsEmpty)
                {
                    tileCount++;

                    // Early return if we've found enough tiles
                    if (tileCount >= MinimumTiles)
                        return tileCount;
                }
            }
        }

        return tileCount;
    }

    /// <summary>
    /// Checks if there are any players on the grid or its children.
    /// </summary>
    private bool HasPlayersOnGrid(EntityUid uid)
    {
        var xform = Transform(uid);
        var childEnumerator = xform.ChildEnumerator;

        while (childEnumerator.MoveNext(out var child))
        {
            // Ghosts don't prevent grid cleanup
            if (HasComp<GhostComponent>(child))
                continue;

            // Check if we have a player entity that's either still around or alive and may come back
            if (_mind.TryGetMind(child, out _, out var mindComp)
                && (mindComp.UserId != null && _player.ValidSessionId(mindComp.UserId.Value)
                || !_mind.IsCharacterDeadPhysically(mindComp)))
            {
                return true;
            }

            // Recursively check children
            if (HasPlayersOnGrid(child))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Rechecks grids that were previously skipped due to players.
    /// If players have left and grid is still small, adds it back to cleanup queue.
    /// </summary>
    private void RecheckSkippedGrids()
    {
        if (_skippedDueToPlayers.Count == 0)
            return;

        var toRemove = new List<EntityUid>();

        foreach (var gridUid in _skippedDueToPlayers)
        {
            // Check if grid still exists
            if (!EntityManager.EntityExists(gridUid))
            {
                toRemove.Add(gridUid);
                continue;
            }

            // Skip if already in cleanup queue
            if (_pendingCleanup.ContainsKey(gridUid))
            {
                toRemove.Add(gridUid);
                continue;
            }

            // Skip gateway destination grids
            if (HasComp<GatewayGeneratorDestinationComponent>(gridUid))
            {
                toRemove.Add(gridUid);
                continue;
            }

            // Skip if this is a planet expedition grid
            if (HasComp<SalvageExpeditionComponent>(gridUid))
            {
                toRemove.Add(gridUid);
                continue;
            }

            // Skip if the parent map has a SalvageExpeditionComponent or BiomeComponent
            var transform = Transform(gridUid);
            var mapId = transform.MapID;
            var mapUid = _mapSystem.GetMapOrInvalid(mapId);

            if (mapUid != EntityUid.Invalid)
            {
                if (HasComp<SalvageExpeditionComponent>(mapUid))
                {
                    toRemove.Add(gridUid);
                    continue;
                }

                // Skip if map has BiomeComponent - biomes generate tiles dynamically
                if (HasComp<BiomeComponent>(mapUid))
                {
                    toRemove.Add(gridUid);
                    continue;
                }
            }

            // Check if players are still present
            if (HasPlayersOnGrid(gridUid))
            {
                // Players still present, keep in recheck list
                continue;
            }

            // Players left, check if grid is still small
            if (!TryComp<MapGridComponent>(gridUid, out var grid))
            {
                toRemove.Add(gridUid);
                continue;
            }

            var tileCount = CountTiles((gridUid, grid));
            if (tileCount < MinimumTiles)
            {
                // Grid is still small and players left, schedule for cleanup
                _sawmill.Debug($"RecheckSkippedGrids: Re-adding grid {gridUid} to cleanup queue with {tileCount} tiles (players left)");
                ScheduleGridCleanup(gridUid);
            }

            // Remove from recheck list (either scheduled or no longer small)
            toRemove.Add(gridUid);
        }

        // Clean up removed grids
        foreach (var gridUid in toRemove)
        {
            _skippedDueToPlayers.Remove(gridUid);
        }
    }
}
