using Content.Shared.Actions;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Shared._Horizon.NightVision;

public sealed class PNVSystem : EntitySystem
{
    [Dependency] private readonly NightVisionSystem _nightvisionableSystem = null!;
    [Dependency] private readonly IEntityManager _entManager = null!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PNVComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<PNVComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<PNVComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<PNVComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<PNVComponent, InventoryRelayedEvent<CanVisionAttemptEvent>>(OnPNVTrySee);
    }

    // ReSharper disable once MemberCanBeMadeStatic.Local
    private void OnPNVTrySee(EntityUid uid, PNVComponent component, InventoryRelayedEvent<CanVisionAttemptEvent> args)
    {
        args.Args.Cancel();
    }

    private void OnEquipped(EntityUid uid, PNVComponent component, GotEquippedEvent args)
    {
        if (args.Slot != "eyes" && args.Slot != "mask" && args.Slot != "head")
            return;

        var pnvComp = _entManager.GetComponent<NightVisionComponent>(args.Equipee);
        if (pnvComp == null)
            return;

        _nightvisionableSystem.UpdateIsNightVision(args.Equipee);
        _actionsSystem.AddAction(args.Equipee, ref component.ActionContainer, component.ActionProto);
    }

    private void OnUnequipped(EntityUid uid, PNVComponent component, GotUnequippedEvent args)
    {
        if (args.Slot != "eyes" && args.Slot != "mask" && args.Slot != "head")
            return;

        var pnvComp = _entManager.GetComponent<NightVisionComponent>(args.Equipee);
        if (pnvComp == null)
            return;

        _nightvisionableSystem.UpdateIsNightVision(args.Equipee);
        _actionsSystem.RemoveAction(args.Equipee, component.ActionContainer);
    }

    private void OnComponentInit(EntityUid uid, PNVComponent component, ComponentInit args)
    {
        if (!HasComp<EyeComponent>(uid))
            return;

        _nightvisionableSystem.UpdateIsNightVision(uid);
        _actionsSystem.AddAction(uid, ref component.ActionContainer, component.ActionProto);
    }

    private void OnComponentRemove(EntityUid uid, PNVComponent component, ComponentRemove args)
    {
        if (!HasComp<EyeComponent>(uid))
            return;

        _nightvisionableSystem.UpdateIsNightVision(uid);
        _actionsSystem.RemoveAction(uid, component.ActionContainer);
    }
}
