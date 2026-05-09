using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.FoodBoost;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FoodMovespeedBoostComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Modifier = 1.1f;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan End;
}
