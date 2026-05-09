using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.OutpostCapture.Components;

/// <summary>
/// Используется для регистрации grid, которые можно захватить
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class OutpostCaptureComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string OutpostName = "empty";

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int NeedCaptured = 1;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SpawnCooldown = TimeSpan.FromMinutes(1);

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<OutpostSpawnPrototype> SpawnList = "default";

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<NetEntity> LinkedConsoles = [];

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<NetEntity> CapturedConsoles = [];

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<NetEntity> CapturingConsoles = [];

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string? CapturedFaction;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? NextSpawn;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityCoordinates? SpawnLocation;
}
