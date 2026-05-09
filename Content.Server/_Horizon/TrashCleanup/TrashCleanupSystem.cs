using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Shared._Horizon.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Tag;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.TrashCleanup;

/// <summary>
/// Система автоматического удаления мусорных сущностей с периодической очисткой.
/// Активируется только после задержки от начала раунда.
/// </summary>
public sealed class TrashCleanupSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private bool _enabled;
    private float _cleanupInterval;
    private float _startDelay;

    /// <summary>
    /// Время начала текущего раунда.
    /// </summary>
    private TimeSpan _roundStartTime;

    /// <summary>
    /// Активна ли система (после истечения задержки).
    /// </summary>
    private bool _isActive;

    /// <summary>
    /// Время последней очистки.
    /// </summary>
    private TimeSpan _lastCleanupTime;

    /// <summary>
    /// Теги, определяющие сущности для очистки.
    /// </summary>
    private static readonly string[] CleanupTags = { "Cartridge" };

    /// <summary>
    /// Префикс ID прототипа для мусорных сущностей.
    /// </summary>
    private const string TrashPrefix = "Trash";

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(HorizonCCVars.TrashCleanupEnabled, OnEnabledChanged, true);
        _cfg.OnValueChanged(HorizonCCVars.TrashCleanupInterval, OnIntervalChanged, true);
        _cfg.OnValueChanged(HorizonCCVars.TrashCleanupStartDelay, OnStartDelayChanged, true);

        // Подписываемся на события раунда
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnEnabledChanged(bool value)
    {
        _enabled = value;
        if (_enabled)
        {
            Log.Info("TrashCleanup: Система включена.");
            // Если раунд уже идёт и у нас нет времени старта, устанавливаем его сейчас
            if (_roundStartTime == TimeSpan.Zero && _gameTicker.RunLevel == GameRunLevel.InRound)
            {
                _roundStartTime = _timing.CurTime;
                Log.Info($"TrashCleanup: Раунд уже идёт. Система активируется через {_startDelay} секунд.");
            }
        }
        else
        {
            Log.Info("TrashCleanup: Система отключена.");
        }
    }

    private void OnIntervalChanged(float value)
    {
        _cleanupInterval = value;
        Log.Info($"TrashCleanup: Интервал очистки установлен на {value} секунд.");
    }

    private void OnStartDelayChanged(float value)
    {
        _startDelay = value;
        Log.Info($"TrashCleanup: Задержка старта установлена на {value} секунд.");
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        _roundStartTime = _timing.CurTime;
        _isActive = false;
        _lastCleanupTime = TimeSpan.Zero;

        if (_enabled)
            Log.Info($"TrashCleanup: Раунд {ev.Id} начался. Система активируется через {_startDelay} секунд.");
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _isActive = false;
        _roundStartTime = TimeSpan.Zero;
        _lastCleanupTime = TimeSpan.Zero;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled)
            return;

        // Проверяем, нужно ли активировать систему
        if (!_isActive && _roundStartTime != TimeSpan.Zero)
        {
            var timeSinceRoundStart = _timing.CurTime - _roundStartTime;
            if (timeSinceRoundStart.TotalSeconds >= _startDelay)
            {
                _isActive = true;
                _lastCleanupTime = _timing.CurTime;
                Log.Info($"TrashCleanup: Система активирована после {_startDelay} секунд задержки.");
            }
            else
            {
                return; // Ещё ждём задержку
            }
        }

        if (!_isActive)
            return;

        // Проверяем, нужно ли выполнить очистку
        var curTime = _timing.CurTime;
        var timeSinceLastCleanup = curTime - _lastCleanupTime;

        if (timeSinceLastCleanup.TotalSeconds < _cleanupInterval)
            return;

        // Выполняем очистку
        _lastCleanupTime = curTime;
        PerformCleanup();
    }

    private void PerformCleanup()
    {
        var deletedCount = 0;
        var query = EntityQueryEnumerator<TagComponent>();
        var entitiesToDelete = new List<EntityUid>();

        // Собираем все сущности с нужными тегами, которые не в контейнерах
        while (query.MoveNext(out var uid, out var tagComponent))
        {
            // Пропускаем сущности в контейнерах (в руках, рюкзаках и т.д.)
            if (_container.IsEntityInContainer(uid))
                continue;

            // Проверяем, есть ли у сущности какой-либо из тегов очистки
            var hasCleanupTag = false;
            foreach (var tag in CleanupTags)
            {
                if (_tag.HasTag(uid, tag))
                {
                    hasCleanupTag = true;
                    break;
                }
            }

            // Также проверяем префикс прототипа для мусора
            if (!hasCleanupTag)
            {
                var meta = MetaData(uid);
                var prototypeId = meta.EntityPrototype?.ID;
                if (prototypeId != null && prototypeId.StartsWith(TrashPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    hasCleanupTag = true;
                }
            }

            if (hasCleanupTag)
            {
                entitiesToDelete.Add(uid);
            }
        }

        // Удаляем собранные сущности
        foreach (var uid in entitiesToDelete)
        {
            var name = MetaData(uid).EntityName;
            Log.Debug($"TrashCleanup: Удаление '{name}' ({uid}).");
            QueueDel(uid);
            deletedCount++;
        }

        if (deletedCount > 0)
            Log.Info($"TrashCleanup: Удалено {deletedCount} мусорных сущностей.");
    }
}
