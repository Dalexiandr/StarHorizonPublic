using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.MediaPlayer;

public abstract class SharedMediaPlayerSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = null!;
}

[Serializable, NetSerializable]
public enum MediaPlayerUIKey : byte
{
    Key,
}
