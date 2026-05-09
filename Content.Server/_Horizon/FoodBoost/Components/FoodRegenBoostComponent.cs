using Content.Shared.Damage;

namespace Content.Server._Horizon.FoodBoost;

[RegisterComponent]
public sealed partial class FoodRegenBoostComponent : Component
{
    [DataField]
    public DamageSpecifier Regen = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan End;
}
