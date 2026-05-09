using Content.Shared._Horizon.Cytology.Components;
using Robust.Shared.Random;
using System.Linq;
using Content.Shared.Examine;

namespace Content.Shared._Horizon.Cytology.Systems;

public sealed class CytologyDirtSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CytologyDirtComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CytologyDirtComponent, MapInitEvent>(OnMapInit);
    }

    private void OnExamined(Entity<CytologyDirtComponent> dirt, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if(dirt.Comp.CurrentCellSamples.Count > 0)
            args.PushMarkup(Loc.GetString("cytology-polluted"));
    }
    private void OnMapInit(Entity<CytologyDirtComponent> dirt, ref MapInitEvent args)
    {

        foreach (var cellSample in dirt.Comp.PossibleCellSamples)
        {
            if (dirt.Comp.CurrentCellSamples.Count >= dirt.Comp.MaxSamples) //If we have reached the limit that an object can store
                break;

            if (!_random.Prob(dirt.Comp.SampleChance)) //The chance with which the next cell will appear
                continue;

            dirt.Comp.CurrentCellSamples.Add(cellSample);
        }

        DirtyField(dirt.Owner, dirt.Comp, nameof(dirt.Comp.CurrentCellSamples)); //Predicted doesn't work here because random doesn't support it
    }
}
