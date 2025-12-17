using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.RemoteControl.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]

public sealed partial class UnderControlComponent : Component
{
    [DataField]
    public EntityUid OriginalBody;

    /// <summary>
    ///     If under control a mech
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HostIsRemotePilot = false;

    [DataField, AutoNetworkedField]
    public EntProtoId ReturnToBodyAction = "ReturnToBodyAction";

    [DataField, AutoNetworkedField]
    public EntityUid? ReturnToBodyActionEntity;
}
