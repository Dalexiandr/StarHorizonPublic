using Content.Server.DoAfter;
using Content.Shared._White.Actions.Events;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared._White.Actions;

namespace Content.Server._White.Actions;

public sealed class ActionsSystem : EntitySystem
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("TEST.actions");
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;

    [Dependency] private readonly AnchorableSystem _anchorable = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PlasmaCostActionSystem _plasmaCost = default!; // Goobstation=

    public override void Initialize()
    {
        _sawmill.Debug("ActionsSystem initialized");
        SubscribeLocalEvent<SpawnTileEntityActionEvent>(OnSpawnTileEntityAction);
        SubscribeLocalEvent<PlaceTileEntityEvent>(OnPlaceTileEntityEvent);

        SubscribeLocalEvent<PlaceTileEntityDoAfterEvent>(OnPlaceTileEntityDoAfter);
    }

    private void OnSpawnTileEntityAction(SpawnTileEntityActionEvent args)
    {
        _sawmill.Debug($"OnSpawnTileEntityAction: performer={args.Performer}, tileId={args.TileId}, entity={args.Entity}");
        if (!args.Handled && CreationTileEntity(args.Performer, args.Performer.ToCoordinates(), args.TileId, args.Entity, args.Audio, args.BlockedCollisionLayer, args.BlockedCollisionMask))
        {
            args.Handled = true;
            _sawmill.Debug($"OnSpawnTileEntityAction: handled successfully");
        }
    }

    private void OnPlaceTileEntityEvent(PlaceTileEntityEvent args)
    {
        _sawmill.Debug($"OnPlaceTileEntityEvent: performer={args.Performer}, target={args.Target}, entity={args.Entity}, tileId={args.TileId}, length={args.Length}");
        if (args.Handled)
            return;

        // Check if this is a plasma-cost action and get the cost
        // Goobstation
        TryComp<PlasmaCostActionComponent>(args.Action, out var plasmaCost);
        var plasmaCostValue = plasmaCost?.PlasmaCost ?? FixedPoint2.Zero;
        _sawmill.Debug($"OnPlaceTileEntityEvent: plasmaCost={plasmaCostValue}");

        if (args.Length != 0)
        {
            if (CheckTileBlocked(args.Target, args.BlockedCollisionLayer, args.BlockedCollisionMask))
                return;

            var ev = new PlaceTileEntityDoAfterEvent
            {
                Target = GetNetCoordinates(args.Target),
                Entity = args.Entity,
                TileId = args.TileId,
                Audio = args.Audio,
                BlockedCollisionLayer = args.BlockedCollisionLayer,
                BlockedCollisionMask = args.BlockedCollisionMask, // Goobstation start
                PlasmaCost = plasmaCostValue,
                Action = GetNetEntity(args.Action) // Goobstation end
            };

            var doAfter = new DoAfterArgs(EntityManager, args.Performer, args.Length, ev, null)
            {
                BlockDuplicate = true,
                BreakOnDamage = true,
                BreakOnMove = true, // Goobstation start
                NeedHand = false,
                CancelDuplicate = true, // Gooobstation end
                Broadcast = true
            };

            _doAfter.TryStartDoAfter(doAfter);
            _sawmill.Debug($"OnPlaceTileEntityEvent: doAfter started");
            return;
        }

        if (CreationTileEntity(args.Performer, args.Target, args.TileId, args.Entity, args.Audio, args.BlockedCollisionLayer, args.BlockedCollisionMask))
        {
            args.Handled = true;
            _sawmill.Debug($"OnPlaceTileEntityEvent: handled successfully");
        }
    }

    /// Goobstation
    /// <summary>
    /// Handles the placement of a tile entity after the placement action is confirmed.
    /// Verifies plasma cost and creates the tile if conditions are met.
    /// </summary>
    /// <param name="args">Event data containing placement details and cost</param>
    private void OnPlaceTileEntityDoAfter(PlaceTileEntityDoAfterEvent args)
    {
        _sawmill.Debug($"OnPlaceTileEntityDoAfter: user={args.User}, target={args.Target}, entity={args.Entity}, plasmaCost={args.PlasmaCost}, cancelled={args.Cancelled}, handled={args.Handled}");
        if (args.Cancelled || args.Handled)
            return;

        // Check plasma cost only when the action is about to complete
        if (!_plasmaCost.HasEnoughPlasma(args.User, args.PlasmaCost))
        {
            _sawmill.Debug($"OnPlaceTileEntityDoAfter: not enough plasma");
            return;
        }

        _plasmaCost.DeductPlasma(args.User, args.PlasmaCost);
        _sawmill.Debug($"OnPlaceTileEntityDoAfter: plasma deducted");

        if (CreationTileEntity(args.User, GetCoordinates(args.Target), args.TileId, args.Entity, args.Audio, args.BlockedCollisionLayer, args.BlockedCollisionMask))
        {
            args.Handled = true;
            _sawmill.Debug($"OnPlaceTileEntityDoAfter: handled successfully");
        }
    }

    #region Helpers

    private bool CreationTileEntity(EntityUid user, EntityCoordinates coordinates, string? tileId, EntProtoId? entProtoId, SoundSpecifier? audio, int collisionLayer = 0, int collisionMask = 0)
    {
        _sawmill.Debug($"CreationTileEntity: user={user}, coordinates={coordinates}, tileId={tileId}, entProtoId={entProtoId}");
        if (_container.IsEntityOrParentInContainer(user))
        {
            _sawmill.Debug($"CreationTileEntity: user in container, returning false");
            return false;
        }

        if (tileId != null)
        {
            if (_transform.GetGrid(coordinates) is not { } grid || !TryComp(grid, out MapGridComponent? mapGrid))
                return false;

            var tileDef = _tileDef[tileId];
            var tile = new Tile(tileDef.TileId);

            _mapSystem.SetTile(grid, mapGrid, coordinates, tile);
        }

        _audio.PlayPvs(audio, coordinates);

        if (entProtoId == null || CheckTileBlocked(coordinates, collisionLayer, collisionMask))
        {
            _sawmill.Debug($"CreationTileEntity: tile blocked or no entity, returning false");
            return false;
        }

        var spawned = Spawn(entProtoId, coordinates);
        _sawmill.Debug($"CreationTileEntity: spawned entity={spawned}");

        return true;
    }

    private bool CheckTileBlocked(EntityCoordinates coordinates, int collisionLayer = 0, int collisionMask = 0)
    {
        _sawmill.Debug($"CheckTileBlocked: coordinates={coordinates}, collisionLayer={collisionLayer}, collisionMask={collisionMask}");
        if (_transform.GetGrid(coordinates) is not { } grid || !TryComp(grid, out MapGridComponent? mapGrid))
        {
            _sawmill.Debug($"CheckTileBlocked: no grid found, returning true (blocked)");
            return true;
        }

        var tileIndices = _mapSystem.TileIndicesFor(grid, mapGrid, coordinates);
        var isFree = _anchorable.TileFree(mapGrid, tileIndices, collisionLayer, collisionMask);
        _sawmill.Debug($"CheckTileBlocked: tileFree={isFree}");
        return !isFree;
    }

    #endregion
}
