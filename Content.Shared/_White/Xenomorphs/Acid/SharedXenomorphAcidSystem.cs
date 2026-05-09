using Content.Shared._White.Other;
using Content.Shared._White.Xenomorphs.Acid.Components;
using Content.Shared.Coordinates;
using Content.Shared.Popups;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Content.Shared.FixedPoint;
using Content.Shared._White.Actions;

namespace Content.Shared._White.Xenomorphs.Acid;

public abstract class SharedXenomorphAcidSystem : EntitySystem
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("TEST.sharedxenomorphacid");
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PlasmaCostActionSystem _plasmaCost = default!; // Goobstation

    public override void Initialize()
    {
        base.Initialize();

        _sawmill.Debug("SharedXenomorphAcidSystem initialized");
        SubscribeLocalEvent<XenomorphAcidComponent, AcidActionEvent>(OnXenomorphAcidActionEvent);
    }

    private void OnXenomorphAcidActionEvent(EntityUid uid, XenomorphAcidComponent component, AcidActionEvent args)
    {
        _sawmill.Debug($"OnXenomorphAcidActionEvent: uid={uid}, target={args.Target}, handled={args.Handled}");
        if (args.Handled)
            return;

        // Check if this is a plasma-cost action and get the cost
        // Goobstation start
        TryComp<PlasmaCostActionComponent>(args.Action, out var plasmaCost);
        var plasmaCostValue = plasmaCost?.PlasmaCost ?? FixedPoint2.Zero;
        _sawmill.Debug($"OnXenomorphAcidActionEvent: plasmaCost={plasmaCostValue}");

        // Check plasma cost before proceeding
        if (plasmaCostValue > FixedPoint2.Zero && !_plasmaCost.HasEnoughPlasma(uid, plasmaCostValue))
        {
            _sawmill.Debug($"OnXenomorphAcidActionEvent: not enough plasma");
            _popup.PopupEntity(Loc.GetString("xenomorphs-acid-not-enough-plasma"), uid, uid, type: PopupType.SmallCaution);
            return;
        }

        if (!HasComp<StructureComponent>(args.Target)) // TODO: This should check whether the target is a structure.
        {
            _sawmill.Debug($"OnXenomorphAcidActionEvent: target not corrodible");
            _popup.PopupEntity(Loc.GetString("xenomorphs-acid-not-corrodible", ("target", args.Target)), uid, uid, type: PopupType.SmallCaution);
            return;
        }

        if (HasComp<AcidCorrodingComponent>(args.Target))
        {
            _sawmill.Debug($"OnXenomorphAcidActionEvent: target already corroding");
            _popup.PopupEntity(Loc.GetString("xenomorphs-acid-already-corroding", ("target", args.Target)), uid, uid, type: PopupType.SmallCaution);
            return;
        }

        // Deduct the plasma cost after all checks pass
        if (plasmaCostValue > FixedPoint2.Zero)
            _plasmaCost.DeductPlasma(uid, plasmaCostValue);

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("xenomorphs-acid-apply", ("target", args.Target)), uid, uid, type: PopupType.Small);

        // Goobstation end

        if (_net.IsClient)
            return;

        var acid = SpawnAttachedTo(component.AcidId, args.Target.ToCoordinates());
        var acidCorroding = new AcidCorrodingComponent
        {
            Acid = acid,
            AcidExpiresAt = Timing.CurTime + component.AcidLifeTime,
            DamagePerSecond = component.DamagePerSecond
        };
        AddComp(args.Target, acidCorroding);
        _sawmill.Debug($"OnXenomorphAcidActionEvent: spawned acid={acid} on target={args.Target}");
    }
}
