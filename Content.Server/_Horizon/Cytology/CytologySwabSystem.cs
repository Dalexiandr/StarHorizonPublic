using Content.Shared._Horizon.Cytology.Systems;
using Content.Shared._Horizon.Cytology.Components;
using Content.Shared._Horizon.Cytology.Prototypes;
using Content.Shared._Horizon.Cytology;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server._Horizon.Cytology;

public sealed class CytologySwabSystem : SharedCytologySwabSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CytologySwabComponent, CytologySwabTakeDirtDoAfterEvent>(OnTakeDirtDoAfter);
    }
    private void OnTakeDirtDoAfter(Entity<CytologySwabComponent> swab, ref CytologySwabTakeDirtDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (args.Target is not { } dirtUid)
            return;

        if (!TryComp<CytologySampleContainerComponent>(swab.Owner, out var swabSampleContainerComp) ||
            !TryComp<CytologyDirtComponent>(dirtUid, out var dirtComp))
            return;

        if (dirtComp.CurrentCellSamples.Count <= 0)
            return;

        var availableSpace = swabSampleContainerComp.MaxSamples - swabSampleContainerComp.CellSamples.Count;
        var collectedCells = dirtComp.CurrentCellSamples.Take(availableSpace).ToList();

        swabSampleContainerComp.CellSamples.AddRange(collectedCells);
        dirtComp.CurrentCellSamples.RemoveAll(x => collectedCells.Contains(x));

        if (collectedCells.Count > 0 && _prototypeManager.TryIndex<CellSamplePrototype>(collectedCells.Last().ProtoID, out var proto))
        {
            swab.Comp.TextureState = proto.TextureState;
            Appearance.SetData(swab.Owner, CytologySwabVisualStates.IsVisible, true);
        }

        PopupSystem.PopupClient(Loc.GetString("cytology-swab-collected", ("samples", collectedCells.Count)), args.Args.Target.Value, args.Args.User);
        DirtyField(swab.Owner, swab.Comp, nameof(swab.Comp.TextureState));
        DirtyField(swab.Owner, swabSampleContainerComp, nameof(swabSampleContainerComp.CellSamples));
        DirtyField(dirtUid, dirtComp, nameof(dirtComp.CurrentCellSamples));

        args.Handled = true;
    }
}
