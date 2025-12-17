using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Pain.Prototypes;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype("screamOfPain")]
public sealed partial class ScreamOfPainPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = null!;

    [DataField("screamList")]
    public Dictionary<FixedPoint2, List<string>> ScreamList = [];
}
