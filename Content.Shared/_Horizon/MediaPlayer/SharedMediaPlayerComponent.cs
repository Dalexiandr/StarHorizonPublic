using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.MediaPlayer;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedMediaPlayerSystem))]
public sealed partial class MediaPlayerComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<MediaFilePrototype>? SelectedSongId;

    [DataField, AutoNetworkedField]
    public EntityUid? AudioStream;

    [DataField, AutoNetworkedField]
    public SoundPathSpecifier? AudioPath;

    [ViewVariables, AutoNetworkedField]
    public float Volume = -10;

    [ViewVariables, AutoNetworkedField]
    public RepeatType Repeat = RepeatType.None;

    [ViewVariables]
    public float SelectAccumulator;
}

[Serializable, NetSerializable]
public sealed class MediaPlayMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MediaPauseMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MediaStopMessage(bool repeat) : BoundUserInterfaceMessage
{
    public bool Repeat { get; } = repeat;
}


[Serializable, NetSerializable]
public sealed class MediaSelectedMessage(float volume, string? songId, SoundPathSpecifier? path) : BoundUserInterfaceMessage
{
    public float Volume { get; } = volume;
    public string? SongId { get; } = songId;
    public SoundPathSpecifier? AudioPath { get; } = path;
}

[Serializable, NetSerializable]
public sealed class MediaRepeatMessage(RepeatType type) : BoundUserInterfaceMessage
{
    public RepeatType Type { get; } = type;
}

[Serializable, NetSerializable]
public sealed class MediaVolumeMessage(float volume) : BoundUserInterfaceMessage
{
    public float Volume { get; } = volume;
}

[Serializable, NetSerializable]
public sealed class MediaSetTimeMessage(float songTime) : BoundUserInterfaceMessage
{
    public float SongTime { get; } = songTime;
}

[Serializable, NetSerializable]
public enum RepeatType : byte
{
    None = 0,
    Playlist = 1,
    Single = 2,
}

[Serializable, NetSerializable]
public sealed class RepeatMessage(NetEntity entity, string id) : EntityEventArgs
{
    public NetEntity Entity { get; } = entity;
    public string Id { get; } = id;
}
