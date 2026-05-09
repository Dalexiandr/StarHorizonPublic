using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.RemoteControl.Components;

/// <summary>
///     Can a creature be taken under control
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CanBeTakenUnderControlComponent : Component
{
    [DataField]
    public EntityUid? MonitorUid;

    /// <summary>
    ///     If a mech is controlled, a remote pilot is created
    /// </summary>
    [DataField]
    public ProtoId<EntityPrototype>? RemotePilot;
}
