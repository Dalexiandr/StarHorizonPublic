using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.OutpostCapture.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class OutpostConsoleComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanUseAsSpawnPoint;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CaptureTime = TimeSpan.FromSeconds(30);

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string ContainerSlot = "id-card-slot";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public ItemSlot IdCardSlot = new();

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public OutpostConsoleState State = 0;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public NetEntity? LinkedOutpost;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string? CapturedFaction;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string? CapturedFactionName;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? CapturingTime;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public int ProgressBar { get; set; }
}

[Serializable, NetSerializable]
public enum OutpostConsoleState : byte
{
    Uncaptured,
    Capturing,
    Captured,
}
