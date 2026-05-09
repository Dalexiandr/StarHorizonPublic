using Content.Client._Horizon.RCD;
using Content.Shared._Horizon.Mech.Components;
using Content.Shared._Horizon.Mech.EntitySystems;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.RCD;
using Content.Shared.RCD.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Horizon.Mech;

public sealed class MechToolsSystem : SharedMechToolsSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechRCDComponent, MechRCDMenuEvent>(OnRCDMenu);

        SubscribeLocalEvent<MechPilotComponent, GetRCDEntityEvent>(OnGetRCD);
    }

    private void OnRCDMenu(Entity<MechRCDComponent> ent, ref MechRCDMenuEvent args)
    {
        _ui.TryOpenUi(ent.Owner, RcdUiKey.Key, args.Performer, true);
    }

    private void OnGetRCD(EntityUid uid, MechPilotComponent comp, ref GetRCDEntityEvent args)
    {
        if (!comp.Mech.IsValid())
            return;

        if (!HasComp<RCDComponent>(comp.Mech) || !TryComp<MechRCDComponent>(comp.Mech, out var mechRCD) || !mechRCD.Active)
            return;

        args.Entity = comp.Mech;
    }
}
