using Content.Shared._Horizon.NightVision;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Horizon.NightVision;

public sealed class NightVisionOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _greyscaleShader;
    private readonly Color _baseNightVisionColor;

    private float _currentIntensity = 0f;
    private const float TransitionSpeed = 1.5f;
    private const float MaxIntensity = 0.9f;

    private NightVisionComponent _nightVisionComponent = default!;
    private bool _isTransitioning = false;
    private bool _lastNightVisionState = false;

    public NightVisionOverlay(Color color)
    {
        IoCManager.InjectDependencies(this);
        _greyscaleShader = _prototypeManager.Index<ShaderPrototype>("GreyscaleFullscreen").InstanceUnique();
        _baseNightVisionColor = color;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!TryGetPlayerComponents(args, out var _, out var nightvisionComp))
            return false;

        _nightVisionComponent = nightvisionComp;

        if (_lastNightVisionState != _nightVisionComponent.IsNightVision)
        {
            _isTransitioning = true;
            _lastNightVisionState = _nightVisionComponent.IsNightVision;
        }

        if (!_nightVisionComponent.IsNightVision && _currentIntensity <= 0f && _nightVisionComponent.DrawShadows)
        {
            _lightManager.DrawLighting = true;
            _nightVisionComponent.DrawShadows = false;
            _nightVisionComponent.GraceFrame = true;
            return true;
        }

        return _nightVisionComponent.IsNightVision || _currentIntensity > 0f;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        if (!_nightVisionComponent.GraceFrame)
        {
            if (_isTransitioning)
            {
                if (_nightVisionComponent.IsNightVision)
                {
                    _nightVisionComponent.DrawShadows = true;
                    _lightManager.DrawLighting = false;
                    HandleNightVisionActivation();

                    if (_currentIntensity >= MaxIntensity)
                        _isTransitioning = false;
                }
                else
                {
                    HandleNightVisionDeactivation();

                    if (_currentIntensity <= 0f)
                        _isTransitioning = false;
                }
            }
        }
        else
        {
            _nightVisionComponent.GraceFrame = false;
        }

        _greyscaleShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;
        var targetColor = _baseNightVisionColor.WithAlpha(_currentIntensity);

        worldHandle.UseShader(_greyscaleShader);
        worldHandle.DrawRect(viewport, targetColor);
        worldHandle.UseShader(null);
    }

    private bool TryGetPlayerComponents(in OverlayDrawArgs args, out EyeComponent? eyeComp, out NightVisionComponent nightvisionComp)
    {
        eyeComp = null;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null ||
            !_entityManager.TryGetComponent(playerEntity, out eyeComp) ||
            args.Viewport.Eye != eyeComp.Eye ||
            !_entityManager.TryGetComponent<NightVisionComponent>(playerEntity.Value, out var nvComp))
        {
            nightvisionComp = default!;
            return false;
        }

        nightvisionComp = nvComp;
        return true;
    }

    private void HandleNightVisionActivation()
    {
        _currentIntensity = Math.Min(_currentIntensity + TransitionSpeed * (float)_timing.FrameTime.TotalSeconds, MaxIntensity);
    }

    private void HandleNightVisionDeactivation()
    {
        _currentIntensity = Math.Max(_currentIntensity - TransitionSpeed * (float)_timing.FrameTime.TotalSeconds, 0);

        _lightManager.DrawLighting = true;
        _nightVisionComponent.DrawShadows = false;
        _nightVisionComponent.GraceFrame = true;
    }
}
