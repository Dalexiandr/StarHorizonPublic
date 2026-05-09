using Content.Shared._Horizon.RemoteControl.Systems;
using Content.Shared._Horizon.RemoteControl.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Horizon.RemoteControl.Systems;

public sealed class RemotePilotSystem : SharedRemotePilotSystem
{

    [Dependency] private readonly SpriteSystem _sprite = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RemotePilotComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<RemotePilotComponent> pilot, ref ComponentStartup args)
    {
        if (TryComp(pilot.Owner, out SpriteComponent? sprite))
            _sprite.SetVisible((pilot.Owner, sprite), false);
    }
}
