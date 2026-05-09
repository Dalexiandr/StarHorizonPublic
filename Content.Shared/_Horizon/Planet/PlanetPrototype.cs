using Content.Shared.Atmos;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Markers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Horizon.Planet;

[Prototype]
public sealed partial class PlanetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; set; } = string.Empty;

    /// <summary>
    /// Спавнить ли планету раундстартом
    /// </summary>
    [DataField]
    public bool SpawnRoundstart = false;

    /// <summary>
    /// Карта основной локации планеты (0 0 координаты)
    /// </summary>
    [DataField]
    public ResPath? MapPath;

    /// <summary>
    /// Биом, применяющийся к карте планеты
    /// </summary>
    [DataField(required: true)]
    public ProtoId<BiomeTemplatePrototype> Biome;

    /// <summary>
    /// Имя планеты
    /// </summary>
    [DataField(required: true)]
    public LocId MapName;

    /// <summary>
    /// Освещение планеты
    /// </summary>
    [DataField]
    public Color MapLight = Color.FromHex("#D8B059");

    /// <summary>
    /// Компоненты, добавляемые к карте планеты
    /// </summary>
    [DataField]
    public ComponentRegistry? AddedComponents;

    /// <summary>
    /// Атмосферы планеты
    /// </summary>
    [DataField(required: true)]
    public GasMixture Atmosphere = new();

    /// <summary>
    /// Слои маркеров биомов, по типу руд
    /// </summary>
    [DataField]
    public List<ProtoId<BiomeMarkerLayerPrototype>> BiomeMarkerLayers = new();
}
