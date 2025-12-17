using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Sponsors;

/// <summary>
///     Используется для определения различных категорий в магазине спонсоров.
/// </summary>
[Prototype("sponsorShopCategory")]
[Serializable, NetSerializable, DataDefinition]
public sealed partial class SponsorShopCategoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name")]
    public string Name { get; private set; } = "";

    [DataField("priority")]
    public int Priority { get; private set; } = 0;

    [DataField("items")]
    public List<string> Items { get; private set; } = new();
}
