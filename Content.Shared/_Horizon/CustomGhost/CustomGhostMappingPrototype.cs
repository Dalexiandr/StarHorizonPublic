using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared._Horizon.CustomGhost;

/// <summary>
/// Проверка нужного CKey для выдачи кастомного призрака
/// </summary>
[Prototype("customGhost")]
public sealed class CustomGhostMappingPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("customGhost")]
    public Dictionary<string, string> Mappings { get; set; } = new();
}
