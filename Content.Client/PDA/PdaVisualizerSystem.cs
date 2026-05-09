using Content.Client._Horizon.WorldItem;
using Content.Shared.Light;
using Content.Shared.PDA;
using Robust.Client.GameObjects;

namespace Content.Client.PDA;

public sealed class PdaVisualizerSystem : VisualizerSystem<PdaVisualsComponent>
{
    [Dependency] private readonly WorldItemSystem _worldItemSystem = null!;

    protected override void OnAppearanceChange(EntityUid uid, PdaVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // _HORIZON STARTS
        if (AppearanceSystem.TryGetData<string>(uid, PdaVisuals.PdaType, out var pdaType, args.Component))
        {
            if (_worldItemSystem.GetWorldState(uid, out var prefix, out _))
                args.Sprite.LayerSetState(PdaVisualLayers.Base, pdaType + prefix);
            else
                args.Sprite.LayerSetState(PdaVisualLayers.Base, pdaType);
        }

        if (AppearanceSystem.TryGetData<bool>(uid,
                UnpoweredFlashlightVisuals.LightOn,
                out var isFlashlightOn,
                args.Component))
        {
            var layer = args.Sprite.LayerMapGet(PdaVisualLayers.Flashlight);
            if (_worldItemSystem.GetWorldState(uid, out var prefix, out var defaultStates))
            {
                args.Sprite.LayerSetState(PdaVisualLayers.Flashlight, defaultStates[layer] + prefix);
                args.Sprite.LayerSetVisible(PdaVisualLayers.Flashlight, isFlashlightOn);
            }
            else if (defaultStates is not null)
            {
                args.Sprite.LayerSetState(PdaVisualLayers.Flashlight, defaultStates[layer]);
                args.Sprite.LayerSetVisible(PdaVisualLayers.Flashlight, isFlashlightOn);
            }
        }

        // ReSharper disable once InvertIf
        if (AppearanceSystem.TryGetData<bool>(uid, PdaVisuals.IdCardInserted, out var isCardInserted, args.Component))
        {
            var layer = args.Sprite.LayerMapGet(PdaVisualLayers.IdLight);
            if (_worldItemSystem.GetWorldState(uid, out var prefix, out var defaultStates))
            {
                args.Sprite.LayerSetState(PdaVisualLayers.IdLight, defaultStates[layer] + prefix);
                args.Sprite.LayerSetVisible(PdaVisualLayers.IdLight, isCardInserted);
            }
            else if (defaultStates is not null)
            {
                args.Sprite.LayerSetState(PdaVisualLayers.IdLight, defaultStates[layer]);
                args.Sprite.LayerSetVisible(PdaVisualLayers.IdLight, isCardInserted);
            }
        }
        // _HORIZON ENDS
    }

    public enum PdaVisualLayers : byte
    {
        Base,
        Flashlight,
        IdLight,
    }
}
