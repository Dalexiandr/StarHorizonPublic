using Robust.Shared.Serialization;

namespace Content.Shared._Horizon._Fractions.AnCo.Companions;

public abstract class SharedCompanionSystem : EntitySystem
{
    [Serializable, NetSerializable]
    public enum CompanionUiKey : byte
    {
        Key,
    }
}
