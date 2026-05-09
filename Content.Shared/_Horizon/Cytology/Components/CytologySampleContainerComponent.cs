using Robust.Shared.GameStates;


namespace Content.Shared._Horizon.Cytology.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class CytologySampleContainerComponent : Component
{
    /// <summary>
    ///     How many samples does the object store
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<CellSample> CellSamples = new();

    /// <summary>
    ///     Maximum number of samples which can store an object
    /// </summary>
    [DataField]
    public int MaxSamples = 5;
}
