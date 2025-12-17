using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Shared._Horizon.Language;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Content.Shared.Speech;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._Horizon.Language;

[DataDefinition]
public sealed partial class Emotes : ILanguageType
{
    public ProtoId<LanguagePrototype> Language { get; set; }

    [DataField]
    public Color? Color { get; set; }

    [DataField]
    public Color? WhisperColor { get; set; }

    [DataField]
    public bool RaiseEvent { get; set; } = false;

    /// <inheritdoc/>
    [DataField("verbs")]
    public Dictionary<string, List<string>> SuffixSpeechVerbs { get; set; } = new()
    {
        { "chat-speech-verb-suffix-exclamation-strong", new() },
        { "chat-speech-verb-suffix-exclamation", new() },
        { "chat-speech-verb-suffix-question", new() },
        { "chat-speech-verb-suffix-stutter", new() },
        { "chat-speech-verb-suffix-mumble", new() },
    };

    /// <inheritdoc/>
    [DataField]
    public int? FontSize { get; set; } = null;

    /// <inheritdoc/>
    [DataField]
    public string? Font { get; set; } = null;

    [DataField(required: true)]
    public List<string> Replacement = new();

    [DataField]
    public SoundSpecifier? Sound;

    public void Speak(EntityUid uid, string message, string name, SpeechVerbPrototype verb, byte range, IEntityManager entMan, out bool success, out string resultMessage)
        => Send(uid, message, name, range, entMan, out success, out resultMessage, out _);

    public void Whisper(EntityUid uid, string message, string name, string nameIdentity, byte range, IEntityManager entMan, out bool success, out string resultMessage, out string resultObfMessage)
        => Send(uid, message, name, range, entMan, out success, out resultMessage, out resultObfMessage);

    private void Send(EntityUid uid, string message, string name, byte range, IEntityManager entMan, out bool success, out string resultMessage, out string resultObfMessage)
    {
        var lang = entMan.System<LanguageSystem>();
        var chat = entMan.System<ChatSystem>();
        var audio = entMan.System<AudioSystem>();
        var random = IoCManager.Resolve<IRobustRandom>();
        var chatMan = IoCManager.Resolve<IChatManager>();
        success = false;

        chat.TryProccessRadioMessage(uid, message, out message, out _);
        string coloredMessage = lang.AccentuateMessage(uid, Language, message);
        string coloredLanguageMessage = Loc.GetString(random.Pick(Replacement));
        resultMessage = FormattedMessage.EscapeText(coloredMessage);
        resultObfMessage = FormattedMessage.EscapeText(coloredMessage);
        if (string.IsNullOrEmpty(coloredMessage))
            return;

        var verb = chat.GetSpeechVerb(uid, message);

        if (Color != null)
        {
            coloredMessage = $"[color={Color.Value.ToHex()}]{coloredMessage}[/color]";
        }

        if (string.IsNullOrEmpty(FormattedMessage.EscapeText(coloredMessage)))
            return;

        // Getting verbs
        List<string> verbStrings = verb.SpeechVerbStrings;
        bool verbsReplaced = false;
        foreach (var str in ILanguageType.SpeechSuffixes)
        {
            if (message.EndsWith(Loc.GetString(str)) && SuffixSpeechVerbs.TryGetValue(str, out var strings))
            {
                verbStrings = strings;
                verbsReplaced = true;
            }
        }

        if (!verbsReplaced && SuffixSpeechVerbs.TryGetValue("Default", out var defaultStrings))
            verbStrings = defaultStrings;

        int fontSize = FontSize.HasValue ? FontSize.Value : verb.FontSize;
        string font = Font != null && Font != "" ? Font : verb.FontId;

        name = FormattedMessage.EscapeText(name);
        var wrappedMessage = Loc.GetString(verb.Bold ? "chat-manager-entity-say-bold-wrap-message" : "chat-manager-entity-say-wrap-message",
            ("entityName", name),
            ("verb", Loc.GetString(random.Pick(verbStrings))),
            ("fontType", font),
            ("fontSize", fontSize),
            ("defaultFont", verb.FontId),
            ("defaultSize", verb.FontSize),
            ("message", coloredMessage));

        var wrappedLanguageMessage = Loc.GetString("chat-manager-entity-me-wrap-message",
            ("entityName", name),
            ("entity", Identity.Entity(uid, entMan)),
            ("message", FormattedMessage.RemoveMarkupOrThrow(coloredLanguageMessage)));

        foreach (var (session, data) in chat.GetRecipients(uid, ChatSystem.VoiceRange))
        {
            EntityUid listener;

            if (session.AttachedEntity is not { Valid: true } playerEntity)
                continue;
            listener = session.AttachedEntity.Value;

            var entRange = chat.MessageRangeCheck(session, data, (ChatTransmitRange)range);
            if (entRange == ChatSystem.MessageRangeCheckResult.Disallowed)
                continue;
            var entHideChat = entRange == ChatSystem.MessageRangeCheckResult.HideChat;

            if (!lang.CanUnderstand(listener, Language))
                chatMan.ChatMessageToOne(ChatChannel.Emotes, message, wrappedLanguageMessage, uid, entHideChat, session.Channel, author: session.UserId);
            else
                chatMan.ChatMessageToOne(ChatChannel.Local, message, wrappedMessage, uid, entHideChat, session.Channel, author: session.UserId);
        }

        audio.PlayPvs(Sound, uid);
        success = true;
    }
}
