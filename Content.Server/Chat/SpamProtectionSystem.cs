using Robust.Shared.Prototypes;
using Content.Shared.StatusEffect;
using Content.Shared.Chat;
using Content.Shared.Chat.Components;
using Robust.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Content.Server.Chat.Systems;
using Content.Server.Chat;
using Robust.Shared.Localization;
using Content.Shared.Speech;
using Content.Server.Chat.Managers;
using Content.Shared.Players;
using Robust.Shared.Utility;

namespace Content.Server.Chat;

[Access(typeof(SpamProtectionComponent))]
public sealed class SpamProtectionSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    // Убрали ActionBlockerSystem, так как он не используется

    // ID эффекта немоты из реагента MuteToxin
    private const string MuteEffectId = "Muted";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntitySpokeEvent>(OnEntitySpoke, before: new[] { typeof(ChatSystem) });
        SubscribeLocalEvent<SpamProtectionComponent, ComponentInit>(OnComponentInit);

        // Подписываемся на проверку возможности говорить
        SubscribeLocalEvent<SpeakAttemptEvent>(OnSpeakAttempt);
    }

    private void OnComponentInit(EntityUid uid, SpamProtectionComponent component, ComponentInit args)
    {
        // Инициализация массива временных меток
        component.MessageTimestamps = new TimeSpan[component.MaxTrackedMessages];
        component.RecentMessages = new string[component.MaxTrackedMessages];
    }

    private void OnEntitySpoke(EntitySpokeEvent args)
    {
        if (!TryComp<SpamProtectionComponent>(args.Source, out var spamComp))
            return;

        // Клич в ближнем бою и прочие системные реплики — одна и та же строка много раз подряд; это не спам в чате.
        if (args.IgnoreSpamProtection)
            return;

        // Пропускаем исключенные префиксы
        if (IsExcludedMessage(args.Message, spamComp))
            return;

        var currentTime = _gameTiming.CurTime;

        // Обновляем историю сообщений
        UpdateMessageHistory(args.Message, currentTime, spamComp);

        // Проверяем на спам
        if (IsSpamming(spamComp, currentTime))
        {
            // Получаем сессию игрока
            var session = GetPlayerSession(args.Source);

            // Сохраняем сообщение для логирования перед очисткой
            var spamMessage = spamComp.RecentMessages[0] ?? args.Message;

            HandleSpam(args.Source, spamComp, session);

            // Логируем нарушение
            LogSpamViolation(args.Source, spamMessage);
        }
    }

    private void OnSpeakAttempt(SpeakAttemptEvent args)
    {
        // Проверяем, не немой ли уже игрок
        if (_statusEffects.HasStatusEffect(args.Uid, MuteEffectId))
        {
            args.Cancel(); // Блокируем речь

            // Получаем сессию игрока
            var session = GetPlayerSession(args.Uid);

            // Отправляем сообщение если есть сессия
            if (session != null)
            {
                _chatManager.DispatchServerMessage(session,
                    Loc.GetString("spam-protection-muted"));
            }
            return;
        }
    }

    // Вспомогательный метод для получения сессии игрока по EntityUid
    private ICommonSession? GetPlayerSession(EntityUid uid)
    {
        // Пробуем получить ActorComponent
        if (TryComp<ActorComponent>(uid, out var actor))
        {
            return actor.PlayerSession;
        }

        // Альтернативный способ через ISharedPlayerManager
        foreach (var session in _playerManager.Sessions)
        {
            if (session.AttachedEntity == uid)
                return session;
        }

        return null;
    }

    private bool IsExcludedMessage(string message, SpamProtectionComponent comp)
    {
        if (string.IsNullOrWhiteSpace(message))
            return true;

        foreach (var prefix in comp.ExcludedPrefixes)
        {
            if (message.StartsWith(prefix))
                return true;
        }

        return false;
    }

    private void UpdateMessageHistory(string message, TimeSpan currentTime, SpamProtectionComponent comp)
    {
        // Сдвигаем массив на одну позицию
        for (int i = comp.MaxTrackedMessages - 1; i > 0; i--)
        {
            comp.RecentMessages[i] = comp.RecentMessages[i - 1];
            comp.MessageTimestamps[i] = comp.MessageTimestamps[i - 1];
        }

        // Добавляем новое сообщение
        comp.RecentMessages[0] = message;
        comp.MessageTimestamps[0] = currentTime;

        comp.MessageCount = Math.Min(comp.MessageCount + 1, comp.MaxTrackedMessages);
    }

    private bool IsSpamming(SpamProtectionComponent comp, TimeSpan currentTime)
    {
        if (comp.MessageCount < comp.SpamTriggerCount)
            return false;

        // Проверяем, что все N сообщений одинаковые
        string firstMessage = comp.RecentMessages[0];
        if (string.IsNullOrEmpty(firstMessage))
            return false;

        for (int i = 1; i < comp.SpamTriggerCount; i++)
        {
            if (comp.RecentMessages[i] != firstMessage)
                return false;
        }

        // Проверяем временной интервал
        var oldestIndex = comp.SpamTriggerCount - 1;
        var timeDiff = currentTime - comp.MessageTimestamps[oldestIndex];

        return timeDiff.TotalSeconds <= comp.SpamTimeThreshold;
    }

    private void HandleSpam(EntityUid uid, SpamProtectionComponent comp, ICommonSession? session)
    {
        // 1. Эмоут кашля
        _chat.TryEmoteWithChat(uid, "cough", ignoreActionBlocker: true);

        // 2. Применяем эффект немоты (аналогично MuteToxin)
        ApplyMuteEffect(uid, comp, session);

        // 3. Очищаем историю сообщений
        ClearMessageHistory(comp);

        // 4. Визуальные и звуковые эффекты
        PlayCoughEffects(uid);
    }

    private void ApplyMuteEffect(EntityUid uid, SpamProtectionComponent comp, ICommonSession? session)
    {
        // Применяем статус-эффект
        _statusEffects.TryAddStatusEffect(uid, MuteEffectId,
            TimeSpan.FromSeconds(comp.MuteDuration),
            refresh: false);

        // Уведомление игроку если есть сессия
        if (session != null)
        {
            var message = Loc.GetString("spam-protection-you-choked",
                ("duration", comp.MuteDuration));
            _chatManager.DispatchServerMessage(session, message);
        }

        // Глобальное уведомление (опционально)
        if (comp.AnnounceToChat)
        {
            _chat.DispatchGlobalAnnouncement(
                Loc.GetString("spam-protection-choked-announcement",
                    ("player", Name(uid))));
        }
    }

    private void ClearMessageHistory(SpamProtectionComponent comp)
    {
        Array.Clear(comp.RecentMessages, 0, comp.RecentMessages.Length);
        Array.Clear(comp.MessageTimestamps, 0, comp.MessageTimestamps.Length);
        comp.MessageCount = 0;
    }

    private void PlayCoughEffects(EntityUid uid)
    {
        try
        {
            // Звук кашля - используйте существующий звук
            _audio.PlayPvs("/Audio/Effects/male_cough_2.ogg", uid,
                AudioParams.Default.WithVolume(-2f));
        }
        catch (Exception ex)
        {
            // Попробуем другой звук или просто пропустим
            Logger.Warning($"Не удалось воспроизвести звук кашля: {ex.Message}");

            // Альтернативный звук
            try
            {
                _audio.PlayPvs("/Audio/Voice/Human/malescream.ogg", uid,
                    AudioParams.Default.WithVolume(-4f).WithPitchScale(1.2f));
            }
            catch
            {
                // Просто пропускаем если звуков нет
            }
        }
    }

    private void LogSpamViolation(EntityUid uid, string message)
    {
        // Безопасное получение имени entity
        string playerName;
        if (TryComp<MetaDataComponent>(uid, out var meta))
        {
            playerName = meta.EntityName;
        }
        else
        {
            playerName = uid.ToString();
        }

        // Безопасная обработка сообщения
        var shortMessage = string.IsNullOrEmpty(message)
            ? "[пустое сообщение]"
            : (message.Length > 50 ? message[..47] + "..." : message);

        Logger.Info($"{playerName} поперхнулся от спама: \"{shortMessage}\"");
    }
}
