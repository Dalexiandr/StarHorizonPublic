using Content.Shared._Horizon.Cytology;
using Content.Shared._Horizon.Cytology.Components;
using Content.Shared._Horizon.Cytology.Systems;
using Robust.Client.GameObjects;
using System.Runtime.CompilerServices;
using Dependency = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Client._Horizon.Cytology.Swab;

public sealed class CytologySwabSystem : SharedCytologySwabSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CytologySwabComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<CytologySwabComponent> swab, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var swabSprite = (swab.Owner, args.Sprite);
        if (_sprite.LayerMapTryGet(swabSprite, CytologySwabVisualLayers.Sample, out var sampleLayer, false))
        {
            Appearance.TryGetData(swab.Owner, CytologySwabVisualStates.IsVisible, out bool isSampleVisible);
            _sprite.LayerSetVisible(swabSprite, sampleLayer, isSampleVisible);

            if (swab.Comp.TextureState != null)
            {
                _sprite.LayerSetRsiState(swabSprite, sampleLayer, swab.Comp.TextureState);
            }
        }
    }

}
