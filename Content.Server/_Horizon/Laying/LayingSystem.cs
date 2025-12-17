using Content.Shared._Horizon.Pain.Components;
using Content.Shared.Input;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Content.Shared._Horizon.Laying;
using Content.Shared.Gravity;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Body.Components;
using Content.Server.Guardian;
using Content.Shared._Horizon.Pain.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Map;

namespace Content.Server._Horizon.Laying;

public sealed class LayingSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = null!;
    [Dependency] private readonly StandingStateSystem _standing = null!;
    [Dependency] private readonly SharedGravitySystem _gravity = null!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = null!;
    [Dependency] private readonly SharedPopupSystem _popup = null!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = null!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.Laying, InputCmdHandler.FromDelegate(ToggleLaying))
            .Register<LayingSystem>();

        SubscribeLocalEvent<LayingComponent, RefreshMovementSpeedModifiersEvent>(RefreshMovementSpeed);
        SubscribeLocalEvent<LayingComponent, StandAttemptEvent>(OnCheckLegs);
        SubscribeLocalEvent<LayingComponent, StandingUpDoAfterEvent>(OnStandingUpComplete);
        SubscribeLocalEvent<LayingComponent, EntParentChangedMessage>(OnEntParentChanged);
    }

    private void ToggleLaying(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { } player || _gravity.IsWeightless(player))
            return;

        if (!HasComp<LayingComponent>(player))
        {
            if (HasComp<CanHostGuardianComponent>(player))
                AddComp<LayingComponent>(player);
        }

        if (!TryComp<LayingComponent>(player, out var laying))
            return;

        OnChangeLayingState(player, laying);
    }

    private void OnChangeLayingState(EntityUid uid, LayingComponent laying)
    {
        if (!TryComp(uid, out StandingStateComponent? standing) || !TryComp<InputMoverComponent>(uid, out var _))
            return;

        if (HasComp<KnockedDownComponent>(uid) || !_mobState.IsAlive(uid))
            return;

        if (_standing.IsDown(uid, standing))
        {
            TryStandUp(uid, laying, standing);
        }
        else
        {
            TryLieDown(uid, laying, standing);
        }
    }

    private void OnCheckLegs(Entity<LayingComponent> ent, ref StandAttemptEvent args)
    {
        if (!TryComp<BodyComponent>(ent, out var body))
            return;

        if (body.LegEntities.Count < body.RequiredLegs || body.LegEntities.Count == 0)
            args.Cancel();
    }

    private void RefreshMovementSpeed(EntityUid uid, LayingComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (_standing.IsDown(uid))
            args.ModifySpeed(0.4f, 0.4f);
    }

    public bool TryStandUp(EntityUid uid, LayingComponent? laying = null, StandingStateComponent? standing = null)
    {
        if (!Resolve(uid, ref standing, ref laying))
            return false;

        if (standing.Standing)
            return false;

        if (TryComp<PainComponent>(uid, out var pain) && pain.CurrentStage == PainStages.UnbeatablePain
            && !HasComp<PainNumbnessComponent>(uid))
        {
            _popup.PopupEntity(Loc.GetString("pain-try-stand-up"), uid, PopupType.MediumCaution);
            return false;
        }


        var args = new DoAfterArgs(EntityManager, uid, 2f, new StandingUpDoAfterEvent(), uid)
        {
            BreakOnHandChange = false,
            RequireCanInteract = false,
        };

        return _doAfter.TryStartDoAfter(args);
    }

    private void OnStandingUpComplete(EntityUid uid, LayingComponent component, StandingUpDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        Stand(uid);
    }

    private void OnEntParentChanged(EntityUid uid, LayingComponent component, EntParentChangedMessage args)
    {
        var transform = args.Transform;

        if (InSpace(uid, transform))
            Stand(uid);
    }

    private bool InSpace(EntityUid uid, TransformComponent? transform = null)
    {
        if (!Resolve(uid, ref transform))
            return false;

        if (transform.MapID == MapId.Nullspace)
            return false;

        return _gravity.IsWeightless(uid, xform: transform);
    }
    public void Stand(EntityUid uid)
    {
        if (!TryComp<StandingStateComponent>(uid, out var standing))
            return;

        _standing.Stand(uid, standing, force: true);
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    public bool TryLieDown(EntityUid uid, LayingComponent? laying = null, StandingStateComponent? standing = null)
    {
        if (!Resolve(uid, ref standing, ref laying))
            return false;

        if (standing.Standing != true)
            return false;

        _standing.Down(uid, true, false);
        _movement.RefreshMovementSpeedModifiers(uid);
        return true;
    }
}
