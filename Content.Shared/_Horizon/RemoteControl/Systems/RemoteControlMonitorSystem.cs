using Content.Shared.Interaction;
using Content.Shared._Horizon.RemoteControl.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power;

namespace Content.Shared._Horizon.RemoteControl.Systems;

public sealed class RemoteControlMonitorSystem : EntitySystem
{

    [Dependency] private readonly RemoteControlSystem _remoteControlSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RemoteControlMonitorComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<RemoteControlMonitorComponent, PowerChangedEvent>(OnPowerAppearance);
    }

    private void OnActivate(Entity<RemoteControlMonitorComponent> monitor, ref ActivateInWorldEvent args)
    {
        if (monitor.Comp.ControllerUid != null)
            return;

        if (!monitor.Comp.IsPowered)
            return;

        _remoteControlSystem.TakeControl(monitor.Comp.HostUid, args.User);
    }

    private void OnPowerAppearance(Entity<RemoteControlMonitorComponent> monitor, ref PowerChangedEvent args)
    {
        monitor.Comp.IsPowered = args.Powered;
    }
}
