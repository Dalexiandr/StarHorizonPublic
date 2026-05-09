using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Trade;

/// <summary>
/// Stores base market price and per-faction multipliers for trade goods.
/// <see cref="PriceMarket"/> is the absolute price at Market; other fields are multipliers.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HorizonFactionPriceComponent : Component
{
    /// <summary>
    /// Base price when selling at a Market station.
    /// </summary>
    [DataField]
    public double PriceMarket = 5000;

    /// <summary>
    /// Price multiplier for AnCo stations.
    /// </summary>
    [DataField]
    public double PriceAnCo = 1.0;

    /// <summary>
    /// Price multiplier for NanoTrasen-aligned stations.
    /// </summary>
    [DataField]
    public double PriceNanoTrasen = 1.0;

    /// <summary>
    /// Price multiplier for Frontier stations.
    /// </summary>
    [DataField]
    public double PriceFrontier = 1.0;

    /// <summary>
    /// Price multiplier for Pirate stations.
    /// </summary>
    [DataField]
    public double PricePirate = 1.0;

    /// <summary>
    /// Price multiplier for Syndicate stations.
    /// </summary>
    [DataField]
    public double PriceSyndicate = 1.0;
}
