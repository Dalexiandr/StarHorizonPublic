using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Horizon.Materials;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedLumberMillSystem)), AutoGenerateComponentPause]
public sealed partial class ActiveLumberMillComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public Container ProcessingContainer = default!;

    [DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan EndTime;

    [DataField("duration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Duration;
}
