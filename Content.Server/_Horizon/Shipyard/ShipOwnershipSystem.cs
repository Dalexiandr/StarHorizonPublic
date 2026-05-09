using Content.Server.Mind;
using Content.Shared._Horizon.CCVar;
using Content.Shared._Horizon.Shipyard.Components;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Log;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.Shipyard;

/// <summary>
/// Manages ship ownership and handles cleanup of ships when owners are offline too long
/// </summary>
public sealed class ShipOwnershipSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("shipOwnership");

    private readonly HashSet<EntityUid> _pendingDeletionShips = new();

    // Timer for deletion checks
    private TimeSpan _nextDeletionCheckTime;
    private const int DeletionCheckIntervalSeconds = 60;

    private bool _autoDeleteEnabled;

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to player events to track when they join/leave
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        // Initialize tracking for ships
        SubscribeLocalEvent<ShipOwnershipComponent, ComponentStartup>(OnShipOwnershipStartup);
        SubscribeLocalEvent<ShipOwnershipComponent, ComponentShutdown>(OnShipOwnershipShutdown);

        // Initialize the deletion check timer
        _nextDeletionCheckTime = _gameTiming.CurTime;

        Subs.CVar(_cfg, HorizonCCVars.AutoDeleteEnabled, value => _autoDeleteEnabled = value, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    /// <summary>
    /// Register a ship as being owned by a player
    /// </summary>
    public void RegisterShipOwnership(EntityUid gridUid, ICommonSession owningPlayer)
    {
        // Don't register ownership if the entity isn't valid
        if (!EntityManager.EntityExists(gridUid))
            return;

        // Add ownership component to the ship
        var comp = EnsureComp<ShipOwnershipComponent>(gridUid);
        comp.OwnerUserId = owningPlayer.UserId;
        comp.IsOwnerOnline = true;
        comp.LastStatusChangeTime = _gameTiming.CurTime;

        Dirty(gridUid, comp);

        // Log ship registration
        _sawmill.Info($"Registered ship {ToPrettyString(gridUid)} to player {owningPlayer.Name} ({owningPlayer.UserId})");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_autoDeleteEnabled)
            return;

        // Only check for ship deletion every DeletionCheckIntervalSeconds
        if (_gameTiming.CurTime < _nextDeletionCheckTime)
            return;

        // Update next check time
        _nextDeletionCheckTime = _gameTiming.CurTime + TimeSpan.FromSeconds(DeletionCheckIntervalSeconds);

        // Log that we're checking for ships to delete
        _sawmill.Debug($"Checking for abandoned ships to delete");

        // Check for ships that need to be deleted due to owner absence
        var query = EntityQueryEnumerator<ShipOwnershipComponent>();
        while (query.MoveNext(out var uid, out var ownership))
        {
            // Skip ships with online owners
            if (ownership.IsOwnerOnline)
                continue;

            // Calculate how long the owner has been offline
            var offlineTime = _gameTiming.CurTime - ownership.LastStatusChangeTime;
            var timeoutSeconds = TimeSpan.FromSeconds(ownership.DeletionTimeoutSeconds);

            // Check if we've passed the timeout
            if (offlineTime >= timeoutSeconds)
            {
                // Check if there are any living beings on the ship before deleting
                var mobQuery = GetEntityQuery<MobStateComponent>();
                var xformQuery = GetEntityQuery<TransformComponent>();

                if (HasLivingBeingsOnShip(uid, mobQuery, xformQuery))
                {
                    // Skip deletion if living beings are on the ship
                    _sawmill.Debug($"Skipping deletion of abandoned ship {ToPrettyString(uid)} because there are living beings on it");

                    // Reset the timer to check again later
                    ownership.LastStatusChangeTime = _gameTiming.CurTime;
                    Dirty(uid, ownership);
                    continue;
                }

                // Queue ship for deletion
                _pendingDeletionShips.Add(uid);
            }
        }

        // Process deletions outside of enumeration
        foreach (var shipUid in _pendingDeletionShips)
        {
            if (!EntityManager.EntityExists(shipUid))
                continue;

            // Only handle deletion if this entity has a transform and is a grid
            var transform = Transform(shipUid);
            if (transform.GridUid == shipUid)
            {
                _sawmill.Info($"Deleting abandoned ship {ToPrettyString(shipUid)}");

                // Delete the grid entity
                QueueDel(shipUid);
            }
        }

        _pendingDeletionShips.Clear();
    }

    /// <summary>
    /// Checks if there are any living beings aboard a ship
    /// </summary>
    /// <param name="uid">The ship entity to check</param>
    /// <param name="mobQuery">Query for accessing MobState components</param>
    /// <param name="xformQuery">Query for accessing Transform components</param>
    /// <returns>True if living beings are found, false otherwise</returns>
    private bool HasLivingBeingsOnShip(EntityUid uid, EntityQuery<MobStateComponent> mobQuery, EntityQuery<TransformComponent> xformQuery)
    {
        // Check if a living entity is on this ship
        return FoundOrganics(uid, mobQuery, xformQuery) != null;
    }

    /// <summary>
    /// Looks for a living, sapient being aboard a particular entity.
    /// Only considers online players as obstacles - offline players (former players) don't prevent deletion.
    /// </summary>
    /// <param name="uid">The entity to search (e.g. a shuttle, a station)</param>
    /// <param name="mobQuery">A query to get the MobState from an entity</param>
    /// <param name="xformQuery">A query to get the transform component of an entity</param>
    /// <returns>The name of the sapient being if one was found, null otherwise.</returns>
    private string? FoundOrganics(EntityUid uid, EntityQuery<MobStateComponent> mobQuery, EntityQuery<TransformComponent> xformQuery)
    {
        var xform = xformQuery.GetComponent(uid);
        var childEnumerator = xform.ChildEnumerator;

        while (childEnumerator.MoveNext(out var child))
        {
            // Ghosts don't stop a ship deletion
            if (HasComp<GhostComponent>(child))
                continue;

            // Check if we have a player entity with an active session
            if (_mind.TryGetMind(child, out var mind, out var mindComp))
            {
                // Only consider players with an active session (online) as obstacles
                // Offline players (former players) don't prevent ship deletion
                if (mindComp.UserId != null && _playerManager.TryGetSessionById(mindComp.UserId.Value, out _))
                {
                    // Player is online and character is not dead, so they prevent deletion
                    if (!_mind.IsCharacterDeadPhysically(mindComp))
                    {
                        return Name(child);
                    }
                }
                // If player is offline (no active session), they don't prevent deletion - continue searching
            }

            // Recursively check children
            var charName = FoundOrganics(child, mobQuery, xformQuery);
            if (charName != null)
                return charName;
        }

        return null;
    }

    private void OnShipOwnershipStartup(EntityUid uid, ShipOwnershipComponent component, ComponentStartup args)
    {
        // If player is already online, mark them as such
        if (_playerManager.TryGetSessionById(component.OwnerUserId, out var player))
        {
            component.IsOwnerOnline = true;
            component.LastStatusChangeTime = _gameTiming.CurTime;
            Dirty(uid, component);
        }
    }

    private void OnShipOwnershipShutdown(EntityUid uid, ShipOwnershipComponent component, ComponentShutdown args)
    {
        // Nothing to do here for now
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.Session == null)
            return;

        var userId = e.Session.UserId;
        var query = EntityQueryEnumerator<ShipOwnershipComponent>();

        // Update all ships owned by this player
        while (query.MoveNext(out var shipUid, out var ownership))
        {
            if (ownership.OwnerUserId != userId)
                continue;

            switch (e.NewStatus)
            {
                case SessionStatus.Connected:
                case SessionStatus.InGame:
                    // Player has connected, update ownership
                    ownership.IsOwnerOnline = true;
                    ownership.LastStatusChangeTime = _gameTiming.CurTime;
                    _sawmill.Debug($"Owner of ship {ToPrettyString(shipUid)} has connected");
                    break;

                case SessionStatus.Disconnected:
                    // Player has disconnected, update ownership
                    ownership.IsOwnerOnline = false;
                    ownership.LastStatusChangeTime = _gameTiming.CurTime;
                    _sawmill.Debug($"Owner of ship {ToPrettyString(shipUid)} has disconnected");
                    break;
            }

            Dirty(shipUid, ownership);
        }
    }
}
