using Robust.Shared.GameStates;

namespace Content.Shared._Horizon._Fractions.AnCoDisposableFabricator;

/// <summary>
/// Shared component for AnCoDisposableFabricator visuals.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AnCoDisposableFabricatorVisualsComponent : Component
{
    /// <summary>
    /// The sprite state to use when idle.
    /// </summary>
    [DataField]
    public string IdleState = "icon";

    /// <summary>
    /// The sprite state to use when working.
    /// </summary>
    [DataField]
    public string WorkingState = "work";
}
