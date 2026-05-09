using Content.Shared.Hands;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Shared.Interaction;

// TODO deduplicate with AdminFrozenComponent
/// <summary>
/// Handles <see cref="BlockMovementComponent"/>, which prevents various
/// kinds of movement and interactions when attached to an entity.
/// </summary>
public partial class SharedInteractionSystem
{
    private void InitializeBlocking()
    {
        SubscribeLocalEvent<BlockMovementComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<BlockMovementComponent, UseAttemptEvent>(CancelEvent);
        SubscribeLocalEvent<BlockMovementComponent, InteractionAttemptEvent>(CancelInteractEvent);
        SubscribeLocalEvent<BlockMovementComponent, DropAttemptEvent>(CancelEvent);
        SubscribeLocalEvent<BlockMovementComponent, PickupAttemptEvent>(CancelEvent);
        SubscribeLocalEvent<BlockMovementComponent, ChangeDirectionAttemptEvent>(CancelEvent);

        SubscribeLocalEvent<BlockMovementComponent, ComponentStartup>(OnBlockingStartup);
        SubscribeLocalEvent<BlockMovementComponent, ComponentShutdown>(OnBlockingShutdown);
    }

    private void CancelInteractEvent(Entity<BlockMovementComponent> ent, ref InteractionAttemptEvent args)
    {
        // StarHorizon start
        if (!ent.Comp.BlockInteraction)
            return;

        // Фикс бага с взаимодействием мозга
        if (args.Target != null && HasComp<BorgChassisComponent>(args.Target.Value))
        {
            // Если цель - борг, то разрешаем взаимодействие
            // Это позволит взаимодействовать с боргом - позже в цепочке вызовов проверяется, что игрок держит мозг
            return;
        }

        args.Cancelled = true;
        // StarHorizon end
    }

    private void OnMoveAttempt(EntityUid uid, BlockMovementComponent component, UpdateCanMoveEvent args)
    {
        // If we're relaying then don't cancel.
        if (HasComp<RelayInputMoverComponent>(uid))
            return;

        args.Cancel(); // no more scurrying around
    }

    private void CancelEvent(EntityUid uid, BlockMovementComponent component, CancellableEntityEventArgs args)
    {
        args.Cancel();
    }

    private void OnBlockingStartup(EntityUid uid, BlockMovementComponent component, ComponentStartup args)
    {
        _actionBlockerSystem.UpdateCanMove(uid);
    }

    private void OnBlockingShutdown(EntityUid uid, BlockMovementComponent component, ComponentShutdown args)
    {
        _actionBlockerSystem.UpdateCanMove(uid);
    }
}

