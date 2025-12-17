using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Horizon.Materials;

/// <summary>
/// Перенесённая из прошлого (16a0957a50d4ae1560235caeba03938dcbc8aa4c) старый компонент утилизации отходов
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedHorizonMaterialReclaimerSystem)), AutoGenerateComponentPause]
public sealed partial class ActiveHorizonMaterialReclaimerComponent : Component
{
    /// <summary>
    /// Container used to store the item currently being reclaimed
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Container ReclaimingContainer = default!;

    /// <summary>
    /// When the reclaiming process ends.
    /// </summary>
    [DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// The length of the reclaiming process.
    /// Used for calculations.
    /// </summary>
    [DataField("duration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Duration;
}
