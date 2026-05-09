using Content.Shared._Horizon.FoodBoost;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Horizon.FoodBoost;

public sealed class FoodBoostSystem : SharedFoodBoostSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private SpriteSpecifier _defaultSparks = new SpriteSpecifier.Rsi(new ResPath("_Horizon/Objects/Misc/food-sparks.rsi"), "sparks-silv");
    private SpriteSpecifier _superSparks = new SpriteSpecifier.Rsi(new ResPath("_Horizon/Objects/Misc/food-sparks.rsi"), "sparks-gold");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrantBoostOnConsumeComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(Entity<GrantBoostOnConsumeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!_sprite.LayerMapTryGet(ent.Owner, "food-sparks", out var layer, false))
            layer = _sprite.LayerMapReserve(ent.Owner, "food-sparks");

        _sprite.LayerSetSprite(ent.Owner, layer, ent.Comp.Advanced ? _superSparks : _defaultSparks);
    }
}
