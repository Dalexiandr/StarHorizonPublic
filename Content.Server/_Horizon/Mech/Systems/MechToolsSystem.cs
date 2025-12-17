using Content.Server.Actions;
using Content.Shared._Horizon.Mech.Components;
using Content.Shared._Horizon.Mech.EntitySystems;
using Content.Shared.Mech.Components;
using Robust.Server.GameObjects;

namespace Content.Server._Horizon.Mech;

public sealed class MechToolsSystem : SharedMechToolsSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechRCDComponent, MapInitEvent>(OnRCDInit);
        SubscribeLocalEvent<MechRCDComponent, ComponentShutdown>(OnRCDShutdown);
    }

    private void OnRCDInit(Entity<MechRCDComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<MechComponent>(ent.Owner, out var mech) || !mech.PilotSlot.ContainedEntity.HasValue)
            return;

        _actions.AddAction(mech.PilotSlot.ContainedEntity.Value, ref ent.Comp.ToggleAction, "MechRCDToggleAction", ent.Owner);
        _actions.AddAction(mech.PilotSlot.ContainedEntity.Value, ref ent.Comp.MenuAction, "MechRCDMenuAction", ent.Owner);
    }

    private void OnRCDShutdown(Entity<MechRCDComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.ToggleAction);
        _actions.RemoveAction(ent.Comp.MenuAction);
    }
}
