using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Laying;

[Serializable, NetSerializable]
public sealed partial class StandingUpDoAfterEvent : SimpleDoAfterEvent;
