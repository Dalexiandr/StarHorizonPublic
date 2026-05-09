using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared._Horizon.Cytology.Prototypes;

/// <summary>
///     It contains all the information about the cell, which will then be used during its growth
/// </summary>
[Serializable, NetSerializable, DataDefinition]
[Prototype("cellSample")]
public sealed partial class CellSamplePrototype : IPrototype, ICloneable
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    ///     The name of the cell that will be displayed in the microscope
    /// </summary>
    [DataField] public string Name = "cell";

    /// <summary>
    ///     How many seconds will the cell take to grow in the vat, excluding all modifiers
    /// </summary>
    [DataField] public float GrowthRateInSeconds = 1f;

    /// <summary>
    ///     It has no use. It's on the wiki, but I haven't found what it's used for
    /// </summary>
    [DataField] public float ViralSusceptibility = 1f;

    /// <summary>
    ///     Which cell requires chemicals for growth in the vat. If they are not present, the vat will stop working with an error
    /// </summary>
    [DataField] public List<ProtoId<ReagentPrototype>> RequiredChemicals = new();

    /// <summary>
    ///     Chemicals that speed up cell growth
    ///     The id of the chemical is listed first, followed by how much it affects
    /// </summary>
    [DataField] public Dictionary<ProtoId<ReagentPrototype>, float> SupplementaryChemicals = new();

    /// <summary>
    ///     Chemicals that speed down cell growth
    ///     The id of the chemical is listed first, followed by how much it affects
    /// </summary>
    [DataField] public Dictionary<ProtoId<ReagentPrototype>, float> SuppressiveChemicals = new();

    /// <summary>
    ///     Which mob spawns when the cage is grown. You can specify multiple mobs at once
    /// </summary>
    [DataField] public HashSet<string>? SpawnMobByPrototype;

    /// <summary>
    ///     The texture that will be displayed on the swab
    /// </summary>
    [DataField] public String? TextureState;

    public object Clone()
    {
        return new CellSamplePrototype()
        {
            ID = ID,
            Name = Name,
            GrowthRateInSeconds = GrowthRateInSeconds,
            ViralSusceptibility = ViralSusceptibility,
            RequiredChemicals = RequiredChemicals,
            SupplementaryChemicals = SupplementaryChemicals,
            SuppressiveChemicals = SuppressiveChemicals,
            SpawnMobByPrototype = SpawnMobByPrototype,
            TextureState = TextureState
        };
    }
}

