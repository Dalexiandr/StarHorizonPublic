using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Pain.Prototypes;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype("PainEffects")]
public sealed partial class PainEffectsPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Эффекты которые можно применить и уровень боли для них
    /// </summary>
    [DataField]
    public Dictionary<string, PainStages> Effects = new();
}
