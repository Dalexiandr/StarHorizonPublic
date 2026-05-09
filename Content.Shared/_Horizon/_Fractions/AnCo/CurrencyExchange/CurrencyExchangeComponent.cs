using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon._Fractions.AnCo.CurrencyExchange;

/// <summary>
/// Component for currency exchange terminal that converts disk credits to station credits.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CurrencyExchangeComponent : Component
{
    /// <summary>
    /// Base exchange rate (credits per 1 disk credit).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BaseExchangeRate = 1.0f;

    /// <summary>
    /// Current exchange rate after market fluctuation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CurrentExchangeRate = 1.0f;

    /// <summary>
    /// Minimum exchange rate (floor).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinExchangeRate = 0.5f;

    /// <summary>
    /// Maximum exchange rate (ceiling).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxExchangeRate = 2.0f;

    /// <summary>
    /// How much the rate can change per update (volatility).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RateVolatility = 0.05f;

    /// <summary>
    /// Time between rate updates in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float UpdateInterval = 60f;

    /// <summary>
    /// Commission percentage taken on each exchange (0.1 = 10%).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Commission = 0.05f;

    /// <summary>
    /// Rate trend direction (-1 = falling, 0 = stable, 1 = rising).
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public int RateTrend;

    /// <summary>
    /// Slot name for the disk.
    /// </summary>
    public const string DiskSlot = "disk_slot";
}

[Serializable, NetSerializable]
public enum CurrencyExchangeVisuals : byte
{
    HasDisk
}

[Serializable, NetSerializable]
public enum CurrencyExchangeUiKey : byte
{
    Key
}
