using Robust.Shared.Serialization;

namespace Content.Shared._Horizon._Fractions.AnCo.Companions;

[Serializable, NetSerializable]
public sealed class CompanionPDABoundUserInterfaceState : BoundUserInterfaceState
{
    public List<CompanionEntry> Companions;

    public CompanionPDABoundUserInterfaceState(List<CompanionEntry> companions)
    {
        Companions = companions;
    }
}

[Serializable, NetSerializable]
public record struct CompanionEntry(NetEntity Entity, string Name, string Status, float Health);
