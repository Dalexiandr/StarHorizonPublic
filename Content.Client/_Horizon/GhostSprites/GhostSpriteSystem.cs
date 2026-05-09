using Content.Client._Horizon.GhostSprites.UI;
using Content.Shared._Horizon.GhostSprites;
using Content.Shared.Ghost;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client._Horizon.GhostSprites;

/// <summary>
/// Client-side system that handles ghost sprite selection UI and visual updates.
/// </summary>
public sealed class GhostSpriteSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    private GhostSpriteWindow? _window;
    private List<ProtoId<GhostSpritePrototype>> _availableSprites = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<GhostSpritesResponseEvent>(OnGhostSpritesResponse);
        SubscribeNetworkEvent<GhostSpriteChangedEvent>(OnGhostSpriteChanged);
        SubscribeLocalEvent<GhostSpriteComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<GhostSpriteComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<GhostSpriteComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    /// <summary>
    /// Opens the ghost sprite selection window.
    /// </summary>
    public void OpenWindow()
    {
        if (_window != null && !_window.IsOpen)
        {
            RequestSprites();
            _window.OpenCentered();
            return;
        }

        _window = new GhostSpriteWindow();
        _window.OnSpriteSelected += OnSpriteSelected;
        _window.OnClose += OnWindowClosed;

        RequestSprites();
        _window.OpenCentered();
    }

    /// <summary>
    /// Closes the ghost sprite selection window.
    /// </summary>
    public void CloseWindow()
    {
        _window?.Close();
    }

    private void RequestSprites()
    {
        RaiseNetworkEvent(new RequestGhostSpritesEvent());
    }

    private void OnGhostSpritesResponse(GhostSpritesResponseEvent msg)
    {
        _availableSprites = msg.Sprites;

        if (_window != null && _window.IsOpen)
        {
            _window.Populate(_availableSprites);
        }
    }

    private void OnGhostSpriteChanged(GhostSpriteChangedEvent msg)
    {
        // Try to get the local entity from the network entity
        // If the entity doesn't exist locally (outside PVS), skip
        if (!TryGetEntity(msg.Ghost, out var entity) || entity == null)
            return;

        if (!TryComp<SpriteComponent>(entity.Value, out var spriteComponent))
            return;

        ApplySpriteChange(entity.Value, spriteComponent, msg.SpriteId);
    }

    private void OnSpriteSelected(ProtoId<GhostSpritePrototype> spriteId)
    {
        RaiseNetworkEvent(new ChangeGhostSpriteRequestEvent(spriteId));
    }

    private void OnWindowClosed()
    {
        if (_window != null)
        {
            _window.OnSpriteSelected -= OnSpriteSelected;
            _window.OnClose -= OnWindowClosed;
            _window = null;
        }
    }

    private void OnComponentStartup(EntityUid uid, GhostSpriteComponent component, ComponentStartup args)
    {
        if (component.SelectedSprite != null && TryComp<SpriteComponent>(uid, out var spriteComponent))
        {
            ApplySpriteChange(uid, spriteComponent, component.SelectedSprite.Value);
        }
    }

    private void OnComponentShutdown(EntityUid uid, GhostSpriteComponent component, ComponentShutdown args)
    {
        // Component removed, no action needed
    }

    private void OnAfterAutoHandleState(EntityUid uid, GhostSpriteComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (component.SelectedSprite != null && TryComp<SpriteComponent>(uid, out var spriteComponent))
        {
            ApplySpriteChange(uid, spriteComponent, component.SelectedSprite.Value);
        }
    }

    private void ApplySpriteChange(EntityUid entity, SpriteComponent spriteComponent, ProtoId<GhostSpritePrototype> spriteId)
    {
        if (!_prototypeManager.TryIndex(spriteId, out var prototype))
            return;

        var path = SpriteSpecifierSerializer.TextureRoot / prototype.RsiPath;
        if (!_resourceCache.TryGetResource<RSIResource>(path, out var rsiResource) || rsiResource.RSI == null)
            return;

        var rsi = rsiResource.RSI;
        var stateId = prototype.State;
        if (!rsi.TryGetState(stateId, out _))
            return;

        var sprite = (entity, spriteComponent);
        _spriteSystem.LayerSetRsi(sprite, 0, rsi, stateId);
    }
}
