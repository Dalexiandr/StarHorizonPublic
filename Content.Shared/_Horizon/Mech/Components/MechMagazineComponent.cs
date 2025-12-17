using Robust.Shared.GameStates;

namespace Content.Server._Horizon.Mech.Equipment.Components;

/// <summary>
/// Магазин для оружия меха.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MechMagazineComponent : Component
{
    /// <summary>
    /// The change in energy after each drill.
    /// </summary>
    [DataField("magazinetype", required: true)]
    public string MagazineType;
}

