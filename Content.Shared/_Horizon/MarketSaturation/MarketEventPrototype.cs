using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Horizon.MarketSaturation;

/// <summary>
/// Прототип рыночного события.
/// Определяет название, описание (для объявления), затронутые станции и предметы,
/// модификатор цены и длительность.
/// Система случайным образом выбирает одно из событий каждые 2-3 часа.
/// </summary>
[Prototype("marketEvent")]
public sealed partial class MarketEventPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// Ключ локализации для текста объявления.
    /// Доступные параметры: {$station} — название станции, {$duration} — длительность в минутах.
    /// </summary>
    [DataField(required: true)]
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// ID прототипов станций, на которых может произойти событие.
    /// Если пустой — событие может произойти на любой станции с MarketSaturationComponent.
    /// </summary>
    [DataField]
    public List<string> Stations { get; private set; } = new();

    /// <summary>
    /// ID прототипов предметов, цена которых изменяется при событии.
    /// </summary>
    [DataField(required: true)]
    public List<string> Items { get; private set; } = new();

    /// <summary>
    /// Модификатор цены. Больше 1.0 = цена выше, меньше 1.0 = цена ниже.
    /// Например: 2.0 = цена в 2 раза выше, 0.5 = цена в 2 раза ниже.
    /// </summary>
    [DataField]
    public double PriceModifier { get; private set; } = 2.0;

    /// <summary>
    /// Длительность события. По умолчанию: 30 минут.
    /// </summary>
    [DataField]
    public TimeSpan Duration { get; private set; } = TimeSpan.FromMinutes(30);
}
