using Robust.Shared.Serialization;

namespace Content.Shared._Horizon._Fractions.AnCo.CurrencyExchange;

[Serializable, NetSerializable]
public sealed class CurrencyExchangeBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly float CurrentExchangeRate;
    public readonly float Commission;
    public readonly int RateTrend;
    public readonly int DiskCredits;
    public readonly int DiskMaxCredits;
    public readonly bool HasDisk;

    public CurrencyExchangeBoundUserInterfaceState(
        float currentExchangeRate,
        float commission,
        int rateTrend,
        int diskCredits,
        int diskMaxCredits,
        bool hasDisk)
    {
        CurrentExchangeRate = currentExchangeRate;
        Commission = commission;
        RateTrend = rateTrend;
        DiskCredits = diskCredits;
        DiskMaxCredits = diskMaxCredits;
        HasDisk = hasDisk;
    }
}

/// <summary>
/// Message to exchange credits from disk to station bank.
/// </summary>
[Serializable, NetSerializable]
public sealed class CurrencyExchangeMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// Amount of disk credits to exchange.
    /// </summary>
    public readonly int Amount;

    public CurrencyExchangeMessage(int amount)
    {
        Amount = amount;
    }
}

/// <summary>
/// Message to exchange all credits from disk.
/// </summary>
[Serializable, NetSerializable]
public sealed class CurrencyExchangeAllMessage : BoundUserInterfaceMessage
{
}
