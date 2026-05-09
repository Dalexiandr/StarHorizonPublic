using Content.Server.Humanoid;
using Content.Server.Humanoid.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Localization;

namespace Content.Server._Horizon.Humanoid;

public sealed class FixedAppearanceSystem : EntitySystem
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly GrammarSystem _grammarSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FixedAppearanceComponent, MapInitEvent>(OnMapInit,
            after: new []{ typeof(RandomHumanoidAppearanceSystem) });
    }

    private void OnMapInit(EntityUid uid, FixedAppearanceComponent component, MapInitEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        bool changed = false;
        if (!string.IsNullOrEmpty(component.Species))
        {
            if (_prototypeManager.HasIndex<SpeciesPrototype>(component.Species))
            {
                if (humanoid.Species != component.Species)
                {
                    _humanoid.SetSpecies(uid, component.Species, false, humanoid);
                    changed = true;
                }
            }
        }

        if (component.Sex.HasValue)
        {
            if (humanoid.Sex != component.Sex.Value)
            {
                _humanoid.SetSex(uid, component.Sex.Value, false, humanoid);
                changed = true;
            }
            var newGender = component.Gender ?? component.Sex.Value switch
            {
                Sex.Male => Gender.Male,
                Sex.Female => Gender.Female,
                _ => Gender.Epicene
            };

            if (humanoid.Gender != newGender)
            {
                humanoid.Gender = newGender;
                changed = true;
            }
            if (TryComp<GrammarComponent>(uid, out var grammar))
            {
                _grammarSystem.SetGender((uid, grammar), newGender);
            }
        }
        else if (component.Gender.HasValue)
        {
            if (humanoid.Gender != component.Gender.Value)
            {
                humanoid.Gender = component.Gender.Value;
                changed = true;
            }
            if (TryComp<GrammarComponent>(uid, out var grammar))
            {
                _grammarSystem.SetGender((uid, grammar), component.Gender.Value);
            }
        }

        if (component.SkinColor.HasValue && humanoid.SkinColor != component.SkinColor.Value)
        {
            _humanoid.SetSkinColor(uid, component.SkinColor.Value, false, true, humanoid);
            changed = true;
        }

        if (component.EyeColor.HasValue && humanoid.EyeColor != component.EyeColor.Value)
        {
            humanoid.EyeColor = component.EyeColor.Value;
            changed = true;
        }

        if (component.Age.HasValue && humanoid.Age != component.Age.Value)
        {
            humanoid.Age = component.Age.Value;
            changed = true;
        }
        if (component.Markings != null && component.Markings.Count > 0)
        {
            foreach (var markingCategory in component.Markings.Keys)
            {
                humanoid.MarkingSet.RemoveCategory(markingCategory);
            }
            foreach (var (category, markings) in component.Markings)
            {
                foreach (var marking in markings)
                {
                    if (!_markingManager.Markings.TryGetValue(marking.MarkingId, out var markingPrototype))
                        continue;
                    if (!_markingManager.CanBeApplied(humanoid.Species, humanoid.Sex, markingPrototype, _prototypeManager))
                        continue;
                    var colors = marking.Colors.Count > 0 ? marking.Colors : new List<Color> { Color.Black };
                    _humanoid.AddMarking(uid, marking.MarkingId, colors, false);
                }
            }

            changed = true;
        }
        if (!string.IsNullOrEmpty(component.HairStyleId))
        {
            if (_markingManager.Markings.TryGetValue(component.HairStyleId, out var hairMarking) &&
                _markingManager.CanBeApplied(humanoid.Species, humanoid.Sex, hairMarking, _prototypeManager))
            {
                humanoid.MarkingSet.RemoveCategory(MarkingCategories.Hair);
                var hairColor = component.HairColor ?? Color.Black;
                _humanoid.AddMarking(uid, component.HairStyleId, hairColor, false);
                changed = true;
            }
        }


        if (!string.IsNullOrEmpty(component.FacialHairStyleId))
        {
            if (_markingManager.Markings.TryGetValue(component.FacialHairStyleId, out var facialHairMarking) &&
                _markingManager.CanBeApplied(humanoid.Species, humanoid.Sex, facialHairMarking, _prototypeManager))
            {
                humanoid.MarkingSet.RemoveCategory(MarkingCategories.FacialHair);
                var facialHairColor = component.FacialHairColor ?? Color.Black;
                _humanoid.AddMarking(uid, component.FacialHairStyleId, facialHairColor, false);
                changed = true;
            }
        }

        if (changed)
        {
            Dirty(uid, humanoid);
        }
    }
}
