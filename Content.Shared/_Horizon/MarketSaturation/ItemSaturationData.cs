using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.MarketSaturation;

/// <summary>
/// Хранит данные насыщения для одного прототипа предмета на рынке станции.
/// Отслеживает общее количество проданных единиц.
/// Множитель цены вычисляется сразу: max(0, 1.0 - (TotalSold / BatchSize) * InitialReductionPercent / 100).
/// </summary>
[DataDefinition]
public sealed partial class ItemSaturationData
{
    /// <summary>
    /// Общее количество проданных единиц данного товара на этой станции.
    /// </summary>
    [DataField]
    public int TotalSold;
}
