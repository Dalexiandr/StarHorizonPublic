using Robust.Client.GameObjects;
using Content.Shared.Mech.EntitySystems;
using Content.Shared._Horizon.Mech.Components;

namespace Content.Client._Horizon.Mech;

public sealed class MechPhazeVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechPhazeComponent, AppearanceChangeEvent>(OnAppearanceChange);

    }

    private void OnAppearanceChange(EntityUid uid, MechPhazeComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<bool>(uid, MechPhazingVisuals.Phazing, out var phaze, args.Component)
            || !args.Sprite.LayerMapTryGet(MechPhazingVisuals.Phazing, out var layer))
            return;

        args.Sprite.LayerSetVisible(layer, phaze);
    }
}
