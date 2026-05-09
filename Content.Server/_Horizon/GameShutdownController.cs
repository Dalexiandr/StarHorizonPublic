using System.IO;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Robust.Server;
using Robust.Shared.ContentPack;
using Content.Shared._Horizon.CCVar;
using Content.Shared.Chat;
using Robust.Shared.Configuration;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Horizon;

public sealed class GameShutdownController
{
    [Dependency] private readonly IEntityManager _entityManager = null!;
    [Dependency] private readonly IResourceManager _resManager = null!;
    [Dependency] private readonly IConfigurationManager _cfg = null!;
    [Dependency] private readonly IGameTiming _gameTiming = null!;
    [Dependency] private readonly IChatManager _chatManager = null!;
    [Dependency] private readonly IBaseServer _server = null!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("ShutdownController");
    private Dictionary<string, ShutdownData> _shutdownTime = [];
    private TimeSpan _sendCooldown;
    private TimeSpan? _startTime;
    private bool _shutdown;

    public void Init()
    {
        _startTime = TimeSpan.Parse(DateTime.Now.ToString("HH:mm:ss"));
        _shutdown = _cfg.GetCVar(HorizonCCVars.ShutdownEnabled);
        if (_shutdown == false)
            return;
        
        TryFoundShutdownTimers();
    }

    private async void TryFoundShutdownTimers()
    {
        try
        {
            _shutdownTime = await CollectTimers();
            if (_shutdownTime.Count == 0)
                throw new Exception("No shutdown times found.");
        }
        catch (Exception e)
        {
            _sawmill.Error($"{e.Message}");
        }
    }

    private Task<Dictionary<string, ShutdownData>> CollectTimers()
    {
        return Task.Run(() =>
        {
            var stream = new ResPath(Path.Combine(_resManager.UserData.RootDir!,
                _cfg.GetCVar(HorizonCCVars.ShutdownTimersPath))).ToRootedPath();
            var timeSpan = new Dictionary<string, ShutdownData>();
            if (!_resManager.ContentFileExists(stream))
            {
                _shutdown = false;
                _sawmill.Error($"{stream} does not exist. Create or Add exist stream in CCVar");
                return timeSpan;
            }

            try
            {
                var yamlStream = _resManager.ContentFileReadYaml(stream);

                if (yamlStream.Documents[0].RootNode.ToDataNode() is not SequenceDataNode sequence)
                    throw new Exception("Attributions file is not a list of attributions.");

                foreach (var attribution in sequence.Sequence)
                {
                    var message = string.Empty;
                    var restart = false;
                    var restartAlways = false;
                    var beforeShutdownTime = TimeSpan.Zero;
                    var minServerPlay = TimeSpan.Zero;
                    if (attribution is not MappingDataNode map)
                        throw new Exception("Attribution is not a mapping.");

                    if (!map.TryGet("timer", out var name))
                        throw new Exception("Attempted to get timers from a non-map.");

                    if (!map.TryGet("shutdownTime", out var time) ||
                        !TimeSpan.TryParse(time.ToString(), out var timeSpanParsed))
                        throw new Exception("Attempted to get shutdown time.");

                    if (map.TryGet("serverMessage", out var serverMessage))
                        message = serverMessage.ToString();

                    if (map.TryGet<ValueDataNode>("restartRound", out var restartNode))
                        restart = restartNode.AsBool();

                    if (map.TryGet<ValueDataNode>("restartRoundAlways", out var restartAlwaysNode))
                        restartAlways = restartAlwaysNode.AsBool();

                    if (map.TryGet("beforeShutdown", out var beforeShutdown) &&
                        TimeSpan.TryParse(beforeShutdown.ToString(), out var beforeShutdownParsed))
                        beforeShutdownTime = beforeShutdownParsed;

                    if (_startTime.HasValue && timeSpanParsed <= _startTime.Value)
                        timeSpanParsed += TimeSpan.FromHours(24); // Flip to next day if we passed that point

                    var data = new ShutdownData(timeSpanParsed, message, restart, restartAlways, beforeShutdownTime);
                    timeSpan.Add(name.ToString(), data);
                }

                return timeSpan;
            }
            catch (Exception e)
            {
                _sawmill.Error($"{stream.ToString()}\n{e}");
                return timeSpan;
            }
        });
    }

    public void Update()
    {
        if (!_shutdown || _shutdownTime.Count == 0 || _startTime == null)
            return;

        foreach (var (name, data) in _shutdownTime)
        {
            var actualTime = _startTime.Value + _gameTiming.RealTime;
            if (actualTime >= data.ShutdownTime - data.BeforeShutdownTime && _sendCooldown <= _gameTiming.RealTime)
                SendServerMessage(data.Message);

            if (actualTime < data.ShutdownTime)
                continue;

            if (data.Restart)
            {
                if (data.RestartAlways)
                {
                    if (_entityManager.EntitySysManager.TryGetEntitySystem(out GameTicker? gameTicker))
                        gameTicker.RestartRound();
                }
                else if (_entityManager.EntitySysManager.TryGetEntitySystem(out RoundEndSystem? roundEndSystem))
                    roundEndSystem.EndRound();

                var newData = data;
                newData.ShutdownTime += TimeSpan.FromHours(24);
                _shutdownTime[name] = newData;
            }
            else
            {
                _server.Shutdown($"GameShutdown controller start shutdown in {actualTime}");
                _shutdownTime.Clear();
                break;
            }
        }
    }

    private void SendServerMessage(string message)
    {
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        _chatManager.ChatMessageToAll(ChatChannel.Server, message, wrappedMessage, default, false, true);
        _sendCooldown += TimeSpan.FromMinutes(5) + _gameTiming.RealTime;
    }

    private record struct ShutdownData(TimeSpan ShutdownTime, string Message, bool Restart, bool RestartAlways, TimeSpan BeforeShutdownTime);
}
