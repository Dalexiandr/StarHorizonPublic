using Robust.Shared.GameStates;
using Content.Shared.Chemistry.Components;

namespace Content.Shared._Horizon.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SyringeMechGunComponent : Component
{
    [DataField]
    public List<string> AllowedReagents = new();

    [DataField, AutoNetworkedField]
    public string CurrentReagent = "";

    [DataField, AutoNetworkedField]
    public float Amount = 5f;

    [DataField, AutoNetworkedField]
    public string SolutionName = "";
}
