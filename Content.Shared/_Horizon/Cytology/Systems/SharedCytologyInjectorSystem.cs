using Content.Shared._Horizon.Cytology.Components;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects;
using System.Linq;


namespace Content.Shared._Horizon.Cytology.Systems;

public sealed class SharedCytologyInjectorSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedCytologyPetriDishSystem _petriDishSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CytologyInjectorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CytologyInjectorComponent, CytologyInjectorTakeDoAfterEvent>(OnTakeDoAfter);
    }

    private void OnAfterInteract(Entity<CytologyInjectorComponent> injector, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach)
            return;

        TryCollectCellsFromCreature(injector, args);
        // or this or this
        _petriDishSystem.TryTransferCellsToPetriDish(injector.Owner, args.Target, args.User);

        if (TryComp<CytologySampleContainerComponent>(injector.Owner, out var injectorSampleContainerComp))
        {
            _appearance.SetData(injector.Owner, CytologyInjectorVisualStates.HasSamples, injectorSampleContainerComp.CellSamples.Count() > 0);
        }
    }

    private void TryCollectCellsFromCreature(Entity<CytologyInjectorComponent> injector, AfterInteractEvent args)
    {
        if (!TryComp<SampleSourceComponent>(args.Target, out var sampleSourceComp))
            return;

        if (!TryComp<CytologySampleContainerComponent>(injector.Owner, out var injectorSampleContainerComp))
            return;

        if (sampleSourceComp.AvailableCellSamples == null)
            return;

        if (injectorSampleContainerComp.CellSamples.Count >= injectorSampleContainerComp.MaxSamples)
        {
            _popupSystem.PopupClient(Loc.GetString("cytology-injector-full"), injector.Owner, args.User);
            return;
        }

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, injector.Comp.TakeDelay, new CytologyInjectorTakeDoAfterEvent(), injector.Owner, target: args.Target, used: injector.Owner)
        {
            Broadcast = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void OnTakeDoAfter(Entity<CytologyInjectorComponent> injector, ref CytologyInjectorTakeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        EntityUid target = args.Args.Target.Value;

        if (!TryComp<SampleSourceComponent>(target, out var sampleSourceComp))
            return;

        if (!TryComp<CytologySampleContainerComponent>(injector.Owner, out var injectorSampleContainerComp))
            return;


        var availableSpace = injectorSampleContainerComp.MaxSamples - injectorSampleContainerComp.CellSamples.Count();

        var collectedCells = sampleSourceComp.AvailableCellSamples
            .Take(availableSpace)
            .Select(cell => cell.Clone())
            .ToList();

        collectedCells.ForEach(cell => SetHumanoidData(target, cell));

        injectorSampleContainerComp.CellSamples.AddRange(collectedCells);

        DirtyField(injector.Owner, injectorSampleContainerComp, nameof(injectorSampleContainerComp.CellSamples));

        _popupSystem.PopupClient(Loc.GetString("cytology-injector-collected"), target, args.Args.User);

        _appearance.SetData(injector.Owner, CytologyInjectorVisualStates.HasSamples, injectorSampleContainerComp.CellSamples.Count() > 0);

        args.Handled = true;
    }

    private void SetHumanoidData(EntityUid target, CellSample cell)
    {

        var profile = GetProfileFromEntity(target);
        if (profile != null)
        {
            profile.Age = 18; //It's should be "0" but we don't feed predators
            profile.Name = HumanoidCharacterProfile.GetName(profile.Species, profile.Gender); //Take new random name
        }

        cell.StoredProfile = profile;
    }

    private HumanoidCharacterProfile? GetProfileFromEntity(EntityUid target) //This should be in API but I don't find any. Maybe it's I blind
    {
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoidAppearance))
            return null;

        var characterAppearance = GetCharacterAppearanceFromHumanoidAppearance(humanoidAppearance);

        return HumanoidCharacterProfile.DefaultWithSpecies(humanoidAppearance.Species)
            .WithName(Name(target))
            .WithAge(humanoidAppearance.Age)
            .WithSex(humanoidAppearance.Sex)
            .WithGender(humanoidAppearance.Gender)
            .WithCharacterAppearance(characterAppearance)
            .WithBarkPitch(humanoidAppearance.Bark.Pitch)
            .WithBarkProto(humanoidAppearance.Bark.Proto);
    }

    private HumanoidCharacterAppearance GetCharacterAppearanceFromHumanoidAppearance(HumanoidAppearanceComponent humanoidAppearance) //This should be in API but I don't find any. Maybe it's I blind
    {
        // Get hair from MarkingSet
        var hairStyleId = HairStyles.DefaultHairStyle;
        var hairColor = Color.Black;
        if (humanoidAppearance.MarkingSet.TryGetCategory(MarkingCategories.Hair, out var hairMarkings) && hairMarkings.Count > 0)
        {
            var hair = hairMarkings[0];
            hairStyleId = hair.MarkingId;
            hairColor = hair.MarkingColors.Count > 0 ? hair.MarkingColors[0] : Color.Black;
        }

        // Get facial hair from MarkingSet
        var facialHairStyleId = HairStyles.DefaultFacialHairStyle;
        var facialHairColor = Color.Black;
        if (humanoidAppearance.MarkingSet.TryGetCategory(MarkingCategories.FacialHair, out var facialHairMarkings) && facialHairMarkings.Count > 0)
        {
            var facialHair = facialHairMarkings[0];
            facialHairStyleId = facialHair.MarkingId;
            facialHairColor = facialHair.MarkingColors.Count > 0 ? facialHair.MarkingColors[0] : Color.Black;
        }

        // Get Other markings from MarkingSet
        var otherMarkings = new List<Marking>();
        foreach (var (category, markings) in humanoidAppearance.MarkingSet.Markings)
        {
            if (category == MarkingCategories.Hair || category == MarkingCategories.FacialHair)
                continue;

            otherMarkings.AddRange(markings);
        }

        return new HumanoidCharacterAppearance(
            hairStyleId,
            hairColor,
            facialHairStyleId,
            facialHairColor,
            humanoidAppearance.EyeColor,
            humanoidAppearance.SkinColor,
            otherMarkings
        );
    }

}
