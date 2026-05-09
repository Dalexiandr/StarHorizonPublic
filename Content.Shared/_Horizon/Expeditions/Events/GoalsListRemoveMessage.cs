using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Expeditions;

[Serializable, NetSerializable]
public sealed class GoalsListRemoveMessage : CartridgeMessageEvent
{
    public readonly int Id;
    public GoalsListRemoveMessage(int id)
    {
        Id = id;
    }
}
