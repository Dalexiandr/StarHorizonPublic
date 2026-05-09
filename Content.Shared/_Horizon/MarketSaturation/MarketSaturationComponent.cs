using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Horizon.MarketSaturation;

/// <summary>
/// Прикрепляется к сущности станции для отслеживания насыщения рынка.
/// У каждой станции свой независимый рынок — продажа товаров снижает их цену
/// через механику ускоряющегося снижения ("насыщение рынка").
///
/// Формула одного шага насыщения:
///   абсолютноеСнижение = текущаяЦена * текущийПроцентСнижения / 100
///   новыйМножительЦены = старыйМножитель * (1 - min(текущийПроцентСнижения, 100) / 100)
///   следующийПроцентСнижения = min(абсолютноеСнижение, 100)
///
/// Дорогие товары обесцениваются быстрее (большое абсолютное снижение → большой следующий %).
/// Дешёвые товары обесцениваются медленнее (малое абсолютное снижение → малый следующий %).
/// </summary>
[RegisterComponent]
public sealed partial class MarketSaturationComponent : Component
{
    /// <summary>
    /// Данные насыщения по каждому товару, ключ — ID прототипа сущности.
    /// </summary>
    [DataField]
    public Dictionary<string, ItemSaturationData> SaturationData = new();

    /// <summary>
    /// Начальный процент снижения, применяемый на первом шаге насыщения.
    /// По умолчанию: 1% — первая партия снижает цену на 1%.
    /// </summary>
    [DataField]
    public double InitialReductionPercent = 1.0;

    /// <summary>
    /// Количество товаров одного типа, которое нужно продать для одного шага насыщения.
    /// По умолчанию: 100 единиц на шаг.
    /// </summary>
    [DataField]
    public int BatchSize = 100;

    /// <summary>
    /// Минимальный множитель цены. Цена не упадёт ниже базоваяЦена * MinPriceMultiplier.
    /// По умолчанию: 0.0 — товары могут полностью обесцениться.
    /// </summary>
    [DataField]
    public double MinPriceMultiplier = 0.0;

    /// <summary>
    /// Список активных рыночных событий на этой станции.
    /// События добавляются системой MarketEventSystem и автоматически удаляются по истечении времени.
    /// </summary>
    [DataField]
    public List<ActiveMarketEvent> ActiveEvents = new();
}
