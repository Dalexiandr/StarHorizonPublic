using Content.Shared._Horizon._Fractions.AnCo;
using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Server._Horizon._Fractions.AnCo;

public sealed class AnCoSabreSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnCoSabreComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<AnCoSabreRecallEvent>(OnRecall);
    }

    private void OnUseInHand(EntityUid uid, AnCoSabreComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (component.BoundOwner != null && component.BoundOwner != args.User)
        {
            _popup.PopupEntity(Loc.GetString("anco-sabre-already-bound"), args.User, args.User);
            return;
        }

        if (component.BoundOwner == null)
        {
            component.BoundOwner = args.User;
            _actions.AddAction(args.User, ref component.RecallActionEntity, component.RecallAction);
            _popup.PopupEntity(Loc.GetString("anco-sabre-bound"), args.User, args.User);
            Dirty(uid, component);
            args.Handled = true;
        }
    }

    private void OnRecall(AnCoSabreRecallEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;

        var query = EntityQueryEnumerator<AnCoSabreComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.BoundOwner != user)
                continue;

            if (_hands.IsHolding(user, uid, out _))
            {
                _popup.PopupEntity(Loc.GetString("anco-sabre-already-held"), user, user);
                args.Handled = true;
                return;
            }

            if (_hands.TryPickupAnyHand(user, uid))
            {
                _popup.PopupEntity(Loc.GetString("anco-sabre-recalled"), user, user);
                args.Handled = true;
                return;
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("anco-sabre-recall-failed"), user, user);
                args.Handled = true;
                return;
            }
        }
    }
}
