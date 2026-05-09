namespace Content.Server._Horizon.Shipyard;

[ByRefEvent]
public record struct GetAdditionalGridCostEvent(int Price = 0);
