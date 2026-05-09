using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.RemoteControl.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoteControlMonitorComponent : Component
{
    [DataField]
    public EntityUid? HostUid;

    [DataField]
    public EntityUid? ControllerUid;

    [DataField, AutoNetworkedField]
    public bool IsPowered = false;
}
