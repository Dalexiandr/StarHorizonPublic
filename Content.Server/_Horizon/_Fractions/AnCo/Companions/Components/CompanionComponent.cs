namespace Content.Server._Horizon._Fractions.AnCo.Companions.Components;

[RegisterComponent]
public sealed partial class CompanionComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid IdCard = default;
}
