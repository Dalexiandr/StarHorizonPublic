using Content.Shared.FixedPoint;
using Content.Shared._White.Xenomorphs.Plasma.Components;
using Content.Shared.Alert;
using Robust.Shared.Log;

namespace Content.Shared._White.Xenomorphs.Plasma;

public abstract class SharedPlasmaSystem : EntitySystem
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("TEST.sharedplasma");
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        _sawmill.Debug("SharedPlasmaSystem initialized");
        SubscribeLocalEvent<PlasmaVesselComponent, ComponentShutdown>(OnPlasmaVesselShutdown);
        SubscribeLocalEvent<PlasmaVesselComponent, TransferPlasmaActionEvent>(OnPlasmaTransfer);
    }

    private void OnPlasmaVesselShutdown(EntityUid uid, PlasmaVesselComponent component, ComponentShutdown args)
    {
        _sawmill.Debug($"OnPlasmaVesselShutdown: uid={uid}");
        _alerts.ClearAlert(uid, component.PlasmaAlert);
    }

    private void OnPlasmaTransfer(EntityUid uid, PlasmaVesselComponent component, TransferPlasmaActionEvent args)
    {
        _sawmill.Debug($"OnPlasmaTransfer: uid={uid}, target={args.Target}, amount={args.Amount}, handled={args.Handled}");
        if (args.Handled
            || !TryComp<PlasmaVesselComponent>(args.Target, out var plasmaVesselTarget)
            || !ChangePlasmaAmount(uid, -args.Amount, component))
        {
            _sawmill.Debug($"OnPlasmaTransfer: transfer failed or already handled");
            return;
        }

        ChangePlasmaAmount(args.Target, args.Amount, plasmaVesselTarget);
        _sawmill.Debug($"OnPlasmaTransfer: transferred {args.Amount} plasma from {uid} to {args.Target}");

        args.Handled = true;
    }

    public bool ChangePlasmaAmount(EntityUid uid, FixedPoint2 amount, PlasmaVesselComponent? component = null)
    {
        _sawmill.Debug($"ChangePlasmaAmount: uid={uid}, amount={amount}");
        if (!Resolve(uid, ref component) || component.Plasma + amount < 0)
        {
            _sawmill.Debug($"ChangePlasmaAmount: failed to resolve or negative result, plasma={component?.Plasma}");
            return false;
        }

        var oldPlasma = component.Plasma;
        component.Plasma = FixedPoint2.Min(component.Plasma + amount, component.MaxPlasma);
        Dirty(uid, component);
        _sawmill.Debug($"ChangePlasmaAmount: changed plasma from {oldPlasma} to {component.Plasma}");

        RaiseLocalEvent(uid, new PlasmaAmountChangeEvent(component.Plasma));

        _alerts.ShowAlert(uid, component.PlasmaAlert);

        return true;
    }

    /// <summary>
    /// Goobstation - checks if a mob has at least a certain amount of plasma.
    /// </summary>
    public bool HasPlasma(EntityUid uid, FixedPoint2 amount)
    {
        var hasPlasma = TryComp<PlasmaVesselComponent>(uid, out var comp)
            && comp.Plasma >= amount;
        _sawmill.Debug($"HasPlasma: uid={uid}, amount={amount}, result={hasPlasma}, plasma={comp?.Plasma}");
        return hasPlasma;
    }
}
