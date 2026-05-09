#nullable enable

using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Shuttles.Components;

/// <summary>
/// When this component is on a disk with ShuttleDestinationCoordinatesComponent,
/// the disk will automatically link to a station's map on spawn.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AutoLinkCoordinatesDiskComponent : Component
{
    /// <summary>
    /// The BecomesStation ID to link to (e.g. "SindiOutpost").
    /// </summary>
    [DataField(required: true)]
    public string TargetStationId = string.Empty;
}
