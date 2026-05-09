// SPDX-FileCopyrightText: 2024 neuPanda
// SPDX-FileCopyrightText: 2025 Ark
// SPDX-FileCopyrightText: 2025 Dvir
// SPDX-FileCopyrightText: 2025 Ilya246
// SPDX-FileCopyrightText: 2025 Redrover1760
// SPDX-FileCopyrightText: 2025 Whatstone
// SPDX-FileCopyrightText: 2025 significant harassment
// SPDX-FileCopyrightText: 2025 starch
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// New Frontiers - This file is licensed under AGPLv3
// Copyright (c) 2024 New Frontiers Contributors
// See AGPLv3.txt for details.
using Content.Server._NF.Station.Components;
using Content.Server.Shuttles.Components;
using Content.Shared._NF.Shuttles.Events;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Physics.Components;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    [Dependency] private readonly RadarConsoleSystem _radarConsole = default!;
    private const float SpaceFrictionStrength = 0.0015f;
    private const float DampenDampingStrength = 0.05f; // FRONTIER MERGE: this should be valuable
    private const float AnchorDampingStrength = 0.5f;
    private void NfInitialize()
    {
        SubscribeLocalEvent<ShuttleConsoleComponent, SetInertiaDampeningRequest>(OnSetInertiaDampening);
        SubscribeLocalEvent<ShuttleConsoleComponent, SetServiceFlagsRequest>(NfSetServiceFlags);
        SubscribeLocalEvent<ShuttleConsoleComponent, SetTargetCoordinatesRequest>(NfSetTargetCoordinates);
        SubscribeLocalEvent<ShuttleConsoleComponent, SetHideTargetRequest>(NfSetHideTarget);
        SubscribeLocalEvent<ShuttleConsoleComponent, SetMaxShuttleSpeedRequest>(OnSetMaxShuttleSpeed);
    }

    private bool SetInertiaDampening(EntityUid uid, PhysicsComponent physicsComponent, ShuttleComponent shuttleComponent, TransformComponent transform, InertiaDampeningMode mode)
    {
        if (!transform.GridUid.HasValue)
        {
            return false;
        }

        if (mode == InertiaDampeningMode.Query)
        {
            _console.RefreshShuttleConsoles(transform.GridUid.Value);
            return false;
        }

        if (!EntityManager.HasComponent<ShuttleDeedComponent>(transform.GridUid) ||
            EntityManager.HasComponent<StationDampeningComponent>(_station.GetOwningStation(transform.GridUid)))
        {
            return false;
        }

        shuttleComponent.BodyModifier = mode switch
        {
            InertiaDampeningMode.Off => SpaceFrictionStrength,
            InertiaDampeningMode.Dampen => DampenDampingStrength,
            InertiaDampeningMode.Anchor => AnchorDampingStrength,
            _ => DampenDampingStrength, // other values: default to some sane behaviour (assume normal dampening)
        };

        if (shuttleComponent.DampingModifier != 0)
            shuttleComponent.DampingModifier = shuttleComponent.BodyModifier;
        _console.RefreshShuttleConsoles(transform.GridUid.Value);
        return true;
    }

    private void OnSetInertiaDampening(EntityUid uid, ShuttleConsoleComponent component, SetInertiaDampeningRequest args)
    {
        // Ensure that the entity requested is a valid shuttle (stations should not be togglable)
        if (!EntityManager.TryGetComponent(uid, out TransformComponent? transform) ||
            !transform.GridUid.HasValue ||
            !EntityManager.TryGetComponent(transform.GridUid, out PhysicsComponent? physicsComponent) ||
            !EntityManager.TryGetComponent(transform.GridUid, out ShuttleComponent? shuttleComponent))
        {
            return;
        }

        if (SetInertiaDampening(uid, physicsComponent, shuttleComponent, transform, args.Mode) && args.Mode != InertiaDampeningMode.Query)
            component.DampeningMode = args.Mode;
    }

    public InertiaDampeningMode NfGetInertiaDampeningMode(EntityUid entity)
    {
        if (!EntityManager.TryGetComponent<TransformComponent>(entity, out var xform))
            return InertiaDampeningMode.Dampen;

        // Not a shuttle, shouldn't be togglable
        if (!EntityManager.HasComponent<ShuttleDeedComponent>(xform.GridUid) ||
            EntityManager.HasComponent<StationDampeningComponent>(_station.GetOwningStation(xform.GridUid)))
            return InertiaDampeningMode.Station;

        if (!EntityManager.TryGetComponent(xform.GridUid, out ShuttleComponent? shuttle))
            return InertiaDampeningMode.Dampen;

        if (shuttle.BodyModifier >= AnchorDampingStrength)
            return InertiaDampeningMode.Anchor;
        else if (shuttle.BodyModifier <= SpaceFrictionStrength)
            return InertiaDampeningMode.Off;
        else
            return InertiaDampeningMode.Dampen;
    }

    private void OnSetMaxShuttleSpeed(EntityUid uid, ShuttleConsoleComponent component, SetMaxShuttleSpeedRequest args)
    {
        // Ensure that the entity requested is a valid shuttle
        if (!EntityManager.TryGetComponent(uid, out TransformComponent? transform) ||
            !transform.GridUid.HasValue ||
            !EntityManager.TryGetComponent(transform.GridUid, out ShuttleComponent? shuttleComponent))
        {
            return;
        }

        // Mono - fix
        var maxSpeed = Math.Max(args.MaxSpeed, 0f);

        // Don't do anything if the value didn't change
        if (Math.Abs(shuttleComponent.SetMaxVelocity - maxSpeed) < 0.01f)
            return;

        // Mono - fix
        shuttleComponent.SetMaxVelocity = maxSpeed;

        // Refresh the shuttle consoles to update the UI
        _console.RefreshShuttleConsoles(transform.GridUid.Value);
    }

    public void NfSetPowered(EntityUid uid, ShuttleConsoleComponent component, bool powered)
    {
        // Ensure that the entity requested is a valid shuttle (stations should not be togglable)
        if (!EntityManager.TryGetComponent(uid, out TransformComponent? transform) ||
            !transform.GridUid.HasValue ||
            !EntityManager.TryGetComponent(transform.GridUid, out PhysicsComponent? physicsComponent) ||
            !EntityManager.TryGetComponent(transform.GridUid, out ShuttleComponent? shuttleComponent))
        {
            return;
        }

        // Update dampening physics without adjusting requested mode.
        if (!powered)
        {
            SetInertiaDampening(uid, physicsComponent, shuttleComponent, transform, InertiaDampeningMode.Anchor);
        }
        else
        {
            // Update our dampening mode if we need to, and if we aren't a station.
            var currentDampening = NfGetInertiaDampeningMode(uid);
            if (currentDampening != component.DampeningMode &&
                currentDampening != InertiaDampeningMode.Station &&
                component.DampeningMode != InertiaDampeningMode.Station)
            {
                SetInertiaDampening(uid, physicsComponent, shuttleComponent, transform, component.DampeningMode);
            }
        }
    }

    /// <summary>
    /// Get the current service flags for this grid.
    /// </summary>
    public ServiceFlags NfGetServiceFlags(EntityUid uid)
    {
        var transform = Transform(uid);
        // Get the grid entity from the console transform
        if (!transform.GridUid.HasValue)
            return ServiceFlags.None;

        var gridUid = transform.GridUid.Value;

        // Set the service flags on the IFFComponent.
        if (!EntityManager.TryGetComponent<IFFComponent>(gridUid, out var iffComponent))
            return ServiceFlags.None;

        return iffComponent.ServiceFlags;
    }

    /// <summary>
    /// Set the service flags for this grid.
    /// </summary>
    public void NfSetServiceFlags(EntityUid uid, ShuttleConsoleComponent component, SetServiceFlagsRequest args)
    {
        var transform = Transform(uid);
        // Get the grid entity from the console transform
        if (!transform.GridUid.HasValue)
            return;

        var gridUid = transform.GridUid.Value;

        // Set the service flags on the IFFComponent.
        if (!EntityManager.TryGetComponent<IFFComponent>(gridUid, out var iffComponent))
            return;

        iffComponent.ServiceFlags = args.ServiceFlags;
        _console.RefreshShuttleConsoles(gridUid);
        Dirty(gridUid, iffComponent);
    }

    public void NfSetTargetCoordinates(EntityUid uid, ShuttleConsoleComponent component, SetTargetCoordinatesRequest args)
    {
        if (!TryComp<RadarConsoleComponent>(uid, out var radarConsole))
            return;

        var transform = Transform(uid);
        // Get the grid entity from the console transform
        if (!transform.GridUid.HasValue)
            return;

        var gridUid = transform.GridUid.Value;

        _radarConsole.SetTarget((uid, radarConsole), args.TrackedEntity, args.TrackedPosition);
        _radarConsole.SetHideTarget((uid, radarConsole), false); // Force target visibility
        _console.RefreshShuttleConsoles(gridUid);
    }

    public void NfSetHideTarget(EntityUid uid, ShuttleConsoleComponent component, SetHideTargetRequest args)
    {
        if (!TryComp<RadarConsoleComponent>(uid, out var radarConsole))
            return;

        var transform = Transform(uid);
        // Get the grid entity from the console transform
        if (!transform.GridUid.HasValue)
            return;

        var gridUid = transform.GridUid.Value;

        _radarConsole.SetHideTarget((uid, radarConsole), args.Hidden);
        _console.RefreshShuttleConsoles(gridUid);
    }
}
