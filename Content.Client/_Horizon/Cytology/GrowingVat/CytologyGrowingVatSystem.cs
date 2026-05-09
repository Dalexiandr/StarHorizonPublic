using Content.Shared._Horizon.Cytology;
using Content.Shared._Horizon.Cytology.Components;
using Content.Shared._Horizon.Cytology.Systems;
using Content.Shared.Power;
using Robust.Shared.Prototypes;
using Robust.Client.GameObjects;
using Content.Client.Power;

namespace Content.Client._Horizon.Cytology.GrowingVat;

public sealed class CytologyGrowingVatSystem : SharedCytologyGrowingVatSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CytologyGrowingVatComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }


    private void OnAppearanceChange(Entity<CytologyGrowingVatComponent> growingVat, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateIndicatorLayer(growingVat, args.Sprite, args.Component);
        UpdateLiquidLayer(growingVat, args.Sprite, args.Component);
    }

    private void UpdateLiquidLayer(Entity<CytologyGrowingVatComponent> growingVat, SpriteComponent sprite, AppearanceComponent appearance)
    {
        var growingVatSprite = (growingVat.Owner, sprite);
        if (_sprite.LayerMapTryGet(growingVatSprite, CytologyGrowingVatVisualLayers.Liquid, out var liquidLayer, false))
        {

            if (TryGetSolutionFromBeaker(growingVat.Owner, out var beakerSolution, out _) || beakerSolution?.Volume <= 0)
            {
                _sprite.LayerSetVisible(growingVatSprite, liquidLayer, true);

                var averageColor = beakerSolution.GetColor(_prototypeManager);
                _sprite.LayerSetColor(growingVatSprite, liquidLayer, averageColor);
            }
            else _sprite.LayerSetVisible(growingVatSprite, liquidLayer, false);
        }

        if (_sprite.LayerMapTryGet(growingVatSprite, CytologyGrowingVatVisualLayers.Foam, out var foamLayer, false))
        {
            if (Appearance.TryGetData(growingVat.Owner, CytologyGrowingVatVisualStates.WithFoam, out bool isFoamVisible))
            {
                _sprite.LayerSetVisible(growingVatSprite, foamLayer, isFoamVisible);
            }
        }
    }

    private void UpdateIndicatorLayer(Entity<CytologyGrowingVatComponent> growingVat, SpriteComponent sprite, AppearanceComponent appearance)
    {
        var growingVatSprite = (growingVat.Owner, sprite);

        var powered = Appearance.TryGetData<bool>(growingVat.Owner, PowerDeviceVisuals.Powered, out var powerData, appearance) && powerData;

        if (_sprite.LayerMapTryGet(growingVatSprite, PowerDeviceVisualLayers.Powered, out var powerLayer, false))
        {
            _sprite.LayerSetVisible(growingVatSprite, powerLayer, powered);

            if (_sprite.LayerMapTryGet(growingVatSprite, CytologyGrowingVatVisualLayers.Indicator, out var indicatorLayer, false))
            {
                var shouldShowIndicator = powered && growingVat.Comp.IsActive;
                var state = growingVat.Comp.StopWithError ? "red" : "green";

                _sprite.LayerSetVisible(growingVatSprite, indicatorLayer, shouldShowIndicator);
                _sprite.LayerSetRsiState(growingVatSprite, indicatorLayer, state);
            }
        }
    }
}
