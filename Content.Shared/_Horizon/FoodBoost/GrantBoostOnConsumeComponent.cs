using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.FoodBoost;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class GrantBoostOnConsumeComponent : Component
{
    [DataField]
    public float? MoveSpeedModifier;

    [DataField]
    public DamageSpecifier? RegenAmount;

    [DataField]
    public float Duration = 40f;

    [DataField, AutoNetworkedField]
    public bool Advanced = false;
}
