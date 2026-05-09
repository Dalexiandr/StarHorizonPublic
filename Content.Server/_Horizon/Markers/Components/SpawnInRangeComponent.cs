using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

namespace Content.Server._Horizon.Markers;

[RegisterComponent]
public sealed partial class SpawnInRangeComponent : Component
{
    [DataField]
    public int MaxRange = 100;

    [DataField]
    public int MinRange = 25;

    [DataField]
    public float MinDistanceBetweenSpawns = 10f;

    [DataField(required: true)]
    public Dictionary<EntProtoId, MinMax> Spawns = new();
}
