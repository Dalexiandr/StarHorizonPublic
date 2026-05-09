using Content.Shared._Horizon.Mech.Components;
using Content.Shared._Horizon.RCD;
using Content.Shared._Mono.Radar;
using Content.Shared.Actions;
using Content.Shared.Charges.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.RCD;
using Content.Shared.RCD.Systems;
using Content.Shared.Tools.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Horizon.Mech.EntitySystems;

public abstract class SharedMechToolsSystem : EntitySystem
{
    [Dependency] private readonly RCDSystem _rcd = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedMechSystem _mech = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedJetpackSystem _jet = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechJetpackComponent, SetupMechUserEvent>(OnSetupJetpackUser);
        SubscribeLocalEvent<MechJetpackComponent, MechJetpackToggleEvent>(OnJetpackToggle);
        SubscribeLocalEvent<MechJetpackComponent, MapInitEvent>(OnInitJetpack);
        SubscribeLocalEvent<MechJetpackComponent, ComponentShutdown>(OnJetpackShutdown);

        SubscribeLocalEvent<MechRCDComponent, SetupMechUserEvent>(OnSetupRCDUser);
        SubscribeLocalEvent<MechRCDComponent, MechRCDToggleEvent>(OnRCDToggle);

        SubscribeLocalEvent<MechRCDComponent, AfterInteractEvent>(OnRCDAfterInteract);
        SubscribeLocalEvent<MechRCDComponent, RCDPlacementFinishedEvent>(OnRCDFinish);
        SubscribeLocalEvent<MechRCDComponent, GetUsedEntityEvent>(OnGetUsedRCDEntity);
    }

    private void OnSetupJetpackUser(Entity<MechJetpackComponent> ent, ref SetupMechUserEvent args)
    {
        _actions.AddAction(args.Pilot, ref ent.Comp.ToggleAction, "MechJetpackToggleAction", ent.Owner);
    }

    private void OnInitJetpack(Entity<MechJetpackComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<MechComponent>(ent.Owner, out var mech) || mech.PilotSlot.ContainedEntity is not { Valid: true } pilot)
            return;

        _actions.AddAction(pilot, ref ent.Comp.ToggleAction, "MechJetpackToggleAction", ent.Owner);
    }

    private void OnJetpackToggle(Entity<MechJetpackComponent> ent, ref MechJetpackToggleEvent args)
    {
        if (!TryComp<JetpackComponent>(ent.Owner, out var jetpack))
            return;

        _jet.SetEnabled(ent.Owner, jetpack, !HasComp<ActiveJetpackComponent>(ent.Owner), ent.Owner);
    }

    private void OnJetpackShutdown(Entity<MechJetpackComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<PhysicsComponent>(ent.Owner, out var physics))
            _physics.SetBodyStatus(ent.Owner, physics, BodyStatus.OnGround);

        RemComp<CanMoveInAirComponent>(ent.Owner);
        RemComp<ActiveJetpackComponent>(ent.Owner);
        RemComp<RadarBlipComponent>(ent.Owner);

        _movementSpeedModifier.RefreshWeightlessModifiers(ent.Owner);

        _actions.RemoveAction(ent.Comp.ToggleAction);
    }

    private void OnSetupRCDUser(Entity<MechRCDComponent> ent, ref SetupMechUserEvent args)
    {
        _actions.AddAction(args.Pilot, ref ent.Comp.ToggleAction, "MechRCDToggleAction", ent.Owner);
        _actions.AddAction(args.Pilot, ref ent.Comp.MenuAction, "MechRCDMenuAction", ent.Owner);
    }

    private void OnRCDToggle(Entity<MechRCDComponent> ent, ref MechRCDToggleEvent args)
    {
        ent.Comp.Active = !ent.Comp.Active;
        _popup.PopupPredicted(Loc.GetString($"popup-mech-rcd-toggle-{ent.Comp.Active}"), ent.Owner, args.Performer);

        Dirty(ent);
    }

    private void OnRCDAfterInteract(Entity<MechRCDComponent> ent, ref AfterInteractEvent args)
    {
        if (!TryComp<MechComponent>(ent.Owner, out var mech) || !mech.PilotSlot.ContainedEntity.HasValue)
            return;

        if (!ent.Comp.Active)
            return;

        _charges.SetCharges(ent.Owner, 50);
        _mech.TryChangeEnergy(ent, -ent.Comp.PlaceCost / 2);

        _rcd.TryInteract(ent, mech.PilotSlot.ContainedEntity.Value, args.Target, args.ClickLocation);
    }

    private void OnRCDFinish(Entity<MechRCDComponent> ent, ref RCDPlacementFinishedEvent args)
    {
        _mech.TryChangeEnergy(ent, -ent.Comp.PlaceCost / 2);
    }

    private void OnGetUsedRCDEntity(Entity<MechRCDComponent> ent, ref GetUsedEntityEvent args)
    {
        args.Used = ent.Owner;
    }
}
