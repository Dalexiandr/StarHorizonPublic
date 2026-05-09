using Content.Shared._White.Xenomorphs;
using Content.Shared._White.Xenomorphs.Plasma;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.FixedPoint;
using Content.Shared._White.Xenomorphs.Plasma.Components;
using Robust.Shared.Log;

namespace Content.Shared._White.Actions;

public sealed class PlasmaCostActionSystem : EntitySystem
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("TEST.plasmacostaction");
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPlasmaSystem _plasma = default!;

    public override void Initialize()
    {
        _sawmill.Debug("PlasmaCostActionSystem initialized");
        SubscribeLocalEvent<PlasmaCostActionComponent, ActionRelayedEvent<PlasmaAmountChangeEvent>>(OnPlasmaAmountChange);
        SubscribeLocalEvent<PlasmaCostActionComponent, ActionAttemptEvent>(OnActionAttempt); // Goobstation
    }

    /// <summary>
    /// Checks if the performer has enough plasma for the action.
    /// Returns true if the action should proceed, false if it should be blocked.
    /// Goobstation
    /// </summary>
    public bool HasEnoughPlasma(EntityUid performer, FixedPoint2 cost)
    {
        _sawmill.Debug($"HasEnoughPlasma: performer={performer}, cost={cost}");
        if (cost <= 0)
            return true;

        var hasEnough = TryComp<PlasmaVesselComponent>(performer, out var plasmaVessel) &&
               plasmaVessel.Plasma >= cost;
        _sawmill.Debug($"HasEnoughPlasma: result={hasEnough}, plasma={plasmaVessel?.Plasma}");
        return hasEnough;
    }

    /// <summary>
    /// Deducts plasma from the performer. Call this after confirming the action succeeds.
    /// </summary>
    public void DeductPlasma(EntityUid performer, FixedPoint2 cost)
    {
        _sawmill.Debug($"DeductPlasma: performer={performer}, cost={cost}");
        if (cost > 0)
            _plasma.ChangePlasmaAmount(performer, -cost);
    }

    [Obsolete("Use HasEnoughPlasma and DeductPlasma separately for better control")]
    public bool CheckPlasmaCost(EntityUid performer, FixedPoint2 cost)
    {
        if (!HasEnoughPlasma(performer, cost))
            return false;

        DeductPlasma(performer, cost);
        return true;
    }

    private void OnPlasmaAmountChange(EntityUid uid, PlasmaCostActionComponent component, ActionRelayedEvent<PlasmaAmountChangeEvent> args)
    {
        var enabled = component.PlasmaCost <= args.Args.Amount;
        _sawmill.Debug($"OnPlasmaAmountChange: uid={uid}, plasmaCost={component.PlasmaCost}, amount={args.Args.Amount}, enabled={enabled}");
        _actions.SetEnabled(uid, enabled);
    }

    private void OnActionAttempt(Entity<PlasmaCostActionComponent> ent, ref ActionAttemptEvent args)
    {
        _sawmill.Debug($"OnActionAttempt: uid={ent.Owner}, user={args.User}, plasmaCost={ent.Comp.PlasmaCost}");
        if (!_plasma.HasPlasma(args.User, ent.Comp.PlasmaCost))
        {
            _sawmill.Debug($"OnActionAttempt: not enough plasma, cancelling");
            args.Cancelled = true;
        }
    }
}
