using Content.Shared.Implants.Components;
using Robust.Shared.Containers;
using Content.Shared.Implants;

namespace Content.Shared._Horizon.Language;

public abstract partial class SharedLanguageSystem
{
    private void InitializeImplant()
    {
        SubscribeLocalEvent<TranslatorImplantComponent, GetLanguagesEvent>(OnGetLanguages);
        SubscribeLocalEvent<TranslatorImplantComponent, ImplantImplantedEvent>(OnImplanted);
        SubscribeLocalEvent<TranslatorImplantComponent, EntGotRemovedFromContainerMessage>(OnUnimplanted);
        SubscribeLocalEvent<TranslatorImplantComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
    }

    private void OnGetLanguages(EntityUid uid, TranslatorImplantComponent component, ref GetLanguagesEvent args)
    {
        foreach (var (key, value) in component.Languages)
        {
            if (args.Translator.TryGetValue(key, out var currentKnowledge) && currentKnowledge < value)
                args.Translator[key] = value;
            else
                args.Translator.Add(key, value);
        }
    }

    private void OnImplanted(EntityUid uid, TranslatorImplantComponent comp, ref ImplantImplantedEvent args)
    {
        if (!args.Implanted.HasValue)
            return;

        UpdateUi(args.Implanted.Value);
        comp.ImplantedEntity = args.Implanted;
    }

    private void OnUnimplanted(EntityUid uid, TranslatorImplantComponent comp, ref EntGotRemovedFromContainerMessage args)
    {
        if (!comp.ImplantedEntity.HasValue)
            return;

        UpdateUi(comp.ImplantedEntity.Value);
        comp.ImplantedEntity = null;
    }
    private void OnRemoveAttempt(EntityUid uid, TranslatorImplantComponent component, ContainerGettingRemovedAttemptEvent args)
    {
        if (component.Permanent && component.ImplantedEntity != null)
            args.Cancel();
    }
}
