using Content.Shared._Horizon.RemoteControl.Components;
using Content.Shared._Horizon.RemoteControl;
using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Content.Shared.Popups;

namespace Content.Shared._Horizon.RemoteControl.Systems;
public sealed class RemoteControlDeviceSystem : EntitySystem
{

    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] protected readonly SharedAppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RemoteControlDeviceComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<RemoteControlDeviceComponent, MakeConnectWithHostDoAfterEvent>(OnMakeConnectWithHostDoAfter);
    }

    private void OnAfterInteract(Entity<RemoteControlDeviceComponent> device, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (HasComp<CanBeTakenUnderControlComponent>(target))
        {
            MakeConnectWithHost(device, args);
            return;
        }
        if (TryComp<RemoteControlMonitorComponent>(target, out var controlMonitorComp) && controlMonitorComp!= null)
        {
            MakeConnectWithMonitor(device, args, controlMonitorComp);
            return;
        }

        _popupSystem.PopupClient(Loc.GetString("remote-control-device-its-not-host"), args.User);
    }

    private void MakeConnectWithHost(Entity<RemoteControlDeviceComponent> device, AfterInteractEvent args)
    {
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, device.Comp.MakeHostDelay, new MakeConnectWithHostDoAfterEvent(), device.Owner, target: args.Target!, used: device.Owner)
        {
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void MakeConnectWithMonitor(Entity<RemoteControlDeviceComponent> device, AfterInteractEvent args, RemoteControlMonitorComponent controlMonitorComp)
    {
        if (device.Comp.HostUid == null)
        {
            _popupSystem.PopupClient(Loc.GetString("remote-control-device-no-host"), args.User);
        }

        controlMonitorComp.HostUid = device.Comp.HostUid;

        _popupSystem.PopupClient(Loc.GetString("remote-control-success-set-host-with-monitor"), args.User);
    }

    private void OnMakeConnectWithHostDoAfter(Entity<RemoteControlDeviceComponent> device, ref MakeConnectWithHostDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        _appearance.SetData(device.Owner, RemoteControlDeviceVisualStates.IsActive, true);
        device.Comp.HostUid = args.Target;

        args.Handled = true;
    }
}
