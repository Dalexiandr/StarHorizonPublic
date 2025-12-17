using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Cytology.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SampleSourceComponent : Component
{
    /// <summary>
    ///     What kind of cell will the injector get when it tries to take it from the creature
    /// </summary>
    [DataField]
    public List<CellSample> AvailableCellSamples;
}
