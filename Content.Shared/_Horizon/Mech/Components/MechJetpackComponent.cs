using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Mech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechJetpackComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? ToggleAction;
}
