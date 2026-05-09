using Robust.Shared.GameStates;

namespace Content.Shared._Horizon._Fractions.AnCo;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnCoHammerRepulseComponent : Component
{
    /// <summary>
    /// Текущий заряд молота (0-100).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentCharge;

    /// <summary>
    /// Максимальный заряд (100%).
    /// </summary>
    [DataField]
    public int MaxCharge = 100;

    /// <summary>
    /// Сколько заряда добавляется за один удар.
    /// </summary>
    [DataField]
    public int ChargePerHit = 5;

    [DataField]
    public float StaminaDamagePercent = 0.5f;

    [DataField]
    public float ThrowStrength = 10f;

    [DataField]
    public float Distance = 5f;
}
