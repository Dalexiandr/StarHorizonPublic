using Content.Shared._Horizon.Cytology;
using Content.Shared._Horizon.Cytology.Components;
using Content.Shared._Horizon.Cytology.Prototypes;
using Content.Shared._Horizon.Cytology.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using System.Runtime.CompilerServices;
using Dependency = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Client._Horizon.Cytology.PetriDish;

public sealed class CytologyPetriDishSystem : SharedCytologyPetriDishSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CytologyPetriDishComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<CytologyPetriDishComponent> petriDish, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!TryComp<CytologySampleContainerComponent>(petriDish.Owner, out var petriDishSampleContainerComp))
            return;

        var petriDishSprite = (petriDish.Owner, args.Sprite);

        if (_sprite.LayerMapTryGet(petriDishSprite, CytologyPetriDishVisualLayers.Fill, out var fillLayer, false))
        {
            var hasSamples = petriDishSampleContainerComp.CellSamples.Count > 0;
            _sprite.LayerSetVisible(petriDishSprite, fillLayer, hasSamples);

            if (hasSamples)
            {
                Appearance.TryGetData(petriDish.Owner, CytologyPetriDishVisualStates.Color, out Color fillColor);
                _sprite.LayerSetColor(petriDishSprite, fillLayer, fillColor);
            }

            if (_sprite.LayerMapTryGet(petriDishSprite, CytologyPetriDishVisualLayers.Foam, out var foamLayer, false))
            {
                _sprite.LayerSetVisible(petriDishSprite, foamLayer, hasSamples);
            }
        }
    }
}
