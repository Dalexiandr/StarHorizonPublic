using Content.Shared._Horizon.Cytology.Components;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared._Horizon.Cytology.Prototypes;
using Robust.Shared.Prototypes;
using Content.Shared.Verbs;
using System.Linq;
using System.Numerics;

namespace Content.Shared._Horizon.Cytology.Systems;

public abstract class SharedCytologyPetriDishSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CytologyPetriDishComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CytologyPetriDishComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);
    }

    private void OnGetVerb(Entity<CytologyPetriDishComponent> petriDish, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract || args.Hands == null)
            return;

        AlternativeVerb verb = new()
        {
            Act = () => ClearSamples(petriDish),
            Text = Loc.GetString("verb-split-samples")
        };

        args.Verbs.Add(verb);

    }

    private void OnExamined(Entity<CytologyPetriDishComponent> petriDish, ref ExaminedEvent args)
    {
        if (!TryComp<CytologySampleContainerComponent>(petriDish.Owner, out var petriDishSampleContainerComp))
            return;

        if (args.IsInDetailsRange)
        {
            if (petriDishSampleContainerComp.CellSamples.Count > 0)
                args.PushMarkup(Loc.GetString("cytology-dish-used", ("samples", petriDishSampleContainerComp.CellSamples.Count)));
            else
                args.PushMarkup(Loc.GetString("cytology-dish-unused"));
        }
    }

    public void ClearSamples(Entity<CytologyPetriDishComponent> petriDish) //TODO оно есть, потому-что, может быть, уборщик будет иметь возможность чистить объекты
    {
        if (!TryComp<CytologySampleContainerComponent>(petriDish.Owner, out var petriDishSampleContainerComp))
            return;

        petriDishSampleContainerComp.CellSamples.Clear();
        PetriDishUpdateAppearance(petriDish.Owner);
    }

    public void PetriDishUpdateAppearance(EntityUid petriDish)
    {
        if (!TryComp<CytologySampleContainerComponent>(petriDish, out var petriDishSampleContainerComp))
            return;

        Appearance.SetData(petriDish, CytologyPetriDishVisualStates.Color, CalculateAverageCellSampleColor(petriDishSampleContainerComp.CellSamples));
        Appearance.SetData(petriDish, CytologyPetriDishVisualStates.Samples, petriDishSampleContainerComp.CellSamples.Count);
    }

    private Color CalculateAverageCellSampleColor(List<CellSample> cellSamples) //Calculates the overall color based on the samples inside
    {
        if (cellSamples.Count == 0)
            return Color.White;

        var colorSum = Vector3.Zero;
        var totalSamples = cellSamples.Count;

        foreach (var sample in cellSamples)
        {
            if (!_prototypeManager.TryIndex<CellSamplePrototype>(sample.ProtoID, out var proto))
                continue;

            var sampleColor = GetColorFromTextureState(proto.TextureState);
            var colorVector = new Vector3(sampleColor.R, sampleColor.G, sampleColor.B);
            colorSum += colorVector;
        }

        if (totalSamples == 0)
            return Color.White;

        var averageColorVector = colorSum / totalSamples;
        return new Color(
            Math.Clamp(averageColorVector.X, 0f, 1f),
            Math.Clamp(averageColorVector.Y, 0f, 1f),
            Math.Clamp(averageColorVector.Z, 0f, 1f),
            1f
        );
    }

    private Color GetColorFromTextureState(string? textureState)
    {
        return textureState switch
        {
            "black" => Color.Black,
            "yellow" => Color.Yellow,
            "green" => Color.Green,
            "brown" => Color.Brown,
            "violet" => Color.Purple,
            _ => Color.White
        };
    }

    public bool TryTransferCellsToPetriDish(EntityUid transferDevice, EntityUid? petriDish, EntityUid user)
    {

        if (petriDish is not { } petriDishUid)
            return false;

        if (!TryComp<CytologySampleContainerComponent>(transferDevice, out var transferDeviceSampleContainerComp) ||
            !TryComp<CytologySampleContainerComponent>(petriDishUid, out var petriDishSampleContainerComp))
            return false;

        var availableSpace = petriDishSampleContainerComp.MaxSamples - petriDishSampleContainerComp.CellSamples.Count();
        if(availableSpace <= 0)
        {
            _popupSystem.PopupClient(Loc.GetString("cytology-petri-dish-is-full"), petriDishUid, user);
            return false;
        }
        var collectedCells = transferDeviceSampleContainerComp.CellSamples
            .Take(availableSpace)
            .ToList();


        petriDishSampleContainerComp.CellSamples.AddRange(collectedCells);

        transferDeviceSampleContainerComp.CellSamples.RemoveAll(x => collectedCells.Contains(x));

        PetriDishUpdateAppearance(petriDishUid);

        return true;
    }
}
