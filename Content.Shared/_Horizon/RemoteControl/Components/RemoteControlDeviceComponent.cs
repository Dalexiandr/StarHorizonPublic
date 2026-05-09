using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.RemoteControl.Components;

/// <summary>
///     Connects an entity that can be controlled and monitor
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RemoteControlDeviceComponent : Component
{
    [DataField]
    public EntityUid? HostUid;

    [DataField]
    public EntityUid? MonitorUid;

    [DataField]
    public float MakeHostDelay = 5f;
}
