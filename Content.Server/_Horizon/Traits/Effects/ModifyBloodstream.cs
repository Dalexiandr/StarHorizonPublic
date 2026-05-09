using Content.Shared.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared._Horizon.Traits;
using Content.Shared.Weapons.Melee;

namespace Content.Server._Horizon.Traits;

public sealed partial class ModifyBloodstream : BaseTraitEffect
{
    [DataField]
    public float RefreshModifier = 1f;

    [DataField]
    public float BleedReductionModifier = 1f;

    [DataField]
    public string? BloodReagent;

    public override void DoEffect(EntityUid uid, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<BloodstreamComponent>(uid, out var comp))
            return;

        var bloodstream = entMan.System<BloodstreamSystem>();

        bloodstream.SetBloodRefreshAmount(uid, comp, comp.BloodRefreshAmount * RefreshModifier);
        bloodstream.SetBleedReductionAmount(uid, comp, comp.BleedReductionAmount * BleedReductionModifier);

        if (BloodReagent != null)
            bloodstream.ChangeBloodReagent(uid, BloodReagent);
    }
}
