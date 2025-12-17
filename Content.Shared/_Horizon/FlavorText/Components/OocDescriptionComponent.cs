using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.FlavorText;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OocDescriptionComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Description = "";
}
