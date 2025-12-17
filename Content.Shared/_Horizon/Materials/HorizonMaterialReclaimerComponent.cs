using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Horizon.Materials;

/// <summary>
/// Перенесённая из прошлого (16a0957a50d4ae1560235caeba03938dcbc8aa4c) старый компонент утилизации отходов
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedHorizonMaterialReclaimerSystem))]
public sealed partial class HorizonMaterialReclaimerComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Powered;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Efficiency = 1f;

    [DataField]
    public bool ScaleProcessSpeed = true;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float MaterialProcessRate = 100f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MinimumProcessDuration = TimeSpan.FromSeconds(0.5f);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string SolutionContainerId = "output";

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public bool CutOffSound = true;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextSound;

    [DataField]
    public TimeSpan SoundCooldown = TimeSpan.FromSeconds(0.8f);

    public EntityUid? Stream;

    [DataField, AutoNetworkedField]
    public int ItemsProcessed;
}
