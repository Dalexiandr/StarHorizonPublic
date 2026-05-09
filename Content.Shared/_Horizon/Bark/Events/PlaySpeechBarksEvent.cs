using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Bark;

[Serializable, NetSerializable]
public sealed class PlaySpeechBarksEvent : EntityEventArgs
{
    public NetEntity Source;
    public string? Message;
    public SoundSpecifier Sound;
    public float Pitch;
    public float LowVar;
    public float HighVar;
    public bool IsWhisper;

    public PlaySpeechBarksEvent(
        NetEntity source,
        string? message,
        SoundSpecifier sound,
        float pitch,
        float lowVar,
        float highVar,
        bool isWhisper)
    {
        Source = source;
        Message = message;
        Sound = sound;
        Pitch = pitch;
        LowVar = lowVar;
        HighVar = highVar;
        IsWhisper = isWhisper;
    }
}
