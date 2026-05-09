using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Expeditions;

[Serializable, NetSerializable]
public sealed partial class ExpeditionGoalsConsoleUiState : BoundUserInterfaceState
{
    public Dictionary<ProtoId<ExpeditionGoalCategoryPrototype>, Dictionary<int, ExpeditionGoal>> Goals;
    public List<ProtoId<ExpeditionGoalCategoryPrototype>> AvailableSpecifications;
    public TimeSpan OfferCooldown;
    public TimeSpan Cooldown;

    public ExpeditionGoalsConsoleUiState(Dictionary<ProtoId<ExpeditionGoalCategoryPrototype>, Dictionary<int, ExpeditionGoal>> goals,
                                         List<ProtoId<ExpeditionGoalCategoryPrototype>> availableSpecifications, TimeSpan cooldown, TimeSpan offerCooldown)
    {
        Goals = goals;
        AvailableSpecifications = availableSpecifications;
        Cooldown = cooldown;
        OfferCooldown = offerCooldown;
    }
}

[Serializable, NetSerializable]
public enum ExpeditionGoalsConsoleUiKey
{
    Key,
}
