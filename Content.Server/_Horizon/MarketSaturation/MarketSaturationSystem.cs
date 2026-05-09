using Content.Server.Cargo.Systems;
using Content.Server._NF.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Horizon.MarketSaturation;
using Content.Shared.Stacks;

namespace Content.Server._Horizon.MarketSaturation;

/// <summary>
/// Управляет насыщением рынка для каждой станции.
/// Когда товары продаются через карго-паллеты, система отслеживает количество
/// и применяет снижение цены.
///
/// Формула:
///   шаги = TotalSold / BatchSize
///   множитель = max(MinPriceMultiplier, 1.0 - шаги * InitialReductionPercent / 100)
///   итоговаяЦена = round(базоваяЦена * множитель)
///
/// Каждые BatchSize проданных единиц — цена падает на InitialReductionPercent процентов.
/// </summary>
public sealed class MarketSaturationSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntitySoldEvent>(OnEntitySold);
        SubscribeLocalEvent<NFEntitySoldEvent>(OnNFEntitySold);
    }

    /// <summary>
    /// Вычисляет множитель цены на основе общего количества проданных единиц.
    /// </summary>
    public static double GetMultiplier(int totalSold, MarketSaturationComponent comp)
    {
        var steps = totalSold / comp.BatchSize;
        var multiplier = 1.0 - steps * comp.InitialReductionPercent / 100.0;
        return Math.Max(comp.MinPriceMultiplier, multiplier);
    }

    private void OnEntitySold(ref EntitySoldEvent args)
    {
        if (!TryComp<MarketSaturationComponent>(args.Station, out var saturation))
            return;

        ProcessSoldEntities(args.Sold, saturation);
    }

    private void OnNFEntitySold(ref NFEntitySoldEvent args)
    {
        var station = _station.GetOwningStation(args.Grid);
        if (station == null)
            return;

        if (!TryComp<MarketSaturationComponent>(station.Value, out var saturation))
            return;

        ProcessSoldEntities(args.Sold, saturation);
    }

    /// <summary>
    /// Обновляет TotalSold после продажи. Множитель цены вычисляется на лету при следующем расчёте.
    /// </summary>
    private void ProcessSoldEntities(HashSet<EntityUid> sold, MarketSaturationComponent saturation)
    {
        foreach (var soldEnt in sold)
        {
            var protoId = MetaData(soldEnt).EntityPrototype?.ID;
            if (protoId == null)
                continue;

            var count = 1;
            if (TryComp<StackComponent>(soldEnt, out var stack))
                count = stack.Count;

            if (!saturation.SaturationData.TryGetValue(protoId, out var data))
            {
                data = new ItemSaturationData();
                saturation.SaturationData[protoId] = data;
            }

            data.TotalSold += count;
        }
    }
}
