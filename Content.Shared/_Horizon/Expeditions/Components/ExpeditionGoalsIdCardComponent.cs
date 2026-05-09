using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Expeditions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ExpeditionGoalsIdCardComponent : Component
{
    [AutoNetworkedField]
    public List<int> AssignedGoals = new();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxGoals = 2;
}
