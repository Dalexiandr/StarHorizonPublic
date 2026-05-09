using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Traits;

public sealed partial class SpeciesWhitelist : BaseTraitRequirment
{
    [DataField(required: true)]
    public List<ProtoId<SpeciesPrototype>> Species = new();

    public override bool CanApply(HumanoidCharacterProfile profile, IEntityManager entMan)
    {
        var result = Species.Contains(profile.Species);
        return Inverted ? !result : result;
    }
}
