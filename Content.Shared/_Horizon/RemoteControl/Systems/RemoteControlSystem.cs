using Content.Shared.Mech.Components;
using Content.Shared._Horizon.RemoteControl.Components;
using Content.Shared.Mind;
using Content.Shared.DoAfter;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Shared._Horizon.RemoteControl.Systems;

public sealed class RemoteControlSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedRemotePilotSystem _remotePilotSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UnderControlComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<UnderControlComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<UnderControlComponent, ReturnToBodyActionEvent>(OnActivateReturnToBodyAction);
        SubscribeLocalEvent<UnderControlComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnComponentInit(Entity<UnderControlComponent> ent, ref ComponentInit args)
    {
        _actionsSystem.AddAction(ent.Owner, ref ent.Comp.ReturnToBodyActionEntity, ent.Comp.ReturnToBodyAction);
    }

    private void OnComponentRemove(Entity<UnderControlComponent> ent, ref ComponentRemove args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.ReturnToBodyActionEntity);
    }

    public void TakeControl(EntityUid? host, EntityUid? controller) //TODO если код позволит, вынести в ентити
    {
        if (host is not { } hostUid || controller is not { } controllerUid)
            return;

        // If Host is mech we have different logic
        // I didn't want to replicate the systems, although I admit that the code would have been cleaner
        if (TryComp<MechComponent>(hostUid, out var mechComp))
        {
            if (mechComp.Broken)
                return;

            if (mechComp.PilotSlot.ContainedEntity != null)
            {
                _popupSystem.PopupClient(Loc.GetString("remote-control-already-under-control"), controller);
                return;
            }

            if (!_remotePilotSystem.TryCreateRemotePilot(hostUid, controllerUid, out var pilotUid))
                return;

            TransferMindInHost(pilotUid.Value, controllerUid, true);
            return;
        }

        TransferMindInHost(hostUid, controllerUid, false);
    }

    private void TransferMindInHost(EntityUid host, EntityUid controller, bool hostIsRemotePilot)
    {
        if (HasComp<UnderControlComponent>(controller))
        {
            _popupSystem.PopupClient(Loc.GetString("remote-control-already-under-control"), controller);
            return;
        }

        if (_mindSystem.TryGetMind(host, out _, out _))
            return;

        if(TryComp<MobThresholdsComponent>(host, out var mobThresholdsComp) &&
            (mobThresholdsComp.CurrentThresholdState == MobState.Critical ||
            mobThresholdsComp.CurrentThresholdState == MobState.Dead))
        {
            _popupSystem.PopupClient(Loc.GetString("remote-control-host-unconscious"), controller);
            return;
        }

        EnsureComp<UnderControlComponent>(host, out var underControlComp);
        underControlComp.OriginalBody = controller;
        underControlComp.HostIsRemotePilot = hostIsRemotePilot;

        if (_mindSystem.TryGetMind(controller, out var mindId, out var mind))
        {
            _mindSystem.TransferTo(mindId, host, true, false, mind: mind);
        }
    }
    private void OnActivateReturnToBodyAction(Entity<UnderControlComponent> ent, ref ReturnToBodyActionEvent args)
    {
        ReturnToBody(ent);
    }

    private void OnMobStateChanged(Entity<UnderControlComponent> ent, ref MobStateChangedEvent args)
    {
        if(args.NewMobState == MobState.Critical || args.NewMobState == MobState.Dead)
            ReturnToBody(ent);
    }

    public void ReturnToBody(EntityUid controller)
    {
        if (!TryComp<UnderControlComponent>(controller, out var underControlComp))
            return;

        // If someone took over the original body
        if (_mindSystem.TryGetMind(underControlComp.OriginalBody, out _, out _))
            return;

        if (_mindSystem.TryGetMind(controller, out var mindId, out var mind))
        {
            _mindSystem.TransferTo(mindId, underControlComp.OriginalBody, true, false, mind: mind);

            RemComp<UnderControlComponent>(controller);

            if (TryComp<MechPilotComponent>(controller, out var mechPilotComp) && underControlComp.HostIsRemotePilot)
            {
                // If you just delete it, the mech won't update its appearance, so it will remain closed
                _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, controller, 0f, new MechExitEvent(), mechPilotComp.Mech, target: mechPilotComp.Mech)
                {
                    Broadcast = false,
                    Hidden = true
                });

                PredictedDel(controller);
            }
        }
    }
}
