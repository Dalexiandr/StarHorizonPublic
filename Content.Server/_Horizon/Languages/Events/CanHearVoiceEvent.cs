namespace Content.Server._Horizon.Language;

[ByRefEvent]
public record struct CanHearVoiceEvent(EntityUid Source, bool Whisper)
{
    public bool Cancelled = false;
}
