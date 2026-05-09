using System.Linq;
using Content.Shared._White.Xenomorphs;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.Log;

namespace Content.Shared._White.Actions;

public sealed class ActionRelaySystem : EntitySystem
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("TEST.actionrelay");
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        _sawmill.Debug("ActionRelaySystem initialized");
        SubscribeLocalEvent<ActionsComponent, PlasmaAmountChangeEvent>(RelayEvent);
    }

    public void RelayEvent<T>(EntityUid uid, ActionsComponent component, T args) where T : EntityEventArgs
    {
        _sawmill.Debug($"RelayEvent: uid={uid}, eventType={typeof(T).Name}");
        var ev = new ActionRelayedEvent<T>(args);
        var actions = _actions.GetActions(uid, component);
        foreach (var action in actions)
        {
            RaiseLocalEvent(action.Owner, ev);
        }
        _sawmill.Debug($"RelayEvent: relayed to {actions.Count()} actions");
    }
}

public sealed class ActionRelayedEvent<TEvent>(TEvent args) : EntityEventArgs
{
    public TEvent Args = args;
}
