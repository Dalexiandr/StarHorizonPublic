using Robust.Shared.GameStates;

namespace Content.Shared._Horizon._Fractions.AnCo;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnCoBlockingOnWieldComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsWielded;
}
