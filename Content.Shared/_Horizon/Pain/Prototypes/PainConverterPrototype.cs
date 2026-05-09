using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Pain.Prototypes;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype("painConverter")]
public sealed partial class PainConverterPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = null!;

    [DataField("convert")]
    public Dictionary<string, FixedPoint2> PainPerDamage = new();
}
