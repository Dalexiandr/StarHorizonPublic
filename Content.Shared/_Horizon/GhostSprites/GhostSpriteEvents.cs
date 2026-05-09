using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.GhostSprites;

/// <summary>
/// Client requests to change their ghost sprite.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangeGhostSpriteRequestEvent : EntityEventArgs
{
    public ProtoId<GhostSpritePrototype> SpriteId { get; }

    public ChangeGhostSpriteRequestEvent(ProtoId<GhostSpritePrototype> spriteId)
    {
        SpriteId = spriteId;
    }
}

/// <summary>
/// Server notifies client that their ghost sprite has been changed.
/// </summary>
[Serializable, NetSerializable]
public sealed class GhostSpriteChangedEvent : EntityEventArgs
{
    public NetEntity Ghost { get; }
    public ProtoId<GhostSpritePrototype> SpriteId { get; }

    public GhostSpriteChangedEvent(NetEntity ghost, ProtoId<GhostSpritePrototype> spriteId)
    {
        Ghost = ghost;
        SpriteId = spriteId;
    }
}

/// <summary>
/// Client requests the list of available ghost sprites.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestGhostSpritesEvent : EntityEventArgs
{
}

/// <summary>
/// Server sends available ghost sprites to client.
/// </summary>
[Serializable, NetSerializable]
public sealed class GhostSpritesResponseEvent : EntityEventArgs
{
    public List<ProtoId<GhostSpritePrototype>> Sprites { get; }

    public GhostSpritesResponseEvent(List<ProtoId<GhostSpritePrototype>> sprites)
    {
        Sprites = sprites;
    }
}
