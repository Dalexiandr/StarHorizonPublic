using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Actions;

namespace Content.Shared._Horizon.RemoteControl;

[Serializable, NetSerializable]
public sealed partial class MakeConnectWithHostDoAfterEvent : SimpleDoAfterEvent
{
}

public sealed partial class ReturnToBodyActionEvent : InstantActionEvent;
