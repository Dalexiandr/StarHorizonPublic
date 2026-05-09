using Content.Server.Station.Systems;
using Content.Shared._Horizon.Trade;
using Content.Shared.Cargo;

namespace Content.Server._Horizon.Trade;

/// <summary>
/// Computes sell price for trade goods: <see cref="HorizonFactionPriceComponent.PriceMarket"/> × faction multiplier.
/// </summary>
public sealed class HorizonFactionPricingSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HorizonFactionPriceComponent, PriceCalculationEvent>(OnPriceCalculation);
    }

    private void OnPriceCalculation(Entity<HorizonFactionPriceComponent> ent, ref PriceCalculationEvent ev)
    {
        if (ev.Handled)
            return;

        var basePrice = ent.Comp.PriceMarket;

        var owningStation = _station.GetOwningStation(ent);

        var faction = HorizonFaction.Market;

        if (owningStation != null && TryComp<HorizonStationFactionComponent>(owningStation, out var factionComp))
            faction = factionComp.Faction;

        var factionMultiplier = GetFactionMultiplier(ent.Comp, faction);
        ev.Price = basePrice * factionMultiplier;
        ev.Price = double.Max(0.0, ev.Price);

        ev.Handled = true;
    }

    private static double GetFactionMultiplier(HorizonFactionPriceComponent comp, HorizonFaction faction)
    {
        return faction switch
        {
            HorizonFaction.Market => 1.0,
            HorizonFaction.AnCo => comp.PriceAnCo > 0 ? comp.PriceAnCo : 1.0,
            HorizonFaction.Syndicate => comp.PriceSyndicate > 0 ? comp.PriceSyndicate : 1.0,
            HorizonFaction.Frontier => comp.PriceFrontier > 0 ? comp.PriceFrontier : 1.0,
            HorizonFaction.Pirate => comp.PricePirate > 0 ? comp.PricePirate : 1.0,
            HorizonFaction.NanoTrasen => comp.PriceNanoTrasen > 0 ? comp.PriceNanoTrasen : 1.0,
            _ => 1.0
        };
    }

    /// <summary>
    /// Gets the sell price of an entity for a specific faction (does not require a station).
    /// </summary>
    public double GetFactionPrice(EntityUid uid, HorizonFaction faction)
    {
        if (!TryComp<HorizonFactionPriceComponent>(uid, out var priceComp))
            return 0;

        var basePrice = priceComp.PriceMarket;
        var multiplier = GetFactionMultiplier(priceComp, faction);
        return double.Max(0.0, basePrice * multiplier);
    }

    /// <summary>
    /// Resolves the trade faction for a station entity, or <see cref="HorizonFaction.Market"/> if none.
    /// </summary>
    public HorizonFaction GetStationFaction(EntityUid? station)
    {
        if (station == null)
            return HorizonFaction.Market;

        if (TryComp<HorizonStationFactionComponent>(station, out var factionComp))
            return factionComp.Faction;

        return HorizonFaction.Market;
    }
}
