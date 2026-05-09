using Content.Shared._Horizon._Fractions.AnCoDisposableFabricator;
using Robust.Client.GameObjects;

namespace Content.Client._Horizon._Fractions.AnCoDisposableFabricator;

public sealed class AnCoDisposableFabricatorVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnCoDisposableFabricatorVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, AnCoDisposableFabricatorVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<bool>(uid, AnCoDisposableFabricatorVisuals.IsWorking, out var isWorking, args.Component))
            return;

        if (!_sprite.LayerMapTryGet((uid, args.Sprite), AnCoDisposableFabricatorVisualLayers.IsWorking, out var layer, false))
            return;

        var state = isWorking ? component.WorkingState : component.IdleState;
        _sprite.LayerSetRsiState((uid, args.Sprite), layer, state);
    }
}
