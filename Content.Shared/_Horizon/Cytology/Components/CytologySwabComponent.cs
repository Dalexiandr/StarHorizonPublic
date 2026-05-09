using Robust.Shared.GameStates;
using Robust.Shared.Serialization;


namespace Content.Shared._Horizon.Cytology.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class CytologySwabComponent : Component
{
    [DataField]
    public float SwabDelay = 2f;

    /// <summary>
    ///     Stores information about which texture to display to show the cell on it
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? TextureState;
}
