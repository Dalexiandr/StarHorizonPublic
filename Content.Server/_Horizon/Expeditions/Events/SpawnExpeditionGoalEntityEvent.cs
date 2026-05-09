namespace Content.Server._Horizon.Expeditions;

[DataDefinition]
public sealed partial class SpawnExpeditionGoalEntityEvent : EntityEventArgs
{
    [DataField(required: true)]
    public string SpawnerTag = "";

    [DataField(required: true)]
    public string Planet = "";

    [DataField(required: true)]
    public List<string> SpawnedEntities = new();

    [DataField]
    public int SpawnsPerMarker = 1;

    [DataField]
    public int MarkersCount = 1;
}
