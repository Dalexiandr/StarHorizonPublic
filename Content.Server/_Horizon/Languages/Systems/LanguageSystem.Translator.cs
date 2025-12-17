using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Shared.Interaction;
using Content.Shared._Horizon.Language;
using Content.Shared.PowerCell;
using Content.Shared.Interaction.Events;
using Content.Shared.Hands;
using Content.Shared.Toggleable;

namespace Content.Server._Horizon.Language;

public sealed partial class LanguageSystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    private void InitializeTranslator()
    {
        SubscribeLocalEvent<HandheldTranslatorComponent, ActivateInWorldEvent>(OnTranslatorActivateInWorld);
        SubscribeLocalEvent<HandheldTranslatorComponent, UseInHandEvent>(OnTranslatorUseInHand);

        SubscribeLocalEvent<HandheldTranslatorComponent, GotEquippedHandEvent>(OnPickUp);
        SubscribeLocalEvent<HandheldTranslatorComponent, GotUnequippedHandEvent>(OnDrop);

        SubscribeLocalEvent<HandheldTranslatorComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
    }

    private void OnTranslatorActivateInWorld(EntityUid translator, HandheldTranslatorComponent component, ActivateInWorldEvent args)
    {
        if (!component.ToggleOnInteract)
            return;
        Dirty(translator, component);

        ToggleTranslator(translator);

        UpdateUi(args.User);
    }

    private void OnTranslatorUseInHand(EntityUid translator, HandheldTranslatorComponent component, UseInHandEvent args)
    {
        if (!component.ToggleOnInteract)
            return;
        Dirty(translator, component);

        ToggleTranslator(translator);
        component.User = component.Enabled ? GetNetEntity(args.User) : null;

        UpdateUi(args.User);
    }

    private void OnPickUp(EntityUid translator, HandheldTranslatorComponent component, GotEquippedHandEvent args)
    {
        Dirty(translator, component);

        component.User = GetNetEntity(args.User);

        UpdateUi(args.User);
    }

    private void OnDrop(EntityUid translator, HandheldTranslatorComponent component, GotUnequippedHandEvent args)
    {
        Dirty(translator, component);

        if (component.User.HasValue)
            SelectDefaultLanguage(GetEntity(component.User.Value));

        component.User = null;

        UpdateUi(args.User);
    }

    private void ToggleTranslator(EntityUid uid, HandheldTranslatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var hasPower = _powerCell.HasDrawCharge(uid);

        if (hasPower)
        {
            component.Enabled = !component.Enabled;
            var popupMessage = Loc.GetString(component.Enabled ? "translator-component-turnon" : "translator-component-shutoff", ("translator", uid));
            _popup.PopupEntity(popupMessage, uid);
            if (!component.Enabled && component.User.HasValue)
                SelectDefaultLanguage(GetEntity(component.User.Value));
        }

        Dirty(uid, component);
        _appearance.SetData(uid, ToggleableVisuals.Enabled, component.Enabled);
    }
    private void OnPowerCellSlotEmpty(EntityUid translator, HandheldTranslatorComponent component, PowerCellSlotEmptyEvent args)
    {
        component.Enabled = false;

        component.User = null;

        Dirty(translator, component);
        _appearance.SetData(translator, ToggleableVisuals.Enabled, component.Enabled);
    }
}
