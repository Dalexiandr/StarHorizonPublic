using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Sponsors;

/// <summary>
///     Определяет товары для магазина спонсоров.
/// </summary>
[Prototype("sponsorShopListing")]
[Serializable, NetSerializable, DataDefinition]
public sealed partial class SponsorShopListingPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name")]
    public string Name { get; private set; } = "";

    [DataField("description")]
    public string Description { get; private set; } = "";

    [DataField("cost")]
    public int Cost { get; private set; } = 0;

    [DataField("priority")]
    public int Priority { get; private set; } = 0;

    [DataField("state")]
    public string State { get; private set; } = "icon";

    [DataField("prototypeId")]
    public string PrototypeId { get; private set; }
}
