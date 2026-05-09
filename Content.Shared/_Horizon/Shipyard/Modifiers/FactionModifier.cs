using Content.Shared._Horizon.FlavorText;

namespace Content.Shared._Horizon.Shipyard;

public sealed partial class FactionCostModifier : BaseVesselCostModifier
{
    [DataField("faction", required: true)]
    private string _faction = string.Empty;

    public override void Modify(EntityUid? user, EntityUid console, ref int cost, IEntityManager entMan)
    {
        if (user == null)
            return;

        if (!entMan.TryGetComponent<CharacterFactionMemberComponent>(user.Value, out var factionComp))
            return;

        if (factionComp.Faction != _faction)
            return;

        cost = (int)(cost * CostMultiplier);
    }
}
