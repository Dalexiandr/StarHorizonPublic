using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Shared._Horizon.MarketSaturation;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.MarketSaturation;

/// <summary>
/// Управляет рыночными событиями для станций.
/// Каждые 2-3 часа случайным образом выбирает событие из прототипов,
/// активирует его на подходящей станции и объявляет всем игрокам.
/// Активные события изменяют цены на указанные предметы на время действия.
/// </summary>
public sealed class MarketEventSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    /// <summary>
    /// Минимальный интервал между событиями (2 часа).
    /// </summary>
    private static readonly TimeSpan MinInterval = TimeSpan.FromHours(2);
    /// <summary>
    /// Максимальный интервал между событиями (3 часа).
    /// </summary>
    private static readonly TimeSpan MaxInterval = TimeSpan.FromHours(3);
    /// <summary>
    /// Время следующего рыночного события.
    /// </summary>
    private TimeSpan _nextEventTime;

    /// <summary>
    /// Флаг: идёт ли раунд.
    /// </summary>
    private bool _roundStarted;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _logManager.GetSawmill("market.events");

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New == GameRunLevel.InRound)
        {
            _roundStarted = true;
            ResetTimer();
            _sawmill.Info("Раунд начался — таймер рыночных событий запущен.");
        }
        else if (ev.Old == GameRunLevel.InRound)
        {
            _roundStarted = false;
            _sawmill.Info("Раунд завершён — рыночные события остановлены.");
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Не запускаем события вне раунда
        if (!_roundStarted)
            return;

        var curTime = _timing.CurTime;

        // Убираем истёкшие события
        RemoveExpiredEvents(curTime);

        // Проверяем, пора ли запустить новое событие
        if (curTime >= _nextEventTime)
        {
            TriggerRandomEvent();
            ResetTimer();
        }
    }

    /// <summary>
    /// Сбрасывает таймер на случайный интервал 2-3 часа.
    /// </summary>
    private void ResetTimer()
    {
        var intervalSeconds = _random.Next(
            (int) MinInterval.TotalSeconds,
            (int) MaxInterval.TotalSeconds + 1);
        _nextEventTime = _timing.CurTime + TimeSpan.FromSeconds(intervalSeconds);
        _sawmill.Info($"Следующее рыночное событие через {intervalSeconds} сек.");
    }

    /// <summary>
    /// Выбирает случайное рыночное событие и активирует его на подходящей станции.
    /// </summary>
    private void TriggerRandomEvent()
    {
        // Получаем все прототипы рыночных событий
        var prototypes = _proto.EnumeratePrototypes<MarketEventPrototype>().ToList();
        if (prototypes.Count == 0)
        {
            _sawmill.Warning("Нет прототипов рыночных событий!");
            return;
        }

        // Выбираем случайный прототип
        var eventProto = _random.Pick(prototypes);
        _sawmill.Info($"Выбрано событие: {eventProto.ID}, станции: [{string.Join(", ", eventProto.Stations)}], предметы: [{string.Join(", ", eventProto.Items)}], модификатор: {eventProto.PriceModifier}");

        // Находим подходящие станции с MarketSaturationComponent
        var candidates = new List<(EntityUid Uid, MarketSaturationComponent Comp, string Name)>();
        var query = EntityQueryEnumerator<MarketSaturationComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var stationProtoId = MetaData(uid).EntityPrototype?.ID ?? "null";
            var stationName = MetaData(uid).EntityName;

            _sawmill.Info($"  Станция: {stationName} (uid={uid}, proto={stationProtoId}), активных событий: {comp.ActiveEvents.Count}");

            // Если в прототипе указаны станции — фильтруем по ID прототипа станции
            if (eventProto.Stations.Count > 0)
            {
                if (stationProtoId == "null" || !eventProto.Stations.Contains(stationProtoId))
                {
                    _sawmill.Info($"    → Не подходит (proto={stationProtoId} не в [{string.Join(", ", eventProto.Stations)}])");
                    continue;
                }
            }

            candidates.Add((uid, comp, stationName));
        }

        if (candidates.Count == 0)
        {
            _sawmill.Warning("Нет подходящих станций для события!");
            return;
        }

        // Выбираем случайную станцию
        var (stationUid, saturation, name) = _random.Pick(candidates);
        _sawmill.Info($"Событие {eventProto.ID} активировано на станции {name} (uid={stationUid})");

        // Создаём активное событие
        var activeEvent = new ActiveMarketEvent
        {
            EventPrototypeId = eventProto.ID,
            AffectedItems = new HashSet<string>(eventProto.Items),
            PriceModifier = eventProto.PriceModifier,
            EndTime = _timing.CurTime + eventProto.Duration,
        };

        saturation.ActiveEvents.Add(activeEvent);

        _sawmill.Info($"  ActiveEvents на станции теперь: {saturation.ActiveEvents.Count}, предметы: [{string.Join(", ", activeEvent.AffectedItems)}]");

        // Отправляем глобальное объявление
        var durationMinutes = (int) eventProto.Duration.TotalMinutes;
        var message = Loc.GetString(eventProto.Description,
            ("station", name),
            ("duration", durationMinutes));

        _chat.DispatchGlobalAnnouncement(
            message,
            Loc.GetString("market-event-sender"),
            playSound: true);
    }

    /// <summary>
    /// Удаляет истёкшие рыночные события со всех станций.
    /// </summary>
    private void RemoveExpiredEvents(TimeSpan curTime)
    {
        var query = EntityQueryEnumerator<MarketSaturationComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            comp.ActiveEvents.RemoveAll(e => curTime >= e.EndTime);
        }
    }

    /// <summary>
    /// Вычисляет общий модификатор цены от активных рыночных событий для указанного предмета.
    /// Если предмет попадает под несколько событий — модификаторы перемножаются.
    /// </summary>
    public static double GetEventModifier(string protoId, MarketSaturationComponent comp)
    {
        var modifier = 1.0;
        foreach (var activeEvent in comp.ActiveEvents)
        {
            if (activeEvent.AffectedItems.Contains(protoId))
                modifier *= activeEvent.PriceModifier;
        }
        return modifier;
    }
}
