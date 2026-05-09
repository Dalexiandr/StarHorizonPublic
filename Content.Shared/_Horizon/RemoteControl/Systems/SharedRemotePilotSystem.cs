using Content.Shared._Horizon.RemoteControl.Components;
using Content.Shared.Mech;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.DoAfter;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Popups;

namespace Content.Shared._Horizon.RemoteControl.Systems;

public abstract class SharedRemotePilotSystem : EntitySystem
{

    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly RemoteControlSystem _remoteControlSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RemotePilotComponent, OnPilotEjectEvent>(OnPilotEject);
    }

    private void OnPilotEject(Entity<RemotePilotComponent> pilot, ref OnPilotEjectEvent args)
    {
        _remoteControlSystem.ReturnToBody(pilot.Owner);
    }

    public bool TryCreateRemotePilot(EntityUid mech, EntityUid controller, [NotNullWhen(true)] out EntityUid? pilotUid)
    {
        pilotUid = null;

        if (!TryComp<CanBeTakenUnderControlComponent>(mech, out var hostComp))
            return false;

        pilotUid = PredictedSpawnAtPosition(hostComp.RemotePilot, Transform(mech).Coordinates);

        //Don't create a pilot in the mech immediately, because we need to call the MechEntryEvent to initialize the UI update for controlling the mech.
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, pilotUid.Value, 0f, new MechEntryEvent(), mech, target: mech)
        {
            Broadcast = false,
            Hidden = true
        });

        return true;
    }

}
