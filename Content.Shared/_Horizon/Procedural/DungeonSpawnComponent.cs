using Content.Shared.Procedural;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Procedural;

[RegisterComponent]
public sealed partial class AnCoDungeonMarkerComponent : Component
{
    /// <summary>
    /// Указывает какой пресет использовать для генерации данжа
    /// </summary>
    [DataField("dungeonPreset", required: true)]
    public ProtoId<DungeonConfigPrototype> DungeonPreset { get; set; }

    /// <summary>
    /// Генерация структур по сидам, если пусто сид выбирается рандомно
    /// </summary>
    [DataField("seed")]
    public int? Seed { get; set; }

}