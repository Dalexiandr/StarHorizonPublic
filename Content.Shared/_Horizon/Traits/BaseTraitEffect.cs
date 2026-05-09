namespace Content.Shared._Horizon.Traits;

[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseTraitEffect
{
    public abstract void DoEffect(EntityUid uid, IEntityManager entMan);
}
