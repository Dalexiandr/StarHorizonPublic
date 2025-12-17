using Content.Shared._Horizon.Language;

namespace Content.Server.Speech;

public sealed class ListenEvent : EntityEventArgs
{
    public readonly string Message;
    public readonly EntityUid Source;
    public readonly LanguagePrototype Language; // Horiaon languages

    public ListenEvent(string message, EntityUid source, LanguagePrototype language)    // Horizon languages
    {
        Message = message;
        Source = source;
        Language = language;    // Horizon languages
    }
}

public sealed class ListenAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Source;

    public ListenAttemptEvent(EntityUid source)
    {
        Source = source;
    }
}
