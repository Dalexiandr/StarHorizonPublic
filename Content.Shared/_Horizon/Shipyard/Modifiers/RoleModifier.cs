using Content.Shared._NF.Shipyard.Components;
using Content.Shared.Access.Components;

namespace Content.Shared._Horizon.Shipyard;

public sealed partial class RoleModifier : BaseVesselCostModifier
{
    [DataField("role", required: true)]
    private string _role = string.Empty;

    public override void Modify(EntityUid? user, EntityUid console, ref int cost, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<ShipyardConsoleComponent>(console, out var comp) || entMan.GetEntity(comp.CurIdCard) is not { Valid: true } id)
            return;

        if (!entMan.TryGetComponent<IdCardComponent>(id, out var idCardComp))
            return;

        if (idCardComp.JobPrototype != _role)
            return;

        cost = (int)(cost * CostMultiplier);
    }
}
