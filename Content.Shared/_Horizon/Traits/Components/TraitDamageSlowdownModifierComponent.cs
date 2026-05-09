using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Traits;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TraitDamageSlowdownModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<float> Modifiers = new();
}
