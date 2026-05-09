using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Horizon.Materials;

/// <summary>
/// Machine that processes entities with Log component (wood, mushroom caps) and spawns their output as stacks.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedLumberMillSystem))]
public sealed partial class LumberMillComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Powered;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;

    /// <summary>
    /// Fixed duration to process one item.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ProcessDuration = TimeSpan.FromSeconds(2f);

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
