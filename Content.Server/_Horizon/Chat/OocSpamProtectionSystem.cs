using Robust.Shared.Prototypes;
using Content.Shared.StatusEffect;
using Content.Shared.Chat;
using Content.Shared.Chat.Components;
using Robust.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Content.Server.Chat.Systems;
using Robust.Shared.Localization;
using Content.Shared.Speech;
using Content.Server.Chat.Managers;
using Content.Shared.Players;
using Robust.Shared.Utility;

namespace Content.Server._Horizon.Chat;

[Access(typeof(OocSpamProtectionComponent), typeof(ChatSystem))]
public sealed class OocSpamProtectionSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    // ID эффекта немоты
    private const string MuteEffectId = "Muted";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OocSpamProtectionComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, OocSpamProtectionComponent component, ComponentInit args)
    {
        component.MessageTimestamps = new TimeSpan[component.MaxTrackedMessages];
        component.RecentMessages = new string[component.MaxTrackedMessages];
    }

    // Метод для проверки OOC сообщения (будет вызываться из ChatSystem)
    public bool CheckOocSpam(EntityUid uid, string message, OocSpamProtectionComponent comp)
    {
        // Проверяем, не немой ли уже игрок
        if (_statusEffects.HasStatusEffect(uid, MuteEffectId))
        {
            var session = GetPlayerSession(uid);
            if (session != null)
            {
                _chatManager.DispatchServerMessage(session,
                    Loc.GetString("spam-protection-muted"));
            }
            return false; // Блокируем сообщение
        }

        var currentTime = _gameTiming.CurTime;

        // Обновляем историю сообщений
        UpdateMessageHistory(message, currentTime, comp);

        // Проверяем на спам
        if (IsSpamming(comp, currentTime))
        {
            var session = GetPlayerSession(uid);

            // Сохраняем сообщение для логирования перед очисткой
            var spamMessage = comp.RecentMessages[0] ?? message;

            HandleOocSpam(uid, comp, session);

            // Логируем нарушение
            LogSpamViolation(uid, spamMessage);

            return false; // Блокируем сообщение
        }

        return true; // Разрешаем сообщение
    }

    private void UpdateMessageHistory(string message, TimeSpan currentTime, OocSpamProtectionComponent comp)
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

    private bool IsSpamming(OocSpamProtectionComponent comp, TimeSpan currentTime)
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

    private void HandleOocSpam(EntityUid uid, OocSpamProtectionComponent comp, ICommonSession? session)
    {
        // 1. Применяем эффект немоты
        ApplyMuteEffect(uid, comp, session);

        // 2. Очищаем историю сообщений
        ClearMessageHistory(comp);
    }

    private void ApplyMuteEffect(EntityUid uid, OocSpamProtectionComponent comp, ICommonSession? session)
    {
        // Применяем статус-эффект
        _statusEffects.TryAddStatusEffect(uid, MuteEffectId,
            TimeSpan.FromSeconds(comp.MuteDuration),
            refresh: false);

        // Уведомление игроку
        if (session != null)
        {
            var message = Loc.GetString("spam-protection-you-choked-ooc",
                ("duration", comp.MuteDuration));
            _chatManager.DispatchServerMessage(session, message);
        }
    }

    private void ClearMessageHistory(OocSpamProtectionComponent comp)
    {
        Array.Clear(comp.RecentMessages, 0, comp.RecentMessages.Length);
        Array.Clear(comp.MessageTimestamps, 0, comp.MessageTimestamps.Length);
        comp.MessageCount = 0;
    }

    private ICommonSession? GetPlayerSession(EntityUid uid)
    {
        if (TryComp<ActorComponent>(uid, out var actor))
            return actor.PlayerSession;

        foreach (var session in _playerManager.Sessions)
        {
            if (session.AttachedEntity == uid)
                return session;
        }

        return null;
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

        Logger.Info($"{playerName} получил мут в OOC за спам: \"{shortMessage}\"");
    }
}
