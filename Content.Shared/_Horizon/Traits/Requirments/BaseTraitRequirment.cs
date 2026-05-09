using Content.Shared.Preferences;

namespace Content.Shared._Horizon.Traits;

[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseTraitRequirment
{
    [DataField]
    protected bool Inverted = false;

    public abstract bool CanApply(HumanoidCharacterProfile profile, IEntityManager entMan);
}
