using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Cytology.Components;

/// <summary>
///     Specified for objects that may have samples
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class CytologyDirtComponent : Component
{
    /// <summary>
    ///     What samples can an object have
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<CellSample> PossibleCellSamples = new();

    /// <summary>
    ///     What samples does the object have after initialization
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<CellSample> CurrentCellSamples = new();

    /// <summary>
    ///     What is the chance that a cell will appear
    ///     For example, 0.5 means that approximately half of the possible cells will appear on the object
    ///     If we do not consider the maximum cell limit
    /// </summary>
    [DataField]
    public float SampleChance = 0.5f;

    /// <summary>
    ///     How many samples can there be in the object
    /// </summary>
    [DataField]
    public int MaxSamples = 3;
}
