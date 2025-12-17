using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Shared._Horizon.Language;
using Content.Shared.Chat;
using Content.Shared.Speech;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Horizon.Language;

[DataDefinition]
public sealed partial class CollectiveMind : ILanguageType
{
    public ProtoId<LanguagePrototype> Language { get; set; }

    /// <inheritdoc/>
    [DataField]
    public Color? Color { get; set; }

    /// <inheritdoc/>
    [DataField]
    public Color? WhisperColor { get; set; }

    /// <inheritdoc/>
    [DataField]
    public bool RaiseEvent { get; set; } = false;

    /// <summary>
    /// Будет ли отображаться ранг автора в чате
    /// Для установки ранга используется <seealso cref="CollectiveMindRankComponent"/>
    /// </summary>
    [DataField]
    public bool ShowRank = true;

    /// <summary>
    /// Будет ли отображаться имя автора в чате
    /// </summary>
    [DataField]
    public bool ShowName = true;

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

    public void Speak(EntityUid uid, string message, string name, SpeechVerbPrototype verb, byte range, IEntityManager entMan, out bool success, out string resultMessage)
        => Send(uid, message, entMan, out success, out resultMessage);

    public void Whisper(EntityUid uid, string message, string name, string nameIdentity, byte range, IEntityManager entMan, out bool success, out string resultMessage, out string resultObfMessage)
    {
        Send(uid, message, entMan, out success, out var result);
        resultMessage = result;
        resultObfMessage = result;
    }

    private void Send(EntityUid uid, string message, IEntityManager entMan, out bool success, out string resultMessage)
    {
        var lang = entMan.System<LanguageSystem>();
        var chat = entMan.System<ChatSystem>();
        var admin = IoCManager.Resolve<IAdminManager>();
        var proto = IoCManager.Resolve<IPrototypeManager>();
        var chatMan = IoCManager.Resolve<IChatManager>();

        success = false;
        chat.TryProccessRadioMessage(uid, message, out message, out _);
        resultMessage = message;

        if (string.IsNullOrEmpty(message))
            return;

        var clients = Filter.Empty();
        var admins = Filter.Empty();
        var mindQuery = entMan.EntityQueryEnumerator<LanguageSpeakerComponent, ActorComponent>();
        while (mindQuery.MoveNext(out var player, out _, out var actorComp))
        {
            if (lang.CanUnderstand(player, Language))
                clients.AddPlayer(actorComp.PlayerSession);
            else if (admin.IsAdmin(actorComp.PlayerSession))
                admins.AddPlayer(actorComp.PlayerSession);
        }

        var rank = entMan.TryGetComponent<CollectiveMindRankComponent>(uid, out var collective) ? Loc.GetString(collective.RankName) : "";
        var language = proto.Index(Language);

        // Build messages
        string messageWrap =
            ShowRank && ShowName ?
                Loc.GetString("chat-manager-send-collective-mind-chat-wrap-message-rank-name",
                ("fontType", Font ?? "NotoSansDisplay"),
                ("fontSize", FontSize ?? 12),
                ("defaultFont", "NotoSansDisplay"),
                ("defaultSize", 12),
                ("source", uid),
                ("rank", rank),
                ("message", message),
                ("channel", language.LocalizedName)) :

            ShowName ?
                Loc.GetString("chat-manager-send-collective-mind-chat-wrap-message-name",
                ("fontType", Font ?? "NotoSansDisplay"),
                ("fontSize", FontSize ?? 12),
                ("defaultFont", "NotoSansDisplay"),
                ("defaultSize", 12),
                ("source", uid),
                ("message", message),
                ("channel", language.LocalizedName)) :

            ShowRank ?
                Loc.GetString("chat-manager-send-collective-mind-chat-wrap-message-rank",
                ("fontType", Font ?? "NotoSansDisplay"),
                ("fontSize", FontSize ?? 12),
                ("defaultFont", "NotoSansDisplay"),
                ("defaultSize", 12),
                ("rank", rank),
                ("message", message),
                ("channel", language.LocalizedName)) :

           Loc.GetString("chat-manager-send-collective-mind-chat-wrap-message",
                ("fontType", Font ?? "NotoSansDisplay"),
                ("fontSize", FontSize ?? 12),
                ("defaultFont", "NotoSansDisplay"),
                ("defaultSize", 12),
                ("message", message),
                ("channel", language.LocalizedName));

        string adminMessageWrap = Loc.GetString("chat-manager-send-collective-mind-chat-wrap-message-admin",
            ("fontType", Font ?? "NotoSansDisplay"),
            ("fontSize", FontSize ?? 12),
            ("defaultFont", "NotoSansDisplay"),
            ("defaultSize", 12),
            ("source", uid),
            ("rank", rank),
            ("message", message),
            ("channel", language.LocalizedName));

        // Apply language color
        if (Color != null)
        {
            messageWrap = $"[color={Color.Value.ToHex()}]{messageWrap}[/color]";
            adminMessageWrap = $"[color={Color.Value.ToHex()}]{adminMessageWrap}[/color]";
        }

        chatMan.ChatMessageToManyFiltered(clients, ChatChannel.Radio, message, messageWrap, uid, false, false, Color);
        chatMan.ChatMessageToManyFiltered(admins, ChatChannel.Radio, message, adminMessageWrap, uid, false, false, Color);

        success = true;
    }
}
