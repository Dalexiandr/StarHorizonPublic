using Content.Shared.Procedural;
using Content.Shared.Salvage.Expeditions;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._Horizon.Planet;

[RegisterComponent]
public sealed partial class DungeonSpawnComponent : Component
{
    [DataField]
    public List<ProtoId<DungeonConfigPrototype>> Dungeons;

    [DataField]
    public List<ProtoId<SalvageFactionPrototype>> MobFactions;

    [DataField]
    public int MobBudget = 35;
}
