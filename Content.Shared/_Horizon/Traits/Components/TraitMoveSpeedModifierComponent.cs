using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Traits;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TraitMoveSpeedModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<(float, float)> Modifiers = new();
}
