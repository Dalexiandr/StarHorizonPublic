using Content.Shared.Preferences;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Traits;

public sealed partial class AnotherTrait : BaseTraitRequirment
{
    [DataField(required: true)]
    public ProtoId<TraitPrototype> Trait;

    public override bool CanApply(HumanoidCharacterProfile profile, IEntityManager entMan)
    {
        var result = profile.TraitPreferences.Contains(Trait);
        return Inverted ? !result : result;
    }
}
