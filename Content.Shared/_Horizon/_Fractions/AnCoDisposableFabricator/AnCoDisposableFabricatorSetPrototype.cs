using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Horizon._Fractions.AnCoDisposableFabricator;

/// <summary>
/// A prototype that defines a set of items and visuals for the disposable fabricator structure.
/// </summary>
[Prototype]
public sealed partial class AnCoDisposableFabricatorSetPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField] public string Name { get; private set; } = string.Empty;
    [DataField] public string Description { get; private set; } = string.Empty;
    [DataField] public SpriteSpecifier Sprite { get; private set; } = SpriteSpecifier.Invalid;

    [DataField] public List<EntProtoId> Content = new();
}
