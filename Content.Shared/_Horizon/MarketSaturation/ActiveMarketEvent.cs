using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Horizon.MarketSaturation;

/// <summary>
/// Хранит информацию об активном рыночном событии на конкретной станции.
/// Создаётся при срабатывании события и удаляется по истечении времени.
/// </summary>
[DataDefinition]
public sealed partial class ActiveMarketEvent
{
    /// <summary>
    /// ID прототипа рыночного события.
    /// </summary>
    [DataField]
    public string EventPrototypeId = string.Empty;

    /// <summary>
    /// ID прототипов предметов, цена которых изменяется.
    /// </summary>
    [DataField]
    public HashSet<string> AffectedItems = new();

    /// <summary>
    /// Модификатор цены для затронутых предметов.
    /// </summary>
    [DataField]
    public double PriceModifier = 1.0;

    /// <summary>
    /// Время окончания события (игровое время).
    /// </summary>
    [DataField]
    public TimeSpan EndTime;
}
