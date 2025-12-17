using System.Linq;
using Content.Server._Horizon.Language;
using Content.Shared._Horizon.Language;
using Content.Shared.Chat;
using Content.Shared.Ghost;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    [Dependency] private readonly LanguageSystem _language = default!;

    public void SendInVoiceRangeLanguaged(ChatChannel channel, string message, string wrappedMessage, string wrappedLanguageMessage, EntityUid source, byte range, NetUserId? author = null, ProtoId<LanguagePrototype>? language = null)
    {
        var lang = language != null ? _prototypeManager.Index(language.Value) : _language.GetCurrentLanguage(source);

        foreach (var (session, data) in GetRecipients(source, VoiceRange))
        {
            EntityUid listener;

            if (session.AttachedEntity is not { Valid: true } playerEntity)
                continue;
            listener = session.AttachedEntity.Value;

            bool condition = true;
            foreach (var item in lang.Conditions.Where(x => x.RaiseOnListener))
            {
                if (!item.Condition(listener, source, EntityManager))
                    condition = false;
            }
            if (!condition)
                continue;

            var entRange = MessageRangeCheck(session, data, (ChatTransmitRange)range);
            if (entRange == MessageRangeCheckResult.Disallowed)
                continue;
            var entHideChat = entRange == MessageRangeCheckResult.HideChat;

            _chatManager.ChatMessageToOne(channel,
                                          message,
                                          _language.CanUnderstand(listener, lang) ? wrappedMessage : wrappedLanguageMessage,
                                          source, entHideChat, session.Channel, author: author);
        }

        _replay.RecordServerMessage(new ChatMessage(channel, message, wrappedMessage, GetNetEntity(source), null, MessageRangeHideChatForReplay((ChatTransmitRange)range)));
    }

    public Dictionary<ICommonSession, ICChatRecipientData> GetWhisperRecipients(EntityUid source, float clearRange, float muffledRange)
    {
        var recipients = new Dictionary<ICommonSession, ICChatRecipientData>();
        var ghostHearing = GetEntityQuery<GhostHearingComponent>();
        var xforms = GetEntityQuery<TransformComponent>();

        var transformSource = xforms.GetComponent(source);
        var sourceMapId = transformSource.MapID;
        var sourceCoords = transformSource.Coordinates;

        foreach (var player in _playerManager.Sessions)
        {
            if (player.AttachedEntity is not { Valid: true } playerEntity)
                continue;

            var transformEntity = xforms.GetComponent(playerEntity);

            if (transformEntity.MapID != sourceMapId)
                continue;

            if (ghostHearing.HasComponent(playerEntity))
            {
                recipients.Add(player, new ICChatRecipientData(-1, true));
                continue;
            }

            // even if they are a ghost hearer, in some situations we still need the range
            if (sourceCoords.TryDistance(EntityManager, transformEntity.Coordinates, out var distance))
            {
                if (distance < clearRange)
                {
                    recipients.Add(player, new ICChatRecipientData(distance, false, Muffled: false));
                    continue;
                }
                if (distance < muffledRange)
                {
                    recipients.Add(player, new ICChatRecipientData(distance, false, Muffled: true));
                    continue;
                }
            }
        }

        RaiseLocalEvent(new ExpandICChatRecipientsEvent(source, muffledRange, recipients));
        return recipients;
    }

    public void SendWhisper(
                            EntityUid source, ProtoId<LanguagePrototype> language, byte range,
                            string message, string obfuscatedMessage,
                            string wrappedMessage, string wrappedobfuscatedMessage, string wrappedUnknownMessage,
                            string wrappedLanguageMessage, string wrappedobfuscatedLanguageMessage, string wrappedUnknownLanguageMessage)
    {
        var lang = _prototypeManager.Index(language);

        foreach (var (session, data) in GetWhisperRecipients(source, WhisperClearRange, WhisperMuffledRange))
        {
            EntityUid listener;

            if (session.AttachedEntity is not { Valid: true } playerEntity)
                continue;
            listener = session.AttachedEntity.Value;

            bool condition = true;
            foreach (var item in lang.Conditions.Where(x => x.RaiseOnListener))
            {
                if (!item.Condition(listener, source, EntityManager))
                    condition = false;
            }
            if (!condition)
                continue;

            if (MessageRangeCheck(session, data, (ChatTransmitRange)range) != MessageRangeCheckResult.Full)
                continue; // Won't get logged to chat, and ghosts are too far away to see the pop-up, so we just won't send it to them.

            // В зависимости от понимания присваиваем разные значения трём переменным сразу
            var (langMessage, wrappedLangMessage, wrappedUnknownLangMessage) =
                    _language.CanUnderstand(listener, language) ?
                    (wrappedMessage, wrappedobfuscatedMessage, wrappedUnknownMessage) :
                    (wrappedLanguageMessage, wrappedobfuscatedLanguageMessage, wrappedUnknownLanguageMessage);

            if (!data.Muffled)
                _chatManager.ChatMessageToOne(ChatChannel.Whisper, message, langMessage, source, false, session.Channel);

            //If listener is too far, they only hear fragments of the message
            else if (_examineSystem.InRangeUnOccluded(source, listener, WhisperMuffledRange))
                _chatManager.ChatMessageToOne(ChatChannel.Whisper, obfuscatedMessage, wrappedLangMessage, source, false, session.Channel);

            //If listener is too far and has no line of sight, they can't identify the whisperer's identity
            else
                _chatManager.ChatMessageToOne(ChatChannel.Whisper, obfuscatedMessage, wrappedUnknownLangMessage, source, false, session.Channel);
        }
    }
}
