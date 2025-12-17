using Content.Shared._Horizon.Sponsors.Systems;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Horizon.Sponsors.Systems;

public sealed class SponsorObserverSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        var entity = args.Entity;

        if (EntityManager.TryGetComponent<MetaDataComponent>(entity, out var metaData) &&
            metaData.EntityPrototype != null &&
            _prototypeManager.TryIndex<EntityPrototype>(metaData.EntityPrototype.ID, out var prototype) &&
            prototype.ID is "MobObserver" or "AdminObserver")
        {
            if (EntityManager.TryGetComponent<SpriteComponent>(entity, out var spriteComponent))
            {
                var spriteOverrideSystem = EntityManager.EntitySysManager.GetEntitySystem<SpriteOverrideSystem>();
                var spriteSpecifier = spriteOverrideSystem.GetSpriteForPlayer(args.Player.Name);

                if (spriteSpecifier != null)
                {
                    switch (spriteSpecifier)
                    {
                        case SpriteSpecifier.Texture textureSpecifier:
                            spriteComponent.LayerSetSprite(0, textureSpecifier);
                            break;

                        case SpriteSpecifier.Rsi rsiSpecifier:
                            {
                                var rsiResource = _resCache.GetResource<RSIResource>(rsiSpecifier.RsiPath);
                                var rsi = rsiResource?.RSI;

                                if (rsi != null)
                                {
                                    spriteComponent.LayerSetState(0, "animated", rsi);
                                }
                                break;
                            }
                    }
                }
            }
        }
    }
}
