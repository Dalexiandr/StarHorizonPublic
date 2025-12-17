using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Cytology;

[Serializable, NetSerializable]
public sealed partial class CytologySwabTakeDirtDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class CytologyTransferDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class CytologyInjectorTakeDoAfterEvent : SimpleDoAfterEvent
{
}
