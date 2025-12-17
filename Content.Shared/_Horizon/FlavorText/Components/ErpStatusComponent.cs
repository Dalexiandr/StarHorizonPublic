using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.FlavorText;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ErpStatusComponent : Component
{
    [DataField, AutoNetworkedField]
    public ErpStatus Status = ErpStatus.No;
}
