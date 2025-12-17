using Content.Shared.Actions;
using Content.Shared.Inventory;
using JetBrains.Annotations;

namespace Content.Shared._Horizon.NightVision;

public sealed class NightVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    private const string NightVisionToggleAction = "SwitchNightVision";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<NightVisionComponent, NVInstantActionEvent>(OnActionToggle);
    }

    private void OnComponentStartup(EntityUid uid, NightVisionComponent component, ComponentStartup args)
    {
        if (component.IsToggle)
        {
            _actionsSystem.AddAction(uid, ref component.ActionContainer, NightVisionToggleAction);
        }
    }

    private void OnActionToggle(EntityUid uid, NightVisionComponent component, NVInstantActionEvent args)
    {
        component.IsNightVision = !component.IsNightVision;
        var changeEvent = new NightVisionnessChangedEvent(component.IsNightVision);
        RaiseLocalEvent(uid, ref changeEvent);
        Dirty(uid, component);
    }

    [PublicAPI]
    public void UpdateIsNightVision(EntityUid uid, NightVisionComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var previousState = component.IsNightVision;
        var canVisionEvent = new CanVisionAttemptEvent();
        RaiseLocalEvent(uid, canVisionEvent);

        component.IsNightVision = canVisionEvent.NightVision;

        if (previousState == component.IsNightVision)
            return;

        var changeEvent = new NightVisionnessChangedEvent(component.IsNightVision);
        RaiseLocalEvent(uid, ref changeEvent);
        Dirty(uid, component);
    }
}

[ByRefEvent]
public record struct NightVisionnessChangedEvent(bool NightVision);

public sealed class CanVisionAttemptEvent : CancellableEntityEventArgs, IInventoryRelayEvent
{
    public bool NightVision => Cancelled;
    public SlotFlags TargetSlots => SlotFlags.EYES | SlotFlags.MASK | SlotFlags.HEAD;
}
