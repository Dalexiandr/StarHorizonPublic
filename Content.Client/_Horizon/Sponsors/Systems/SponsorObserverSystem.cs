using Content.Shared._Horizon.GhostSprites;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client._Horizon.Sponsors.Systems;

public sealed class SponsorObserverSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        var entity = args.Entity;

        if (!EntityManager.TryGetComponent<MetaDataComponent>(entity, out var metaData) ||
            metaData.EntityPrototype == null ||
            !_prototypeManager.TryIndex<EntityPrototype>(metaData.EntityPrototype.ID, out var prototype) ||
            prototype.ID is not ("MobObserver" or "AdminObserver"))
            return;

        if (!EntityManager.TryGetComponent<SpriteComponent>(entity, out var spriteComponent))
            return;

        // Try to find individual ghost sprite for this player
        var playerName = args.Player.Name;
        var individualSprite = FindIndividualGhostSprite(playerName);
        if (individualSprite == null)
            return;

        ApplyGhostSprite(entity, spriteComponent, individualSprite);
    }

    /// <summary>
    /// Finds an individual ghost sprite prototype for the given player.
    /// </summary>
    private GhostSpritePrototype? FindIndividualGhostSprite(string playerName)
    {
        foreach (var proto in _prototypeManager.EnumeratePrototypes<GhostSpritePrototype>())
        {
            if (proto.IsIndividual && proto.AllowedPlayers.Contains(playerName))
                return proto;
        }
        return null;
    }

    private void ApplyGhostSprite(EntityUid entity, SpriteComponent spriteComponent, GhostSpritePrototype prototype)
    {
        var path = SpriteSpecifierSerializer.TextureRoot / prototype.RsiPath;
        if (!_resCache.TryGetResource<RSIResource>(path, out var rsiResource) || rsiResource.RSI == null)
            return;

        var rsi = rsiResource.RSI;
        var stateId = prototype.State;
        if (!rsi.TryGetState(stateId, out _))
            return;

        var sprite = (entity, spriteComponent);
        _spriteSystem.LayerSetRsi(sprite, 0, rsi, stateId);
    }
}
