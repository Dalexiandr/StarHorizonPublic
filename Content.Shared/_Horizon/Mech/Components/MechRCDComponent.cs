using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Mech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechRCDComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? MenuAction;

    [AutoNetworkedField]
    public EntityUid? ToggleAction;

    [DataField]
    public float PlaceCost = 12f;

    [AutoNetworkedField]
    public bool Active = false;
}
