using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Cytology.Components;

/// <summary>
/// Component that defines parameter modifications applied to entities spawned from cell samples
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CytologyDiskComponent : Component
{
    /// <summary>
    /// List of parameter modifications this disk applies
    /// </summary>
    [DataField(required: true)]
    public List<CytologyDiskModifier> Modifiers = new();
}

/// <summary>
/// Defines a single parameter modification
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class CytologyDiskModifier
{
    /// <summary>
    /// Name of the component type to modify (e.g., "MovementSpeedModifier", "MeleeWeapon", "HungerComponent")
    /// </summary>
    [DataField(required: true)]
    public string ComponentType = string.Empty;

    /// <summary>
    /// Percent modifier to apply (multiplier, e.g., 1.3 = +30%, 0.5 = -50%)
    /// </summary>
    [DataField]
    public float? PercentModifier;
}

