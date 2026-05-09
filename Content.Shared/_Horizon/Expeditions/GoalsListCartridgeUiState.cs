using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Expeditions;

[Serializable, NetSerializable]
public sealed partial class GoalsListCartridgeUiState : BoundUserInterfaceState
{
    public Dictionary<int, ExpeditionGoal> Goals;

    public GoalsListCartridgeUiState(Dictionary<int, ExpeditionGoal> goals)
    {
        Goals = goals;
    }
}
