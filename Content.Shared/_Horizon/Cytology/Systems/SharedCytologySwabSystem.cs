using Content.Shared._Horizon.Cytology.Components;
using Content.Shared._Horizon.Cytology.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared._Horizon.Cytology.Systems;

public abstract class SharedCytologySwabSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedCytologyPetriDishSystem _petriDishSystem = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CytologySwabComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CytologySwabComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnExamined(Entity<CytologySwabComponent> swab, ref ExaminedEvent args)
    {
        if (!TryComp<CytologySampleContainerComponent>(swab.Owner, out var swabSampleContainerComp))
            return;

        if (args.IsInDetailsRange)
        {
            if (swabSampleContainerComp.CellSamples.Count > 0)
                args.PushMarkup(Loc.GetString("cytology-swab-used", ("samples", swabSampleContainerComp.CellSamples.Count)));
            else
                args.PushMarkup(Loc.GetString("cytology-swab-unused"));
        }
    }
    private void OnAfterInteract(Entity<CytologySwabComponent> swab, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach)
            return;

        TryCollectCellsFromDirt(swab, args);

        _petriDishSystem.TryTransferCellsToPetriDish(swab.Owner, args.Target, args.User);
        if(TryComp<CytologySampleContainerComponent>(swab.Owner, out var swabSampleContainerComp))
        {
            Appearance.SetData(swab.Owner, CytologySwabVisualStates.IsVisible, swabSampleContainerComp.CellSamples.Count() > 0);
        }
    }

    private void TryCollectCellsFromDirt(Entity<CytologySwabComponent> swab, AfterInteractEvent args)
    {
        if (!TryComp<CytologyDirtComponent>(args.Target, out var dirt))
            return;

        if (!TryComp<CytologySampleContainerComponent>(swab.Owner, out var swabSampleContainerComp))
            return;

        if (dirt.CurrentCellSamples.Count <= 0)
        {
            PopupSystem.PopupClient(Loc.GetString("cytology-swab-no-samples"), args.Target.Value, args.User);
            return;
        }

        if (swabSampleContainerComp.CellSamples.Count >= swabSampleContainerComp.MaxSamples)
        {
            PopupSystem.PopupClient(Loc.GetString("cytology-swab-full"), swab.Owner, args.User);
            return;
        }

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, swab.Comp.SwabDelay, new CytologySwabTakeDirtDoAfterEvent(), swab.Owner, target: args.Target, used: swab.Owner)
        {
            Broadcast = false,
            BreakOnMove = true,
            NeedHand = true,
        });
    }
}
