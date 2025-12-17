using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared._Horizon.CursedKatana;
using Content.Shared.Inventory;
using Content.Shared.Speech;
using Content.Shared.Inventory.Events;

namespace Content.Server._Horizon.CursedKatana;

public sealed class DemonMaskSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CursedUserComponent, RecallCursedKatanaEvent>(OnRecallKatana);
        SubscribeLocalEvent<DemonMaskComponent, GotEquippedEvent>(OnMaskEquipped);
        SubscribeLocalEvent<DemonMaskComponent, GotUnequippedEvent>(OnMaskUnequipped);
    }

    private void OnMaskEquipped(EntityUid uid, DemonMaskComponent component, GotEquippedEvent args)
    {
        if (args.SlotFlags != SlotFlags.MASK)
            return;

        SaveMask(uid, component, args.Equipee);
    }

    private void OnMaskUnequipped(EntityUid uid, DemonMaskComponent component, GotUnequippedEvent args)
    {
        if (args.SlotFlags != SlotFlags.MASK)
            return;

        RemoveMask(uid, component, args.Equipee);
    }

    private void SaveMask(EntityUid maskUid, DemonMaskComponent maskComp, EntityUid ownerUid)
    {
        if (TryComp<SpeechComponent>(ownerUid, out var speechComp))
        {
            maskComp.OriginalSpeechSounds = speechComp.SpeechSounds;
            speechComp.SpeechSounds = "CursedSpech";
        }

        _actionSystem.AddAction(ownerUid, ref maskComp.RecallCursedKatanaActionEntity, maskComp.RecallCursedKatanaAction);
    }

    private void RemoveMask(EntityUid maskUid, DemonMaskComponent maskComp, EntityUid ownerUid)
    {
        if (TryComp<SpeechComponent>(ownerUid, out var speechComp) && maskComp.OriginalSpeechSounds.HasValue)
        {
            speechComp.SpeechSounds = maskComp.OriginalSpeechSounds;
        }

        _actionSystem.RemoveAction(ownerUid, maskComp.RecallCursedKatanaActionEntity);
    }

    private void OnRecallKatana(EntityUid ownerUid, CursedUserComponent component, RecallCursedKatanaEvent args)
    {
        if (TryComp<CursedUserComponent>(ownerUid, out var ownerComp) && TryComp<DemonMaskComponent>(ownerComp.MaskUid, out var maskComp))
        {
            if (component.KatanaUid == null)
                return;

            var user = args.Performer;
            var katana = component.KatanaUid.Value;

            _hands.TryPickupAnyHand(user, katana);
            _popupSystem.PopupEntity("Проклятая катана появляется в руках.", user, user);
            args.Handled = true;
        }
    }
}
