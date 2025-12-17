using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Cytology.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CytologyInjectorComponent : Component
{
    [DataField]
    public float TakeDelay = 2f;
}
