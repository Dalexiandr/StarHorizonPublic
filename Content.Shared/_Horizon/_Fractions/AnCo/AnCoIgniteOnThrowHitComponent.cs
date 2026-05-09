using Robust.Shared.GameStates;

namespace Content.Shared._Horizon._Fractions.AnCo;

[RegisterComponent, NetworkedComponent]
public sealed partial class AnCoIgniteOnThrowHitComponent : Component
{
    [DataField]
    public float FireStacks = 1f;
}
