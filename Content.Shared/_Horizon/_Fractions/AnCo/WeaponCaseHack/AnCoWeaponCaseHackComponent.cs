using Robust.Shared.Serialization;

namespace Content.Shared._Horizon._Fractions.AnCo.WeaponCaseHack;

/// <summary>
/// Component for AnCo weapon cases that can be hacked via wires.
/// The correct wire is determined by the serial number.
/// Uses LockComponent for actual locking.
/// </summary>
[RegisterComponent]
public sealed partial class AnCoWeaponCaseHackComponent : Component
{
    /// <summary>
    /// Whether the correct wire has been cut.
    /// </summary>
    [DataField]
    public bool CutCompleted;

    /// <summary>
    /// Whether the correct wire has been pulsed.
    /// </summary>
    [DataField]
    public bool PulseCompleted;

    /// <summary>
    /// Explosion intensity (total energy).
    /// </summary>
    [DataField]
    public float ExplosionIntensity = 5f;

    /// <summary>
    /// Explosion slope (how fast intensity drops off).
    /// </summary>
    [DataField]
    public float ExplosionSlope = 1f;

    /// <summary>
    /// Maximum explosion tile intensity.
    /// </summary>
    [DataField]
    public float ExplosionMaxTileIntensity = 3f;

    /// <summary>
    /// Explosion prototype ID.
    /// </summary>
    [DataField]
    public string ExplosionPrototype = "Default";
}

[Serializable, NetSerializable]
public enum AnCoWeaponCaseHackWireActionKey : byte
{
    Status
}
